## Why

The Azure subscription moved from the free trial to Pay-As-You-Go, which has no spending limit. The only guard left is a budget that emails at 80% and 100% of 25 per month, so a runaway cost would keep growing until someone reads the email and acts.

## What Changes

- The resource-group budget stays at 25 a month (billing currency, EUR) and emails at **50%, 80% and 100% of actual spend** and when the **forecast reaches 100%**.
- At **100% of actual spend** the budget runs an action group that starts a Logic App. The Logic App **stops the container app** with its own managed identity, so the site goes down until the owner starts it again. Postgres and the monthly reservation job are left alone.
- A Bicep parameter `budgetAutoStop` (default `true`) turns the stop off and keeps the emails.
- `infra/README.md` documents the one-time Owner steps (register `Microsoft.Logic` and `Microsoft.Insights`; a custom role that can only read and stop the app, assigned to the Logic App's identity), how to restart, and how to test.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `hosting`: the monthly budget alerts at more thresholds and stops the app when it runs out.

## Impact

- **Infra**: `infra/modules/budget.bicep` (more notifications, action group on 100%), new `infra/modules/budget-stop.bicep` (Logic App + action group), `infra/main.bicep` wiring, `containerapp.bicep` outputs the app's resource ID. No workflow changes; `BUDGET_CONTACT_EMAIL` is still the recipient.
- **Cost**: the Logic App and action group run only when the budget runs out; cents at most.
- **Risk**: an automatic stop takes the site down. Azure's cost data lags by 8–24 hours, so this limits a runaway rather than capping spend exactly.
- **App**: none. No release note, since nothing changes for users.
