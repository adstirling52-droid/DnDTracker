# Phase 0 — Pre-migration backup script
# Run on the Azure VM in an elevated PowerShell session BEFORE any migration work.
#
# Usage:
#   .\scripts\phase0-backup-vm.ps1 -BackupRoot "D:\Backups\phase0"
#
# Creates:
#   - DnDTracker site folder zip (excluding logs if large)
#   - SQL database .bak (if sqlcmd available and instance reachable)
#   - applicationHost.config copy
#   - win-acme folder copy (if found)

param(
    [string]$BackupRoot = "D:\Backups\phase0",
    [string]$SitePath = "C:\inetpub\DnDTracker",
    [string]$SqlInstance = "localhost",
    [string]$DatabaseName = "DnDTracker"
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outDir = Join-Path $BackupRoot $timestamp
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Write-Host "Phase 0 backup — output: $outDir" -ForegroundColor Cyan

# --- Site folder ---
if (Test-Path $SitePath) {
    Write-Host "Zipping site folder: $SitePath"
    $zipPath = Join-Path $outDir "DnDTracker-site.zip"
    Compress-Archive -Path $SitePath -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "  Created: $zipPath"
} else {
    Write-Warning "Site path not found: $SitePath — adjust -SitePath parameter"
}

# --- SQL database ---
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if ($sqlcmd) {
    $bakPath = Join-Path $outDir "$DatabaseName.bak"
    $sqlBakPath = $bakPath -replace '\\', '\\'
    Write-Host "Backing up SQL database: $DatabaseName"
    $query = @"
BACKUP DATABASE [$DatabaseName]
TO DISK = N'$bakPath'
WITH FORMAT, INIT, COMPRESSION, STATS = 10;
"@
    try {
        & sqlcmd -S $SqlInstance -E -Q $query
        Write-Host "  Created: $bakPath"
    } catch {
        Write-Warning "SQL backup failed. Run manually in SSMS if needed: $_"
        "SQL backup failed — run manually in SSMS" | Set-Content (Join-Path $outDir "sql-backup-NOTE.txt")
    }
} else {
    Write-Warning "sqlcmd not found — back up the database manually in SSMS"
    @"
Manual SQL backup required:
1. Open SSMS, connect to $SqlInstance
2. Right-click database '$DatabaseName' → Tasks → Back Up...
3. Save .bak file to: $outDir
"@ | Set-Content (Join-Path $outDir "sql-backup-NOTE.txt")
}

# --- IIS config ---
$appHost = "$env:windir\system32\inetsrv\config\applicationHost.config"
if (Test-Path $appHost) {
    Copy-Item $appHost (Join-Path $outDir "applicationHost.config")
    Write-Host "Copied applicationHost.config"
}

# --- win-acme ---
$wacsPaths = @("C:\ProgramData\win-acme", "C:\tools\win-acme", "C:\win-acme")
foreach ($p in $wacsPaths) {
    if (Test-Path $p) {
        $dest = Join-Path $outDir ("win-acme-" + ($p -replace '[\\:]', '-'))
        Copy-Item $p $dest -Recurse -Force
        Write-Host "Copied win-acme from: $p"
    }
}

# --- Manifest ---
$manifest = [ordered]@{
    Timestamp   = $timestamp
    SitePath    = $SitePath
    Database    = $DatabaseName
    SqlInstance = $SqlInstance
    OutputDir   = $outDir
    Notes       = "Verify zip and .bak open successfully before starting migration Phase 1"
}
$manifest | ConvertTo-Json | Set-Content (Join-Path $outDir "backup-manifest.json")

Write-Host ""
Write-Host "Backup complete: $outDir" -ForegroundColor Green
Write-Host "Copy this folder to safe storage (external drive, Azure Storage, or dev PC)."
