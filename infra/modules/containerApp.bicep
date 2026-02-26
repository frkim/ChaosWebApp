@description('Name of the Container App.')
param containerAppName string

@description('Name of the Container Apps Environment.')
param containerEnvName string

@description('Azure region.')
param location string

@description('Full container image reference.')
param containerImage string

@description('Log Analytics workspace resource ID.')
param logAnalyticsWorkspaceId string

@description('Log Analytics workspace primary shared key.')
@secure()
param logAnalyticsWorkspaceKey string

@description('Application Insights connection string.')
@secure()
param appInsightsConnectionString string

@description('Azure App Configuration endpoint URL.')
param appConfigEndpoint string

@description('ASP.NET Core environment name.')
param aspNetCoreEnvironment string

@description('ACR login server.')
param acrLoginServer string

// ── Container Apps Environment ────────────────────────────────────────────────
resource containerEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name:     containerEnvName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: reference(logAnalyticsWorkspaceId, '2022-10-01').customerId
        sharedKey:  logAnalyticsWorkspaceKey
      }
    }
  }
}

// ── Container App ─────────────────────────────────────────────────────────────
resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name:     containerAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: containerEnv.id
    configuration: {
      secrets: [
        {
          name:  'ai-connection-string'
          value: appInsightsConnectionString
        }
      ]
      ingress: {
        external:      true
        targetPort:    8080
        transport:     'http'
        allowInsecure: false
      }
      registries: [
        {
          server:   acrLoginServer
          identity: 'system'
        }
      ]
    }
    template: {
      containers: [
        {
          name:  'chaoswebapp'
          image: containerImage
          resources: {
            cpu:    json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT',               value: aspNetCoreEnvironment }
            { name: 'ASPNETCORE_URLS',                      value: 'http://+:8080' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', secretRef: 'ai-connection-string' }
            { name: 'AZURE_APPCONFIG_ENDPOINT',              value: appConfigEndpoint }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path:   '/health/live'
                port:   8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 15
              periodSeconds:       30
              failureThreshold:    3
            }
            {
              type: 'Readiness'
              httpGet: {
                path:   '/health/ready'
                port:   8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds:       15
              failureThreshold:    3
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '20'
              }
            }
          }
        ]
      }
    }
  }
}

output fqdn        string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output resourceId  string = containerApp.id
output principalId string = containerApp.identity.principalId
