<#
.SYNOPSIS
  One-off conversion of an existing LOCAL database to the store currency (HNL): relabels every stored amount and,
  optionally, multiplies the amounts by an exchange rate.
.DESCRIPTION
  The app shows each price with the currency it was stored with, so rows created before the store moved to HNL
  (PR-040) keep showing their old currency. This script updates, in ONE transaction:
    catalog.product_variants, cart.carts/cart_items, orders.orders/order_items, payments.payments,
    promotions.coupons/coupon_redemptions, shipping.shipping_methods/shipments.
  Only rows whose currency equals -From are touched, so re-running it after a successful run does nothing. Amounts
  become round(amount * Rate, 2); with the default Rate of 1 the currency is only relabelled.
  Percentage coupons carry no currency, so their minimum subtotal and cap are multiplied too; because of that the
  script stops if no variant is left in -From (it would otherwise multiply them a second time).
  Amounts already copied into outbox messages and notification texts are history and are NOT rewritten.
  Use -DryRun first: it runs everything inside a transaction and rolls it back.
  Back up the database before running it for real:  pg_dump -h localhost -U postgres -Fc commercehub > backup.dump
.EXAMPLE
  ./scripts/local/convert-currency.ps1 -DryRun
  ./scripts/local/convert-currency.ps1 -Rate 25.5
  ./scripts/local/convert-currency.ps1 -From USD -Rate 25.5 -Yes -ConnectionString 'Host=localhost;Port=5432;Database=commercehub;Username=postgres;Password=...'
#>
param(
  [string]$From = 'USD',
  [string]$To = 'HNL',
  [decimal]$Rate = 1,
  [switch]$DryRun,
  [switch]$Yes,
  [string]$ConnectionString
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"

if ($Rate -le 0) { throw 'Rate must be greater than zero.' }
if ($From -notmatch '^[A-Za-z]{3}$' -or $To -notmatch '^[A-Za-z]{3}$') { throw 'Currencies must be 3-letter ISO codes.' }
$From = $From.ToUpperInvariant(); $To = $To.ToUpperInvariant()
if ($From -eq $To) { throw 'From and To are the same currency; nothing to do.' }
$rateText = $Rate.ToString([System.Globalization.CultureInfo]::InvariantCulture)

$pg = ConvertFrom-NpgsqlConnectionString (Get-CommerceHubConnectionString $ConnectionString)
$env:PGPASSWORD = $pg.Password
$psql = Get-Psql
function Run([string]$Sql) {
  $out = & $psql -h $pg.Host -p $pg.Port -U $pg.Username -d $pg.Database -v ON_ERROR_STOP=1 -X -q -tA -c $Sql
  if ($LASTEXITCODE) { throw 'psql failed.' }
  return $out
}

Write-Host ">> Database: $($pg.Database) on $($pg.Host):$($pg.Port) as $($pg.Username)"
Write-Host ">> Converting $From -> $To at rate $rateText$(if ($DryRun) { ' (DRY RUN: nothing will be saved)' })"

$variants = [int](Run "SELECT count(*) FROM catalog.product_variants WHERE upper(currency) = '$From'")
if ($variants -eq 0) {
  Write-Host "No product variants are priced in ${From}: nothing to convert (already done, or the data is already in $To)."
  exit 0
}
Write-Host "   $variants product variant(s) priced in $From"

if (-not $DryRun -and -not $Yes) {
  $answer = Read-Host "This rewrites the database above. Do you have a backup and want to continue? (yes/no)"
  if ($answer -ne 'yes') { Write-Host 'Cancelled.'; exit 1 }
}

$end = if ($DryRun) { 'ROLLBACK;' } else { 'COMMIT;' }
$sql = @"
BEGIN;
\echo order_items
UPDATE orders.order_items i SET unit_price = round(i.unit_price * $rateText, 2)
  FROM orders.orders o WHERE o.id = i.order_id AND upper(o.currency) = '$From';
\echo orders
UPDATE orders.orders SET discount_total = round(discount_total * $rateText, 2), tax_total = round(tax_total * $rateText, 2), currency = '$To'
  WHERE upper(currency) = '$From';
\echo payments
UPDATE payments.payments SET amount = round(amount * $rateText, 2), currency = '$To' WHERE upper(currency) = '$From';
\echo cart_items
UPDATE cart.cart_items SET unit_price = round(unit_price * $rateText, 2), currency = '$To' WHERE upper(currency) = '$From';
\echo carts
UPDATE cart.carts SET currency = '$To' WHERE upper(currency) = '$From';
\echo product_variants
UPDATE catalog.product_variants SET price = round(price * $rateText, 2), currency = '$To' WHERE upper(currency) = '$From';
\echo coupon_redemptions
UPDATE promotions.coupon_redemptions SET discount_amount = round(discount_amount * $rateText, 2), currency = '$To' WHERE upper(currency) = '$From';
\echo coupons (fixed amount)
UPDATE promotions.coupons SET discount_value = round(discount_value * $rateText, 2) WHERE discount_type = 'FixedAmount' AND upper(currency) = '$From';
\echo coupons (minimum subtotal and cap, every coupon in $From or without currency)
UPDATE promotions.coupons SET minimum_subtotal = round(minimum_subtotal * $rateText, 2), max_discount_amount = round(max_discount_amount * $rateText, 2)
  WHERE currency IS NULL OR upper(currency) = '$From';
UPDATE promotions.coupons SET currency = '$To' WHERE upper(currency) = '$From';
\echo shipments
UPDATE shipping.shipments SET cost = round(cost * $rateText, 2), currency = '$To' WHERE upper(currency) = '$From';
\echo shipping_methods
UPDATE shipping.shipping_methods SET cost = round(cost * $rateText, 2), currency = '$To' WHERE upper(currency) = '$From';
\echo
\echo Rows per currency after the conversion:
SELECT 'catalog.product_variants', currency, count(*) FROM catalog.product_variants GROUP BY currency
UNION ALL SELECT 'orders.orders', currency, count(*) FROM orders.orders GROUP BY currency
UNION ALL SELECT 'payments.payments', currency, count(*) FROM payments.payments GROUP BY currency
UNION ALL SELECT 'cart.cart_items', currency, count(*) FROM cart.cart_items GROUP BY currency
UNION ALL SELECT 'shipping.shipping_methods', currency, count(*) FROM shipping.shipping_methods GROUP BY currency
ORDER BY 1, 2;
$end
"@

$file = Join-Path ([System.IO.Path]::GetTempPath()) "convert-currency-$([guid]::NewGuid().ToString('N')).sql"
try {
  Set-Content -Path $file -Value $sql -Encoding utf8
  & $psql -h $pg.Host -p $pg.Port -U $pg.Username -d $pg.Database -v ON_ERROR_STOP=1 -X -f $file
  if ($LASTEXITCODE) { throw 'The conversion failed and was rolled back; no data was changed.' }
} finally {
  Remove-Item -LiteralPath $file -ErrorAction SilentlyContinue
}
Write-Host ''
Write-Host $(if ($DryRun) { 'Dry run finished: the transaction was rolled back, nothing changed.' } else { "Done. Reload the apps: prices now show in $To." })
