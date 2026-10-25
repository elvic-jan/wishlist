@description('Base name; the email service and communication service derive their names from it.')
param name string

@description('Where email data is stored at rest.')
param dataLocation string = 'Europe'

@description('Own sending domain (e.g. giftcircles.app). Empty = only the Azure-managed domain.')
param customDomainName string = ''

@description('Set to true once the custom domain\'s DNS records are verified; links it and switches the sender to it.')
param customDomainVerified bool = false

@description('Local part of the sender address on the custom domain.')
param senderUsername string = 'noreply'

@description('Display name shown with the sender address.')
param senderDisplayName string = 'GiftCircles'

var hasCustomDomain = !empty(customDomainName)
var useCustomDomain = hasCustomDomain && customDomainVerified

resource emailService 'Microsoft.Communication/emailServices@2023-04-01' = {
  name: '${name}-email'
  location: 'global'
  properties: {
    dataLocation: dataLocation
  }
}

// Free Azure-managed sender domain (DoNotReply@<guid>.azurecomm.net); no DNS needed.
// Stays linked as a fallback after the custom domain is in use.
resource managedDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: 'global'
  properties: {
    domainManagement: 'AzureManaged'
    userEngagementTracking: 'Disabled'
  }
}

// Customer-managed domain. Azure generates its DNS records (ownership TXT, SPF,
// two DKIM CNAMEs), shown in the infra run outputs; it can only be linked once
// they verify, hence the two-phase customDomainVerified parameter.
resource customDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' = if (hasCustomDomain) {
  parent: emailService
  name: customDomainName
  location: 'global'
  properties: {
    domainManagement: 'CustomerManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource customSender 'Microsoft.Communication/emailServices/domains/senderUsernames@2023-04-01' = if (useCustomDomain) {
  parent: customDomain
  name: senderUsername
  properties: {
    username: senderUsername
    displayName: senderDisplayName
  }
}

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: name
  location: 'global'
  properties: {
    dataLocation: dataLocation
    linkedDomains: useCustomDomain ? [managedDomain.id, customDomain.id] : [managedDomain.id]
  }
}

output communicationServiceName string = communicationService.name
output communicationServiceId string = communicationService.id
output emailServiceName string = emailService.name
output senderAddress string = useCustomDomain
  ? '${senderUsername}@${customDomainName}'
  : 'DoNotReply@${managedDomain.properties.mailFromSenderDomain}'
output customDomainVerificationRecords object = hasCustomDomain ? customDomain.?properties.?verificationRecords ?? {} : {}
output customDomainVerificationStates object = hasCustomDomain ? customDomain.?properties.?verificationStates ?? {} : {}
