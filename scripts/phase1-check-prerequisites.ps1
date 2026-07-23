# Phase 1 - Check VM readiness for Grav CMS test site
# Run on the Azure VM in elevated PowerShell.
# Does NOT change anything - read-only checks only.
#
# Usage:
#   .\phase1-check-prerequisites.ps1

$ErrorActionPreference = "Continue"

function Write-Check($label, $ok, $detail) {
    $status = if ($ok) { "OK" } else { "ACTION NEEDED" }
    $color = if ($ok) { "Green" } else { "Yellow" }
    Write-Host ("[{0}] {1}" -f $status, $label) -ForegroundColor $color
    if ($detail) { Write-Host "       $detail" }
}

Write-Host ""
Write-Host "=== Phase 1 prerequisites check ===" -ForegroundColor Cyan
Write-Host "Test site target: cms-test.alanstirling.com" -ForegroundColor Cyan
Write-Host "This script only reads information. It does not change your server." -ForegroundColor Cyan
Write-Host ""

# Disk space on C:
$disk = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'"
$freeGb = [math]::Round($disk.FreeSpace / 1GB, 2)
Write-Check "Free disk space on C:" ($freeGb -ge 5) "${freeGb} GB free (recommend at least 5 GB)"

# PHP
$phpCmd = Get-Command php -ErrorAction SilentlyContinue
if ($phpCmd) {
    $phpVersion = (php -r "echo PHP_VERSION;")
    $okVersion = [version]$phpVersion -ge [version]"8.3.11"
    Write-Check "PHP installed" $okVersion "Version $phpVersion at $($phpCmd.Source)"
    if (-not $okVersion) {
        Write-Host "       Grav needs PHP 8.3.11 or higher." -ForegroundColor Yellow
    }
} else {
    Write-Check "PHP installed" $false "PHP not found on PATH - will need to install"
}

# URL Rewrite module
$rewrite = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\IIS Extensions\URL Rewrite" -ErrorAction SilentlyContinue
Write-Check "IIS URL Rewrite module" ($null -ne $rewrite) $(if ($rewrite) { "Installed" } else { "Not detected - required for Grav routing" })

# IIS CGI (needed for PHP)
$cgi = Get-WindowsFeature Web-CGI -ErrorAction SilentlyContinue
if ($cgi) {
    Write-Check "IIS CGI feature" $cgi.Installed "Required for PHP via FastCGI"
} else {
    Write-Check "IIS CGI feature" $false "Could not detect - check Server Manager > Web Server > Application Development > CGI"
}

# Check if cms-test site already exists
Import-Module WebAdministration -ErrorAction SilentlyContinue
$cmsSite = Get-Website -Name "cms-test" -ErrorAction SilentlyContinue
$cmsPath = "C:\inetpub\cms-test"
Write-Check "cms-test IIS site exists" ($null -ne $cmsSite) $(if ($cmsSite) { "Site already present" } else { "Not yet created - expected for Phase 1" })

if (Test-Path $cmsPath) {
    $fileCount = (Get-ChildItem $cmsPath -Recurse -File -ErrorAction SilentlyContinue | Measure-Object).Count
    Write-Check "C:\inetpub\cms-test folder" ($fileCount -gt 0) "$fileCount files present"
} else {
    Write-Check "C:\inetpub\cms-test folder" $false "Folder does not exist yet - will be created in a later step"
}

# Certificate check for cms-test
$cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object {
    $_.NotAfter -gt (Get-Date) -and (
        ($_.DnsNameList | ForEach-Object { $_.Unicode }) -contains "cms-test.alanstirling.com"
    )
} | Select-Object -First 1

Write-Check "TLS certificate for cms-test.alanstirling.com" ($null -ne $cert) $(if ($cert) { "Found, expires $($cert.NotAfter)" } else { "Not found yet - will need win-acme or manual cert" })

# Main site still running
$mainSite = Get-Website | Where-Object { $_.Bindings.Collection.bindingInformation -match "alanstirling" } | Select-Object -First 1
Write-Check "Main site (alanstirling.com) still in IIS" ($null -ne $mainSite) $(if ($mainSite) { "Site: $($mainSite.Name), State: $($mainSite.State)" } else { "Could not find - check IIS manually" })

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Phase 1 builds a TEST site at cms-test.alanstirling.com only."
Write-Host "www.alanstirling.com and DnDTracker are NOT changed in Phase 1."
Write-Host ""
Write-Host "Copy this output when replying in chat so we know what to do next."
