# Exercise the production package checker with temporary files and Windows Script Host.
$ErrorActionPreference = 'Stop'
$engine = if ($env:SystemRoot) { Join-Path $env:SystemRoot 'System32/cscript.exe' } else { '' }
if (-not $engine -or -not (Test-Path -LiteralPath $engine)) {
    Write-Host 'PE package checker runtime: NOT RUN (Windows cscript.exe is required).'
    return
}
$root = Join-Path ([IO.Path]::GetTempPath()) ('UL package check ' + [guid]::NewGuid().ToString('N'))
$packages = Join-Path $root 'WinPEUpdates'
$checker = Join-Path $root 'package checker.vbs'
$package = Join-Path $packages 'Update fixture.cab'
$extra = Join-Path $packages 'Other update.msu'
$template = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../Core/Resources/PePackageCheck.vbs'))
$null = New-Item -Path $packages -ItemType Directory -Force

function Write-PackageChecker([string]$Hash) {
    $entries = '    packages.Add "Update fixture.cab", "' + $Hash + '"'
    [IO.File]::WriteAllText($checker, $template.Replace('@@PACKAGES@@', $entries), [Text.Encoding]::ASCII)
}

function Test-PackageChecker([string]$Mode, [int]$Code, [string]$Message) {
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = $engine
    $start.Arguments = '//Nologo //E:vbscript "' + $checker + '" "' + $packages + '" ' + $Mode
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [Diagnostics.Process]::Start($start)
    try {
        if (-not $process.WaitForExit(15000)) { $process.Kill(); throw 'Package checker timed out.' }
        $output = $process.StandardOutput.ReadToEnd() + $process.StandardError.ReadToEnd()
        if ($process.ExitCode -ne $Code -or -not $output.Contains($Message)) {
            throw "Unexpected package result $($process.ExitCode), expected $Code and '$Message': $output"
        }
    } finally { $process.Dispose() }
}

try {
    Write-PackageChecker ''
    Test-PackageChecker 'read' 1 'Missing update file'
    [IO.File]::WriteAllBytes($package, [byte[]]@())
    Test-PackageChecker 'read' 1 'Empty update file'
    [IO.File]::WriteAllBytes($package, [byte[]]@(1, 2, 3))
    Test-PackageChecker 'read' 0 'Checking offline package'
    Test-PackageChecker 'list' 0 ''
    [IO.File]::WriteAllBytes($extra, [byte[]]@(4, 5, 6))
    Test-PackageChecker 'read' 1 'was not listed'
    Test-PackageChecker 'list' 1 'was not listed'
    Remove-Item -LiteralPath $extra
    $hash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
    Write-PackageChecker $hash
    if (Test-Path -LiteralPath (Join-Path $env:SystemRoot 'System32/certutil.exe')) {
        Test-PackageChecker 'read' 0 'Verifying SHA-256'
        Test-PackageChecker 'list' 0 'Verifying SHA-256'
        [IO.File]::WriteAllBytes($package, [byte[]]@(9, 8, 7))
        Test-PackageChecker 'read' 1 'SHA-256 mismatch'
        Test-PackageChecker 'list' 1 'SHA-256 mismatch'
        Write-Host 'PE package checker runtime: 10 checks passed. No updates were installed.'
    } else {
        Test-PackageChecker 'read' 1 'requires certutil.exe'
        Write-Host 'PE package checker runtime: 7 checks passed. certutil.exe unavailable.'
    }
} finally { Remove-Item -LiteralPath $root -Recurse -Force }
