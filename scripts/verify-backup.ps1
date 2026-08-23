[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupDirectory,
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
        throw "docker compose failed with exit code $LASTEXITCODE: $($ComposeArgs -join ' ')"
    }
}

if (-not (Test-Path -LiteralPath $BackupDirectory -PathType Container)) {
    throw "Backup directory does not exist: $BackupDirectory"
}
$BackupDirectory = (Resolve-Path -LiteralPath $BackupDirectory).Path

$requiredFiles = @("manifest.txt", "SHA256SUMS", "database.dump", "uploads.tar")
foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $BackupDirectory $file) -PathType Leaf)) {
        throw "Backup is incomplete: missing $file"
    }
}

$manifestLines = Get-Content -LiteralPath (Join-Path $BackupDirectory "manifest.txt")
if ($manifestLines -notcontains "format=campuscore-backup-v1") {
    throw "Unsupported or invalid backup manifest."
}

$expectedHashes = @{}
foreach ($line in Get-Content -LiteralPath (Join-Path $BackupDirectory "SHA256SUMS")) {
    if ($line -match '^([0-9a-fA-F]{64})\s+(.+)$') {
        $expectedHashes[$Matches[2].Trim()] = $Matches[1].ToLowerInvariant()
    }
}

foreach ($file in @("database.dump", "uploads.tar")) {
    if (-not $expectedHashes.ContainsKey($file)) {
        throw "Checksum entry is missing for $file."
    }
    $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $BackupDirectory $file)).Hash.ToLowerInvariant()
    if ($actual -ne $expectedHashes[$file]) {
        throw "Checksum validation failed for $file."
    }
}

$databaseTemp = "/tmp/campuscore-verify-$PID.dump"
$uploadsTemp = "/tmp/campuscore-verify-$PID-uploads.tar"

try {
    Invoke-Compose -ComposeArgs @("cp", (Join-Path $BackupDirectory "database.dump"), "postgres:${databaseTemp}")
    Invoke-Compose -ComposeArgs @("exec", "-T", "postgres", "pg_restore", "--list", $databaseTemp)

    Invoke-Compose -ComposeArgs @("cp", (Join-Path $BackupDirectory "uploads.tar"), "api:${uploadsTemp}")
    Invoke-Compose -ComposeArgs @("exec", "-T", "api", "tar", "-tf", $uploadsTemp)

    Write-Host "Backup verification passed: $BackupDirectory"
}
finally {
    & docker compose -f $ComposeFile exec -T postgres rm -f $databaseTemp *> $null
    & docker compose -f $ComposeFile exec -T api rm -f $uploadsTemp *> $null
}
