@description('Azure region.')
param location string

@description('Log Analytics workspace name.')
param name string

@description('Hard daily ingestion cap in GB; ingestion stops for the day once reached.')
param dailyQuotaGb string = '0.1'

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: name
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: json(dailyQuotaGb)
    }
  }
}

output id string = workspace.id
output name string = workspace.name
