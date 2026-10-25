# Optional power plan. May increase energy use and heat; support depends on Windows/hardware.
$ErrorActionPreference = 'Stop'
$powercfg = Join-Path $env:windir 'System32/powercfg.exe'
$source = 'e9a42b02-d5df-448d-aa00-03f14749eb61'
$managed = 'c68ca72b-9c7f-4ad3-931b-67e6ff7aeaa2'
$name = 'UnattendLocal Ultimate Performance'
$list = @(& $powercfg /LIST 2>&1)
if ($LASTEXITCODE -ne 0) { throw 'Cannot list power plans.' }
$existing = @($list | Where-Object { $_.ToString() -match [regex]::Escape($managed) })
if ($existing.Count -gt 0) {
    if ($existing.Count -ne 1 -or -not $existing[0].ToString().Contains("($name)")) {
        throw 'The template power-plan GUID is already used by another plan; no changes were made.'
    }
} else {
    & $powercfg /DUPLICATESCHEME $source $managed | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Ultimate Performance is not available on this system; no fallback plan was activated.' }
    & $powercfg /CHANGENAME $managed $name | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'The power plan was created but could not be named; it was not activated.' }
}
& $powercfg /SETACTIVE $managed | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not activate Ultimate Performance. Check hardware and Windows support.' }
