function Get-CommerceHubConnectionString([string]$Override) {
  if ($Override) { return $Override }
  if ($env:ConnectionStrings__Postgres) { return $env:ConnectionStrings__Postgres }
  $apiDir = Join-Path $PSScriptRoot '../../backend/src/Api/CommerceHub.Api'
  $line = dotnet user-secrets list --project $apiDir 2>$null | Where-Object { $_ -like 'ConnectionStrings:Postgres =*' }
  if (-not $line) {
    throw "No connection string. Run: dotnet user-secrets set ""ConnectionStrings:Postgres"" ""Host=localhost;Port=5432;Database=commercehub;Username=postgres;Password=<pwd>"" --project backend/src/Api/CommerceHub.Api"
  }
  return ($line -replace '^ConnectionStrings:Postgres =\s*', '')
}

function ConvertFrom-NpgsqlConnectionString([string]$Conn) {
  $map = @{}
  foreach ($part in $Conn -split ';') {
    if ($part -match '^\s*([^=]+?)\s*=\s*(.*)$') { $map[$Matches[1].ToLower()] = $Matches[2] }
  }
  [pscustomobject]@{
    Host     = $(if ($map['host']) { $map['host'] } else { 'localhost' })
    Port     = $(if ($map['port']) { $map['port'] } else { '5432' })
    Database = $map['database']
    Username = $(if ($map['username']) { $map['username'] } else { $map['user id'] })
    Password = $map['password']
  }
}

function Get-Psql {
  $cmd = Get-Command psql -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }
  $found = Get-ChildItem 'C:\Program Files\PostgreSQL\*\bin\psql.exe' -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
  if ($found) { return $found.FullName }
  throw 'psql not found. Add PostgreSQL\bin to PATH.'
}
