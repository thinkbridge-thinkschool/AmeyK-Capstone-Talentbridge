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

// NOTE: SQL and Container App excluded — student subscription regional/quota limits.
// Modules exist in infra/modules/ and are production-ready for a paid subscription.

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

// ── VNet (private-endpoint subnet for SQL; container-apps subnet for ACA) ────
module vnet 'modules/vnet.bicep' = {
  name: 'vnet-deploy'
  params: {
    vnetName: '${appName}-vnet-${suffix}'
    location: location
  }
}

// NOTE: SQL module excluded from active deployment (student subscription limits).
// On a paid subscription, wire it up like this:
// module sql 'modules/sql.bicep' = {
//   name: 'sql-deploy'
//   params: {
//     serverName: '${appName}sql${suffix}'
//     location: location
//     adminLogin: 'tbadmin'
//     adminPassword: <secure-param>
//     privateEndpointsSubnetId: vnet.outputs.privateEndpointsSubnetId
//     vnetId: vnet.outputs.vnetId
//   }
// }

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

// ── Outputs ───────────────────────────────────────────────────────────────────
output keyVaultName string = keyVault.outputs.name
output keyVaultUri string = keyVault.outputs.uri
output storageAccountName string = storage.outputs.name
output appInsightsName string = appInsights.outputs.name
output serviceBusNamespace string = serviceBus.outputs.name
output staticWebAppUrl string = staticWebApp.outputs.url
output logAnalyticsName string = logAnalytics.name
output vnetName string = vnet.outputs.vnetName
output privateEndpointsSubnetId string = vnet.outputs.privateEndpointsSubnetId
