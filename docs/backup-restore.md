# Backup and restore operations

CampusCore stores durable state in two places when deployed with the repository's Docker Compose stack:

1. PostgreSQL (`campuscore-postgres`) for application, identity, audit, and academic data.
2. The attachment volume (`campuscore-uploads`) for validated announcement/document uploads.

A recoverable backup must contain both. The scripts in `scripts/` create a versioned backup directory with a PostgreSQL custom-format dump, an attachment tar archive, SHA-256 checksums, and a manifest.

## Prerequisites

- Docker Engine with Docker Compose v2.
- The CampusCore `postgres` and `api` containers must be running for backup and verification.
- Run commands from the repository root unless `COMPOSE_FILE`/`-ComposeFile` points at another Compose file.
- Keep backup files private. They can contain student records, identity data, audit events, and uploaded documents.

## Create a backup

### Linux/macOS/Git Bash

```sh
./scripts/backup.sh
```

To select another destination:

```sh
./scripts/backup.sh /secure/campuscore-backups
```

### Windows PowerShell

```powershell
./scripts/backup.ps1
```

To select another destination:

```powershell
./scripts/backup.ps1 -BackupRoot 'D:\SecureBackups\CampusCore'
```

A successful backup looks like:

```text
backups/
└── campuscore-YYYYMMDDTHHMMSSZ/
    ├── database.dump
    ├── uploads.tar
    ├── SHA256SUMS
    └── manifest.txt
```

The scripts use temporary files inside the containers and `docker compose cp`; database dumps are not sent through PowerShell text redirection.

## Verify a backup

Verification checks all of the following before a restore is allowed:

- required files exist;
- the manifest format is `campuscore-backup-v1`;
- SHA-256 digests match;
- PostgreSQL can parse the custom-format dump with `pg_restore --list`;
- `tar` can parse the attachment archive.

Linux/macOS/Git Bash:

```sh
./scripts/verify-backup.sh backups/campuscore-YYYYMMDDTHHMMSSZ
```

Windows PowerShell:

```powershell
./scripts/verify-backup.ps1 -BackupDirectory 'backups\campuscore-YYYYMMDDTHHMMSSZ'
```

Verification is necessary but is not a substitute for a periodic restore drill.

## Restore

> Restore is destructive. It replaces the current CampusCore database and attachment contents.

Before restoring:

1. Confirm the backup timestamp and environment.
2. Copy the backup to durable storage before making any additional changes.
3. Confirm that no other CampusCore deployment is connected to the same database.
4. Schedule a maintenance window.
5. Verify the archive with the verification command above.

Linux/macOS/Git Bash:

```sh
./scripts/restore.sh backups/campuscore-YYYYMMDDTHHMMSSZ --confirm
```

Windows PowerShell:

```powershell
./scripts/restore.ps1 \
  -BackupDirectory 'backups\campuscore-YYYYMMDDTHHMMSSZ' \
  -ConfirmRestore
```

The restore flow:

1. verifies the backup before changing live state;
2. stops `web` and `api` to prevent writes;
3. force-drops and recreates the configured PostgreSQL database;
4. restores the custom-format dump with `--exit-on-error`;
5. replaces the upload volume contents from `uploads.tar`;
6. starts `api` and `web` again through Compose readiness dependencies.

If database or attachment restoration fails, the scripts leave `api` and `web` stopped so an operator can inspect the partially restored state instead of silently serving it.

## After a restore

Run these checks before reopening CampusCore to users:

```sh
docker compose ps
docker compose exec -T postgres pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"
curl --fail http://localhost:5080/readyz
curl --fail http://localhost:8081/readyz
```

Then sign in with a non-bootstrap administrator account and verify:

- student search and one student record;
- current academic year, section, and subject data;
- an attendance/marks view;
- one attachment download;
- recent audit entries;
- administrator settings.

A successful technical restore does not guarantee that the selected backup is the correct business recovery point.

## Retention and protection

A reasonable starting policy for a small self-hosted installation is:

- daily backups retained for 14 days;
- weekly backups retained for 8 weeks;
- monthly backups retained for 12 months;
- at least one copy stored off the deployment host;
- periodic offline or immutable copies.

Adjust retention to local policy and legal requirements.

Backup directories are ignored by Git. Do not place real backups in source control, CI artifacts with broad access, public object storage, or chat/email attachments. Encrypt backup storage at rest and protect any off-site transfer in transit. Access to backup files should be at least as restricted as access to the production database.

## Restore drills

Run a restore drill on a non-production environment at a regular cadence (for example monthly or before each production release):

1. create a fresh backup;
2. verify it;
3. restore it into an isolated CampusCore Compose project;
4. wait for `/readyz`;
5. exercise representative records and attachment reads;
6. record recovery time and any operator errors;
7. delete the drill environment securely when finished.

A backup strategy is considered operational only after restoration has been demonstrated.

## Custom Compose files

Shell:

```sh
COMPOSE_FILE=docker-compose.production.yml ./scripts/backup.sh /secure/backups
```

PowerShell:

```powershell
./scripts/backup.ps1 -ComposeFile 'docker-compose.production.yml' -BackupRoot 'D:\SecureBackups\CampusCore'
```

Use the same Compose file for verification and restore so service names, volumes, and environment variables resolve consistently.

## Troubleshooting

### `pg_restore --list` fails

Treat the dump as invalid or incomplete. Do not attempt a destructive restore. Recreate the backup and inspect disk space and Docker logs.

### Checksum mismatch

The backup changed after creation or was corrupted in transit/storage. Do not restore it. Retrieve another verified copy.

### `tar` verification fails

The attachment archive is damaged or not a CampusCore upload archive. Do not restore it.

### Restore stops after the database step

Leave the application services stopped until the issue is understood. If the original state must be recovered, restore the pre-maintenance backup rather than attempting manual partial reconstruction.

### Containers are not running

Start the Compose stack before backup/verification:

```sh
docker compose up -d
```

Wait for the `api` service to become healthy before retrying.
