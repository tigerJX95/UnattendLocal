# Upstream sample capability: pause feature and quality updates for one hour.
# Configure a temporary pause only; retain a later existing expiry per category.
$ErrorActionPreference = 'Stop'
$path = 'Registry::HKLM\Software\Microsoft\WindowsUpdate\UX\Settings'
$now = [datetime]::UtcNow
$expiry = $now.AddHours(1)
$format = 'yyyy-MM-ddTHH\:mm\:ssZ'
$null = New-Item -Path $path -Force -ErrorAction Stop
$state = Get-ItemProperty -LiteralPath $path -ErrorAction Stop
$groups = @(
    @('PauseFeatureUpdatesStartTime','PauseFeatureUpdatesEndTime'),
    @('PauseQualityUpdatesStartTime','PauseQualityUpdatesEndTime'),
    @('PauseUpdatesStartTime','PauseUpdatesExpiryTime')
)
$writes = @()
foreach ($group in $groups) {
    $property = $state.PSObject.Properties[$group[1]]
    if ($property -and -not [string]::IsNullOrWhiteSpace([string]$property.Value)) {
        $existing = [datetime]::MinValue
        if (-not [datetime]::TryParse([string]$property.Value, [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::AdjustToUniversal, [ref]$existing)) {
            throw ('Existing update pause value cannot be parsed: ' + $group[1])
        }
        if ($existing -ge $expiry) { continue }
    }
    $writes += ,$group
}
foreach ($group in $writes) {
    Set-ItemProperty -LiteralPath $path -Name $group[0] -Type String -Value $now.ToString($format, [Globalization.CultureInfo]::InvariantCulture) -Force -ErrorAction Stop
    Set-ItemProperty -LiteralPath $path -Name $group[1] -Type String -Value $expiry.ToString($format, [Globalization.CultureInfo]::InvariantCulture) -Force -ErrorAction Stop
}
