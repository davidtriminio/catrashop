#!/bin/sh
# Applies EF Core migrations for every module. Needs CATRASHOP_CONNECTION in the environment.
set -eu
: "${CATRASHOP_CONNECTION:?CATRASHOP_CONNECTION is required}"

API=backend/src/Api/Catrashop.Api/Catrashop.Api.csproj
for m in Cart Catalog Customers Identity Inventory Notifications Orders Payments Promotions Shipping; do
  echo ">> Migrating $m"
  dotnet ef database update \
    --project "backend/src/Modules/$m/Catrashop.Modules.$m/Catrashop.Modules.$m.csproj" \
    --startup-project "$API" \
    --context "${m}DbContext"
done
echo "All migrations applied."
