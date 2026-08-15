# Backup Strategy

IAMS stores two kinds of data that must be protected:

| Data | Where | Backup method |
| --- | --- | --- |
| Relational data (users, audit plans, findings, CAPs, logs, config) | PostgreSQL database `iams` | `pg_dump` (custom format) via `scripts/backup/backup-db.ps1` |
| Uploaded files (finding evidence, CAP attachments, audit reports) | MinIO bucket `iams` | `mc mirror` via `scripts/backup/backup-storage.ps1` |
| Runtime cache (dashboard analytics) | Redis | Optional — cache only, rebuilt on demand; not backed up |

## One-time setup

```powershell
# PostgreSQL: set credentials (match docker-compose.yml)
$env:PGPASSWORD = "iams_dev_password"

# MinIO: set credentials (match docker-compose.yml / your deployment)
$env:MINIO_ACCESS_KEY = "<access-key>"
$env:MINIO_SECRET_KEY = "<secret-key>"

# MinIO client (optional; required for storage backup)
#   winget install minio.mc   (or put mc.exe in scripts/backup/)
```

## Running a backup

```powershell
# Database
.\scripts\backup\backup-db.ps1

# Object storage
.\scripts\backup\backup-storage.ps1

# Both
.\scripts\backup\backup-db.ps1; .\scripts\backup\backup-storage.ps1
```

Output goes to `backups/db/iams_<timestamp>.dump` and `backups/storage/<timestamp>/`.
Old backups older than `RETENTION_DAYS` (default 14) are pruned automatically.

The DB script works either with `pg_dump` on `PATH` or by running inside the
running postgres container (`iams-postgres`), which is auto-detected.

## Scheduling

### Windows (Task Scheduler)

```powershell
$action  = New-ScheduledTaskAction -Execute "powershell.exe" `
  -Argument "-NoProfile -ExecutionPolicy Bypass -File D:\InternalAuditMS\scripts\backup\backup-db.ps1"
$trigger = New-ScheduledTaskTrigger -Daily -At 02:00
Register-ScheduledTask -TaskName "IAMS DB Backup" -Action $action -Trigger $trigger `
  -Description "IAMS nightly database backup" -User "SYSTEM"
```

Create a second task for `backup-storage.ps1` (e.g. 02:30) and keep the storage
backup on a **different disk/volume** than the database backup for resilience.

### Linux / Docker host (cron)

```cron
0 2 * * * cd /opt/iams && /usr/bin/pwsh ./scripts/backup/backup-db.ps1 >> /var/log/iams-backup.log 2>&1
30 2 * * * cd /opt/iams && /usr/bin/pwsh ./scripts/backup/backup-storage.ps1 >> /var/log/iams-storage-backup.log 2>&1
```

## Restore

```powershell
# Preview contents (optional)
pg_restore --list .\backups\db\iams_20260815_100000.dump

# Restore (prompts; creates a safety backup of the current DB first)
.\scripts\backup\restore-db.ps1 -File .\backups\db\iams_20260815_100000.dump
.\scripts\backup\restore-db.ps1 -Latest            # latest dump
.\scripts\backup\restore-db.ps1 -Latest -SkipBackup -Yes   # unattended, no safety copy
```

Object storage is restored by mirroring back:

```powershell
mc alias set iams-restore <endpoint> <access-key> <secret-key> --api S3v4
mc mirror .\backups\storage\<timestamp> iams-restore/iams
```

## Recovery objectives & verification

- **RPO**: up to 24 h if run daily; lower if run more often.
- **RTO**: minutes — restore is a single scripted step.
- **Verify regularly**: at least monthly, do a restore into a scratch database
  (`PGDATABASE=iams_restore_test`) and confirm record counts match the source.
  The DB script already sanity-checks each dump with `pg_restore --list`.

## Disaster recovery (full stack)

1. Recreate infrastructure: `docker compose up -d` (postgres, redis, minio, seq, jaeger).
2. Restore the database (see above) — the app applies migrations on startup.
3. Restore object storage (see above).
4. Start the API; verify `/health` returns 200.

## Notes

- **Secrets**: never commit real `PGPASSWORD` / MinIO keys. Use environment
  variables or a secrets manager (e.g. Windows Credential Manager, a vault).
- **Backup copy**: `backups/` is git-ignored — backups never enter the repo.
- **Encryption**: if the backups directory is on an unencrypted disk, enable
  full-disk encryption or encrypt the dump (e.g. `pg_dump -Fc | gpg --encrypt`).
- The app itself is stateless at runtime except for these two stores, so no
  further in-app backup is required.
