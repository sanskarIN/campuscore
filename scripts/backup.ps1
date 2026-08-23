[CmdletBinding()]
param(
    [string]$BackupRoot = "backups",
    [string]$ComposeFile = ""
)

$ErrorActionPreference = "Stop"

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

& docker compose version *> $null
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose v2 is required."
}

$timestamp = (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ")
$backupDirectory = Join-Path $BackupRoot "campuscore-$timestamp"
$databaseTemp = "/tmp/campuscore-$timestamp.dump"
$uploadsTemp = "/tmp/campuscore-$timestamp-uploads.tar"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

New-Item -ItemType Directory -Force -Path $backupDirectory | Out-Null
$backupDirectory = (Resolve-Path $backupDirectory).Path

try {
    Write-Host "Creating PostgreSQL backup..."
    Invoke-Compose -ComposeArgs @(
        "exec", "-T", "postgres", "sh", "-ec",
        'pg_dump --format=custom --compress=9 --no-owner --no-acl --username "$POSTGRES_USER" "$POSTGRES_DB" > "$1"',
        "sh", $databaseTemp
    )
    Invoke-Compose -ComposeArgs @("cp", "postgres:${databaseTemp}", (Join-Path $backupDirectory "database.dump"))

    Write-Host "Creating attachment backup..."
    Invoke-Compose -ComposeArgs @(
        "exec", "-T", "api", "sh", "-ec",
        'tar -C /data/uploads -cf "$1" .',
        "sh", $uploadsTemp
    )
    Invoke-Compose -ComposeArgs @("cp", "api:${uploadsTemp}", (Join-Path $backupDirectory "uploads.tar"))

    $databaseHash = (Get-FileHash -Algorithm SHA256 (Join-Path $backupDirectory "database.dump")).Hash.ToLowerInvariant()
    $uploadsHash = (Get-FileHash -Algorithm SHA256 (Join-Path $backupDirectory "uploads.tar")).Hash.ToLowerInvariant()
    $checksums = "$databaseHash  database.dump`n$uploadsHash  uploads.tar`n"
    [System.IO.File]::WriteAllText((Join-Path $backupDirectory "SHA256SUMS"), $checksums, $utf8NoBom)

    $manifest = "format=campuscore-backup-v1`ncreated_at_utc=$timestamp`ndatabase_file=database.dump`nuploads_file=uploads.tar`nchecksums_file=SHA256SUMS`n"
    [System.IO.File]::WriteAllText((Join-Path $backupDirectory "manifest.txt"), $manifest, $utf8NoBom)

    Write-Host "Backup created: $backupDirectory"
}
finally {
    & docker compose -f $ComposeFile exec -T postgres rm -f $databaseTemp *> $null
    & docker compose -f $ComposeFile exec -T api rm -f $uploadsTemp *> $null
}
