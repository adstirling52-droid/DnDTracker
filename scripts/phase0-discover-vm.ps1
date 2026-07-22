# Phase 0 — IIS / VM discovery script
# Run on the Azure VM in an elevated PowerShell session.
# Output is written to a timestamped folder under .\phase0-output\
#
# Usage:
#   .\scripts\phase0-discover-vm.ps1
#   .\scripts\phase0-discover-vm.ps1 -OutputRoot "D:\phase0-output"

param(
    [string]$OutputRoot = ".\phase0-output"
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outDir = Join-Path $OutputRoot $timestamp
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

function Write-Section($title) {
    Write-Host ""
    Write-Host "=== $title ===" -ForegroundColor Cyan
}

Write-Host "Phase 0 discovery — output: $outDir"

# --- System ---
Write-Section "System"
$system = [ordered]@{
    ComputerName   = $env:COMPUTERNAME
    OsCaption      = (Get-CimInstance Win32_OperatingSystem).Caption
    OsVersion      = (Get-CimInstance Win32_OperatingSystem).Version
    TotalMemoryGB  = [math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)
    LogicalCpus    = (Get-CimInstance Win32_ComputerSystem).NumberOfLogicalProcessors
    LastBoot       = (Get-CimInstance Win32_OperatingSystem).LastBootUpTime
}
$system | ConvertTo-Json | Set-Content (Join-Path $outDir "system.json")
$system.GetEnumerator() | ForEach-Object { Write-Host "  $($_.Key): $($_.Value)" }

# --- Disk ---
Write-Section "Disk"
Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" |
    Select-Object DeviceID, @{N='SizeGB';E={[math]::Round($_.Size/1GB,2)}}, @{N='FreeGB';E={[math]::Round($_.FreeSpace/1GB,2)}}, FileSystem |
    Export-Csv (Join-Path $outDir "disks.csv") -NoTypeInformation
Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" |
    Format-Table DeviceID, SizeGB, FreeGB -AutoSize | Out-String | Write-Host

# --- Installed software (selected) ---
Write-Section "Installed software"
$patterns = @(
    "Microsoft .NET*",
    "Microsoft SQL Server*",
    "IIS*",
    "PHP*",
    "URL Rewrite*",
    "ASP.NET*"
)
$software = Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*,
    HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\* -ErrorAction SilentlyContinue |
    Where-Object { $_.DisplayName -and ($patterns | ForEach-Object { $_.DisplayName -like $_ }) -contains $true } |
    Select-Object DisplayName, DisplayVersion, Publisher, InstallDate |
    Sort-Object DisplayName -Unique
$software | Export-Csv (Join-Path $outDir "installed-software.csv") -NoTypeInformation
$software | Format-Table DisplayName, DisplayVersion -AutoSize | Out-String | Write-Host

# --- .NET ---
Write-Section ".NET runtimes"
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    dotnet --list-runtimes | Tee-Object -FilePath (Join-Path $outDir "dotnet-runtimes.txt")
    dotnet --list-sdks | Tee-Object -FilePath (Join-Path $outDir "dotnet-sdks.txt")
} else {
    "dotnet CLI not found on PATH" | Set-Content (Join-Path $outDir "dotnet-runtimes.txt")
}

# --- PHP ---
Write-Section "PHP"
if (Get-Command php -ErrorAction SilentlyContinue) {
    php -v | Tee-Object -FilePath (Join-Path $outDir "php-version.txt")
    php -m | Tee-Object -FilePath (Join-Path $outDir "php-modules.txt")
} else {
    "PHP not found on PATH" | Set-Content (Join-Path $outDir "php-version.txt")
}

# --- SQL Server ---
Write-Section "SQL Server services"
Get-Service -Name "MSSQL*" -ErrorAction SilentlyContinue |
    Select-Object Name, Status, StartType |
    Export-Csv (Join-Path $outDir "sql-services.csv") -NoTypeInformation
Get-Service -Name "MSSQL*" -ErrorAction SilentlyContinue | Format-Table -AutoSize | Out-String | Write-Host

# --- IIS sites, bindings, app pools ---
Write-Section "IIS"
Import-Module WebAdministration -ErrorAction Stop

Get-ChildItem IIS:\Sites | ForEach-Object {
    $site = $_
    [PSCustomObject]@{
        Name         = $site.Name
        Id           = $site.Id
        State        = $site.State
        PhysicalPath = $site.PhysicalPath
        AppPool      = $site.ApplicationPool
    }
} | Export-Csv (Join-Path $outDir "iis-sites.csv") -NoTypeInformation

Get-WebBinding | ForEach-Object {
    [PSCustomObject]@{
        Site         = $_.ItemXPath -replace '.*sites/@\[name=''([^'']+)''\].*', '$1'
        Protocol     = $_.protocol
        BindingInfo  = $_.bindingInformation
        CertificateHash = $_.certificateHash
        CertificateStore = $_.certificateStoreName
    }
} | Export-Csv (Join-Path $outDir "iis-bindings.csv") -NoTypeInformation

