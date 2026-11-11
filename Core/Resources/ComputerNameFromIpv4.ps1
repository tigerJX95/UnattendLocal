# Adapt the upstream IPv4 naming example to require one usable Ethernet address.
$ErrorActionPreference = 'Stop'
$adapters = @(Get-NetAdapter -Physical -ErrorAction Stop | Where-Object {
    $_.InterfaceType -eq 6 -and $_.Status -eq 'Up'
})
if ($adapters.Count -eq 0) { throw 'No connected physical Ethernet adapter is available for IPv4 naming.' }
$addresses = @(Get-NetIPAddress -InterfaceIndex $adapters.InterfaceIndex -AddressFamily IPv4 -AddressState Preferred -SkipAsSource $false -ErrorAction Stop | Where-Object {
    [ipaddress] $address = $null
    if (-not [ipaddress]::TryParse($_.IPAddress, [ref] $address)) { return $false }
    $bytes = $address.GetAddressBytes()
    $address.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork -and
        $bytes[0] -gt 0 -and $bytes[0] -lt 224 -and
        -not [ipaddress]::IsLoopback($address) -and
        -not ($bytes[0] -eq 169 -and $bytes[1] -eq 254)
} | Select-Object -ExpandProperty IPAddress -Unique)
if ($addresses.Count -ne 1) { throw 'Expected exactly one usable Ethernet IPv4 address; no name was assigned.' }
$bytes = ([ipaddress] $addresses[0]).GetAddressBytes()
return 'PC-{0}-{1}' -f $bytes[2], $bytes[3]
