// Stops the container app when the budget runs out: budget (100% actual) →
// action group → Logic App → ARM "stop" on ca-wishlist, using the Logic App's
// managed identity. The identity's permission is a one-time Owner step
// (infra/README.md, "Budget auto-stop"), because the deploy identity can't
// grant roles.

param location string

@description('Logic App name.')
param workflowName string

@description('Action group name.')
param actionGroupName string

@description('Resource ID of the container app to stop.')
param containerAppId string

var armUrl = environment().resourceManager // ends with '/'

resource workflow 'Microsoft.Logic/workflows@2019-05-01' = {
  name: workflowName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      triggers: {
        manual: {
          type: 'Request'
          kind: 'Http'
          inputs: {
            schema: {}
          }
        }
      }
      actions: {
        Stop_container_app: {
          type: 'Http'
          runAfter: {}
          inputs: {
            method: 'POST'
            uri: '${armUrl}${substring(containerAppId, 1)}/stop?api-version=2024-03-01'
            authentication: {
              type: 'ManagedServiceIdentity'
              audience: armUrl
            }
          }
        }
      }
      outputs: {}
    }
  }
}

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: actionGroupName
  location: 'global'
  properties: {
    groupShortName: 'budgetstop'
    enabled: true
    logicAppReceivers: [
      {
        name: 'stop-container-app'
        resourceId: workflow.id
        callbackUrl: listCallbackUrl('${workflow.id}/triggers/manual', '2019-05-01').value
        useCommonAlertSchema: false
      }
    ]
  }
}

output actionGroupId string = actionGroup.id
output workflowName string = workflow.name
output principalId string = workflow.identity.principalId
