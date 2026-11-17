# nan_T 2026-11-12: KSKJ_WCS MySQL database backup script.
# Usage: powershell -ExecutionPolicy Bypass -File .\tools\backup.ps1
# Output: backup\kskj_wcs_yyyyMMdd_HHmmss.sql

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$backupDir   = Join-Path $projectRoot "backup"
$appSettings = Join-Path $projectRoot "KSKJ_WCS\appsettings.json"

if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir | Out-Null
}

# Read connection info from appsettings.json
$json = Get-Content $appSettings -Raw | ConvertFrom-Json
$cs = $json.ConnectionStrings.MySqlConnection

$server   = ($cs -split ';') | Where-Object { $_ -match '^Server=' }    | ForEach-Object { ($_ -split '=')[1] }
$port     = ($cs -split ';') | Where-Object { $_ -match '^Port=' }      | ForEach-Object { ($_ -split '=')[1] }
$database = ($cs -split ';') | Where-Object { $_ -match '^Database=' }  | ForEach-Object { ($_ -split '=')[1] }
$user     = ($cs -split ';') | Where-Object { $_ -match '^User=' }      | ForEach-Object { ($_ -split '=')[1] }
$password = ($cs -split ';') | Where-Object { $_ -match '^Password=' }  | ForEach-Object { ($_ -split '=')[1] }

if (-not $server)   { $server = "192.168.3.26" }
if (-not $port)     { $port = "3306" }
if (-not $database) { $database = "KSKJ_WCS" }
if (-not $user)     { $user = "wcs" }

$mysqldump = "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe"
if (-not (Test-Path $mysqldump)) {
    Write-Error "mysqldump not found at $mysqldump"
    exit 1
}

$ts   = Get-Date -Format "yyyyMMdd_HHmmss"
$file = Join-Path $backupDir "$database`_$ts.sql"

$env:MYSQL_PWD = $password
& $mysqldump -h $server -P $port -u $user --single-transaction --routines --triggers --events --no-tablespaces $database > $file

if ($LASTEXITCODE -ne 0) {
    Write-Error "mysqldump failed with exit code $LASTEXITCODE"
    exit 1
}

$size = (Get-Item $file).Length
Write-Host "Backup OK: $file ($size bytes)"
