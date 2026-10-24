# Exercise publishing preflight without compiling or creating real packages.
# The fake dotnet command is scoped to this test script and fails on first use.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
$sourceScript = Join-Path (Split-Path $PSScriptRoot -Parent) 'publish.ps1'
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('UnattendLocal-publish-tests-' + [Guid]::NewGuid().ToString('N'))
$calls = New-Object 'System.Collections.Generic.List[string]'
$previousExit = Get-Variable LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue
$previousExitValue = if ($null -eq $previousExit) { 0 } else { $previousExit.Value }
$count = 0
function dotnet {
    $calls.Add(($args -join ' '))
    $global:LASTEXITCODE = 13
}
try {
    New-Item -ItemType Directory -Path (Join-Path $testRoot 'Desktop') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $testRoot 'Tests') -Force | Out-Null
    Copy-Item -LiteralPath $sourceScript -Destination (Join-Path $testRoot 'publish.ps1')
    Set-Content -LiteralPath (Join-Path $testRoot 'Tests/Tests.csproj') -Value '<Project />' -Encoding ascii
    $cases = @(
        @{ Name='two-part'; Version='1.18'; Info='1.18'; File='1.18.0.0'; Assembly='1.18.0.0'; Valid=$true },
        @{ Name='explicit-zero'; Version='1.18.0'; Info='1.18'; File='1.18.0.0'; Assembly='1.18.0.0'; Valid=$true },
        @{ Name='patch'; Version='1.18.2'; Info='1.18.2'; File='1.18.2.0'; Assembly='1.18.2.0'; Valid=$true },
        @{ Name='four-part-project'; Version='1.18.0.0'; Info='1.18'; File='1.18.0.0'; Assembly='1.18.0.0'; Valid=$true },
        @{ Name='project-mismatch'; Version='1.19'; Info='1.18'; File='1.18.0.0'; Assembly='1.18.0.0'; Valid=$false },
        @{ Name='missing-patch'; Version='1.18.2'; Info='1.18'; File='1.18.2.0'; Assembly='1.18.2.0'; Valid=$false },
        @{ Name='unlabelled-revision'; Version='1.18.0.1'; Info='1.18'; File='1.18.0.1'; Assembly='1.18.0.1'; Valid=$false },
        @{ Name='assembly-mismatch'; Version='1.18'; Info='1.18'; File='1.18.0.0'; Assembly='1.17.0.0'; Valid=$false },
        @{ Name='preview-label'; Version='1.18'; Info='1.18-pre1'; File='1.18.0.0'; Assembly='1.18.0.0'; Valid=$false },
        @{ Name='nonnumeric-project'; Version='1.18-beta'; Info='1.18'; File='1.18.0.0'; Assembly='1.18.0.0'; Valid=$false },
        @{ Name='short-file-version'; Version='1.18'; Info='1.18'; File='1.18'; Assembly='1.18.0.0'; Valid=$false },
        @{ Name='existing-exe'; Version='1.18'; Info='1.18'; File='1.18.0.0'; Assembly='1.18.0.0'; Valid=$false; Existing='.exe' },
        @{ Name='existing-zip'; Version='1.18'; Info='1.18'; File='1.18.0.0'; Assembly='1.18.0.0'; Valid=$false; Existing='.zip' }
    )
    foreach ($case in $cases) {
        $xml = '<Project><PropertyGroup><Version>{0}</Version><InformationalVersion>{1}</InformationalVersion><FileVersion>{2}</FileVersion><AssemblyVersion>{3}</AssemblyVersion></PropertyGroup></Project>' -f $case.Version, $case.Info, $case.File, $case.Assembly
        Set-Content -LiteralPath (Join-Path $testRoot 'Desktop/Desktop.csproj') -Value $xml -Encoding ascii
        $output = Join-Path $testRoot $case.Name
        $existing = $null
        if ($case.ContainsKey('Existing')) {
            New-Item -ItemType Directory -Path $output | Out-Null
            $existing = Join-Path $output ('UnattendLocal_V1.18_x64_SelfContained' + $case.Existing)
            Set-Content -LiteralPath $existing -Value 'KEEP' -Encoding ascii
        }
        $calls.Clear()
        $failure = $null
        try { & (Join-Path $testRoot 'publish.ps1') -OutputRoot $output -CreateZip }
        catch { $failure = $_.Exception.Message }
        if ($null -eq $failure) { throw "Expected preflight or synthetic build failure: $($case.Name)" }
        if ($case.Valid) {
            if ($calls.Count -ne 1 -or $failure -notlike 'Building the version display check failed.*') {
                throw "Valid release metadata was rejected before the build: $($case.Name): $failure"
            }
        } elseif ($calls.Count -ne 0) {
            throw "Invalid metadata or existing output reached the build: $($case.Name)"
        }
        if ($null -ne $existing -and (Get-Content -LiteralPath $existing -Raw).Trim() -ne 'KEEP') {
            throw "Existing package was changed: $($case.Name)"
        }
        if (@(Get-ChildItem -LiteralPath $testRoot -Filter '.publish-*' -Directory -Recurse).Count -ne 0) {
            throw "Preflight failure left publishing state: $($case.Name)"
        }
        $count++
    }
    Write-Host "Build script preflight: $count checks passed."
} finally {
    $global:LASTEXITCODE = $previousExitValue
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
