$ErrorActionPreference='Stop'
$root=Join-Path $PSScriptRoot '../Core/Resources'
$checks=@{Count=0}
function Assert-V124($ok,$message){if(-not $ok){throw $message};$checks.Count++}
foreach($name in 'ComputerNameFromMac.ps1','Templates/PauseSetupUpdates.ps1') {
    $tokens=$null;$errors=$null
    [void][Management.Automation.Language.Parser]::ParseFile((Join-Path $root $name),[ref]$tokens,[ref]$errors)
    Assert-V124 ($errors.Count -eq 0) 'PowerShell must parse'
}
function Run-Mac($adapters) {
    function Get-NetAdapter {[CmdletBinding()]param([switch]$Physical)
        Assert-V124 $Physical 'Only inspect physical adapters'
        return $adapters
    }
    & (Join-Path $root 'ComputerNameFromMac.ps1')
}
$one=[pscustomobject]@{Name='以太网';InterfaceType=6;MacAddress='00-11-22-aa-bb-cc'}
Assert-V124 ((Run-Mac @($one)) -ceq 'PC-001122AABBCC') 'Localized adapter names are independent; name is 15 characters'
$wifi=[pscustomobject]@{Name='Wi-Fi';InterfaceType=71;MacAddress='11-22-33-44-55-66'}
Assert-V124 ((Run-Mac @($one,$wifi)) -ceq 'PC-001122AABBCC') 'Ignore Wi-Fi interface'
foreach($items in @(@(),@($one,$one),@($wifi),@([pscustomobject]@{InterfaceType=6;MacAddress='000000000000'}),@([pscustomobject]@{InterfaceType=6;MacAddress='00-00-00-00-00-00'}),@([pscustomobject]@{InterfaceType=6;MacAddress='FF-FF-FF-FF-FF-FF'}))) {
    $failed=$false;try{Run-Mac $items|Out-Null}catch{$failed=$true};Assert-V124 $failed 'Missing, ambiguous or invalid adapter must fail'
}
function Run-Pause([hashtable]$existing=@{},[switch]$FailWrite) {
    $trace=@{Writes=@();Failed=$false}
    function New-Item {[CmdletBinding()]param($Path,[switch]$Force)}
    function Get-ItemProperty {[CmdletBinding()]param($LiteralPath);[pscustomobject]$existing}
    function Set-ItemProperty {[CmdletBinding()]param($LiteralPath,$Name,$Type,$Value,[switch]$Force)
        Assert-V124 ($LiteralPath -ceq 'Registry::HKLM\Software\Microsoft\WindowsUpdate\UX\Settings' -and $Type -eq 'String') 'Only write expected pause values'
        if($FailWrite){throw 'Simulated registry failure'}
        $trace.Writes+=,[pscustomobject]@{Name=$Name;Value=$Value}
    }
    try{& (Join-Path $root 'Templates/PauseSetupUpdates.ps1')}catch{$trace.Failed=$true}
    return $trace
}
$s=Run-Pause
Assert-V124 (-not $s.Failed -and $s.Writes.Count -eq 6) 'Set all three pause intervals'
foreach($pair in @(@('PauseFeatureUpdatesStartTime','PauseFeatureUpdatesEndTime'),@('PauseQualityUpdatesStartTime','PauseQualityUpdatesEndTime'),@('PauseUpdatesStartTime','PauseUpdatesExpiryTime'))) {
    $start=[datetime]::Parse(($s.Writes|Where-Object Name -eq $pair[0]).Value,[Globalization.CultureInfo]::InvariantCulture)
    $end=[datetime]::Parse(($s.Writes|Where-Object Name -eq $pair[1]).Value,[Globalization.CultureInfo]::InvariantCulture)
    Assert-V124 (($end-$start).TotalHours -eq 1) 'Pause is exactly one hour'
}
$later=[datetime]::UtcNow.AddDays(3).ToString('o');$old=@{PauseFeatureUpdatesEndTime=$later;PauseQualityUpdatesEndTime=$later;PauseUpdatesExpiryTime=$later}
$s=Run-Pause $old;Assert-V124 (-not $s.Failed -and $s.Writes.Count -eq 0) 'Do not shorten existing later pauses'
$s=Run-Pause @{PauseUpdatesExpiryTime='bad-date'};Assert-V124 ($s.Failed -and $s.Writes.Count -eq 0) 'Validate all dates before changing pause values'
$s=Run-Pause -FailWrite;Assert-V124 $s.Failed 'Registry failure surfaces'
Write-Host "V1.24 scripts: $($checks.Count) simulated checks passed. No naming or registry changes were performed."
