@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Base name used to derive all resource names.')
param appName string = 'chaoswebapp'

@description('Full container image reference, e.g. myregistry.azurecr.io/chaoswebapp:latest')
param containerImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@description('Azure App Configuration SKU (Free: 1k req/day, 10MB | Standard: unlimited).')
param appConfigSku string = 'Standard'

@description('ASP.NET Core environment (Development / Production).')
param aspNetCoreEnvironment string = 'Production'

// ── Derived names ─────────────────────────────────────────────────────────────
var uniqueSuffix     = uniqueString(resourceGroup().id)
var acrName          = '${replace(appName, '-', '')}${uniqueSuffix}'
var logAnalyticsName = '${appName}-logs-${uniqueSuffix}'
var appInsightsName  = '${appName}-ai-${uniqueSuffix}'
var appConfigName    = '${appName}-cfg-${uniqueSuffix}'
var containerEnvName = '${appName}-env-${uniqueSuffix}'
var containerAppName = '${appName}-app-${uniqueSuffix}'
var loadTestName     = '${appName}-lt-${uniqueSuffix}'
var identityName     = '${appName}-id-${uniqueSuffix}'

// ── User-assigned managed identity (created before Container App) ─────────────
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name:     identityName
  location: location
}

// ── Modules ───────────────────────────────────────────────────────────────────
module logAnalytics 'modules/logAnalytics.bicep' = {
  name: 'logAnalyticsDeploy'
  params: {
    name:     logAnalyticsName
    location: location
  }
}

module acr 'modules/acr.bicep' = {
  name: 'acrDeploy'
  params: {
    name:     acrName
    location: location
  }
}

// ── AcrPull role assignment (before Container App creation) ───────────────────
var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'

resource acrResource 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

resource acrPullAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acrResource.id, managedIdentity.id, acrPullRoleId)
  scope: acrResource
  dependsOn: [ acr ]
  properties: {
    principalId:      managedIdentity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalType:    'ServicePrincipal'
  }
}

// ── App Configuration Data Reader role ────────────────────────────────────────
var appConfigDataReaderRoleId = '516239f1-63e1-4d78-a4de-a74fb236a071'

resource appConfigResource 'Microsoft.AppConfiguration/configurationStores@2023-03-01' existing = {
  name: appConfigName
}

resource appConfigDataReaderAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfigResource.id, managedIdentity.id, appConfigDataReaderRoleId)
  scope: appConfigResource
  dependsOn: [ appConfiguration ]
  properties: {
    principalId:      managedIdentity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', appConfigDataReaderRoleId)
    principalType:    'ServicePrincipal'
  }
}

// ── App Configuration Data Owner role (for writes) ───────────────────────────
var appConfigDataOwnerRoleId = '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b'

resource appConfigDataOwnerAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfigResource.id, managedIdentity.id, appConfigDataOwnerRoleId)
  scope: appConfigResource
  dependsOn: [ appConfiguration ]
  properties: {
    principalId:      managedIdentity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', appConfigDataOwnerRoleId)
    principalType:    'ServicePrincipal'
  }
}

module appInsights 'modules/appInsights.bicep' = {
  name: 'appInsightsDeploy'
  params: {
    appInsightsName:  appInsightsName
    location:         location
    logAnalyticsId:   logAnalytics.outputs.workspaceId
  }
}

module appConfiguration 'modules/appConfiguration.bicep' = {
  name: 'appConfigDeploy'
  params: {
    name:     appConfigName
    location: location
    sku:      appConfigSku
  }
}

module containerApp 'modules/containerApp.bicep' = {
  name: 'containerAppDeploy'
  dependsOn: [ acrPullAssignment ]
  params: {
    containerAppName:            containerAppName
    containerEnvName:            containerEnvName
    location:                    location
    containerImage:              containerImage
    logAnalyticsWorkspaceId:     logAnalytics.outputs.workspaceId
    logAnalyticsWorkspaceKey:    logAnalytics.outputs.primarySharedKey
    appInsightsConnectionString: appInsights.outputs.connectionString
    appConfigEndpoint:           appConfiguration.outputs.endpoint
    aspNetCoreEnvironment:       aspNetCoreEnvironment
    acrLoginServer:              acr.outputs.loginServer
    managedIdentityId:           managedIdentity.id
    managedIdentityClientId:     managedIdentity.properties.clientId
  }
}

module loadTesting 'modules/loadTesting.bicep' = {
  name: 'loadTestingDeploy'
  params: {
    name:     loadTestName
    location: location
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output containerAppUrl            string = containerApp.outputs.fqdn
output appInsightsConnectionString string = appInsights.outputs.connectionString
output acrLoginServer             string = acr.outputs.loginServer
output appConfigEndpoint          string = appConfiguration.outputs.endpoint
output loadTestResourceId         string = loadTesting.outputs.resourceId
