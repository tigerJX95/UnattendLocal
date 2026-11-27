# Shared by local verification and GitHub Actions. Does not publish packages.
# Compatible with Windows PowerShell 5.1 and PowerShell 7.
[CmdletBinding()]
param([string]$LogPath)
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:POWERSHELL_TELEMETRY_OPTOUT = '1'
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$previousConsoleEncoding = [Console]::OutputEncoding
$previousDotnetEncoding = $env:DOTNET_CLI_FORCE_UTF8_ENCODING
$transcriptStarted = $false
Push-Location $PSScriptRoot
try {
    [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $env:DOTNET_CLI_FORCE_UTF8_ENCODING = '1'
    if ($LogPath) {
        Start-Transcript -LiteralPath $LogPath | Out-Null
        $transcriptStarted = $true
    }
    $temporaryProjects = @(Get-ChildItem -LiteralPath 'Desktop', 'Tests/WpfBindings' -Filter '*_wpftmp.csproj' -File)
    if ($temporaryProjects.Count -gt 0) {
        throw 'Remove WPF temporary compilation projects (*_wpftmp.csproj) from the source tree before verification.'
    }
    # Windows PowerShell can interpret BOM-less non-ASCII scripts using the system code page.
    foreach ($testScript in Get-ChildItem -LiteralPath 'Tests' -Filter '*.ps1' -File) {
        $testBytes = [System.IO.File]::ReadAllBytes($testScript.FullName)
        $utf8Bom = ($testBytes.Length -ge 3 -and $testBytes[0] -eq 0xEF -and
            $testBytes[1] -eq 0xBB -and $testBytes[2] -eq 0xBF)
        $utf16Bom = $testBytes.Length -ge 2 -and (
            ($testBytes[0] -eq 0xFF -and $testBytes[1] -eq 0xFE) -or
            ($testBytes[0] -eq 0xFE -and $testBytes[1] -eq 0xFF))
        if (-not ($utf8Bom -or $utf16Bom) -and [System.Text.Encoding]::UTF8.GetString($testBytes) -match '[^\x00-\x7F]') {
            throw "Save this test script as UTF-8 with BOM for Windows PowerShell 5.1: $($testScript.Name)"
        }
    }
    & (Join-Path $PSScriptRoot 'Tests/BuildScripts.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/ScriptTemplates.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/DeploymentTemplates.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/ComputerNamePrompt.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/Ipv4Naming.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/ReleaseAssets.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/V122Templates.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/V123Templates.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/V124Templates.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/V126Msi.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/V126Sandbox.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/PeSafety.Runtime.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/PeImageCheck.Runtime.Tests.ps1')
    & (Join-Path $PSScriptRoot 'Tests/PePackageCheck.Runtime.Tests.ps1')
    & dotnet build (Join-Path $PSScriptRoot 'UnattendLocal.sln') -c Release -m:1 -nr:false -warnaserror | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Build failed (compiler and restore warnings are treated as errors).' }
    $bindingProject = Join-Path $PSScriptRoot 'Tests/WpfBindings/WpfBindings.csproj'
    & dotnet build $bindingProject -c Release -m:1 -nr:false -warnaserror | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'WPF binding regression build failed.' }
    if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
        & dotnet (Join-Path $PSScriptRoot 'Tests/WpfBindings/bin/Release/net10.0-windows/WpfBindings.dll') (Join-Path $PSScriptRoot 'Desktop/MainWindow.xaml') | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'WPF binding regression checks failed.' }
    } else {
        Write-Host 'NOT RUN: WPF binding runtime checks require Windows.'
    }
    $tests = Join-Path $PSScriptRoot 'Tests/bin/Release/net10.0/Tests.dll'
    & (Join-Path $PSScriptRoot 'Tests/PeCommandChain.Runtime.Tests.ps1') -TestsDll $tests
    # Tests is a console regression runner, not a dotnet test project.
    & dotnet $tests | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Regression checks failed.' }
    $versionOutput = @(& dotnet $tests --version-display-only)
    $versionExit = $LASTEXITCODE
    foreach ($line in $versionOutput) { Write-Host $line }
    if ($versionExit -ne 0 -or $versionOutput -notcontains 'Version display check passed.') {
        throw 'Startup version display check failed.'
    }
    Write-Host 'Verification passed. No release packages were created.'
} catch {
    if ($transcriptStarted) { Write-Host "Verification failed: $($_.Exception.Message)" }
    throw
} finally {
    try {
        if ($transcriptStarted) { Stop-Transcript | Out-Null }
    } finally {
        [Console]::OutputEncoding = $previousConsoleEncoding
        $env:DOTNET_CLI_FORCE_UTF8_ENCODING = $previousDotnetEncoding
        Pop-Location
    }
}
