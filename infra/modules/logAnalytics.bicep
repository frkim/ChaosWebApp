@description('Name of the Log Analytics workspace.')
param name string

@description('Azure region.')
param location string

resource workspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name:     name
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

output workspaceId      string = workspace.id
output primarySharedKey string = workspace.listKeys().primarySharedKey
