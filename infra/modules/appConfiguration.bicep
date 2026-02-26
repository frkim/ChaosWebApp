@description('Name of the App Configuration store.')
param name string

@description('Azure region.')
param location string

@description('App Configuration SKU. Free tier is limited to 1k requests/day and 10 MB. Use Standard for production.')
param sku string = 'Standard'

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name:     name
  location: location
  sku: {
    name: sku
  }
  properties: {
    publicNetworkAccess: 'Enabled'
  }
}

output endpoint   string = appConfig.properties.endpoint
output resourceId string = appConfig.id
