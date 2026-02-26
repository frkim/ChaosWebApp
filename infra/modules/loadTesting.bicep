@description('Name of the Azure Load Testing resource.')
param name string

@description('Azure region.')
param location string

resource loadTest 'Microsoft.LoadTestService/loadTests@2022-12-01' = {
  name:     name
  location: location
  properties: {}
}

output resourceId string = loadTest.id
output name       string = loadTest.name
