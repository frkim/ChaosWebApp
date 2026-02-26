@description('Name of the Azure Container Registry (alphanumeric, 5-50 chars).')
param name string

@description('Azure region.')
param location string

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name:     name
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

output loginServer string = registry.properties.loginServer
output resourceId  string = registry.id
