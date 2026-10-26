# Configure local firewall logging only; do not change filtering or rule states.
$ErrorActionPreference = 'Stop'
$logMaxSizeKilobytes = 16384
if ($logMaxSizeKilobytes -lt 1 -or $logMaxSizeKilobytes -gt 32767) {
    throw 'Firewall log size must be between 1 and 32767 KiB.'
}
$profiles = @('Domain', 'Private', 'Public')
# Retain each profile's existing log file location and allowed-traffic logging.
Set-NetFirewallProfile -Name $profiles -PolicyStore PersistentStore -LogBlocked True `
    -LogMaxSizeKilobytes $logMaxSizeKilobytes -ErrorAction Stop
$configured = @(Get-NetFirewallProfile -Name $profiles -PolicyStore PersistentStore -ErrorAction Stop)
foreach ($profile in $profiles) {
    $entry = @($configured | Where-Object { $_.Name -eq $profile })
    if ($entry.Count -ne 1 -or [string]$entry[0].LogBlocked -ne 'True' -or
        $entry[0].LogMaxSizeKilobytes -ne $logMaxSizeKilobytes) {
        throw ('Cannot verify local firewall logging for profile {0}.' -f $profile)
    }
}
Write-Host 'Local dropped-packet logging configured. Organization policy may override the effective settings.'
