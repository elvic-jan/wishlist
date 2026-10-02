@description('Base name; the email service and communication service derive their names from it.')
param name string

@description('Where email data is stored at rest.')
param dataLocation string = 'Europe'

resource emailService 'Microsoft.Communication/emailServices@2023-04-01' = {
  name: '${name}-email'
  location: 'global'
  properties: {
    dataLocation: dataLocation
  }
}

// Free Azure-managed sender domain (DoNotReply@<guid>.azurecomm.net); no DNS needed.
resource managedDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: 'global'
  properties: {
    domainManagement: 'AzureManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: name
  location: 'global'
  properties: {
    dataLocation: dataLocation
    linkedDomains: [
      managedDomain.id
    ]
  }
}

output communicationServiceName string = communicationService.name
output communicationServiceId string = communicationService.id
output senderAddress string = 'DoNotReply@${managedDomain.properties.mailFromSenderDomain}'
