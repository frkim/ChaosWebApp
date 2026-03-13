@description('Name of the App Service Plan.')
param appServicePlanName string

@description('Name of the Web App.')
param webAppName string

@description('Azure region.')
param location string

@description('Full container image reference.')
param containerImage string

@description('Application Insights connection string.')
@secure()
param appInsightsConnectionString string

@description('Azure App Configuration endpoint URL.')
param appConfigEndpoint string

@description('ASP.NET Core environment name.')
param aspNetCoreEnvironment string

@description('ACR login server.')
param acrLoginServer string

@description('User-assigned managed identity resource ID.')
param managedIdentityId string

@description('User-assigned managed identity client ID.')
param managedIdentityClientId string

// ── App Service Plan (Linux, B1) ──────────────────────────────────────────────
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name:     appServicePlanName
  location: location
  kind:     'linux'
  properties: {
    reserved: true
  }
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
}

// ── Web App for Containers ────────────────────────────────────────────────────
resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name:     webAppName
  location: location
  tags: {
    'azd-service-name': 'chaoswebapp'
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOCKER|${containerImage}'
      acrUseManagedIdentityCreds: true
      acrUserManagedIdentityID:   managedIdentityClientId
      alwaysOn:     true
      http20Enabled: true
      healthCheckPath: '/health/live'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT',               value: aspNetCoreEnvironment }
        { name: 'WEBSITES_PORT',                        value: '8080' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
        { name: 'AZURE_APPCONFIG_ENDPOINT',              value: appConfigEndpoint }
        { name: 'AZURE_CLIENT_ID',                       value: managedIdentityClientId }
        { name: 'DOCKER_REGISTRY_SERVER_URL',            value: 'https://${acrLoginServer}' }
      ]
    }
    httpsOnly: true
  }
}

output fqdn       string = 'https://${webApp.properties.defaultHostName}'
output resourceId string = webApp.id
