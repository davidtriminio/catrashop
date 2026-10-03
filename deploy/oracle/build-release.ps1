<#
.SYNOPSIS
  Builds a small release bundle for the Oracle VM: framework-dependent linux-x64 API + idempotent SQL migrations.
.DESCRIPTION
  Run on your dev machine (needs the .NET 10 SDK and `dotnet tool install -g dotnet-ef`).
  Output: deploy/oracle/dist/commercehub-release.tar.gz (~15-25 MB). Nothing is built on the server.
.EXAMPLE
  ./build-release.ps1
  ./build-release.ps1 -Upload -Server ubuntu@api.davidtriminio.dev -Key ~/.ssh/oracle.key
#>
param(
  [switch]$Upload,
  [string]$Server,
  [string]$Key
)
$ErrorActionPreference = 'Stop'
$root    = Resolve-Path (Join-Path $PSScriptRoot '../..')
$backend = Join-Path $root 'backend'
$api     = Join-Path $backend 'src/Api/CommerceHub.Api/CommerceHub.Api.csproj'
$dist    = Join-Path $PSScriptRoot 'dist'
$stage   = Join-Path $dist 'stage'

Remove-Item $dist -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force "$stage/app", "$stage/sql", "$stage/server" | Out-Null

Write-Host '>> Publishing API (linux-x64, framework-dependent)'
dotnet publish $api -c Release -r linux-x64 --self-contained false -o "$stage/app" /p:UseAppHost=false /p:DebugType=none /p:DebugSymbols=false
if ($LASTEXITCODE) { throw 'publish failed' }

# The design-time tools need a Release build of the host for the *current* platform.
dotnet build $api -c Release --nologo -v q
if ($LASTEXITCODE) { throw 'build failed' }

Write-Host '>> Generating idempotent SQL migrations'
$modules = 'Identity','Catalog','Inventory','Customers','Cart','Promotions','Orders','Payments','Shipping','Notifications'
$i = 0
foreach ($m in $modules) {
  $i++
  $out = Join-Path $stage ('sql/{0:00}-{1}.sql' -f $i, $m.ToLower())
  dotnet ef migrations script --idempotent --no-build --configuration Release `
    --project "$backend/src/Modules/$m/CommerceHub.Modules.$m/CommerceHub.Modules.$m.csproj" `
    --startup-project $api --context "${m}DbContext" --output $out | Out-Null
  if ($LASTEXITCODE) { throw "migration script failed for $m" }
}

Copy-Item "$PSScriptRoot/server/*" "$stage/server" -Recurse
$tar = Join-Path $dist 'commercehub-release.tar.gz'
tar -czf $tar -C $stage .
Write-Host ('Release ready: {0} ({1:N1} MB)' -f $tar, ((Get-Item $tar).Length / 1MB))

if ($Upload) {
  if (-not $Server) { throw '-Server user@host is required with -Upload' }
  $sshArgs = @(); if ($Key) { $sshArgs = @('-i', $Key) }
  scp @sshArgs $tar "${Server}:/tmp/commercehub-release.tar.gz"
  Write-Host 'Uploaded. On the server run:  sudo /opt/commercehub/update.sh /tmp/commercehub-release.tar.gz'
}
