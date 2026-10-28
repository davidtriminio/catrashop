<#
.SYNOPSIS
  Applies every module's EF Core migrations to the local PostgreSQL (creates the database if missing).
.DESCRIPTION
  Connection string = -ConnectionString, else $env:ConnectionStrings__Postgres, else the `ConnectionStrings:Postgres`
  user-secret of the API project. Safe to re-run (migrations are idempotent).
.EXAMPLE
  ./scripts/local/setup-db.ps1
#>
param([string]$ConnectionString)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"

$conn = Get-CommerceHubConnectionString $ConnectionString
$pg   = ConvertFrom-NpgsqlConnectionString $conn
Write-Host ">> Database: $($pg.Database) on $($pg.Host):$($pg.Port) as $($pg.Username)"

# Create the database if it does not exist (connects to the maintenance DB 'postgres').
$env:PGPASSWORD = $pg.Password
$exists = & (Get-Psql) -h $pg.Host -p $pg.Port -U $pg.Username -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$($pg.Database)'"
if ($LASTEXITCODE) { throw 'Cannot connect to PostgreSQL. Is it running and are the credentials right?' }
if (-not $exists) {
  Write-Host ">> Creating database $($pg.Database)"
  & (Get-Psql) -h $pg.Host -p $pg.Port -U $pg.Username -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE `"$($pg.Database)`""
  if ($LASTEXITCODE) { throw 'CREATE DATABASE failed' }
}

$root    = Resolve-Path (Join-Path $PSScriptRoot '../..')
$backend = Join-Path $root 'backend'
$api     = Join-Path $backend 'src/Api/CommerceHub.Api/CommerceHub.Api.csproj'
$env:COMMERCEHUB_CONNECTION = $conn

# Build once in Release and reuse it: a running API (Debug) can lock the Debug output and break `dotnet ef`'s own build.
Write-Host '>> Building (Release)'
dotnet build $api -c Release --nologo -v q | Out-Null
if ($LASTEXITCODE) { throw 'Build failed' }

foreach ($m in 'Identity','Catalog','Inventory','Customers','Cart','Promotions','Orders','Payments','Shipping','Notifications') {
  Write-Host ">> Migrating $m"
  dotnet ef database update --no-build --configuration Release `
    --project "$backend/src/Modules/$m/CommerceHub.Modules.$m/CommerceHub.Modules.$m.csproj" `
    --startup-project $api --context "${m}DbContext" | Out-Null
  if ($LASTEXITCODE) { throw "Migration failed for $m" }
}
Write-Host 'All migrations applied.'
