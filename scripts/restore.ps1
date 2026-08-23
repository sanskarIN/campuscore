[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupDirectory,
    [switch]$ConfirmRestore,
    [string]$ComposeFile = ""
)

$ErrorActionPreference = "Stop"

if (-not $ConfirmRestore) {
    throw "Restore is destructive. Re-run with -ConfirmRestore to replace the current database and attachments."
}

if ([string]::IsNullOrWhiteSpace($ComposeFile)) {
    $ComposeFile = $env:COMPOSE_FILE
}
if ([string]::IsNullOrWhiteSpace($ComposeFile)) {
    $ComposeFile = "docker-compose.yml"
}

function Invoke-Compose {
    param([Parameter(Mandatory = $true)][string[]]$ComposeArgs)

    & docker compose -f $ComposeFile @ComposeArgs
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose failed with exit code ${LASTEXITCODE}: $($ComposeArgs -join ' ')"
    }
}

if (-not (Test-Path -LiteralPath $BackupDirectory -PathType Container)) {
    throw "Backup directory does not exist: $BackupDirectory"
}
$BackupDirectory = (Resolve-Path -LiteralPath $BackupDirectory).Path
$uploadsFile = Join-Path $BackupDirectory "uploads.tar"
$databaseTemp = "/tmp/campuscore-restore-$PID.dump"

& (Join-Path $PSScriptRoot "verify-backup.ps1") -BackupDirectory $BackupDirectory -ComposeFile $ComposeFile
if (-not $?) {
    throw "Backup verification failed."
}

Write-Host "Stopping CampusCore application services..."
Invoke-Compose -ComposeArgs @("stop", "web", "api")

try {
    Write-Host "Restoring PostgreSQL database..."
    Invoke-Compose -ComposeArgs @("cp", (Join-Path $BackupDirectory "database.dump"), "postgres:${databaseTemp}")
    Invoke-Compose -ComposeArgs @(
        "exec", "-T", "--user", "root", "postgres", "sh", "-ec",
        'dropdb --if-exists --force --username "$POSTGRES_USER" "$POSTGRES_DB" && createdb --username "$POSTGRES_USER" "$POSTGRES_DB" && pg_restore --exit-on-error --no-owner --no-acl --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" "$1"',
        "sh", $databaseTemp
    )

    Write-Host "Restoring attachment volume..."
    Invoke-Compose -ComposeArgs @(
        "run", "--rm", "--no-deps", "-T", "--user", "root",
        "--volume", "${uploadsFile}:/tmp/campuscore-uploads.tar:ro",
        "--entrypoint", "sh", "api", "-ec",
        'find /data/uploads -mindepth 1 -maxdepth 1 -exec rm -rf {} + && tar -xf /tmp/campuscore-uploads.tar -C /data/uploads && chown -R campuscore:campuscore /data/uploads'
    )
}
catch {
    Write-Warning "Restore failed. API and web services remain stopped for inspection. $($_.Exception.Message)"
    throw
}
finally {
    & docker compose -f $ComposeFile exec -T --user root postgres rm -f $databaseTemp *> $null
}

Write-Host "Starting CampusCore and waiting for readiness dependencies..."
Invoke-Compose -ComposeArgs @("up", "-d", "api", "web")
Write-Host "Restore completed from: $BackupDirectory"
