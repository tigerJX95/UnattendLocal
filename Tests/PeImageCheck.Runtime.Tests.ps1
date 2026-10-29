# Production image-information parser; only temporary files are read and written.
$ErrorActionPreference = 'Stop'
$engine = if ($env:SystemRoot) { Join-Path $env:SystemRoot 'System32/cscript.exe' } else { '' }
if (-not $engine -or -not (Test-Path -LiteralPath $engine)) {
    Write-Host 'PE image checker runtime: NOT RUN (Windows cscript.exe is required).'
    return
}
$root = Join-Path ([IO.Path]::GetTempPath()) ('UL-ImageCheck-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -Path $root -ItemType Directory
$checker = Join-Path $PSScriptRoot '../Core/Resources/PeImageCheck.vbs'
$checks = 0
try {
    $metadata = "Index : 3`r`nEdition : Professional`r`n"
    $cases = @(
        @{ Text = "Architecture : x64`r`n"; Code = 0 },
        @{ Text = "Header`n ARCHITECTURE`t:`tX64 `n"; Code = 0 },
        @{ Text = "Architecture : x64`r"; Code = 0 },
        @{ Text = "Architecture : x64"; Code = 0; Unicode = $true },
        @{ Text = "Architecture : x86"; Code = 42 },
        @{ Text = "Architecture : ARM64"; Code = 42 },
        @{ Text = "Architecture : arm"; Code = 42 },
        @{ Text = "Architecture : ia64"; Code = 42 },
        @{ Text = "Name : Example"; Code = 1 },
        @{ Text = "Architecture : x64`nArchitecture : x64"; Code = 1 },
        @{ Text = "Architecture : x64`nArchitecture : arm64"; Code = 1 },
        @{ Text = "Architecture : unknown"; Code = 1 },
        @{ Text = "Architecture : x64 extra"; Code = 1 },
        @{ Text = "Architecture : ""x64"""; Code = 1 },
        @{ Text = ''; Code = 1 },
        @{ Missing = $true; Code = 1 },
        @{ Text = ('x' * 1048577); Code = 1 },
        @{ Text = "Index : 3`nEdition : CoreSingleLanguage`nArchitecture : x64"; Full = $true; Code = 0; Expected = 'CoreSingleLanguage' },
        @{ Text = "Index : 3`nEdition : EnterpriseS`nArchitecture : x64"; Full = $true; Code = 0; Expected = 'EnterpriseS' },
        @{ Text = "Index : 3`nEdition : FutureEdition_1`nArchitecture : x64"; Full = $true; Code = 0; Expected = 'FutureEdition_1' },
        @{ Text = "Index : 3`nArchitecture : x64"; Full = $true; Code = 0; Unavailable = $true },
        @{ Text = "Index : 3`nEdition : Professional`nEdition : Core`nArchitecture : x64"; Full = $true; Code = 0; Unavailable = $true },
        @{ Text = "Index : 3`nEdition : Professional`nEdition : Professional`nArchitecture : x64"; Full = $true; Code = 0; Unavailable = $true },
        @{ Text = "Index : 3`nEdition : `nArchitecture : x64"; Full = $true; Code = 0; Unavailable = $true },
        @{ Text = "Index : 3`nEdition : Professional & exit`nArchitecture : x64"; Full = $true; Code = 0; Unavailable = $true },
        @{ Text = "Edition : Professional`nArchitecture : x64"; Full = $true; Code = 1 },
        @{ Text = "Index : 2`nEdition : Professional`nArchitecture : x64"; Full = $true; Code = 1 },
        @{ Text = "Index : 3`nIndex : 3`nEdition : Professional`nArchitecture : x64"; Full = $true; Code = 1 },
        @{ Text = "Index : 0`nEdition : Professional`nArchitecture : x64"; Full = $true; Code = 1 },
        @{ Text = "Index : 3abc`nEdition : Professional`nArchitecture : x64"; Full = $true; Code = 1 },
        @{ Text = "Index : 2147483648`nEdition : Professional`nArchitecture : x64"; Full = $true; Code = 1 },
        @{ Text = "Index : 3e0`nEdition : Professional`nArchitecture : x64"; Full = $true; Code = 1 }
    )
    foreach ($case in $cases) {
        $path = Join-Path $root ('image info ' + $checks + '.txt')
        if (-not $case['Missing']) {
            $encoding = if ($case['Unicode']) { [Text.Encoding]::Unicode } else { [Text.Encoding]::ASCII }
            $text = if ($case['Full']) { $case.Text } else { $metadata + $case.Text }
            [IO.File]::WriteAllText($path, $text, $encoding)
        }
        $start = New-Object Diagnostics.ProcessStartInfo
        $start.FileName = $engine
        $start.Arguments = '//Nologo //E:vbscript "' + $checker + '" "' + $path + '" 3'
        $start.UseShellExecute = $false
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        $process = [Diagnostics.Process]::Start($start)
        try {
            if (-not $process.WaitForExit(10000)) { $process.Kill(); throw 'Image parser timed out.' }
            $output = $process.StandardOutput.ReadToEnd() + $process.StandardError.ReadToEnd()
            if ($process.ExitCode -ne $case.Code) { throw "Unexpected image-check result $($process.ExitCode), expected $($case.Code): $output" }
            if ($case.Code -eq 0) {
                $edition = if ($case['Expected']) { $case.Expected } else { 'Professional' }
                $editionReported = if ($case['Unavailable']) { $output.Contains('Edition/SKU unavailable; using the explicitly selected index.') } else { $output.Contains("Selected image edition/SKU: $edition") }
                if (-not $output.Contains('Selected image index: 3') -or -not $editionReported) {
                    throw "Image identity was not reported correctly: $output"
                }
            }
            $checks++
        } finally { $process.Dispose() }
    }
    Write-Host "PE image checker runtime: $checks checks passed. No DISM or disk operations were performed."
} finally { Remove-Item -LiteralPath $root -Recurse -Force }
