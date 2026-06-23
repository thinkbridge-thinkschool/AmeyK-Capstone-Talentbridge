using '../main.bicep'

param appName = 'talentbridge'
param suffix = 'amey'
param environment = 'dev'
param storageSku = 'Standard_LRS'
param staticWebAppSku = 'Free'
param samplingPercentage = 100
param softDeleteRetentionDays = 7
param sqlAdminLogin = 'tbridgeadmin'
// sqlAdminPassword is injected at deploy time: --parameters sqlAdminPassword=<secret>
