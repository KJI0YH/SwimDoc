#Requires -Version 5.1
<#
.SYNOPSIS
    Generates Installer/Version.iss from UI/Version.props for Inno Setup.

.EXAMPLE
    .\Installer\Update-VersionIss.ps1 -VersionPropsPath .\UI\Version.props
#>
param(
    [Parameter(Mandatory)]
    [string]$VersionPropsPath
)

$ErrorActionPreference = 'Stop'

$versionPropsFullPath = (Resolve-Path $VersionPropsPath).Path
$match = Select-String -Path $versionPropsFullPath -Pattern '<Version>([^<]+)</Version>' | Select-Object -First 1
if (-not $match) {
    throw "Version not found in $versionPropsFullPath"
}

$version = $match.Matches[0].Groups[1].Value
$outputPath = Join-Path $PSScriptRoot 'Version.iss'
$content = @"
; Generated from Version.props. Do not edit manually.
#define MyAppVersion "$version"
"@

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($outputPath, $content, $utf8NoBom)

Write-Host "Wrote $outputPath (MyAppVersion=$version)"
