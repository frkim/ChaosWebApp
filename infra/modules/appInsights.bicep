@description('Application Insights resource name.')
param appInsightsName string

@description('Azure region.')
param location string

@description('Resource ID of the Log Analytics workspace.')
param logAnalyticsId string

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name:     appInsightsName
  location: location
  kind:     'web'
  properties: {
    Application_Type:                  'web'
    WorkspaceResourceId:               logAnalyticsId
    IngestionMode:                     'LogAnalytics'
    publicNetworkAccessForIngestion:   'Enabled'
    publicNetworkAccessForQuery:       'Enabled'
  }
}

output connectionString  string = appInsights.properties.ConnectionString
output instrumentationKey string = appInsights.properties.InstrumentationKey
output resourceId         string = appInsights.id
