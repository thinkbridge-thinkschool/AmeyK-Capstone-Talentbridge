using '../main.bicep'

// ── Identity ──────────────────────────────────────────────────────────────────
param appName = 'talentbridge'
param suffix = 'amey'
param environment = 'prod'

// ── SQL — S2 / 50 DTU / 10 GB ────────────────────────────────────────────────
param sqlAdminLogin = 'sqladmin'
param sqlEdition = 'Standard'
param sqlCapacity = 50
param sqlMaxSizeGB = 10

// ── Storage — geo-redundant ───────────────────────────────────────────────────
param storageSku = 'Standard_GRS'

// ── Container App — always on, higher resources ───────────────────────────────
param minReplicas = 1
param maxReplicas = 3
param containerCpu = '0.5'
param containerMemory = '1.0Gi'

// ── Static Web App ────────────────────────────────────────────────────────────
param staticWebAppSku = 'Standard'

// ── Observability — 10% sampling in prod to control costs ────────────────────
param samplingPercentage = 10

// ── Key Vault soft delete — 90 days in prod ───────────────────────────────────
param softDeleteRetentionDays = 90
