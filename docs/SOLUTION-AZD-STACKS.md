# TalentBridge — Deployment Stacks + azd

## Exercise: Deploy the full stack with Azure Deployment Stacks driven by azd CLI. Deploy to dev, then promote to prod.

---

## What this builds

| Component | Technology |
|---|---|
| Infrastructure lifecycle | Azure Deployment Stacks (`az stack group create`) |
| App provisioning tool | Azure Developer CLI (`azd`) |
| Backend | .NET 10 API → Docker → Azure Container Registry → App Service |
| Frontend | Angular 17 → Azure Static Web App |
| Dev/Prod separation | Separate stacks per environment, different params |

---

## Why Deployment Stacks over plain deployments

Plain `az deployment group create` has no memory — orphaned resources from removed Bicep modules stay forever, drift is invisible, and teardown is manual.

A Deployment Stack **owns** every resource it creates:
- Detects drift from anything added outside the stack
- Auto-deletes resources removed from Bicep (`--action-on-unmanage deleteAll`)
- Clean teardown in one command — no orphans

---

## Stack architecture

```
talentbridge-rg-amey/
├── talentbridge-stack-dev    ← owns 15 resources (suffix: amey)
│   ├── talentbridge-ai-amey        App Insights
│   ├── talentbridge-kv-amey        Key Vault
│   ├── talentbridge-law-amey       Log Analytics
│   ├── talentbridge-sb-amey        Service Bus + topics + subs
│   ├── talentbridgestamey          Storage + containers
│   └── talentbridge-swa-amey       Static Web App
│
└── talentbridge-stack-prod   ← owns 15 resources (suffix: ameyp)
    ├── talentbridge-ai-ameyp       App Insights
    ├── talentbridge-kv-ameyp       Key Vault (90-day soft delete)
    ├── talentbridge-law-ameyp      Log Analytics
    ├── talentbridge-sb-ameyp       Service Bus + topics + subs
    ├── talentbridgestameyp         Storage (GRS — geo-redundant)
    └── talentbridge-swa-ameyp      Static Web App (Standard SKU)
```

---

## Dev vs Prod differences

| Setting | Dev | Prod |
|---|---|---|
| Suffix | `amey` | `ameyp` |
| Storage SKU | Standard_LRS | Standard_GRS |
| Static Web App SKU | Free | Standard |
| App Insights sampling | 100% | 10% |
| Key Vault soft delete | 7 days | 90 days |
| Stack name | `talentbridge-stack-dev` | `talentbridge-stack-prod` |

---

## How to deploy

### Prerequisites
```bash
az login --use-device-code
winget install Microsoft.Azd
azd auth login --use-device-code
```

### Deploy dev (Deployment Stack)
```bash
az stack group create \
  --name talentbridge-stack-dev \
  --resource-group talentbridge-rg-amey \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam \
  --action-on-unmanage deleteAll \
  --deny-settings-mode none \
  --yes
```

### Promote to prod
```bash
az stack group create \
  --name talentbridge-stack-prod \
  --resource-group talentbridge-rg-amey \
  --template-file infra/main.bicep \
  --parameters infra/parameters/prod.bicepparam \
  --action-on-unmanage deleteAll \
  --deny-settings-mode none \
  --yes
```

### List both stacks
```bash
az stack group list --resource-group talentbridge-rg-amey --output table
```

### Teardown (clean delete — no orphans)
```bash
az stack group delete --name talentbridge-stack-dev \
  --resource-group talentbridge-rg-amey --action-on-unmanage deleteAll --yes

az stack group delete --name talentbridge-stack-prod \
  --resource-group talentbridge-rg-amey --action-on-unmanage deleteAll --yes
```

---

## azd config (azure.yaml)

```yaml
name: talentbridge
infra:
  provider: bicep
  path: infra
  module: main
services:
  api:
    project: src/API/TalentBridge.API
    language: dotnet
    host: containerapp
    docker:
      path: ./Dockerfile
      context: .
pipeline:
  provider: github
```

---

## GitHub Actions CI/CD (deploy.yml)

4-job pipeline triggered via `workflow_dispatch`:

```
deploy-infra  → az stack group create  (provisions all Azure resources)
    ↓
deploy-backend → az acr build + az webapp deploy  (Docker → App Service)
deploy-frontend → npm run build:prod + Azure/static-web-apps-deploy  (Angular → SWA)
    ↓
summary → prints live URLs
```

---

## Dev stack deploy output — 2026-06-16

**Command:**
```bash
az stack group create --name talentbridge-stack-dev --resource-group talentbridge-rg-amey \
  --template-file infra/main.bicep --parameters infra/parameters/dev.bicepparam \
  --action-on-unmanage deleteAll --deny-settings-mode none --yes
```

**Result:** `provisioningState: succeeded` — Duration: PT59.2097359S

**Stack ID:**
```
/subscriptions/50f9dc41-193b-4389-85f2-420f2684cee2/resourceGroups/talentbridge-rg-amey/
providers/Microsoft.Resources/deploymentStacks/talentbridge-stack-dev
```

**Stack outputs:**
| Output | Value |
|---|---|
| appInsightsName | `talentbridge-ai-amey` |
| keyVaultName | `talentbridge-kv-amey` |
| keyVaultUri | `https://talentbridge-kv-amey.vault.azure.net/` |
| logAnalyticsName | `talentbridge-law-amey` |
| serviceBusNamespace | `talentbridge-sb-amey` |
| storageAccountName | `talentbridgestamey` |
| staticWebAppUrl | `https://jolly-island-0a6a1580f.7.azurestaticapps.net` |

**actionOnUnmanage:** `deleteAll` — resources removed from Bicep are automatically deleted.

---

## Prod stack deploy output

[Paste `az stack group create --name talentbridge-stack-prod` output here after it completes]

---

## Both stacks live

```bash
az stack group list --resource-group talentbridge-rg-amey --output table
```

[Paste output here]
