using '../main.bicep'

param appName = 'talentbridge'
param suffix = 'ameyp'
param environment = 'prod'
param storageSku = 'Standard_GRS'
param staticWebAppSku = 'Standard'
param samplingPercentage = 10
param softDeleteRetentionDays = 90
