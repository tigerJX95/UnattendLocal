# UnattendLocal template: install NetFx3 from matching local Windows media.
$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Version.Build -ge 28000) {
    throw '.NET Framework 3.5 requires a separate installer on build 28000 and later.'
}
if ((Get-WindowsOptionalFeature -Online -FeatureName NetFx3).State -eq 'Enabled') {
    return
}
$sources = @([System.IO.DriveInfo]::GetDrives() | Where-Object { $_.IsReady } | ForEach-Object {
    $candidate = Join-Path $_.RootDirectory.FullName 'sources\sxs'
    if (Test-Path -LiteralPath $candidate -PathType Container) {
        if (Get-ChildItem -LiteralPath $candidate -Filter 'microsoft-windows-netfx3-ondemand-package~*.cab' -File -ErrorAction Stop) {
            $candidate
        }
    }
})
if ($sources.Count -eq 0) {
    throw 'No local NetFx3 source found. Attach matching Windows installation media.'
}
$installed = $false
foreach ($source in $sources) {
    try {
        $result = Enable-WindowsOptionalFeature -Online -FeatureName NetFx3 -All -Source $source -LimitAccess -NoRestart -ErrorAction Stop
        if ($result.RestartNeeded) { Write-Warning 'NetFx3 installation requires a later restart.' }
        $installed = $true
        break
    } catch {
        Write-Warning ('NetFx3 source failed: {0}. {1}' -f $source, $_.Exception.Message)
    }
}
if (-not $installed) { throw 'NetFx3 installation failed with all local sources. Check media compatibility and DISM logs.' }
