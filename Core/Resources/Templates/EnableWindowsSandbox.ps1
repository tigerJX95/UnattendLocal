# Enable the built-in Sandbox component from local Windows component sources.
# Requires an elevated Windows PowerShell session on a supported Windows edition.
$ErrorActionPreference = 'Stop'
$featureName = 'Containers-DisposableClientVM'
$features = @(Get-WindowsOptionalFeature -Online -FeatureName $featureName -ErrorAction Stop)
if ($features.Count -ne 1 -or $features[0].FeatureName -ne $featureName) {
    throw 'Windows Sandbox is unavailable or its feature information is ambiguous.'
}
$state = [string]$features[0].State
if ($state -eq 'Enabled') {
    Write-Host 'The Windows Sandbox component is already enabled. No changes were made.'
    return
}
if ($state -eq 'EnablePending') {
    Write-Warning 'Windows Sandbox enablement is pending. Restart later before using it.'
    return
}
if ($state -eq 'DisabledWithPayloadRemoved') {
    throw 'The local Windows Sandbox component payload is missing. Prepare a matching component source first.'
}
if ($state -ne 'Disabled') {
    throw ('Windows Sandbox cannot be enabled from feature state {0}. Resolve pending servicing or repair the image first.' -f $state)
}
$result = @(Enable-WindowsOptionalFeature -Online -FeatureName $featureName -All -LimitAccess -NoRestart -ErrorAction Stop)
if ($result.Count -ne 1 -or $result[0].RestartNeeded -isnot [bool]) {
    throw 'Windows servicing returned an unexpected result. Check the feature state and servicing logs.'
}
$after = @(Get-WindowsOptionalFeature -Online -FeatureName $featureName -ErrorAction Stop)
if ($after.Count -ne 1 -or $after[0].FeatureName -ne $featureName -or [string]$after[0].State -notin @('Enabled', 'EnablePending')) {
    throw 'Windows Sandbox did not reach an enabled or pending-enabled state. Check Windows servicing logs.'
}
if ($result[0].RestartNeeded -or [string]$after[0].State -eq 'EnablePending') {
    Write-Warning 'The Windows Sandbox component was configured. Restart later to finish enabling it.'
} else {
    Write-Host 'The Windows Sandbox component is enabled. Verify virtualization support before starting it.'
}
