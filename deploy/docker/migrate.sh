#!/bin/sh
# Applies EF Core migrations for every module. Needs COMMERCEHUB_CONNECTION in the environment.
set -eu
: "${COMMERCEHUB_CONNECTION:?COMMERCEHUB_CONNECTION is required}"

API=backend/src/Api/CommerceHub.Api/CommerceHub.Api.csproj
for m in Cart Catalog Customers Identity Inventory Notifications Orders Payments Promotions Shipping; do
  echo ">> Migrating $m"
  dotnet ef database update \
    --project "backend/src/Modules/$m/CommerceHub.Modules.$m/CommerceHub.Modules.$m.csproj" \
    --startup-project "$API" \
    --context "${m}DbContext"
done
echo "All migrations applied."
