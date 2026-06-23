@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Base name used to construct resource names')
param appName string = 'talentbridge'

@description('Suffix for resource naming')
param suffix string = 'amey'

@description('Environment: dev or prod')
@allowed(['dev', 'prod'])
#disable-next-line no-unused-params
param environment string = 'dev'

@description('App Insights sampling percentage')
param samplingPercentage int = 100

@description('Key Vault soft delete retention days')
param softDeleteRetentionDays int = 7

param storageSku string = 'Standard_LRS'
param staticWebAppSku string = 'Free'

@description('SQL Server administrator login')
param sqlAdminLogin string = 'tbridgeadmin'

@description('SQL Server administrator password — injected at deploy time via --parameters')
@secure()
param sqlAdminPassword string

// ── Log Analytics Workspace ───────────────────────────────────────────────────
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${appName}-law-${suffix}'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

// ── Key Vault ─────────────────────────────────────────────────────────────────
module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault-deploy'
  params: {
    name: '${appName}-kv-${suffix}'
    location: location
    softDeleteRetentionDays: softDeleteRetentionDays
  }
}

// ── Storage ───────────────────────────────────────────────────────────────────
module storage 'modules/storage.bicep' = {
  name: 'storage-deploy'
  params: {
    name: '${appName}st${suffix}'
    location: location
    sku: storageSku
  }
}

// ── Application Insights ──────────────────────────────────────────────────────
module appInsights 'modules/appinsights.bicep' = {
  name: 'appinsights-deploy'
  params: {
    name: '${appName}-ai-${suffix}'
    location: location
    logAnalyticsWorkspaceId: logAnalytics.id
    samplingPercentage: samplingPercentage
  }
}

// ── Service Bus ───────────────────────────────────────────────────────────────
module serviceBus 'modules/servicebus.bicep' = {
  name: 'servicebus-deploy'
  params: {
    name: '${appName}-sb-${suffix}'
    location: location
  }
}

// ── Static Web App (eastus2 — eastus does not support staticSites) ───────────
module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'staticwebapp-deploy'
  params: {
    name: '${appName}-swa-${suffix}'
    location: 'eastus2'
    sku: staticWebAppSku
  }
}

// ── Azure SQL Database (Basic 5 DTU) ──────────────────────────────────────────
module sql 'modules/sql.bicep' = {
  name: 'sql-deploy'
  params: {
    serverName: '${appName}-sql-${suffix}'
    location: location
    adminLogin: sqlAdminLogin
    adminPassword: sqlAdminPassword
    edition: 'Basic'
    capacity: 5
    maxSizeGB: 2
  }
}

// ── Container App (API) + ACR ─────────────────────────────────────────────────
module api 'modules/containerapp.bicep' = {
  name: 'containerapp-deploy'
  params: {
    name: '${appName}-api-${suffix}'
    location: location
    logAnalyticsCustomerId: logAnalytics.properties.customerId
    logAnalyticsSharedKey: logAnalytics.listKeys().primarySharedKey
    minReplicas: 0
    maxReplicas: 2
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output keyVaultName string = keyVault.outputs.name
output keyVaultUri string = keyVault.outputs.uri
output storageAccountName string = storage.outputs.name
output appInsightsName string = appInsights.outputs.name
output serviceBusNamespace string = serviceBus.outputs.name
output staticWebAppUrl string = staticWebApp.outputs.url
output logAnalyticsName string = logAnalytics.name
output sqlServerFqdn string = sql.outputs.fqdn
output sqlDatabaseName string = sql.outputs.databaseName
output containerAppName string = api.outputs.name
output containerAppFqdn string = api.outputs.fqdn
output acrLoginServer string = api.outputs.registryLoginServer
output acrName string = api.outputs.registryName
