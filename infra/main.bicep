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
