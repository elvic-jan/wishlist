## 1. Infra code

- [x] 1.1 `budget.bicep`: notifications at 50/80/100% actual and 100% forecast; 100% actual also calls an optional action group
- [x] 1.2 `budget-stop.bicep`: Consumption Logic App with a system-assigned identity whose HTTP trigger POSTs ARM `stop` on the container app; action group `ag-wishlist-budget-stop` with the Logic App as receiver
- [x] 1.3 `main.bicep`: `budgetAutoStop` parameter (default true), wire the modules, output the Logic App name; `containerapp.bicep` outputs the app ID
- [x] 1.4 `infra/README.md`: cost table, provider registration, one-time role for the Logic App, restart and test steps
- [x] 1.5 No release note: nothing changes for users

## 2. Apply (owner)

- [ ] 2.1 Register `Microsoft.Logic` and `Microsoft.Insights`
- [ ] 2.2 Run the **infra** workflow
- [ ] 2.3 Create the "Container App Stopper (wishlist)" role and assign it to the Logic App's identity on `ca-wishlist`
- [ ] 2.4 Check Cost Management → Budgets → `budget-wishlist`: four alert conditions, the 100% actual one with action group `ag-wishlist-budget-stop`
