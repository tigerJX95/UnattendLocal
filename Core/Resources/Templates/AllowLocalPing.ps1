# UnattendLocal template: allow local-subnet echo requests on trusted profiles.
$ErrorActionPreference = 'Stop'
$definitions = @(
    @{ Name = 'UnattendLocal-ICMPv4-Echo'; Protocol = 'ICMPv4'; Type = '8' },
    @{ Name = 'UnattendLocal-ICMPv6-Echo'; Protocol = 'ICMPv6'; Type = '128' }
)
$existing = @{}
# Check both names before changing any rule. Do not overwrite another tool's rules.
foreach ($definition in $definitions) {
    $rules = @(Get-NetFirewallRule -PolicyStore PersistentStore -Name $definition.Name -ErrorAction SilentlyContinue)
    if ($rules.Count -gt 1 -or ($rules.Count -eq 1 -and $rules[0].Group -ne 'UnattendLocal')) {
        throw ('Firewall rule name is already used outside UnattendLocal: ' + $definition.Name)
    }
    $existing[$definition.Name] = $rules.Count -eq 1
}
foreach ($definition in $definitions) {
    $options = @{
        PolicyStore = 'PersistentStore'
        Name = $definition.Name
        Direction = 'Inbound'
        Action = 'Allow'
        Enabled = 'True'
        Profile = @('Domain', 'Private')
        Protocol = $definition.Protocol
        IcmpType = $definition.Type
        RemoteAddress = 'LocalSubnet'
        EdgeTraversalPolicy = 'Block'
    }
    if ($existing[$definition.Name]) {
        $null = Set-NetFirewallRule @options
    } else {
        $null = New-NetFirewallRule @options -DisplayName $definition.Name -Group 'UnattendLocal'
    }
}