Get-ChildItem IIS:\AppPools | ForEach-Object {
    $pool = $_
    $config = Get-Item "IIS:\AppPools\$($pool.Name)"
    [PSCustomObject]@{
        Name           = $pool.Name
        State          = $pool.State
        ManagedRuntime = $config.managedRuntimeVersion
        PipelineMode   = $config.managedPipelineMode
        IdentityType   = $config.processModel.identityType
        Identity       = $config.processModel.userName
    }
} | Export-Csv (Join-Path $outDir "iis-app-pools.csv") -NoTypeInformation

# WebSocket per site
$webSocket = @()
Get-ChildItem IIS:\Sites | ForEach-Object {
    $siteName = $_.Name
    $enabled = (Get-WebConfigurationProperty -PSPath "IIS:\Sites\$siteName" -Filter "system.webServer/webSocket" -Name "enabled").Value
    $webSocket += [PSCustomObject]@{ Site = $siteName; WebSocketsEnabled = $enabled }
}
$webSocket | Export-Csv (Join-Path $outDir "iis-websockets.csv") -NoTypeInformation

# App pool environment variables (names only — no secret values)
$envVars = @()
Get-ChildItem IIS:\AppPools | ForEach-Object {
    $poolName = $_.Name
    $collection = Get-WebConfiguration "system.applicationHost/applicationPools/add[@name='$poolName']/environmentVariables/add" -ErrorAction SilentlyContinue
    if ($collection) {
        foreach ($item in $collection) {
            $envVars += [PSCustomObject]@{
                AppPool = $poolName
                Name    = $item.name
                HasValue = [bool]($item.value)
            }
        }
    }
}
if ($envVars.Count -gt 0) {
    $envVars | Export-Csv (Join-Path $outDir "iis-apppool-env-vars.csv") -NoTypeInformation
}

# --- Certificates (metadata only) ---
Write-Section "TLS certificates (LocalMachine\My)"
Get-ChildItem Cert:\LocalMachine\My | ForEach-Object {
    [PSCustomObject]@{
        Subject    = $_.Subject
        Thumbprint = $_.Thumbprint
        NotBefore  = $_.NotBefore
        NotAfter   = $_.NotAfter
        DnsNames   = ($_.DnsNameList | ForEach-Object { $_.Unicode }) -join "; "
        HasPrivateKey = $_.HasPrivateKey
    }
} | Export-Csv (Join-Path $outDir "certificates.csv") -NoTypeInformation

# --- Site folder inventory (no secrets) ---
Write-Section "DnDTracker site folder"
$dndPaths = @(
    "C:\inetpub\DnDTracker",
    (Get-ChildItem IIS:\Sites | Where-Object { $_.Name -like "*DnD*" -or $_.Name -like "*Tracker*" } | Select-Object -ExpandProperty PhysicalPath -ErrorAction SilentlyContinue)
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -Unique

foreach ($path in $dndPaths) {
    $safeName = ($path -replace '[\\:]', '-')
    $inventory = Get-ChildItem $path -Force -ErrorAction SilentlyContinue |
        Select-Object Name, Length, LastWriteTime, @{N='IsDirectory';E={$_.PSIsContainer}}
    $inventory | Export-Csv (Join-Path $outDir "folder-inventory-$safeName.csv") -NoTypeInformation

    $configFiles = @(
        "web.config",
        "appsettings.Production.json",
        "appsettings.json"
    )
    foreach ($file in $configFiles) {
        $full = Join-Path $path $file
        if (Test-Path $full) {
            $dest = Join-Path $outDir "config-redacted-$safeName-$file.txt"
            # Redact connection strings and passwords before export
            $content = Get-Content $full -Raw
            $content = $content -replace '(Password|pwd|Secret|ConnectionString)\s*[=:]\s*[^;\s"]+', '$1=[REDACTED]'
            $content | Set-Content $dest
        }
    }

    $itemImages = Join-Path $path "Data\item-images"
    if (Test-Path $itemImages) {
        $count = (Get-ChildItem $itemImages -Recurse -File -ErrorAction SilentlyContinue | Measure-Object).Count
        $sizeMb = [math]::Round(((Get-ChildItem $itemImages -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB), 2)
        [PSCustomObject]@{ Path = $itemImages; FileCount = $count; SizeMB = $sizeMb } |
            Export-Csv (Join-Path $outDir "item-images-summary.csv") -NoTypeInformation -Append
    }
}

# --- IIS applicationHost.config backup ---
Write-Section "IIS config export"
$appHost = "$env:windir\system32\inetsrv\config\applicationHost.config"
if (Test-Path $appHost) {
    Copy-Item $appHost (Join-Path $outDir "applicationHost.config")
}

# --- win-acme (if present) ---
Write-Section "win-acme"
$wacsPaths = @("C:\ProgramData\win-acme", "C:\tools\win-acme", "C:\win-acme")
foreach ($p in $wacsPaths) {
    if (Test-Path $p) {
        "Found: $p" | Add-Content (Join-Path $outDir "win-acme-locations.txt")
        Get-ChildItem $p -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Extension -in '.json','.ps1','.log' } |
            Select-Object FullName, Length, LastWriteTime |
            Export-Csv (Join-Path $outDir "win-acme-files.csv") -NoTypeInformation -Append
    }
}

# --- Summary ---
Write-Section "Complete"
Write-Host "Discovery output saved to: $outDir"
Write-Host "Copy this folder off the VM and attach to the Phase 0 baseline report."
Write-Host "Do NOT commit files containing redacted-but-sensitive config to a public repo without review."
