# Shared by local verification and GitHub Actions. Does not publish packages.
# Compatible with Windows PowerShell 5.1 and PowerShell 7.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
Push-Location $PSScriptRoot
try {
    & (Join-Path $PSScriptRoot 'Tests/BuildScripts.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/ScriptTemplates.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/DeploymentTemplates.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/ReleaseAssets.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/V122Templates.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/V123Templates.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/V124Templates.Tests.ps1')
    & dotnet build (Join-Path $PSScriptRoot 'UnattendLocal.sln') -c Release -m:1 -nr:false -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Build failed (compiler and restore warnings are treated as errors).' }
    $tests = Join-Path $PSScriptRoot 'Tests/bin/Release/net10.0/Tests.dll'
    # Tests is a console regression runner, not a dotnet test project.
    & dotnet $tests
    if ($LASTEXITCODE -ne 0) { throw 'Regression checks failed.' }
    $versionOutput = @(& dotnet $tests --version-display-only)
    $versionExit = $LASTEXITCODE
    foreach ($line in $versionOutput) { Write-Host $line }
    if ($versionExit -ne 0 -or $versionOutput -notcontains 'Version display check passed.') {
        throw 'Startup version display check failed.'
    }
    Write-Host 'Verification passed. No release packages were created.'
} finally {
    Pop-Location
}
