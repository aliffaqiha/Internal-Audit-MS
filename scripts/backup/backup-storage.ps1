# ============================================================================
# backup-storage.ps1 - Backs up object storage (MinIO / S3) evidence files
# ============================================================================
# Uses the MinIO Client (mc). Mirrors the evidence bucket to a local directory
# and prunes local mirrors older than the retention window.
#
# Configuration via environment variables (all optional):
#   MINIO_ENDPOINT    e.g. localhost:9000
#   MINIO_ACCESS_KEY  e.g. iams (check docker-compose.yml)
#   MINIO_SECRET_KEY
#   MINIO_BUCKET      default: iams
#   BACKUP_DIR        default: <repo>/backups/storage
#   RETENTION_DAYS    default: 14
#
# Usage:
#   ./scripts/backup/backup-storage.ps1
#   $env:MINIO_SECRET_KEY='...'; ./scripts/backup/backup-storage.ps1
# ============================================================================
[CmdletBinding()]
param(
    [string]$BackupDir = $env:BACKUP_DIR,
    [int]$RetentionDays = 0
)

$ErrorActionPreference = "Stop"

if ($RetentionDays -le 0) {
    $RetentionDays = if ($env:RETENTION_DAYS) { [int]$env:RETENTION_DAYS } else { 14 }
}

$endpoint   = if ($env:MINIO_ENDPOINT) { $env:MINIO_ENDPOINT } else { "localhost:9000" }
$accessKey  = $env:MINIO_ACCESS_KEY
$secretKey  = $env:MINIO_SECRET_KEY
$bucket     = if ($env:MINIO_BUCKET) { $env:MINIO_BUCKET } else { "iams" }
$alias      = "iams-backup"

$mc = Get-Command mc -ErrorAction SilentlyContinue
if (-not $mc) {
    $localMc = Join-Path $PSScriptRoot "mc.exe"
    if (Test-Path $localMc) { $mc = Get-Command $localMc } else { throw "MinIO client 'mc' not found. Install it and put it on PATH (or in scripts/backup/)." }
}

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not $BackupDir) { $BackupDir = Join-Path $repoRoot "backups\storage" }
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$target = Join-Path $BackupDir $stamp

if (-not (Test-Path $BackupDir)) { New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null }

if (-not $accessKey) { throw "MINIO_ACCESS_KEY not set." }
if (-not $secretKey) { throw "MINIO_SECRET_KEY not set." }

& $mc.Source alias set $alias $endpoint $accessKey $secretKey --api S3v4
if ($LASTEXITCODE -ne 0) { throw "mc alias set failed." }

& $mc.Source mirror --overwrite "${alias}/${bucket}" $target
if ($LASTEXITCODE -ne 0) { throw "mc mirror failed." }

# Retention: prune old timestamped mirrors.
$cutoff = (Get-Date).AddDays(-$RetentionDays)
Get-ChildItem $BackupDir -Directory | Where-Object { $_.LastWriteTime -lt $cutoff } | ForEach-Object {
    Remove-Item $_.FullName -Recurse -Force
    Write-Host "Pruned old mirror: $($_.Name)"
}

Write-Host "Storage backup OK: $target"
