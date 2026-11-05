# Azure hosting

Production runs as one scale-to-zero Azure Container App in front of a Burstable
PostgreSQL server, with email via Azure Communication Services. Design and
rationale: `openspec/changes/add-cheap-azure-hosting/design.md` (archived under
`openspec/changes/archive/` once shipped).

| Resource | SKU | Expected cost / month |
|---|---|---|
| Container App `ca-wishlist` (+ environment `cae-wishlist`) | Consumption, 0.25 vCPU / 0.5 GiB, 0–1 replicas | ~$0 (inside free grant) |
| Container Apps job `caj-wishlist-reservations` | Scheduled, 1st of the month 07:00 UTC, same image | ~$0 (seconds per month) |
| PostgreSQL Flexible Server `psql-wishlist-<suffix>` | Standard_B1ms, 32 GiB, 7-day backups | ~$13–16 |
| Communication Services `acs-wishlist-<suffix>` (+ `-email`) | Azure-managed domain | ~$0.00025 / email |
| Log Analytics `log-wishlist` | PerGB2018, 0.1 GB/day cap | cents |
| Budget `budget-wishlist` | 25 EUR (billing currency); emails at 50/80/100% actual and 100% forecast; stops the app at 100% | free |
| Logic App `logic-wishlist-budget-stop` + action group `ag-wishlist-budget-stop` | Consumption; runs only when the budget runs out | ~0 |
| Images | `ghcr.io/<owner>/wishlist` | free |

Everything lives in one resource group, `rg-wishlist-prod`, in Sweden Central.

## Workflows

- **`infra.yml`** (manual, pick `production` or `staging`): deploys `infra/main.bicep` to
  that environment's resource group. Keeps the currently running image.
