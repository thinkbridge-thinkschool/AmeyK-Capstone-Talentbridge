using '../main.bicep'

// ── Identity ──────────────────────────────────────────────────────────────────
param appName = 'talentbridge'
param suffix = 'amey'
param environment = 'dev'

// ── SQL — Basic / 5 DTU / 2 GB ───────────────────────────────────────────────
param sqlAdminLogin = 'sqladmin'
param sqlEdition = 'Basic'
param sqlCapacity = 5
param sqlMaxSizeGB = 2

// ── Storage — locally redundant ───────────────────────────────────────────────
param storageSku = 'Standard_LRS'

// ── Container App — scale to zero (saves student credits when idle) ───────────
param minReplicas = 0
param maxReplicas = 1
param containerCpu = '0.25'
param containerMemory = '0.5Gi'

// ── Static Web App ────────────────────────────────────────────────────────────
param staticWebAppSku = 'Free'

// ── Observability — full sampling in dev ──────────────────────────────────────
param samplingPercentage = 100

// ── Key Vault soft delete — 7 days in dev ────────────────────────────────────
param softDeleteRetentionDays = 7
