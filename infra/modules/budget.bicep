@description('Budget name.')
param name string

@description('Monthly budget in the billing currency (EUR).')
param amount int

@description('First day of the month the budget starts in (yyyy-MM-01). Keep it fixed across deployments.')
param startDate string

@description('Who receives the alert emails.')
param contactEmails array

@description('Action group to run when actual spend reaches 100%. Empty = emails only.')
param stopActionGroupId string = ''

var email = {
  enabled: true
  operator: 'GreaterThanOrEqualTo'
  contactEmails: contactEmails
}

resource budget 'Microsoft.Consumption/budgets@2023-11-01' = {
  name: name
  properties: {
    category: 'Cost'
    amount: amount
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: startDate
    }
    notifications: {
      actual50: union(email, { threshold: 50, thresholdType: 'Actual' })
      actual80: union(email, { threshold: 80, thresholdType: 'Actual' })
      actual100: union(email, {
        threshold: 100
        thresholdType: 'Actual'
        contactGroups: empty(stopActionGroupId) ? [] : [stopActionGroupId]
      })
      forecast100: union(email, { threshold: 100, thresholdType: 'Forecasted' })
    }
  }
}
