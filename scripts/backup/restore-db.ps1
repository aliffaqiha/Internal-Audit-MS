# ============================================================================
# restore-db.ps1 - Restore an IAMS database backup (pg_restore)
# ============================================================================
# DANGER: this drops/recreates the target database. Run with care.
# The existing database is dumped first as a safety net unless -SkipBackup is set.
#
# Configuration via environment variables (same as backup-db.ps1):
#   PGHOST / PGPORT / PGDATABASE / PGUSER / PGPASSWORD
#   PG_BIN_DIR        directory containing pg_dump/pg_restore/psql (optional)
#   PG_CONTAINER      docker container name (optional; restores inside container)
#
# Usage:
#   ./scripts/backup/restore-db.ps1 -File .\backups\db\iams_20260815_100000.dump
#   ./scripts/backup/restore-db.ps1 -Latest -SkipBackup
# ============================================================================
[CmdletBinding()]
param(
    [string]$File,
    [switch]$Latest,
    [switch]$SkipBackup,
    [switch]$Yes
)

$ErrorActionPreference = "Stop"

$pgHost = if ($env:PGHOST) { $env:PGHOST } else { "localhost" }
$pgPort = if ($env:PGPORT) { $env:PGPORT } else { "5432" }
$pgDb   = if ($env:PGDATABASE) { $env:PGDATABASE } else { "iams" }
$pgUser = if ($env:PGUSER) { $env:PGUSER } else { "iams" }
$pgPass = $env:PGPASSWORD
$container = $env:PG_CONTAINER

if ($Latest) {
    $repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    $dir = Join-Path $repoRoot "backups\db"
    $File = Get-ChildItem $dir -Filter "iams_*.dump" | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $File) { throw "No backup file specified. Use -File <path> or -Latest." }
if (-not (Test-Path $File)) { throw "Backup file not found: $File" }

if (-not $Yes) {
    $answer = Read-Host "Restore $File into database '$pgDb'? This will DROP existing data. Type 'yes' to continue"
    if ($answer -ne "yes") { Write-Host "Aborted."; exit 1 }
}

# Safety net: back up the current state before overwriting.
if (-not $SkipBackup) {
    $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $pre = Join-Path (Split-Path $File -Parent) "pre_restore_$stamp.dump"
    Write-Host "Creating safety backup: $pre"
    $env:PGPASSWORD = $pgPass
    if ($container) {
        docker exec -e PGPASSWORD=$pgPass $container pg_dump --username $pgUser --dbname $pgDb --format=custom --no-owner --file "/tmp/iams_pre_restore.dump"
        docker cp "${container}:/tmp/iams_pre_restore.dump" $pre
        docker exec $container rm -f /tmp/iams_pre_restore.dump
    } else {
        $pgDump = Get-Command pg_dump -ErrorAction SilentlyContinue
        if (-not $pgDump) { throw "pg_dump not found; cannot create safety backup." }
        & $pgDump.Source --host $pgHost --port $pgPort --username $pgUser --dbname $pgDb --format=custom --no-owner --file $pre
    }
    if ($LASTEXITCODE -ne 0) { throw "Safety backup failed; aborting restore." }
}

# Terminate existing connections, then restore (creates schema as needed).
$env:PGPASSWORD = $pgPass
if ($container) {
    docker exec -e PGPASSWORD=$pgPass $container psql --username $pgUser --dbname postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$pgDb' AND pid <> pg_backend_pid();"
    docker cp $File "${container}:/tmp/iams_restore.dump"
    docker exec -e PGPASSWORD=$pgPass $container pg_restore --username $pgUser --dbname $pgDb --no-owner --clean --if-exists /tmp/iams_restore.dump
    $code = $LASTEXITCODE
    docker exec $container rm -f /tmp/iams_restore.dump
} else {
    $psql = Get-Command psql -ErrorAction SilentlyContinue
    if (-not $psql) { throw "psql not found on PATH." }
    $pgRestore = Get-Command pg_restore -ErrorAction SilentlyContinue
    if (-not $pgRestore) { throw "pg_restore not found on PATH." }
    & $psql.Source --host $pgHost --port $pgPort --username $pgUser --dbname postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$pgDb' AND pid <> pg_backend_pid();"
    & $pgRestore.Source --host $pgHost --port $pgPort --username $pgUser --dbname $pgDb --no-owner --clean --if-exists $File
    $code = $LASTEXITCODE
}

if ($code -ne 0) { throw "Restore failed with exit code $code." }
Write-Host "Restore OK: $File"
