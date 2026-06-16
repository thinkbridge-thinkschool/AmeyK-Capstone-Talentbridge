@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Base name used to construct resource names')
param appName string = 'quotesapp'

@description('Suffix for resource naming')
param suffix string = 'amey'

@description('Environment: dev or prod')
@allowed(['dev', 'prod'])
param environment string = 'dev'

@description('SQL administrator login')
param sqlAdminLogin string = 'sqladmin'

@description('SQL administrator password')
@secure()
param sqlAdminPassword string

@description('App Insights sampling percentage')
param samplingPercentage int = 100

@description('Key Vault soft delete retention days')
param softDeleteRetentionDays int = 7

param sqlEdition string = 'Basic'
param sqlCapacity int = 5
param sqlMaxSizeGB int = 2
param storageSku string = 'Standard_LRS'
param minReplicas int = 0
param maxReplicas int = 1
param containerCpu string = '0.25'
param containerMemory string = '0.5Gi'
param staticWebAppSku string = 'Free'

// ── Log Analytics Workspace (inline) ─────────────────────────────────────────
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

// ── SQL Server + Database ─────────────────────────────────────────────────────
module sql 'modules/sql.bicep' = {
  name: 'sql-deploy'
  params: {
    serverName: '${appName}-sql-${suffix}'
    location: location
    adminLogin: sqlAdminLogin
    adminPassword: sqlAdminPassword
    edition: sqlEdition
    capacity: sqlCapacity
    maxSizeGB: sqlMaxSizeGB
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

// ── Container Registry + App ──────────────────────────────────────────────────
module containerApp 'modules/containerapp.bicep' = {
  name: 'containerapp-deploy'
  params: {
    name: '${appName}-api-${suffix}'
    location: location
    logAnalyticsCustomerId: logAnalytics.properties.customerId
    logAnalyticsSharedKey: logAnalytics.listKeys().primarySharedKey
    minReplicas: minReplicas
    maxReplicas: maxReplicas
    cpu: containerCpu
    memory: containerMemory
  }
}

// ── Static Web App ────────────────────────────────────────────────────────────
module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'staticwebapp-deploy'
  params: {
    name: '${appName}-swa-${suffix}'
    location: location
    sku: staticWebAppSku
  }
}

// ── Key Vault Secrets (writes all connection strings after resources created) ─
module keyVaultSecrets 'modules/keyvault-secrets.bicep' = {
  name: 'keyvault-secrets-deploy'
  params: {
    keyVaultName: keyVault.outputs.name
    sqlConnectionString: sql.outputs.connectionString
    storageConnectionString: storage.outputs.connectionString
    serviceBusConnectionString: serviceBus.outputs.connectionString
    appInsightsConnectionString: appInsights.outputs.connectionString
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output keyVaultName string = keyVault.outputs.name
output storageAccountName string = storage.outputs.name
output sqlServerFqdn string = sql.outputs.fqdn
output serviceBusNamespace string = serviceBus.outputs.name
output containerAppFqdn string = containerApp.outputs.fqdn
output staticWebAppUrl string = staticWebApp.outputs.url
output containerAppPrincipalId string = containerApp.outputs.principalId
