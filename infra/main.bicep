// Cheapest production hosting for the wishlist app: one resource group with a
// scale-to-zero Container App, a Burstable Postgres server, ACS Email and a
// budget alert. See infra/README.md for the one-time bootstrap.
targetScope = 'resourceGroup'

@description('Azure region for regional resources.')
param location string = 'swedencentral'

@description('Short base name used to derive resource names.')
param baseName string = 'wishlist'

@description('Image to run. infra.yml passes the currently deployed image so re-running infra never rolls the app back.')
param image string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@minValue(0)
@maxValue(1)
param minReplicas int = 0

param customDomainName string = ''
param customDomainCertificateReady bool = false

@description('Own email sending domain, e.g. giftcircles.app. Empty = Azure-managed azurecomm.net sender.')
param emailCustomDomain string = ''

@description('True once the email domain\'s DNS records are verified in Azure; switches the sender to noreply@<domain>.')
param emailCustomDomainVerified bool = false

param postgresVersion string = '17'
param postgresAdministratorLogin string = 'wishlistadmin'

@secure()
param postgresAdministratorPassword string

@secure()
param jwtKey string

@description('ACS SMTP username: <communication service name>.<Entra app id>.<tenant id>.')
@secure()
param smtpUser string

@description('Client secret of the Entra app used for ACS SMTP.')
@secure()
param smtpPassword string

param ghcrUsername string = ''

@secure()
param ghcrToken string = ''

param budgetAmount int = 25

@description('First day of the month the budget starts (yyyy-MM-01). Keep it fixed across deployments.')
param budgetStartDate string

param budgetContactEmails array

// Postgres and ACS names are globally unique; the suffix keeps them stable per resource group.
var suffix = take(uniqueString(resourceGroup().id), 6)

module logs 'modules/logs.bicep' = {
  name: 'logs'
  params: {
    location: location
    name: 'log-${baseName}'
  }
}

module postgres 'modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    location: location
    name: 'psql-${baseName}-${suffix}'
    version: postgresVersion
    administratorLogin: postgresAdministratorLogin
    administratorPassword: postgresAdministratorPassword
  }
}

module email 'modules/email.bicep' = {
  name: 'email'
  params: {
    name: 'acs-${baseName}-${suffix}'
    customDomainName: emailCustomDomain
    customDomainVerified: emailCustomDomainVerified
  }
}

module app 'modules/containerapp.bicep' = {
  name: 'containerapp'
  params: {
    location: location
    environmentName: 'cae-${baseName}'
    appName: 'ca-${baseName}'
    logAnalyticsWorkspaceName: logs.outputs.name
    image: image
    minReplicas: minReplicas
    customDomainName: customDomainName
    customDomainCertificateReady: customDomainCertificateReady
    postgresFqdn: postgres.outputs.fqdn
    postgresDatabaseName: postgres.outputs.databaseName
    postgresAdministratorLogin: postgresAdministratorLogin
    postgresAdministratorPassword: postgresAdministratorPassword
    jwtKey: jwtKey
    smtpFromAddress: email.outputs.senderAddress
    smtpUser: smtpUser
    smtpPassword: smtpPassword
    ghcrUsername: ghcrUsername
    ghcrToken: ghcrToken
  }
}

module budget 'modules/budget.bicep' = {
  name: 'budget'
  params: {
    name: 'budget-${baseName}'
    amount: budgetAmount
    startDate: budgetStartDate
    contactEmails: budgetContactEmails
  }
}

output containerAppName string = app.outputs.name
output appFqdn string = app.outputs.fqdn
output publicBaseUrl string = app.outputs.publicBaseUrl
output customDomainVerificationId string = app.outputs.customDomainVerificationId
output postgresServerName string = postgres.outputs.name
output postgresFqdn string = postgres.outputs.fqdn
output postgresDatabaseName string = postgres.outputs.databaseName
output communicationServiceName string = email.outputs.communicationServiceName
output emailSenderAddress string = email.outputs.senderAddress
output emailServiceName string = email.outputs.emailServiceName
output emailDomainVerificationRecords object = email.outputs.customDomainVerificationRecords
output emailDomainVerificationStates object = email.outputs.customDomainVerificationStates