- **`deploy.yml`** (push to `main`): tests, builds and pushes `ghcr.io/<owner>/wishlist:sha-<sha7>`,
  deploys it to **staging** (see [Staging](#staging)), and once staging's smoke check
  passes, **after approval on the `production` environment**, to production: opens a
  temporary Postgres firewall rule for the runner, applies EF Core migrations, closes the
  rule, rolls the container app to the new image and smoke-checks `/healthz` (which must
  report the new version). Then it tags the commit `vX.Y.Z`, creates a GitHub Release and
  tags the image `X.Y.Z` (see Versions below). The deploy steps are in
  `deploy-environment.yml`, shared by both environments.

Both log in to Azure with OIDC; GitHub holds no Azure secret.

## One-time bootstrap

Needs the Azure CLI (`az login` as a subscription Owner) and the GitHub CLI (`gh auth login`).

```bash
SUB=$(az account show --query id -o tsv)
TENANT=$(az account show --query tenantId -o tsv)
RG=rg-wishlist-prod
REPO=elvic-jan/wishlist

for p in Microsoft.App Microsoft.OperationalInsights Microsoft.DBforPostgreSQL Microsoft.Communication Microsoft.Logic Microsoft.Insights; do
  az provider register --namespace $p
done
az group create -n $RG -l swedencentral
```

### 1. CI identity (OIDC)

```bash
DEPLOY_APP_ID=$(az ad app create --display-name gh-wishlist-deploy --query appId -o tsv)
az ad sp create --id $DEPLOY_APP_ID
az ad app federated-credential create --id $DEPLOY_APP_ID --parameters "{
  \"name\": \"github-production\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$REPO:environment:production\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}"
az role assignment create --assignee $DEPLOY_APP_ID --role Contributor \
  --scope /subscriptions/$SUB/resourceGroups/$RG
```

The credential only matches jobs running in the `production` environment, and the
role only covers this resource group.

### 2. SMTP identity for ACS Email

ACS SMTP signs in with an Entra app; its client secret is the SMTP password.

```bash
SMTP_APP_ID=$(az ad app create --display-name wishlist-acs-smtp --query appId -o tsv)
az ad sp create --id $SMTP_APP_ID
SMTP_PASSWORD=$(az ad app credential reset --id $SMTP_APP_ID --years 2 --query password -o tsv)
```

Record the secret's expiry date (see [Expiring credentials](#expiring-credentials)).

### 3. GHCR pull token

The image is private by default. Create a **classic** personal access token with only
`read:packages` (GitHub → Settings → Developer settings → Tokens (classic)) and note its
expiry. Alternatively make the package public after the first push
(package settings → Change visibility; the image contains no secrets) and leave
`GHCR_PULL_TOKEN` unset.

### 4. GitHub `production` environment

The repo must be public (or on GitHub Pro/Team): private repos on the Free plan get no
environment secrets, and required reviewers on private repos need Enterprise.

In the repo: Settings → Environments → New environment `production` → add yourself as
**Required reviewer** and limit deployment branches to `main`. Then:

```bash
gh variable set AZURE_CLIENT_ID       --env production --body "$DEPLOY_APP_ID" -R $REPO
gh variable set AZURE_TENANT_ID       --env production --body "$TENANT" -R $REPO
gh variable set AZURE_SUBSCRIPTION_ID --env production --body "$SUB" -R $REPO
gh variable set AZURE_RESOURCE_GROUP  --env production --body "$RG" -R $REPO
# A secret, not a variable: the repo is public and variables show in workflow logs.
gh secret set BUDGET_CONTACT_EMAIL    --env production --body "you@example.com" -R $REPO

# Postgres requires 3 of: upper, lower, digit, symbol. Avoid ; and quotes (it goes in a connection string).
gh secret set POSTGRES_ADMIN_PASSWORD --env production --body "$(openssl rand -base64 32 | tr -d '/+=')" -R $REPO
gh secret set POSTGRES_APP_PASSWORD   --env production --body "$(openssl rand -base64 32 | tr -d '/+=')" -R $REPO
gh secret set JWT_KEY                 --env production --body "$(openssl rand -base64 48)" -R $REPO
gh secret set SMTP_PASSWORD           --env production --body "$SMTP_PASSWORD" -R $REPO
gh secret set SMTP_USER               --env production --body "pending" -R $REPO   # real value in step 6
gh secret set GHCR_PULL_TOKEN         --env production -R $REPO                    # paste the PAT
```

Optional variables: `MIN_REPLICAS` (`1` = no cold starts, ~$10–15/month),
`POSTGRES_ADMIN_LOGIN` (default `wishlistadmin`), `CUSTOM_DOMAIN`,
`CUSTOM_DOMAIN_CERT_READY`, `PREVIOUS_CUSTOM_DOMAIN` (see [Custom domain](#custom-domain)), `EMAIL_DOMAIN`,
`EMAIL_DOMAIN_VERIFIED` (see [Custom email domain](#custom-email-domain)).

### 5. First provisioning

Both workflows must be on `main` before they can run: GitHub only dispatches workflows
from the default branch, and the `production` environment only accepts `main`. So merge
first; the push starts `deploy`, which builds and pushes the image, then waits for
approval. **Reject that first deploy** — there is nothing to deploy to yet.

Then run **Actions → infra → Run workflow**. Once it's approved and finished, the run summary
lists the outputs (`appFqdn`, `communicationServiceName`, `emailSenderAddress`, …).
The app runs a placeholder image at this point.

### 6. Finish SMTP

The SMTP app needs permission on the Communication Service, and the SMTP username
embeds its name:

```bash
# plain `az resource` avoids the `communication` CLI extension
ACS=$(az resource list -g $RG --resource-type Microsoft.Communication/CommunicationServices --query "[0].name" -o tsv)
ACS_ID=$(az resource list -g $RG --resource-type Microsoft.Communication/CommunicationServices --query "[0].id" -o tsv)
az role definition create --role-definition "{
  \"Name\": \"ACS SMTP Sender (wishlist)\",
  \"Actions\": [
    \"Microsoft.Communication/CommunicationServices/Read\",
    \"Microsoft.Communication/CommunicationServices/Write\",
    \"Microsoft.Communication/EmailServices/write\"
  ],
  \"AssignableScopes\": [\"/subscriptions/$SUB/resourceGroups/$RG\"]
}"
az role assignment create --assignee $SMTP_APP_ID --role "ACS SMTP Sender (wishlist)" \
  --scope $ACS_ID

gh secret set SMTP_USER --env production --body "$ACS.$SMTP_APP_ID.$TENANT" -R $REPO
```

Run **infra** again so the app picks up the real username.

### 7. First deploy

Run **Actions → deploy → Run workflow** (or push to `main`) and approve the
`deploy` job. When it finishes, create your account with the **owner-access** job (see
[Owner access](#owner-access-first-account-or-getting-back-in)): sign-up always needs an
invitation, so this is the only way to make the first one. Then create a circle, invite
people, and test a password reset.

## Custom domain

Use a subdomain (e.g. `wishlist.example.com`); apex domains need a different validation.

1. At your DNS provider add
   - `CNAME wishlist` → `<appFqdn>`
   - `TXT asuid.wishlist` → `<customDomainVerificationId>` (from the infra run outputs)
2. Set variable `CUSTOM_DOMAIN=wishlist.example.com` and run **infra**. This adds the
   hostname to the app without a certificate.
3. Set `CUSTOM_DOMAIN_CERT_READY=true` and run **infra** again. This issues the free managed
   certificate, binds it, and switches `App__PublicBaseUrl` (used in reset links) to the
   custom domain. Issuing can take several minutes.

With Cloudflare DNS, both records must be **DNS only** (grey cloud); a proxied CNAME stops
the managed certificate from issuing and renewing.

### Moving to another domain

To move without downtime (as from `www.giftcircles.app` to `www.kadoozle.app`), keep the old
hostname bound while the new one is set up:

1. Add the `CNAME` and `TXT asuid` records for the new hostname (DNS only).
2. Set `CUSTOM_DOMAIN=<new>`, `CUSTOM_DOMAIN_CERT_READY=false` and
   `PREVIOUS_CUSTOM_DOMAIN=<old>`, then run **infra**. Both hostnames are on the app; the
   old one keeps its certificate and stays the public base URL.
3. Set `CUSTOM_DOMAIN_CERT_READY=true` and run **infra**. The new certificate is issued and
   the public base URL (links in emails, passkeys) switches to the new hostname.
4. Point the old domain at the new one with a redirect at the DNS provider (for
   Cloudflare: proxied records plus a Redirect Rule keeping path and query), then clear
   `PREVIOUS_CUSTOM_DOMAIN` and run **infra** to unbind it.

Passkeys belong to the public base URL's domain, so they stop working at step 3. Deploy
any code that goes with the move (such as removing old passkeys) right after step 3, not
before: the deploy workflow waits for approval, so merge first and approve after the switch.
Everyone is also signed out once, because sign-in cookies belong to the old hostname.

## Custom email domain

Mail goes out as `DoNotReply@<guid>.azurecomm.net` until you add your own domain; then
it's `noreply@<domain>` with the display name Kadoozle. Same two-phase pattern:

1. Set variable `EMAIL_DOMAIN` (e.g. `kadoozle.app`) and run **infra**. This creates
   the customer-managed domain; the run summary's `emailDomainVerificationRecords` lists
   the DNS records Azure generated. Nothing about sending changes yet.
2. At your DNS provider add those records (all **DNS only** in Cloudflare):
   - `TXT @` → `ms-domain-verification=…` (ownership)
   - `TXT @` → `v=spf1 include:spf.protection.outlook.com -all` (SPF; merge with any
     existing SPF record — a domain may have only one)
   - `CNAME selector1-azurecomm-prod-net._domainkey` and
     `CNAME selector2-azurecomm-prod-net._domainkey` → the targets Azure lists (DKIM)
   - `TXT _dmarc` → e.g. `v=DMARC1; p=none` to start (not generated by Azure; tighten
     to `p=quarantine` once a test mail shows `dkim=pass` and `dmarc=pass`)
3. Ask Azure to check the records (Portal: Email Communication Service → Provision domains
   → the domain → Verify for each, or):

   ```bash
   for t in Domain SPF DKIM DKIM2; do
     az communication email domain initiate-verification -g $RG \
       --email-service-name <emailServiceName> --domain-name <domain> --verification-type $t
   done
   ```
4. When all four show **Verified**, set `EMAIL_DOMAIN_VERIFIED=true` and run **infra**.
   This links the domain to the Communication Service, adds the `noreply` sender and
   switches `Smtp__FromAddress`. The Azure-managed domain stays linked as a fallback.

## Budget and auto-stop

The subscription is Pay-As-You-Go, which has no spending cap. The budget
`budget-wishlist` (resource group only, 25 EUR a month, `budgetAmount` in
`main.parameters.json`) emails `BUDGET_CONTACT_EMAIL` at 50%, 80% and 100% of actual
spend and when the month's forecast reaches 100%. At 100% actual it also runs the action
group `ag-wishlist-budget-stop`, which starts the Logic App `logic-wishlist-budget-stop`;
that **stops `ca-wishlist`, so the site is down** until you start it again.

It is not an exact cap: Azure's cost data lags by 8–24 hours, so spend can pass the
budget before the stop runs. Only the app is stopped. Postgres (the largest fixed cost,
storage and compute) and the monthly reservation job (seconds a month) keep running. To
keep the emails but never stop the app, deploy with `budgetAutoStop=false`.

### One-time setup (subscription Owner)

The deploy identity is only Contributor on the resource group, so it can't register
providers or grant the Logic App its permission. Before the first **infra** run with
auto-stop:

```bash
az provider register --namespace Microsoft.Logic
az provider register --namespace Microsoft.Insights
```

After that **infra** run, give the Logic App's managed identity the right to stop the app
(and nothing else):

```bash
PRINCIPAL=$(az resource show -g $RG -n logic-wishlist-budget-stop \
  --resource-type Microsoft.Logic/workflows --query identity.principalId -o tsv)
APP_ID=$(az containerapp show -g $RG -n ca-wishlist --query id -o tsv)
az role definition create --role-definition "{
  \"Name\": \"Container App Stopper (wishlist)\",
  \"Actions\": [
    \"Microsoft.App/containerApps/read\",
    \"Microsoft.App/containerApps/stop/action\"
  ],
  \"AssignableScopes\": [\"/subscriptions/$SUB/resourceGroups/$RG\"]
}"
az role assignment create --assignee-object-id $PRINCIPAL --assignee-principal-type ServicePrincipal \
  --role "Container App Stopper (wishlist)" --scope $APP_ID
```

The assignment survives later infra runs. If the Logic App is ever deleted and recreated
it gets a new identity: run the last command again.

### When the app was stopped

You get the 100% email, and the Logic App's run history shows `Stop_container_app`.
Find out what used the money (Cost Management → Cost analysis, grouped by resource), fix
it, raise `budgetAmount` if needed, then:

```bash
# plain REST call: `az containerapp start` needs the containerapp CLI extension
APP_ID=$(az containerapp show -g $RG -n ca-wishlist --query id -o tsv)
az rest --method post --url "https://management.azure.com$APP_ID/start?api-version=2024-03-01"
```

Each threshold alerts once per month, so after a restart the budget won't stop the app
again until next month; keep an eye on Cost analysis.

To test the chain, at a quiet moment: Azure portal → Monitor → Alerts → Action groups →
`ag-wishlist-budget-stop` → **Test** (Logic App). The site goes down; start it again with
the command above.

## Staging

A second copy of the app for trying changes on real hosting (phones, passkeys, email,
PWA updates) before production. Design: `openspec/changes/add-staging-environment/`.

- **Where:** resource group `rg-wishlist-staging`, base name `wishlist-stg` (so
  `ca-wishlist-stg`, `caj-wishlist-stg-reservations`, …), at its free
  `*.azurecontainerapps.io` address. Passkeys there belong to that host only.
- **Database:** a free external Postgres (Neon, Frankfurt), database `wishlist`, reached on
  its direct (not pooled) endpoint. No Azure Postgres server, so no firewall steps.
- **Test data only, never a copy of production.** When the app starts and its test users
  don't exist, it creates them: `+anna`, `+ben`, `+carla`, `+david` addresses of the first
  plain address in `EMAIL_ALLOWLIST`, with two circles, wishes and a reservation. They have
  no password: use **Forgot password** on staging; the email arrives in that inbox.
- **Email:** only to `EMAIL_ALLOWLIST` (`name@domain`, which covers `name+tag@domain`, or
  `*@domain`), with `[TEST]` before the subject. Everything else is dropped and logged with
  its subject and a masked recipient (`y**+alice@g****.com`). Sent from the Azure-managed
  `azurecomm.net` address (no `EMAIL_DOMAIN`).
- **Marked:** a TEST banner on every page and `X-Robots-Tag: noindex`.
- **Deploys:** every `main` build goes to staging first, automatically; production waits for
  it. To try a branch: **Actions → deploy → Run workflow** on that branch (or
  `gh workflow run deploy.yml --ref <branch>`). It gets a version like `1.4.0-pr.abc1234` and
  never reaches production. If staging's database has a migration the deployed build doesn't
  (back on `main` after such a branch), the deploy empties it, migrates, and the app seeds
  fresh test data.
- **Never add a deploy trigger outsiders can fire** (`pull_request_target`, comments,
  labels): those run with secrets. Deploys start only from a push to `main` or by hand.

### GitHub `staging` environment

No required reviewers; deployment branches: all (only people with write access create
branches). Same names as production, its own values:

- Variables: `AZURE_CLIENT_ID` (the staging deploy identity), `AZURE_TENANT_ID`,
  `AZURE_SUBSCRIPTION_ID`, `AZURE_RESOURCE_GROUP=rg-wishlist-staging`,
  `BASE_NAME=wishlist-stg`, `BUDGET_AMOUNT=5`, `BUDGET_START_DATE=2026-11-01` (a new budget
  can't start before the month it's created in; keep it fixed afterwards),
  `POSTGRES_HOST` (Neon direct host), `POSTGRES_ADMIN_LOGIN` (Neon owner),
  `APP_ENVIRONMENT_LABEL=TEST`.
- Secrets: `POSTGRES_ADMIN_PASSWORD` (Neon owner), `POSTGRES_APP_PASSWORD`, `JWT_KEY` (its
  own), `GHCR_PULL_TOKEN` (its own), `SMTP_PASSWORD`, `SMTP_USER`, and the email addresses
  `BUDGET_CONTACT_EMAIL` and `EMAIL_ALLOWLIST` (e.g. `you@example.com`). Addresses are
  secrets because the repository is public and a variable's value shows in workflow logs.

The infra workflow refuses to provision staging without `APP_ENVIRONMENT_LABEL`, and
production with it.

### One-time Azure setup (subscription Owner)

As for production ([CI identity](#1-ci-identity-oidc), [SMTP identity](#2-smtp-identity-for-acs-email)),
with `RG=rg-wishlist-staging`, app names `gh-wishlist-deploy-staging` and
`wishlist-acs-smtp-staging`, and federated subject `repo:$REPO:environment:staging`. The
staging deploy identity has Contributor on the staging resource group only.

Then run **infra** for `staging` (from a branch too: the `staging` environment accepts any
branch), and give the roles their own names, since production's custom roles are scoped to
production's resource group:

```bash
RG=rg-wishlist-staging
ACS_ID=$(az resource list -g $RG --resource-type Microsoft.Communication/CommunicationServices --query "[0].id" -o tsv)
ACS=$(az resource list -g $RG --resource-type Microsoft.Communication/CommunicationServices --query "[0].name" -o tsv)
az role definition create --role-definition "{
  \"Name\": \"ACS SMTP Sender (wishlist-stg)\",
  \"Actions\": [
    \"Microsoft.Communication/CommunicationServices/Read\",
    \"Microsoft.Communication/CommunicationServices/Write\",
    \"Microsoft.Communication/EmailServices/write\"
  ],
  \"AssignableScopes\": [\"/subscriptions/$SUB/resourceGroups/$RG\"]
}"
az role assignment create --assignee $SMTP_APP_ID --role "ACS SMTP Sender (wishlist-stg)" --scope $ACS_ID
gh secret set SMTP_USER --env staging --body "$ACS.$SMTP_APP_ID.$TENANT" -R $REPO

PRINCIPAL=$(az resource show -g $RG -n logic-wishlist-stg-budget-stop \
  --resource-type Microsoft.Logic/workflows --query identity.principalId -o tsv)
APP_ID=$(az containerapp show -g $RG -n ca-wishlist-stg --query id -o tsv)
az role definition create --role-definition "{
  \"Name\": \"Container App Stopper (wishlist-stg)\",
  \"Actions\": [
    \"Microsoft.App/containerApps/read\",
    \"Microsoft.App/containerApps/stop/action\"
  ],
  \"AssignableScopes\": [\"/subscriptions/$SUB/resourceGroups/$RG\"]
}"
az role assignment create --assignee-object-id $PRINCIPAL --assignee-principal-type ServicePrincipal \
  --role "Container App Stopper (wishlist-stg)" --scope $APP_ID
```

Run **infra** for `staging` once more so the app picks up `SMTP_USER`, then deploy.

Staging has to be up before the change that introduces it is merged: from then on every
`main` deploy goes to staging first, and production waits for it.

## Operations

- **Logs**: `az containerapp logs show -g $RG -n ca-wishlist --follow`, or query
  `ContainerAppConsoleLogs_CL` in `log-wishlist`.
- **Reservation job** (`caj-wishlist-reservations`): runs on the 1st of each month at
  07:00 UTC with the app's image and settings (`--job reservations`). It emails reminders
  for reservations ending next month, then ends the expired ones and emails their reservers.
  The deploy workflow keeps its image in step with the app. Run it by hand with
  `az containerapp job start -g $RG -n caj-wishlist-reservations`; see past runs with
  `az containerapp job execution list -g $RG -n caj-wishlist-reservations -o table`. Its
  log line ("Reservation run: …") is in `ContainerAppConsoleLogs_CL`.
- **Rollback**: re-run the `deploy` job of an earlier successful run (its image tag and
  migrations bundle are kept for 30 days), or
  `az containerapp update -g $RG -n ca-wishlist --image ghcr.io/<owner>/wishlist:<version>`
  (or `:sha-<old>`). Migrations are not rolled back, so keep them additive. A re-run finds
  its version already tagged and tags nothing.
- **Versions**: every production release is `MAJOR.MINOR.PATCH`, starting at 1.0.0.
  `scripts/release-version.mjs` works it out at build time from the last `vX.Y.Z` tag and
  the `release-notes/*.md` files added since (`major` → major, `feature` → minor,
  otherwise patch; see `release-notes/README.md`), and writes `releases.json` for the
  What's new page. A version exists only once a deploy succeeds: a run that's never
  approved has its notes counted in the next one. Branching stays simple: short branches,
  pull requests into `main`, every approved deploy is a release. If `main` ever holds
  something that mustn't ship yet and a fix can't wait, branch `hotfix/...` from the last
  tag, run **deploy** by hand on it (it gets the next patch version; the `production`
  environment must allow that branch), then merge the fix into `main`.
- **Database logins**: migrations run as the admin; the app and its jobs log in as
  `wishlist_app`, which can only read and write rows (`infra/sql/app-role.sql`). Every
  deploy runs that script after the migrations and sets the login's password from
  `POSTGRES_APP_PASSWORD`.
- **Rotate a secret**: update the GitHub environment secret, run **infra**. For the
  Postgres admin password, also run `az postgres flexible-server update -g $RG -n <server> --admin-password ...` first.
  For `POSTGRES_APP_PASSWORD`, run **deploy** (sets the new password) and then **infra** right after:
  in between, the app can't reach the database.
- **Connect to the database**: add a firewall rule for your IP
  (`az postgres flexible-server firewall-rule create ...`), connect with `sslmode=require`,
  then delete the rule.
- **Teardown**: `pg_dump` first, then `az group delete -n $RG`. Backups are deleted with the server.

## Owner access (first account, or getting back in)

Sign-up always needs an invitation. The `owner-access` job is the way around that for
someone who can reach the database:

- **No account has the email yet** (e.g. the first account on a fresh install): it creates
  one with that email and `--name`, with no password and no circle, and marks the email
  confirmed.
- **The account exists** (e.g. you lost your passkey, your password, or access to your
  mailbox): it leaves the account as it is.

Either way it prints a sign-in link. Nothing is emailed. Open the link within 30 minutes
on the device you want to use, then add a passkey or set a password. The link works once,
and using it signs out every other session.

Run from the repository root in PowerShell, with the Postgres admin password at hand. If
you don't have it any more, set a new one first (see **Rotate a secret** above).

```powershell
$RG = "rg-wishlist-prod"
$PG = az postgres flexible-server list -g $RG --query "[0].name" -o tsv
$IP = Invoke-RestMethod https://api.ipify.org
az postgres flexible-server firewall-rule create -g $RG -s $PG -n owner-access --start-ip-address $IP --end-ip-address $IP --output none

New-Item -ItemType Directory -Force src/Wishlist.API/wwwroot | Out-Null  # the app won't start without it
$env:ConnectionStrings__DefaultConnection = "Host=$PG.postgres.database.azure.com;Port=5432;Database=wishlist;Username=wishlistadmin;Password=<admin password>;SSL Mode=VerifyFull"
dotnet run --project src/Wishlist.API --launch-profile https -- --job owner-access --email you@example.com --name "Your name" --App:PublicBaseUrl https://www.kadoozle.app
Remove-Item Env:ConnectionStrings__DefaultConnection

az postgres flexible-server firewall-rule delete -g $RG -s $PG -n owner-access --yes
```

`--name` is only needed when the account doesn't exist yet. Keep `--App:PublicBaseUrl`: without
it the link points at your own machine. The job prints whether it created or found the
account, then the link. Treat the link like a password until it has been used or has expired.

## Old wishlist site (one-time load)

People can import their wishes from the old site (verlanglijstje) on `/import`. The old
data has to be loaded into production once, from the exported `vl_users.xlsx` and
`vl_items.xlsx`. The export files stay on your PC and never go into git. The job keeps only
usernames and live items (not struck out, not deleted). Emails, names, passwords,
priorities, remarks and reservations are never stored.

Run from the repository root in PowerShell, with the Postgres admin password at hand:

```powershell
$RG = "rg-wishlist-prod"
$PG = az postgres flexible-server list -g $RG --query "[0].name" -o tsv
$IP = Invoke-RestMethod https://api.ipify.org
az postgres flexible-server firewall-rule create -g $RG -s $PG -n legacy-load --start-ip-address $IP --end-ip-address $IP --output none

$env:ConnectionStrings__DefaultConnection = "Host=$PG.postgres.database.azure.com;Port=5432;Database=wishlist;Username=wishlistadmin;Password=<admin password>;SSL Mode=VerifyFull"
dotnet run --project src/Wishlist.API --launch-profile https -- --job legacy-load --users "$HOME\Downloads\vl_users.xlsx" --items "$HOME\Downloads\vl_items.xlsx"
Remove-Item Env:ConnectionStrings__DefaultConnection

az postgres flexible-server firewall-rule delete -g $RG -s $PG -n legacy-load --yes
```

The job logs `Legacy load: 79 usernames and 475 live items stored (…)` and exits. Running it
again is safe: it updates what's there, adds what's missing, and keeps every claim. Until it
has run, `/import` says importing isn't available and the import message isn't shown.

**Release a wrong claim.** Each old username can be imported by one account. If someone took
the wrong one, connect to the database (see **Connect to the database** above) and run:

```sql
UPDATE "LegacyUsers" SET "ClaimedByUserId" = NULL, "ClaimedAt" = NULL, "ImportedItemCount" = NULL
WHERE "UserName" = 'JOHN.DOE';
```

The wishes already imported stay with the account that imported them, which can delete
them. Deleting an account also releases its claim.

## Expiring credentials

| Credential | Where | Expires | Effect when expired |
|---|---|---|---|
| ACS SMTP client secret | `SMTP_PASSWORD` | _record date_ (2 years from creation) | password-reset emails fail (logged; reset requests still return 202) |
| GHCR pull PAT | `GHCR_PULL_TOKEN` | _record date_ | the image can no longer be pulled, so the next scale-up from zero fails and **the site goes down** |
| Staging ACS SMTP client secret | `SMTP_PASSWORD` (`staging`) | 2 years from creation (2026-11-05) | staging emails fail |
| Staging GHCR pull PAT | `GHCR_PULL_TOKEN` (`staging`) | _record date_ | staging can't start from zero |
