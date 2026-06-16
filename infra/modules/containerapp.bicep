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
    type: 'SystemAssigned'   // Managed Identity — used to read Key Vault secrets
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

output id string = containerApp.id
output name string = containerApp.name
output fqdn string = containerApp.properties.configuration.ingress.fqdn
output principalId string = containerApp.identity.principalId
output registryLoginServer string = containerRegistry.properties.loginServer
