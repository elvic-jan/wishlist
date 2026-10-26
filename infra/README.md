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
| Budget `budget-wishlist` | $25, alerts at 80% / 100% | free |
| Images | `ghcr.io/<owner>/wishlist` | free |

Everything lives in one resource group, `rg-wishlist-prod`, in Sweden Central.

## Workflows

- **`infra.yml`** (manual): deploys `infra/main.bicep`. Keeps the currently running image.
- **`deploy.yml`** (push to `main`): tests, builds and pushes `ghcr.io/<owner>/wishlist:sha-<sha7>`,
  then, **after approval on the `production` environment**, opens a temporary Postgres
  firewall rule for the runner, applies EF Core migrations, closes the rule, rolls the
  container app to the new image and smoke-checks `/healthz`.

Both log in to Azure with OIDC; GitHub holds no Azure secret.

## One-time bootstrap

Needs the Azure CLI (`az login` as a subscription Owner) and the GitHub CLI (`gh auth login`).

```bash
SUB=$(az account show --query id -o tsv)
TENANT=$(az account show --query tenantId -o tsv)
RG=rg-wishlist-prod
REPO=elvic-jan/wishlist

for p in Microsoft.App Microsoft.OperationalInsights Microsoft.DBforPostgreSQL Microsoft.Communication; do
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
gh variable set BUDGET_CONTACT_EMAIL  --env production --body "you@example.com" -R $REPO

# Postgres requires 3 of: upper, lower, digit, symbol. Avoid ; and quotes (it goes in a connection string).
gh secret set POSTGRES_ADMIN_PASSWORD --env production --body "$(openssl rand -base64 32 | tr -d '/+=')" -R $REPO
gh secret set JWT_KEY                 --env production --body "$(openssl rand -base64 48)" -R $REPO
gh secret set SMTP_PASSWORD           --env production --body "$SMTP_PASSWORD" -R $REPO
gh secret set SMTP_USER               --env production --body "pending" -R $REPO   # real value in step 6
gh secret set GHCR_PULL_TOKEN         --env production -R $REPO                    # paste the PAT
```

Optional variables: `MIN_REPLICAS` (`1` = no cold starts, ~$10–15/month),
`POSTGRES_ADMIN_LOGIN` (default `wishlistadmin`), `CUSTOM_DOMAIN`,
`CUSTOM_DOMAIN_CERT_READY` (see [Custom domain](#custom-domain)), `EMAIL_DOMAIN`,
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
`deploy` job. When it finishes, open `https://<appFqdn>`, register, and test a
password reset.

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

## Custom email domain

Mail goes out as `DoNotReply@<guid>.azurecomm.net` until you add your own domain; then
it's `noreply@<domain>` with the display name GiftCircles. Same two-phase pattern:

1. Set variable `EMAIL_DOMAIN` (e.g. `giftcircles.app`) and run **infra**. This creates
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
  `az containerapp update -g $RG -n ca-wishlist --image ghcr.io/<owner>/wishlist:sha-<old>`.
  Migrations are not rolled back, so keep them additive.
- **Rotate a secret**: update the GitHub environment secret, run **infra**. For the
  Postgres password, also run `az postgres flexible-server update -g $RG -n <server> --admin-password ...` first.
- **Connect to the database**: add a firewall rule for your IP
  (`az postgres flexible-server firewall-rule create ...`), connect with `sslmode=require`,
  then delete the rule.
- **Teardown**: `pg_dump` first, then `az group delete -n $RG`. Backups are deleted with the server.

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
WHERE "UserName" = 'JAN.VDB';
```

The wishes already imported stay with the account that imported them, which can delete
them. Deleting an account also releases its claim.

## Expiring credentials

| Credential | Where | Expires | Effect when expired |
|---|---|---|---|
| ACS SMTP client secret | `SMTP_PASSWORD` | _record date_ (2 years from creation) | password-reset emails fail (logged; reset requests still return 202) |
| GHCR pull PAT | `GHCR_PULL_TOKEN` | _record date_ | the image can no longer be pulled, so the next scale-up from zero fails and **the site goes down** |
