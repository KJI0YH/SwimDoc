#Requires -Version 5.1
<#
.SYNOPSIS
    Publishes SwimDoc and builds the Inno Setup installer.

.EXAMPLE
    .\Installer\Build-Installer.ps1

.EXAMPLE
    .\Installer\Build-Installer.ps1 -SkipPublish

.EXAMPLE
    .\Installer\Build-Installer.ps1 -InnoSetupPath "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
#>
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$Runtime = 'win-x64',

    [switch]$SkipPublish,

    [string]$InnoSetupPath
)

$ErrorActionPreference = 'Stop'

$installerDir = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $installerDir '..')).Path
$uiProject = Join-Path $repoRoot 'UI\UI.csproj'
$versionProps = Join-Path $repoRoot 'UI\Version.props'
$publishDir = Join-Path $repoRoot "Artifacts\Publish\$Runtime"
$issScript = Join-Path $installerDir 'SwimDocSetupScript.iss'
$updateVersionScript = Join-Path $installerDir 'Update-VersionIss.ps1'

function Get-AppVersion {
    param([string]$Path)

    $match = Select-String -Path $Path -Pattern '<Version>([^<]+)</Version>' | Select-Object -First 1
    if (-not $match) {
        throw "Version not found in $Path"
    }

    return $match.Matches[0].Groups[1].Value
}

function Resolve-InnoSetupCompilerPath {
    param([string]$ExplicitPath)

    if ($ExplicitPath) {
        if (-not (Test-Path $ExplicitPath)) {
            throw "Inno Setup compiler was not found at: $ExplicitPath"
        }

        return (Resolve-Path $ExplicitPath).Path
    }

    if ($env:INNO_SETUP_COMPILER -and (Test-Path $env:INNO_SETUP_COMPILER)) {
        return (Resolve-Path $env:INNO_SETUP_COMPILER).Path
    }

    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 5\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 5\ISCC.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return (Resolve-Path $candidate).Path
        }
    }

    $registryRoots = @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*'
    )

    foreach ($registryRoot in $registryRoots) {
        $installLocations = Get-ItemProperty $registryRoot -ErrorAction SilentlyContinue |
            Where-Object { $_.DisplayName -like 'Inno Setup*' } |
            ForEach-Object { $_.InstallLocation, (Split-Path $_.DisplayIcon -Parent) } |
            Where-Object { $_ } |
            Select-Object -Unique

        foreach ($location in $installLocations) {
            $candidate = Join-Path $location 'ISCC.exe'
            if (Test-Path $candidate) {
                return (Resolve-Path $candidate).Path
            }
        }
    }

    $pathCandidate = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($pathCandidate) {
        return $pathCandidate.Source
    }

    throw @"
Inno Setup compiler (ISCC.exe) was not found.

Install Inno Setup 6:
  https://jrsoftware.org/isdl.php

After installation, run:
  .\Installer\Build-Installer.ps1 -SkipPublish

If ISCC.exe is installed in a custom location, pass it explicitly:
  .\Installer\Build-Installer.ps1 -SkipPublish -InnoSetupPath "C:\Path\To\Inno Setup 6\ISCC.exe"

Or set an environment variable:
  setx INNO_SETUP_COMPILER "C:\Path\To\Inno Setup 6\ISCC.exe"
"@
}

$appVersion = Get-AppVersion -Path $versionProps
$setupExe = Join-Path $installerDir "SwimDocSetup-$appVersion.exe"
Write-Host "SwimDoc version (Version.props): $appVersion"

& $updateVersionScript -VersionPropsPath $versionProps

if (-not $SkipPublish) {
    Write-Host "Publishing $uiProject -> $publishDir"
    dotnet publish $uiProject -c $Configuration -r $Runtime --self-contained true -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet publish failed.'
    }
}

$publishedExe = Join-Path $publishDir 'SwimDoc.exe'
if (-not (Test-Path $publishedExe)) {
    throw "Published application was not found: $publishedExe"
}

$iscc = Resolve-InnoSetupCompilerPath -ExplicitPath $InnoSetupPath
Write-Host "Building installer with $iscc"

& $iscc "/DRepoRoot=$repoRoot" $issScript
if ($LASTEXITCODE -ne 0) {
    throw 'Inno Setup compilation failed.'
}

if (-not (Test-Path $setupExe)) {
    throw "Installer was not created: $setupExe"
}

Write-Host "Installer created: $setupExe"
