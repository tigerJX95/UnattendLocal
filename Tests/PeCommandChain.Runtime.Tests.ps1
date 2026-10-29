# Run the production CMD gates with a harmless script-host stub. Never run
# DiskPart, DISM, real disk discovery or any installation commands.
param([Parameter(Mandatory=$true)][string]$TestsDll)
$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    Write-Host 'PE command chain runtime: NOT RUN (Windows cmd.exe is required).'
    return
}
$root = Join-Path ([IO.Path]::GetTempPath()) ('UL-PeChain-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -Path $root -ItemType Directory
try {
    & dotnet $TestsDll --export-pe-chain-fixtures $root
    if ($LASTEXITCODE -ne 0) { throw 'Production PE gate export failed.' }
    $stub = Join-Path $root 'host.cmd'
    [IO.File]::WriteAllText($stub, "@echo off`r`nif defined UL_TEST_OUTPUT echo %UL_TEST_OUTPUT%`r`nexit /b %UL_TEST_CODE%`r`n", [Text.Encoding]::ASCII)
    $checks = 0
    foreach ($gate in @('selection','verify')) {
        $source = [IO.File]::ReadAllText((Join-Path $root ($gate + '.cmd')))
        if ($source -match '(?im)^diskpart|dism\.exe|bcdboot|wpeutil') { throw 'Unexpected destructive command in fixture.' }
        $source = $source.Replace('cscript.exe', ('call "' + $stub + '"'))
        $source = $source.Replace('X:\target.out', ('"' + (Join-Path $root 'target.out') + '"'))
        $source = $source.Replace('X:\UL-verify.out', ('"' + (Join-Path $root 'verify.out') + '"'))
        # FOR /F needs usebackq to read the quoted temporary filename.
        $source = $source.Replace('for /f %%t in (', 'for /f "usebackq" %%t in (')
        $reached = Join-Path $root 'reached.txt'
        $failed = Join-Path $root 'failed.txt'
        $batch = Join-Path $root 'gate.cmd'
        [IO.File]::WriteAllText($batch, "@echo off`r`n" + $source + "`r`necho reached> `"$reached`"`r`nexit /b 0`r`n:fail`r`necho failed> `"$failed`"`r`nexit 1`r`n", [Text.Encoding]::ASCII)
        foreach ($shadow in @('', '0', '1')) {
            foreach ($case in @(
                @{Code='0';Output='0';Pass=$true},
                @{Code='1';Output='0';Pass=$false},
                @{Code='42';Output='0';Pass=$false},
                @{Code='-2146828235';Output='';Pass=$false},
                @{Code='-2147024894';Output='0';Pass=$false},
                @{Code='0';Output='';Pass=($gate -eq 'verify')}
            )) {
                Remove-Item -LiteralPath $reached,$failed -Force -ErrorAction SilentlyContinue
                $info = New-Object Diagnostics.ProcessStartInfo
                $info.FileName = Join-Path $env:SystemRoot 'System32/cmd.exe'
                $info.Arguments = '/d /c ""' + $batch + '""'
                $info.UseShellExecute = $false
                $info.RedirectStandardOutput = $true
                $info.RedirectStandardError = $true
                $info.EnvironmentVariables['UL_TEST_CODE'] = $case.Code
                $info.EnvironmentVariables['UL_TEST_OUTPUT'] = $case.Output
                $info.EnvironmentVariables['TARGET_DISK'] = '999'
                $info.EnvironmentVariables['ERRORLEVEL'] = $shadow
                $process = [Diagnostics.Process]::Start($info)
                if (-not $process.WaitForExit(10000)) { $process.Kill(); throw 'PE command chain test timed out.' }
                $stdout = $process.StandardOutput.ReadToEnd()
                $stderr = $process.StandardError.ReadToEnd()
                $code = $process.ExitCode
                $process.Dispose()
                if ($case.Pass) {
                    if ($code -ne 0 -or -not (Test-Path -LiteralPath $reached) -or (Test-Path -LiteralPath $failed)) { throw "Valid $gate did not reach the harmless continuation: $stdout $stderr" }
                } else {
                    if ($code -eq 0 -or (Test-Path -LiteralPath $reached) -or -not (Test-Path -LiteralPath $failed)) { throw "Failed $gate continued with code $($case.Code): $stdout $stderr" }
                }
                $checks++
            }
        }
    }
    Write-Host ("PE command chain runtime: $checks checks passed; no disk operations were executed.")
} finally {
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}
