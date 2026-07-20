param(
    [string]$OutputPath = ".\publish"
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "..\DnDTracker.Web\DnDTracker.Web.csproj"
$resolvedOutput = Resolve-Path -Path (Split-Path $OutputPath -Parent) -ErrorAction SilentlyContinue
if ($null -eq $resolvedOutput) {
    $resolvedOutput = (Get-Location).Path
}

$outputFullPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path (Get-Location).Path $OutputPath
}

Write-Host "Publishing DnDTracker.Web for IIS..."
Write-Host "Project: $projectPath"
Write-Host "Output:  $outputFullPath"

dotnet publish $projectPath `
    -c Release `
    -o $outputFullPath `
    --self-contained false

Write-Host ""
Write-Host "Publish complete."
Write-Host "Copy the output folder to your server (for example C:\inetpub\DnDTracker)."
Write-Host "Configure appsettings.Production.json or the IIS connection string on the server before starting the site."
