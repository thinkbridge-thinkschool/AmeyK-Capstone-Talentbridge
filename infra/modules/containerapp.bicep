// NOTE: If a Container App already exists with this name, this module updates it
// incrementally — it does NOT delete or recreate it (deploy uses --mode Incremental).

param name string
param location string
param logAnalyticsCustomerId string
@secure()
param logAnalyticsSharedKey string
param minReplicas int = 0
param maxReplicas int = 1
param cpu string = '0.25'
param memory string = '0.5Gi'

// Resource IDs needed for RBAC role assignments
param sqlServerId string = ''
param serviceBusNamespaceId string = ''
param keyVaultId string = ''

// ── Built-in role definition IDs ──────────────────────────────────────────────
var sqlDbContributorRoleId     = '9b7fa17d-e63e-47b0-bb0a-15c516ac86ec' // SQL DB Contributor
var serviceBusDataOwnerRoleId  = '090c5cfd-751d-490a-894a-3ce6f1109419' // Azure Service Bus Data Owner
var keyVaultSecretsUserRoleId  = '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User

// ── Container Registry ────────────────────────────────────────────────────────
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: replace('${name}acr', '-', '')
  location: location
  sku: { name: 'Basic' }
  properties: {
    adminUserEnabled: true
  }
}

// ── Container Apps Environment ────────────────────────────────────────────────
resource containerAppsEnv 'Microsoft.App/managedEnvironments@2023-11-02-preview' = {
  name: '${name}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
        sharedKey: logAnalyticsSharedKey
      }
    }
  }
}

// ── Container App (System Assigned Managed Identity) ─────────────────────────
resource containerApp 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: name
  location: location
  identity: {
    type: 'SystemAssigned'   // Managed Identity — grants access to SQL, Service Bus, Key Vault
  }
  properties: {
    managedEnvironmentId: containerAppsEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: containerRegistry.listCredentials().passwords[0].value
        }
        // JWT secret pulled from Key Vault via reference — no plaintext secret in config
        {
          name: 'jwt-secret'
          keyVaultUrl: 'https://${split(keyVaultId, '/')[8]}.vault.azure.net/secrets/JwtSecret'
          identity: 'system'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'talentbridge-api'
          image: '${containerRegistry.properties.loginServer}/talentbridge-api:latest'
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas   // 0 in dev = scale to zero, saves credits
        maxReplicas: maxReplicas
      }
    }
  }
}

// ── RBAC: SQL DB Contributor on SQL Server ────────────────────────────────────
// Allows the Container App's MI to authenticate to Azure SQL via Entra ID.
// The SQL-level CREATE USER FROM EXTERNAL PROVIDER is handled in setup-mi-sql.sh.
resource sqlRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(sqlServerId)) {
  name: guid(sqlServerId, containerApp.id, sqlDbContributorRoleId)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', sqlDbContributorRoleId)
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ── RBAC: Azure Service Bus Data Owner on Service Bus namespace ───────────────
// Allows the MI to send and receive messages without a SharedAccessKey.
resource serviceBusRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(serviceBusNamespaceId)) {
  name: guid(serviceBusNamespaceId, containerApp.id, serviceBusDataOwnerRoleId)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', serviceBusDataOwnerRoleId)
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ── RBAC: Key Vault Secrets User on Key Vault ─────────────────────────────────
// Allows the MI to read secrets (JWT key, App Insights connection string).
resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(keyVaultId)) {
  name: guid(keyVaultId, containerApp.id, keyVaultSecretsUserRoleId)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output id string = containerApp.id
output name string = containerApp.name
output fqdn string = containerApp.properties.configuration.ingress.fqdn
output principalId string = containerApp.identity.principalId
output registryLoginServer string = containerRegistry.properties.loginServer
output registryName string = containerRegistry.name
