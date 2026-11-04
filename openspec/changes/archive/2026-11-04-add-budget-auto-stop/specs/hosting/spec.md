## MODIFIED Requirements

### Requirement: Monthly cost is capped and monitored

The infrastructure SHALL be sized so that expected monthly cost stays under 25 (billing currency) at family-scale usage, and SHALL include a monthly budget of 25 on the resource group that emails the owner when actual spend reaches 50%, 80% and 100% and when forecast spend reaches 100%. When actual spend reaches 100%, the budget SHALL stop the container app automatically, unless the auto-stop is turned off in the infrastructure parameters. The identity that stops the app SHALL be able to read and stop only that app.

#### Scenario: Spend crosses an alert threshold

- **WHEN** actual spend for the resource group in the current month reaches 12.50 (50% of the budget)
- **THEN** the configured recipient receives a budget alert email

#### Scenario: Forecast passes the budget

- **WHEN** the forecast spend for the current month reaches 25
- **THEN** the configured recipient receives a budget alert email, and nothing is stopped

#### Scenario: Budget runs out

- **WHEN** actual spend for the resource group in the current month reaches 25 and auto-stop is on
- **THEN** the configured recipient receives a budget alert email
- **AND** the container app is stopped, so the site is unavailable until the owner starts it again
