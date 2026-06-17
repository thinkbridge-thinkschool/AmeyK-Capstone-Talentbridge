#!/usr/bin/env bash
# setup-mi-sql.sh
# Run once against the Azure SQL database to grant the Container App's
# Managed Identity access. This cannot be done via Bicep/ARM — it requires
# an Entra ID-enabled SQL admin to execute T-SQL inside the database.
#
# Prerequisites:
#   - You must be logged in as the SQL Server Entra ID admin (az login)
#   - sqlcmd must be installed (brew install sqlcmd OR via mssql-tools on Linux)
#
# Usage:
#   chmod +x infra/setup-mi-sql.sh
#   ./infra/setup-mi-sql.sh

set -euo pipefail

SERVER="talentbridge-sql-amey.database.windows.net"
DATABASE="talentbridge-sql-amey-db"
MANAGED_IDENTITY_NAME="talentbridge-api-amey"   # Container App name = MI display name

echo "Connecting to $SERVER / $DATABASE as Entra ID admin..."

sqlcmd \
  -S "$SERVER" \
  -d "$DATABASE" \
  --authentication-method=ActiveDirectoryDefault \
  -Q "
-- Create a database user mapped to the Container App's system-assigned MI.
-- The identity name must match the Container App resource name exactly.
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$MANAGED_IDENTITY_NAME')
BEGIN
    CREATE USER [$MANAGED_IDENTITY_NAME] FROM EXTERNAL PROVIDER;
    PRINT 'User created: $MANAGED_IDENTITY_NAME';
END
ELSE
    PRINT 'User already exists: $MANAGED_IDENTITY_NAME';

-- Grant read access
ALTER ROLE db_datareader ADD MEMBER [$MANAGED_IDENTITY_NAME];

-- Grant write access
ALTER ROLE db_datawriter ADD MEMBER [$MANAGED_IDENTITY_NAME];

-- Grant DDL access (EF Core migrations)
ALTER ROLE db_ddladmin ADD MEMBER [$MANAGED_IDENTITY_NAME];

PRINT 'Role assignments complete.';
"

echo "Done. $MANAGED_IDENTITY_NAME can now authenticate to $DATABASE via Managed Identity."
