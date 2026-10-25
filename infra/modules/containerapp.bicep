@description('Azure region.')
param location string

@description('Container Apps environment name.')
param environmentName string

@description('Container app name.')
param appName string

@description('Log Analytics workspace that receives console logs.')
param logAnalyticsWorkspaceName string

@description('Full image reference to run.')
param image string

@description('0 = scale to zero when idle (cold starts); 1 = always warm (~$10-15/month).')
@minValue(0)
@maxValue(1)
param minReplicas int = 0

@description('Custom domain (e.g. wishlist.example.com). Empty = use the generated *.azurecontainerapps.io FQDN.')
param customDomainName string = ''

@description('Set to true once the DNS CNAME + asuid TXT records for customDomainName exist; issues the managed certificate and binds it.')
param customDomainCertificateReady bool = false

@description('Public origin the SPA is served from; used in password-reset links. Empty = derived.')
param publicBaseUrl string = ''

param postgresFqdn string
param postgresDatabaseName string
param postgresAdministratorLogin string

@secure()
param postgresAdministratorPassword string

@secure()
param jwtKey string

param smtpFromAddress string
param smtpFromName string = 'GiftCircles'

@secure()
param smtpUser string

@secure()
param smtpPassword string

@description('GitHub user/org that owns the GHCR package. Ignored when ghcrToken is empty.')
param ghcrUsername string = ''

@description('Token with read:packages for a private GHCR image. Empty = image is public, no registry credential.')
@secure()
param ghcrToken string = ''

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsWorkspaceName
}

// No workload profiles and no VNet: a Consumption-only environment.
resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspace.properties.customerId
        sharedKey: workspace.listKeys().primarySharedKey
      }
    }
    zoneRedundant: false
  }
}

var hasCustomDomain = !empty(customDomainName)
var bindCertificate = hasCustomDomain && customDomainCertificateReady

// A managed certificate can only be issued once the hostname is already on the
// app (bound as Disabled) and DNS validates, hence the two-phase parameter.
resource managedCertificate 'Microsoft.App/managedEnvironments/managedCertificates@2024-03-01' = if (bindCertificate) {
  parent: environment
  name: 'cert-${replace(customDomainName, '.', '-')}'
  location: location
  properties: {
    subjectName: customDomainName
    domainControlValidation: 'CNAME'
  }
}

var effectivePublicBaseUrl = !empty(publicBaseUrl)
  ? publicBaseUrl
  : (bindCertificate ? 'https://${customDomainName}' : 'https://${appName}.${environment.properties.defaultDomain}')

var connectionString = 'Host=${postgresFqdn};Port=5432;Database=${postgresDatabaseName};Username=${postgresAdministratorLogin};Password=${postgresAdministratorPassword};SSL Mode=VerifyFull'

var usePrivateRegistry = !empty(ghcrToken)

var appSecrets = concat(
  [
    { name: 'db-connection', value: connectionString }
    { name: 'jwt-key', value: jwtKey }
    { name: 'smtp-user', value: smtpUser }
    { name: 'smtp-password', value: smtpPassword }
  ],
  usePrivateRegistry ? [{ name: 'ghcr-token', value: ghcrToken }] : []
)

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        // Plain HTTP from the internet is redirected to HTTPS at the edge.
        allowInsecure: false
        customDomains: hasCustomDomain
          ? [
              {
                name: customDomainName
                bindingType: bindCertificate ? 'SniEnabled' : 'Disabled'
                certificateId: bindCertificate ? managedCertificate.id : null
              }
            ]
          : []
      }
      registries: usePrivateRegistry
        ? [
            {
              server: 'ghcr.io'
              username: ghcrUsername
              passwordSecretRef: 'ghcr-token'
            }
          ]
        : []
      secrets: appSecrets
    }
    template: {
      containers: [
        {
          name: 'wishlist'
          image: image
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'App__PublicBaseUrl', value: effectivePublicBaseUrl }
            { name: 'ConnectionStrings__DefaultConnection', secretRef: 'db-connection' }
            { name: 'Jwt__Key', secretRef: 'jwt-key' }
            { name: 'Smtp__Host', value: 'smtp.azurecomm.net' }
            { name: 'Smtp__Port', value: '587' }
            { name: 'Smtp__UseStartTls', value: 'true' }
            { name: 'Smtp__User', secretRef: 'smtp-user' }
            { name: 'Smtp__Password', secretRef: 'smtp-password' }
            { name: 'Smtp__FromAddress', value: smtpFromAddress }
            { name: 'Smtp__FromName', value: smtpFromName }
          ]
          probes: [
            {
              type: 'Startup'
              httpGet: { path: '/healthz', port: 8080 }
              periodSeconds: 2
              failureThreshold: 30
            }
            {
              type: 'Liveness'
              httpGet: { path: '/healthz', port: 8080 }
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: { path: '/healthz', port: 8080 }
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        // One replica keeps the in-process forgot-password rate limiter correct.
        maxReplicas: 1
        rules: [
          {
            name: 'http'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

output name string = app.name
output fqdn string = app.properties.configuration.ingress.fqdn
output customDomainVerificationId string = app.properties.customDomainVerificationId
output publicBaseUrl string = effectivePublicBaseUrl
