# ============================================================================
# backup-db.ps1 - PostgreSQL logical backup (pg_dump) for the IAMS database
# ============================================================================
# Produces a timestamped, compressed dump of the database and prunes old
# backups based on retention. Works either with a local pg_dump binary or
# inside the docker-compose postgres container.
#
# Configuration via environment variables (all optional):
#   PGHOST / PGPORT / PGDATABASE / PGUSER / PGPASSWORD
#   BACKUP_DIR        default: <repo>/backups/db
#   RETENTION_DAYS    default: 14
#   PG_CONTAINER      docker container name to run pg_dump inside.
#                     Auto-detected from `docker ps` if not set.
#   PG_BIN_DIR        directory containing pg_dump/pg_restore (optional).
#
# Usage:
#   ./scripts/backup/backup-db.ps1
#   $env:PGPASSWORD='...'; ./scripts/backup/backup-db.ps1
#   ./scripts/backup/backup-db.ps1 -BackupDir C:\Backups
# ============================================================================
[CmdletBinding()]
param(
    [string]$BackupDir = $env:BACKUP_DIR,
    [int]$RetentionDays = 0,
    [string]$Container = $env:PG_CONTAINER
)

$ErrorActionPreference = "Stop"

if ($RetentionDays -le 0) {
    $RetentionDays = if ($env:RETENTION_DAYS) { [int]$env:RETENTION_DAYS } else { 14 }
}

$pgHost = if ($env:PGHOST) { $env:PGHOST } else { "localhost" }
$pgPort = if ($env:PGPORT) { $env:PGPORT } else { "5432" }
$pgDb   = if ($env:PGDATABASE) { $env:PGDATABASE } else { "iams" }
$pgUser = if ($env:PGUSER) { $env:PGUSER } else { "iams" }
$pgPass = $env:PGPASSWORD

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not $BackupDir) { $BackupDir = Join-Path $repoRoot "backups\db" }
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$outFile = Join-Path $BackupDir "iams_$stamp.dump"

if (-not (Test-Path $BackupDir)) { New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null }

function Invoke-LocalPgDump {
    param([string]$Exe)
    if (-not (Test-Path $Exe)) { throw "pg_dump not found at $Exe" }
    if ($pgPass) { $env:PGPASSWORD = $pgPass }
    & $Exe --host $pgHost --port $pgPort --username $pgUser --dbname $pgDb --format=custom --no-owner --file $outFile
    if ($LASTEXITCODE -ne 0) { throw "pg_dump failed with exit code $LASTEXITCODE" }
}

if ($Container) {
    # Explicit container mode.
    docker exec -e PGPASSWORD=$pgPass $Container pg_dump --username $pgUser --dbname $pgDb --format=custom --no-owner --file "/tmp/iams_dump.dump"
    if ($LASTEXITCODE -ne 0) { throw "pg_dump (container) failed with exit code $LASTEXITCODE" }
    docker cp "${Container}:/tmp/iams_dump.dump" $outFile
    if ($LASTEXITCODE -ne 0) { throw "docker cp failed with exit code $LASTEXITCODE" }
    docker exec $Container rm -f /tmp/iams_dump.dump
}
else {
    $running = docker ps --format "{{.Names}}" 2>$null | Where-Object { $_ -like "*postgres*" } | Select-Object -First 1
    if ($running) {
        # Prefer the postgres container when no local pg_dump is available.
        $localPg = Get-Command pg_dump -ErrorAction SilentlyContinue
        if ($localPg) {
            Invoke-LocalPgDump -Exe $localPg.Source
        } else {
            Write-Host "Using postgres container '$running' for backup."
            docker exec -e PGPASSWORD=$pgPass $running pg_dump --username $pgUser --dbname $pgDb --format=custom --no-owner --file "/tmp/iams_dump.dump"
            if ($LASTEXITCODE -ne 0) { throw "pg_dump (container) failed with exit code $LASTEXITCODE" }
            docker cp "${running}:/tmp/iams_dump.dump" $outFile
            if ($LASTEXITCODE -ne 0) { throw "docker cp failed with exit code $LASTEXITCODE" }
            docker exec $running rm -f /tmp/iams_dump.dump
        }
    } else {
        $localPg = Get-Command pg_dump -ErrorAction SilentlyContinue
        if (-not $localPg) { throw "No running postgres container and pg_dump is not on PATH." }
        Invoke-LocalPgDump -Exe $localPg.Source
    }
}

# Sanity check: the dump file must be readable by pg_restore.
$pgRestore = Get-Command pg_restore -ErrorAction SilentlyContinue
if ($pgRestore) {
    & $pgRestore.Source --list $outFile | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Backup verification (pg_restore --list) failed: file may be corrupt." }
}

# Retention: remove backups older than $RetentionDays days.
$cutoff = (Get-Date).AddDays(-$RetentionDays)
Get-ChildItem $BackupDir -Filter "iams_*.dump" | Where-Object { $_.LastWriteTime -lt $cutoff } | ForEach-Object {
    Remove-Item $_.FullName -Force
    Write-Host "Pruned old backup: $($_.Name)"
}

$size = [math]::Round((Get-Item $outFile).Length / 1MB, 2)
Write-Host "Backup OK: $outFile ($size MB)"
