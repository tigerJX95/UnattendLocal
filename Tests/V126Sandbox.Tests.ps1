# Mock Windows servicing; never enable features or restart the computer.
$ErrorActionPreference = 'Stop'
$results = @{Checks=0}
function Assert-Sandbox($value,$message) { if(-not $value){throw $message};$results.Checks++ }
$path=Join-Path $PSScriptRoot '../Core/Resources/Templates/EnableWindowsSandbox.ps1'
$tokens=$null;$errors=$null
[void][Management.Automation.Language.Parser]::ParseFile($path,[ref]$tokens,[ref]$errors)
Assert-Sandbox ($errors.Count -eq 0) 'Sandbox template parses'
$script=[scriptblock]::Create((Get-Content -LiteralPath $path -Raw))
function Run-Sandbox([string]$initial='Disabled',[string]$final='Enabled',[bool]$needRestart=$false,[bool]$queryFail=$false,[bool]$enableFail=$false,[bool]$invalidResult=$false,[int]$count=1,[bool]$wrongName=$false,[bool]$requeryFail=$false) {
    $observed=@{Queries=0;Enables=0;Warnings=0;Failed=$false}
    function Get-WindowsOptionalFeature { [CmdletBinding()]param([switch]$Online,$FeatureName)
        Assert-Sandbox ($Online -and $FeatureName -ceq 'Containers-DisposableClientVM') 'Query only the Sandbox feature online'
        $observed.Queries++
        if($queryFail -or ($requeryFail -and $observed.Queries -gt 1)){throw 'simulated feature query failure'}
        for($i=0;$i -lt $count;$i++) {
            [pscustomobject]@{FeatureName=$(if($wrongName){'OtherFeature'}else{'Containers-DisposableClientVM'});State=$(if($observed.Queries -eq 1){$initial}else{$final})}
        }
    }
    function Enable-WindowsOptionalFeature { [CmdletBinding()]param([switch]$Online,$FeatureName,[switch]$All,[switch]$LimitAccess,[switch]$NoRestart)
        Assert-Sandbox ($Online -and $FeatureName -ceq 'Containers-DisposableClientVM') 'Enable only Sandbox'
        Assert-Sandbox ($All -and $LimitAccess -and $NoRestart) 'Include dependencies, prevent WU source fallback and automatic restart'
        $observed.Enables++
        if($enableFail){throw 'simulated servicing failure'}
        if($invalidResult){return [pscustomobject]@{RestartNeeded='unknown'}}
        [pscustomobject]@{RestartNeeded=$needRestart}
    }
    function Write-Warning { param($Message) $observed.Warnings++ }
    try { & $script 6>$null } catch { $observed.Failed=$true }
    return $observed
}
foreach($flags in @(@{},@{needRestart=$true},@{final='EnablePending'},@{final='EnablePending';needRestart=$true})) {
    $s=Run-Sandbox @flags
    Assert-Sandbox (-not $s.Failed -and $s.Enables -eq 1 -and $s.Queries -eq 2) 'Enable once and verify the resulting state'
    if($flags.Count -gt 0){Assert-Sandbox ($s.Warnings -eq 1) 'Pending restart is reported without reboot'}
}
$s=Run-Sandbox -initial 'Enabled'
Assert-Sandbox (-not $s.Failed -and $s.Enables -eq 0 -and $s.Queries -eq 1) 'Already enabled component is not reinstalled'
$s=Run-Sandbox -initial 'EnablePending'
Assert-Sandbox (-not $s.Failed -and $s.Enables -eq 0 -and $s.Warnings -eq 1) 'Pending enablement only warns'
foreach($initial in @('DisabledWithPayloadRemoved','DisablePending','PartiallyInstalled','Unknown','')) {
    $s=Run-Sandbox -initial $initial
    Assert-Sandbox ($s.Failed -and $s.Enables -eq 0) 'Missing payload and unsupported servicing states never start enablement'
}
foreach($flags in @(@{queryFail=$true},@{count=0},@{count=2},@{wrongName=$true})) {
    $s=Run-Sandbox @flags
    Assert-Sandbox ($s.Failed -and $s.Enables -eq 0) 'Unavailable, ambiguous or wrong features cannot be enabled'
}
foreach($flags in @(@{enableFail=$true},@{invalidResult=$true},@{requeryFail=$true},@{final='Disabled'},@{final='DisablePending'})) {
    $s=Run-Sandbox @flags
    Assert-Sandbox ($s.Failed -and $s.Enables -eq 1) 'Servicing/result verification failures surface without retry'
}
Write-Host ('V1.26 Sandbox: {0} simulated checks passed. No Windows features were changed.' -f $results.Checks)
