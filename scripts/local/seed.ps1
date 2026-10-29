<#
.SYNOPSIS
  Seeds a LOCAL CommerceHub with an admin user, a sample catalog with stock, a customer and a few orders in
  different states, so the admin SPA and the Flutter app have something to show.
.DESCRIPTION
  Requires: migrations applied (setup-db.ps1) and the API running (dotnet run --project backend/src/Api/CommerceHub.Api).
  Everything goes through the real API, except promoting the admin user (a DB update: there is no endpoint for that).
  Re-running is safe for users/catalog (409s are ignored) but adds 4 more orders each time.
  These are LOCAL TEST credentials only; never reuse them anywhere real.
.EXAMPLE
  ./scripts/local/seed.ps1
  ./scripts/local/seed.ps1 -ApiUrl http://localhost:5227 -AdminEmail me@x.dev -AdminPassword 'MyPass123'
#>
param(
  [string]$ApiUrl = 'http://localhost:5227',
  [string]$AdminEmail = 'admin@commercehub.local',
  [string]$AdminPassword = 'Admin12345',
  [string]$CustomerEmail = 'cliente@commercehub.local',
  [string]$CustomerPassword = 'Cliente12345',
  [string]$ConnectionString
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"

function Call([string]$Method, [string]$Path, $Body = $null, [string]$Token, [hashtable]$Headers = @{}, [switch]$Soft) {
  $h = @{} + $Headers
  if ($Token) { $h['Authorization'] = "Bearer $Token" }
  $req = @{ Method = $Method; Uri = "$ApiUrl$Path"; Headers = $h; ContentType = 'application/json'; SkipHttpErrorCheck = $true; StatusCodeVariable = 'sc' }
  if ($null -ne $Body) { $req.Body = ($Body | ConvertTo-Json -Depth 8) }
  $r = Invoke-RestMethod @req
  if ($sc -ge 400) {
    if ($Soft) { return $null }
    throw "$Method $Path -> HTTP $sc : $($r | ConvertTo-Json -Compress -Depth 5)"
  }
  return $r
}

function Login([string]$email, [string]$password) {
  (Call POST '/api/v1/identity/login' @{ email = $email; password = $password }).accessToken
}

Write-Host ">> Checking API at $ApiUrl"
try { Invoke-RestMethod "$ApiUrl/health/live" | Out-Null } catch { throw "API not reachable at $ApiUrl. Start it first: dotnet run --project backend/src/Api/CommerceHub.Api" }

# --- Users -------------------------------------------------------------------------------------
Write-Host ">> Admin user $AdminEmail"
Call POST '/api/v1/identity/register' @{ email = $AdminEmail; password = $AdminPassword; firstName = 'Admin'; lastName = 'Local' } -Soft | Out-Null

$pg = ConvertFrom-NpgsqlConnectionString (Get-CommerceHubConnectionString $ConnectionString)
$env:PGPASSWORD = $pg.Password
$sql = "INSERT INTO identity.user_roles (user_id, role) SELECT id, 'Administrator' FROM identity.users WHERE lower(email) = lower('$AdminEmail') ON CONFLICT DO NOTHING;"
& (Get-Psql) -h $pg.Host -p $pg.Port -U $pg.Username -d $pg.Database -v ON_ERROR_STOP=1 -q -c $sql
if ($LASTEXITCODE) { throw 'Could not promote the admin user in the database.' }
$admin = Login $AdminEmail $AdminPassword   # fresh login so the token carries the new role

Write-Host ">> Customer user $CustomerEmail"
Call POST '/api/v1/identity/register' @{ email = $CustomerEmail; password = $CustomerPassword; firstName = 'Cliente'; lastName = 'Prueba' } -Soft | Out-Null
Write-Host '   waiting for the customer profile to be provisioned (outbox, ~10s)...'
Start-Sleep -Seconds 10
$cust = Login $CustomerEmail $CustomerPassword

# --- Catalog + stock ---------------------------------------------------------------------------
Write-Host '>> Catalog'
$catId = (Call POST '/api/v1/catalog/categories' @{ name = 'Hogar'; description = 'Articulos para el hogar' } $admin -Soft).id
if (-not $catId) { $catId = (Call GET '/api/v1/catalog/categories' | Where-Object name -eq 'Hogar' | Select-Object -First 1).id }
$brandId = (Call POST '/api/v1/catalog/brands' @{ name = 'Galactic'; description = 'Marca demo' } $admin -Soft).id
if (-not $brandId) { $brandId = (Call GET '/api/v1/catalog/brands' | Where-Object name -eq 'Galactic' | Select-Object -First 1).id }

$products = @(
  @{ name = 'Taza Galactica';  sku = 'TAZA-001';  price = 320 },
  @{ name = 'Lampara Nebula';  sku = 'LAMP-001';  price = 1050 },
  @{ name = 'Cojin Cometa';    sku = 'COJIN-001'; price = 460 }
)
$variantIds = @()
foreach ($p in $products) {
  $created = Call POST '/api/v1/catalog/products' @{
    name = $p.name; description = "$($p.name) - producto de demostracion"; categoryId = $catId; brandId = $brandId
    variants = @(@{ sku = $p.sku; name = 'Unico'; unitPrice = $p.price; currency = 'HNL' })
  } $admin -Soft
  if ($created) { Call POST "/api/v1/catalog/products/$($created.id)/publish" $null $admin | Out-Null }
  Call POST '/api/v1/inventory/items' @{ sku = $p.sku; initialOnHand = 200; lowStockThreshold = 5 } $admin -Soft | Out-Null
  $slug = ($p.name.ToLower() -replace '[^a-z0-9]+', '-').Trim('-')
  $detail = Call GET "/api/v1/catalog/products/$slug"
  $variantIds += $detail.variants[0].id
}

# --- National shipping: zones by department and pickup stores ---------------------------------------
# Costs are example values: edit them in the admin (Shipping). The 409 for an existing zone is ignored, so this is
# safe to re-run. Together the zones cover all 18 departments of Honduras.
Write-Host '>> Shipping zones'
$zones = @(
  @{ code = 'central';   name = 'Central';           cost = 30;  min = 1; max = 2; cod = $true;  departments = @('HN-FM', 'HN-CM', 'HN-LP', 'HN-EP') },
  @{ code = 'norte';     name = 'Norte';             cost = 100; min = 2; max = 3; cod = $true;  departments = @('HN-CR', 'HN-AT', 'HN-YO', 'HN-SB') },
  @{ code = 'occidente'; name = 'Occidente';         cost = 130; min = 3; max = 4; cod = $false; departments = @('HN-CP', 'HN-OC', 'HN-LE', 'HN-IN') },
  @{ code = 'sur-oriente'; name = 'Sur y oriente';   cost = 130; min = 3; max = 4; cod = $false; departments = @('HN-CH', 'HN-VA', 'HN-OL', 'HN-CL') },
  @{ code = 'islas-mosquitia'; name = 'Islas y Mosquitia'; cost = 250; min = 5; max = 7; cod = $false; departments = @('HN-IB', 'HN-GD') }
)
foreach ($z in $zones) {
  Call POST '/api/v1/shipping/admin/zones' @{
    code = $z.code; name = $z.name; cost = $z.cost; currency = 'HNL'; estimatedDaysMin = $z.min; estimatedDaysMax = $z.max
    cashOnDeliveryCovered = $z.cod; departmentCodes = $z.departments
  } $admin -Soft | Out-Null
}

Write-Host '>> Pickup stores'
if (-not (Call GET '/api/v1/shipping/pickup-stores')) {
  Call POST '/api/v1/shipping/admin/pickup-stores' @{ name = 'Tienda Centro'; line1 = 'Blvd. Morazan, frente a Plaza Central'; city = 'Tegucigalpa'; departmentCode = 'HN-FM'; phone = '2222-0000'; hours = 'Lun-Sab 9:00-18:00' } $admin -Soft | Out-Null
  Call POST '/api/v1/shipping/admin/pickup-stores' @{ name = 'Tienda San Pedro Sula'; line1 = 'Col. Trejo, 3 calle'; city = 'San Pedro Sula'; departmentCode = 'HN-CR'; phone = '2550-0000'; hours = 'Lun-Sab 9:00-18:00' } $admin -Soft | Out-Null
}

# --- Orders in different states ----------------------------------------------------------------
Write-Host '>> Orders'
$address = @{ recipientName = 'Cliente Prueba'; line1 = 'Col. Palmira, Calle 5'; city = 'Tegucigalpa'; postalCode = '11101'; country = 'HN'; departmentCode = 'HN-FM' }
$orderIds = @()
for ($i = 0; $i -lt 4; $i++) {
  Call POST '/api/v1/cart/items' @{ variantId = $variantIds[$i % $variantIds.Count]; quantity = 1 + ($i % 3) } $cust | Out-Null
  $order = Call POST '/api/v1/orders/checkout' @{ shippingAddress = $address } $cust
  $orderIds += $order.orderId
  Call POST '/api/v1/payments' @{ orderId = $order.orderId } $cust -Headers @{ 'Idempotency-Key' = [guid]::NewGuid().ToString() } | Out-Null
  Write-Host "   order $($order.orderNumber) created and paid"
}
# leave #1 Paid, move #2 -> Processing, #3 -> Shipped, #4 -> Paid (so the app has actions to try)
Call POST "/api/v1/orders/admin/$($orderIds[1])/process" $null $admin | Out-Null
Call POST "/api/v1/orders/admin/$($orderIds[2])/process" $null $admin | Out-Null
Call POST "/api/v1/orders/admin/$($orderIds[2])/ship" $null $admin | Out-Null

Write-Host ''
Write-Host 'Done. Test accounts (LOCAL ONLY):'
Write-Host "  admin    : $AdminEmail  /  $AdminPassword"
Write-Host "  customer : $CustomerEmail  /  $CustomerPassword"
