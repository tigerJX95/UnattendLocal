# Upstream sample capability: derive the name from one physical Ethernet MAC.
$ErrorActionPreference = 'Stop'
$adapters = @(Get-NetAdapter -Physical -ErrorAction Stop | Where-Object {
    $_.InterfaceType -eq 6 -and $_.MacAddress -match '^(?:[0-9A-Fa-f]{2}-){5}[0-9A-Fa-f]{2}$'
})
if ($adapters.Count -ne 1) { throw 'Expected exactly one physical Ethernet adapter with a valid MAC; no name was assigned.' }
$mac = $adapters[0].MacAddress.Replace('-', '').ToUpperInvariant()
if ($mac -in @('000000000000','FFFFFFFFFFFF')) { throw 'The adapter MAC is not suitable for a computer name.' }
return 'PC-' + $mac
