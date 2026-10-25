# Works with Windows PowerShell 5.1. Version and names come from Desktop.csproj.
[CmdletBinding()]
param([string]$OutputRoot, [switch]$CreateZip)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$project = Join-Path $PSScriptRoot 'Desktop/Desktop.csproj'
[xml]$projectXml = Get-Content -LiteralPath $project -Raw
function Get-ProjectProperty([string]$Name) {
    $nodes = @($projectXml.SelectNodes("/Project/PropertyGroup/$Name"))
    if ($nodes.Count -ne 1 -or [string]::IsNullOrWhiteSpace($nodes[0].InnerText)) {
        throw "Desktop.csproj must define exactly one $Name value."
    }
    return $nodes[0].InnerText.Trim()
}
$version = Get-ProjectProperty 'InformationalVersion'
$projectVersion = Get-ProjectProperty 'Version'
$fileVersion = Get-ProjectProperty 'FileVersion'
$assemblyVersion = Get-ProjectProperty 'AssemblyVersion'
if ($version -notmatch '^\d+\.\d+(?:\.\d+)?$' -or $fileVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw 'Project release versions must use numeric major.minor[.patch] and a four-part FileVersion.'
}
# A two-part version such as 1.17 has Build/Revision == -1 in
# System.Version. Normalize omitted zero parts before comparing metadata.
function Normalize-ReleaseVersion([string]$Value) {
    if ($Value -notmatch '^\d+\.\d+(?:\.\d+){0,2}$') {
        throw 'Release versions must have two to four numeric parts.'
    }
    $parsed = [version]$Value
    return '{0}.{1}.{2}.{3}' -f $parsed.Major, $parsed.Minor, [Math]::Max(0, $parsed.Build), [Math]::Max(0, $parsed.Revision)
}
$expectedVersion = Normalize-ReleaseVersion $version
if ((Normalize-ReleaseVersion $projectVersion) -ne $expectedVersion -or
    $fileVersion -ne $expectedVersion -or $assemblyVersion -ne $expectedVersion) {
    throw 'Version, InformationalVersion, AssemblyVersion and FileVersion do not match. Update Desktop.csproj before publishing.'
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path (Join-Path (Split-Path $PSScriptRoot -Parent) 'UnattendLocal-release') "v$version"
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$variants = @(
    @{ Name = 'SelfContained'; SelfContained = 'true' },
    @{ Name = 'FrameworkDependent'; SelfContained = 'false' }
)
# Keep editable translations beside the executable and inside each ZIP.
$languagesDestination = Join-Path $OutputRoot 'Languages'
if (Test-Path -LiteralPath $languagesDestination) {
    throw "Language directory already exists: $languagesDestination. Choose another OutputRoot to preserve edited translations."
}
# Check all destinations before building; never silently overwrite a release.
foreach ($variant in $variants) {
    $name = "UnattendLocal_V${version}_x64_$($variant.Name)"
    $extensions = @('.exe')
    if ($CreateZip) { $extensions += '.zip' }
    foreach ($extension in $extensions) {
        $destination = Join-Path $OutputRoot ($name + $extension)
        if (Test-Path -LiteralPath $destination) {
            throw "Release file already exists: $destination. Choose another OutputRoot or move the existing file."
        }
    }
}
# Check the shared startup status before publishing. This also detects a stale
# literal UI version even when the EXE's native version resources are correct.
$testsProject = Join-Path $PSScriptRoot 'Tests/Tests.csproj'
if (-not (Test-Path -LiteralPath $testsProject -PathType Leaf)) {
    throw 'Version display check requires the complete source package, including Tests/Tests.csproj.'
}
& dotnet build $testsProject -c Release -m:1 -nr:false
if ($LASTEXITCODE -ne 0) { throw 'Building the version display check failed. No release files were copied.' }
$versionOutput = @(& dotnet (Join-Path $PSScriptRoot 'Tests/bin/Release/net10.0/Tests.dll') --version-display-only)
$versionCheckExit = $LASTEXITCODE
foreach ($line in $versionOutput) { Write-Host $line }
if ($versionCheckExit -ne 0 -or $versionOutput -notcontains 'Version display check passed.') {
    throw 'Version display check failed or is missing. Use the complete updated source package. No release files were copied.'
}
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$staging = Join-Path $OutputRoot ('.publish-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging | Out-Null
$verified = @()
try {
    Write-Host "Publishing UnattendLocal V$version (FileVersion $fileVersion)"
    Write-Host "Project: $project"
    Write-Host "Output:  $OutputRoot"
    foreach ($variant in $variants) {
        $output = Join-Path $staging $variant.Name
        # Keep intermediate outputs separate when switching runtime modes.
        $artifacts = Join-Path $staging ('artifacts-' + $variant.Name)
        & dotnet publish $project -c Release -m:1 -nr:false --artifacts-path $artifacts -r win-x64 --self-contained $variant.SelfContained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $output
        if ($LASTEXITCODE -ne 0) { throw "Publishing failed: $($variant.Name)" }
        $publishedExe = Join-Path $output 'UnattendLocal.exe'
        if (-not (Test-Path -LiteralPath $publishedExe -PathType Leaf)) { throw "Published EXE not found: $publishedExe" }
        $info = (Get-Item -LiteralPath $publishedExe).VersionInfo
        $actualFileVersion = '{0}.{1}.{2}.{3}' -f $info.FileMajorPart, $info.FileMinorPart, $info.FileBuildPart, $info.FilePrivatePart
        $actualProductVersion = ([string]$info.ProductVersion).Split('+')[0]
        if ($actualFileVersion -ne $fileVersion -or $actualProductVersion -ne $version) {
            throw "EXE version mismatch: $($variant.Name), FileVersion=$actualFileVersion, ProductVersion=$actualProductVersion, expected $fileVersion / $version. No release files were copied."
        }
        $languageSource = Join-Path $output 'Languages'
        $sourceLanguages = Join-Path $PSScriptRoot 'Localization/Resources'
        foreach ($resource in @(Get-ChildItem -LiteralPath $sourceLanguages -Filter '*.json' -File | Where-Object { $_.Name -notin @('catalog.json', 'translation-state.json') })) {
            $languageFile = $resource.Name
            $publishedLanguage = Join-Path $languageSource $languageFile
            $sourceLanguage = Join-Path (Join-Path $PSScriptRoot 'Localization/Resources') $languageFile
            if (-not (Test-Path -LiteralPath $publishedLanguage -PathType Leaf) -or
                (Get-FileHash -LiteralPath $publishedLanguage -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $sourceLanguage -Algorithm SHA256).Hash) {
                throw "Published language resource missing or changed: $languageFile. No release files were copied."
            }
        }
        $verified += @{ Source = $publishedExe; Languages = $languageSource; Name = "UnattendLocal_V${version}_x64_$($variant.Name).exe" }
        Write-Host "Verified: $($variant.Name), FileVersion=$actualFileVersion, ProductVersion=$actualProductVersion"
    }
    # Both EXEs and language packs pass checks before any final release file is copied.
    Copy-Item -LiteralPath $verified[0].Languages -Destination $languagesDestination -Recurse -ErrorAction Stop
    foreach ($item in $verified) {
        $destination = Join-Path $OutputRoot $item.Name
        [System.IO.File]::Copy($item.Source, $destination, $false)
        Write-Host "Created: $destination"
        if ($CreateZip) {
            $archive = [System.IO.Path]::ChangeExtension($destination, '.zip')
            Compress-Archive -LiteralPath @($destination, $languagesDestination) -DestinationPath $archive -ErrorAction Stop
            Write-Host "Created: $archive"
        }
    }
} finally {
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
}
Write-Host 'Build completed.'
