#!/usr/bin/env bash
set -euo pipefail

ENV=${1:-dev}
RG="talentbridge-rg-amey"
STACK_NAME="talentbridge-stack-${ENV}"
PARAMS_FILE="infra/parameters/${ENV}.bicepparam"

echo "=== TalentBridge Deployment Stack: ${ENV} ==="
echo ""

echo "Step 1: Ensure resource group exists"
az group create \
  --name "$RG" \
  --location centralindia \
  --tags environment="$ENV" managedBy=deployment-stack \
  --output table

echo ""
echo "Step 2: What-if (preview changes)"
az deployment group what-if \
  --resource-group "$RG" \
  --template-file infra/main.bicep \
  --parameters "$PARAMS_FILE" \
  --parameters sqlAdminPassword="${SQL_ADMIN_PASSWORD}" \
  --result-format ResourceIdOnly

echo ""
read -rp "Deploy as Deployment Stack? (y/N): " CONFIRM
[[ "$CONFIRM" != "y" && "$CONFIRM" != "Y" ]] && { echo "Cancelled."; exit 0; }

echo ""
echo "Step 3: Create/update Deployment Stack"
az stack group create \
  --name "$STACK_NAME" \
  --resource-group "$RG" \
  --template-file infra/main.bicep \
  --parameters "$PARAMS_FILE" \
  --parameters sqlAdminPassword="${SQL_ADMIN_PASSWORD}" \
  --action-on-unmanage deleteAll \
  --deny-settings-mode none \
  --yes \
  --output table

echo ""
echo "Step 4: Show stack status + detect drift"
az stack group show \
  --name "$STACK_NAME" \
  --resource-group "$RG" \
  --output table

echo ""
echo "Step 5: List all managed resources"
az stack group show \
  --name "$STACK_NAME" \
  --resource-group "$RG" \
  --query "resources[].id" \
  --output table

echo ""
echo "Done. Stack ${STACK_NAME} is live."
echo ""
echo "To TEARDOWN cleanly (deletes ALL managed resources):"
echo "  az stack group delete --name ${STACK_NAME} --resource-group ${RG} --action-on-unmanage deleteAll --yes"
