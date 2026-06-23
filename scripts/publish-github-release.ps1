#Requires -Version 5.1
param(
    [string]$Repo = "nlutz97/Vampire-Overhaul",
    [string]$ModuleRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$SaveFilePath = "$env:USERPROFILE\OneDrive\Documents\Mount and Blade II Bannerlord\Game Saves\NUNU.sav"
)

$ErrorActionPreference = "Stop"

function Get-GitHubToken {
    if ($env:GH_TOKEN) { return $env:GH_TOKEN }
    if ($env:GITHUB_TOKEN) { return $env:GITHUB_TOKEN }

    $input = "protocol=https`nhost=github.com`n`n"
    $output = $input | git -C $ModuleRoot credential fill 2>$null
    if ($LASTEXITCODE -ne 0) { return $null }

    foreach ($line in $output -split "`n") {
        if ($line -like "password=*") {
            return $line.Substring("password=".Length)
        }
    }

    return $null
}

function Get-NextReleaseTag {
    param([string]$StatePath)

    $today = Get-Date -Format "yyyy-MM-dd"
    $state = [PSCustomObject]@{ lastDate = ""; lastBuild = 0 }

    if (Test-Path $StatePath) {
        $state = Get-Content $StatePath -Raw | ConvertFrom-Json
    }

    $build = if ($state.lastDate -eq $today) { [int]$state.lastBuild + 1 } else { 1 }
    $tag = "v$today-build-$build"

    [PSCustomObject]@{
        Tag   = $tag
        Date  = $today
        Build = $build
    }
}

function Save-ReleaseBuildState {
    param(
        [string]$StatePath,
        [string]$Date,
        [int]$Build
    )

    [PSCustomObject]@{
        lastDate  = $Date
        lastBuild = $Build
    } | ConvertTo-Json | Set-Content -Path $StatePath -Encoding UTF8
}

function New-ReleaseZip {
    param(
        [string]$ModuleRoot,
        [string]$OutputZip,
        [string]$SaveFilePath
    )

    $required = @(
        (Join-Path $ModuleRoot "SubModule.xml"),
        (Join-Path $ModuleRoot "bin\Win64_Shipping_Client\VampireOverhaul.dll"),
        (Join-Path $ModuleRoot "GUI\Prefabs\Party\PartyTroopTuple.xml")
    )

    foreach ($path in $required) {
        if (-not (Test-Path $path)) {
            throw "Missing required release file: $path"
        }
    }

    if (-not (Test-Path $SaveFilePath)) {
        throw "Missing required save file: $SaveFilePath"
    }

    $stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vo-release-" + [guid]::NewGuid().ToString("N"))
    $packageRoot = Join-Path $stagingRoot "VampireOverhaul"
    $saveRoot = Join-Path $stagingRoot "Game Saves"

    try {
        New-Item -ItemType Directory -Path (Join-Path $packageRoot "bin\Win64_Shipping_Client") -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $packageRoot "GUI\Prefabs\Party") -Force | Out-Null
        New-Item -ItemType Directory -Path $saveRoot -Force | Out-Null

        Copy-Item (Join-Path $ModuleRoot "SubModule.xml") $packageRoot -Force
        Copy-Item (Join-Path $ModuleRoot "bin\Win64_Shipping_Client\VampireOverhaul.dll") (Join-Path $packageRoot "bin\Win64_Shipping_Client") -Force
        Copy-Item (Join-Path $ModuleRoot "GUI\Prefabs\Party\PartyTroopTuple.xml") (Join-Path $packageRoot "GUI\Prefabs\Party") -Force
        Copy-Item $SaveFilePath (Join-Path $saveRoot "NUNU.sav") -Force

        if (Test-Path $OutputZip) {
            Remove-Item $OutputZip -Force
        }

        Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $OutputZip -Force
    }
    finally {
        if (Test-Path $stagingRoot) {
            Remove-Item $stagingRoot -Recurse -Force
        }
    }
}

function Get-ModuleVersion {
    param([string]$SubModulePath)

    [xml]$xml = Get-Content $SubModulePath
    return $xml.Module.Version.value
}

$token = Get-GitHubToken
if (-not $token) {
    throw "GitHub authentication not found. Set GH_TOKEN or authenticate git for https://github.com."
}

$env:GH_TOKEN = $token
$gh = Get-Command gh -ErrorAction SilentlyContinue
if (-not $gh) {
    throw "GitHub CLI (gh) is not installed or not on PATH."
}

$statePath = Join-Path $PSScriptRoot "release-build-state.json"
$releaseInfo = Get-NextReleaseTag -StatePath $statePath
$tag = $releaseInfo.Tag
$zipPath = Join-Path $ModuleRoot "VampireOverhaul.zip"
$moduleVersion = Get-ModuleVersion -SubModulePath (Join-Path $ModuleRoot "SubModule.xml")

Write-Host "Creating release package for $tag (module $moduleVersion)..."
New-ReleaseZip -ModuleRoot $ModuleRoot -OutputZip $zipPath -SaveFilePath $SaveFilePath

$notes = @"
Automated build release for Vampire Overhaul.

- Tag: $tag
- Module version: $moduleVersion
- Built: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

Install:
1. Extract the `VampireOverhaul` folder into your Bannerlord `Modules` folder.
2. Copy `Game Saves/NUNU.sav` into `Documents/Mount and Blade II Bannerlord/Game Saves/`.
"@

Write-Host "Publishing GitHub release $tag to $Repo..."
& gh release create $tag $zipPath `
    --repo $Repo `
    --title $tag `
    --notes $notes

if ($LASTEXITCODE -ne 0) {
    throw "gh release create failed with exit code $LASTEXITCODE"
}

Save-ReleaseBuildState -StatePath $statePath -Date $releaseInfo.Date -Build $releaseInfo.Build
Write-Host "Published $tag successfully."
Write-Host "https://github.com/$Repo/releases/tag/$tag"