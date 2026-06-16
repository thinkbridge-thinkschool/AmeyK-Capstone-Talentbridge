#!/usr/bin/env bash
set -euo pipefail

# Usage:
#   export SQL_ADMIN_PASSWORD="YourP@ssw0rd!"
#   ./deploy.sh dev
#   ./deploy.sh prod

ENV=${1:-dev}

if [[ "$ENV" == "prod" ]]; then
  RESOURCE_GROUP="quotesapp-rg-prod"
else
  RESOURCE_GROUP="quotesapp-rg-amey"
fi

DEPLOYMENT_NAME="talentbridge-${ENV}-iac"
TEMPLATE="main.bicep"
PARAMS="parameters/${ENV}.bicepparam"

# ── Validate SQL password is set ─────────────────────────────────────────────
if [[ -z "${SQL_ADMIN_PASSWORD:-}" ]]; then
  echo "ERROR: SQL_ADMIN_PASSWORD is not set."
  echo "Run: export SQL_ADMIN_PASSWORD='YourP@ssw0rd!'"
  exit 1
fi

echo "========================================"
echo " TalentBridge IaC Deploy"
echo " Environment : $ENV"
echo " Resource Grp: $RESOURCE_GROUP"
echo " Deployment  : $DEPLOYMENT_NAME"
echo "========================================"

# ── Step 1: What-if (dry run) ─────────────────────────────────────────────────
echo ""
echo "==> Running what-if (no changes will be made)..."
az deployment group what-if \
  --resource-group "$RESOURCE_GROUP" \
  --template-file "$TEMPLATE" \
  --parameters "$PARAMS" \
  --parameters sqlAdminPassword="$SQL_ADMIN_PASSWORD" \
  --result-format ResourceIdOnly

# ── Step 2: Confirm ───────────────────────────────────────────────────────────
echo ""
read -r -p "Proceed with deployment to '$ENV'? (y/N) " CONFIRM
if [[ "$CONFIRM" != "y" && "$CONFIRM" != "Y" ]]; then
  echo "Deployment cancelled."
  exit 0
fi

# ── Step 3: Deploy (Incremental — never deletes existing resources) ───────────
echo ""
echo "==> Deploying..."
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file "$TEMPLATE" \
  --parameters "$PARAMS" \
  --parameters sqlAdminPassword="$SQL_ADMIN_PASSWORD" \
  --name "$DEPLOYMENT_NAME" \
  --mode Incremental \
  --output table

# ── Step 4: Show outputs ──────────────────────────────────────────────────────
echo ""
echo "==> Deployment complete! Outputs:"
az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query "properties.outputs" \
  --output table

# ── Step 5: Remind to assign RBAC ────────────────────────────────────────────
echo ""
echo "==> NEXT STEP: Assign Key Vault Secrets User role to the Container App's managed identity."
echo "    Run: chmod +x assign-rbac.sh && ./assign-rbac.sh $ENV"
