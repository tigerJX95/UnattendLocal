$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../Core/Resources/ComputerNameFromIpv4.ps1'
$state = @{ Checks = 0 }
function Assert-Ipv4($condition, $message) {
    if (-not $condition) { throw $message }
    $state.Checks++
}
function Run-Ipv4($adapters, $addresses, [switch]$FailRead) {
    function Get-NetAdapter {
        [CmdletBinding()]param([switch]$Physical)
        Assert-Ipv4 $Physical 'Only physical adapters may be used.'
        return $adapters
    }
    function Get-NetIPAddress {
        [CmdletBinding()]param($InterfaceIndex, $AddressFamily, $AddressState, [bool]$SkipAsSource)
        Assert-Ipv4 ($AddressFamily -eq 'IPv4' -and $AddressState -eq 'Preferred' -and -not $SkipAsSource) 'Address selection must request a preferred source IPv4 address.'
        Assert-Ipv4 (@($InterfaceIndex).Count -gt 0 -and @($InterfaceIndex | Where-Object { $_ -notin @(7, 8) }).Count -eq 0) 'Only selected Ethernet interface indices may be queried.'
        if ($FailRead) { throw 'Simulated address read failure.' }
        return $addresses
    }
    & $scriptPath
}
$one = [pscustomobject]@{ InterfaceType = 6; Status = 'Up'; InterfaceIndex = 7; Name = '以太网' }
$other = [pscustomobject]@{ InterfaceType = 6; Status = 'Up'; InterfaceIndex = 8; Name = 'LAN 2' }
$wifi = [pscustomobject]@{ InterfaceType = 71; Status = 'Up'; InterfaceIndex = 9 }
$down = [pscustomobject]@{ InterfaceType = 6; Status = 'Disconnected'; InterfaceIndex = 10 }
Assert-Ipv4 ((Run-Ipv4 @($one, $wifi, $down) @([pscustomobject]@{ IPAddress = '192.168.12.34' })) -ceq 'PC-12-34') 'Name must use the final two IPv4 octets.'
Assert-Ipv4 ((Run-Ipv4 @($one, $other) @([pscustomobject]@{ IPAddress = '10.20.0.255' })) -ceq 'PC-0-255') 'One usable address across multiple adapters is unambiguous.'
Assert-Ipv4 ((Run-Ipv4 @($one) @([pscustomobject]@{ IPAddress = '10.20.3.4' }, [pscustomobject]@{ IPAddress = '10.20.3.4' })) -ceq 'PC-3-4') 'Duplicate records for the same address are one candidate.'
foreach ($values in @(@(), @('127.0.0.1'), @('169.254.2.3'), @('224.0.0.1'), @('0.0.0.0'), @('::1'), @('bad'), @('10.1.2.3', '10.2.2.3'))) {
    $failed = $false
    try { Run-Ipv4 @($one) @($values | ForEach-Object { [pscustomobject]@{ IPAddress = $_ } }) | Out-Null } catch { $failed = $true }
    Assert-Ipv4 $failed 'Missing, invalid or ambiguous addresses must fail.'
}
foreach ($adapters in @(@(), @($wifi), @($down))) {
    $failed = $false
    try { Run-Ipv4 $adapters @([pscustomobject]@{ IPAddress = '192.168.1.2' }) | Out-Null } catch { $failed = $true }
    Assert-Ipv4 $failed 'A connected Ethernet adapter is required.'
}
$failed = $false
try { Run-Ipv4 @($one) @() -FailRead | Out-Null } catch { $failed = $true }
Assert-Ipv4 $failed 'Address query failures must surface.'
Write-Host "IPv4 naming: $($state.Checks) simulated checks passed."
