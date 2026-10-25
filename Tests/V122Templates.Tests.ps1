$ErrorActionPreference='Stop'
$testRoot=Join-Path $PSScriptRoot '../Core/Resources/Templates'
$results=@{Checks=0}
function Assert-V122($value,$message) { if(-not $value){throw $message};$results.Checks++ }
function Expect-V122Failure([scriptblock]$Action,$message) { $failed=$false;try{& $Action}catch{$failed=$true};Assert-V122 $failed $message }
foreach($name in 'SystemVolumeLabel','UltimatePerformance') {
    $tokens=$null;$errors=$null
    [void][Management.Automation.Language.Parser]::ParseFile((Join-Path $testRoot "$name.ps1"),[ref]$tokens,[ref]$errors)
    Assert-V122 ($errors.Count -eq 0) 'PowerShell template must parse without errors'
}
$labelText=Get-Content -LiteralPath (Join-Path $testRoot 'SystemVolumeLabel.ps1') -Raw
$labelBlock=[scriptblock]::Create($labelText.Replace('$label = ''Windows''','$label = $testLabel'))
$originalDrive=$env:SystemDrive
try {
    function Run-LabelTest([string]$testLabel,[string]$filesystem='NTFS',[string]$existing='Old',[string]$drive='D:') {
        $env:SystemDrive=$drive;$state=@{Writes=0;Seen='';Label=''}
        function Get-Volume { [CmdletBinding()]param($DriveLetter)
            $state.Seen=$DriveLetter;[pscustomobject]@{FileSystem=$filesystem;FileSystemLabel=$existing}
        }
        function Set-Volume { [CmdletBinding()]param($DriveLetter,$NewFileSystemLabel)
            Assert-V122 ($DriveLetter -eq 'D') 'Use installed Windows drive rather than fixed C'
            $state.Writes++;$state.Label=$NewFileSystemLabel
        }
        & $labelBlock
        return $state
    }
    $state=Run-LabelTest 'Windows'
    Assert-V122 ($state.Writes -eq 1 -and $state.Label -ceq 'Windows') 'Set valid label once'
    $state=Run-LabelTest 'Windows' 'NTFS' 'Windows'
    Assert-V122 ($state.Writes -eq 0) 'Existing identical label is unchanged'
    $state=Run-LabelTest ('x'*32)
    Assert-V122 ($state.Label.Length -eq 32) '32-character boundary accepted'
    foreach($label in @('', ' ', ('x'*33),'bad/name','bad\name','bad:name','bad*name','bad?name','bad"name','bad<name','bad>name','bad|name',"bad`nname")) {
        Expect-V122Failure {Run-LabelTest $label} 'Reject invalid label without calling Set-Volume'
    }
    Expect-V122Failure {Run-LabelTest 'Windows' 'FAT32'} 'Reject non-NTFS system volume'
    Expect-V122Failure {Run-LabelTest 'Windows' 'NTFS' 'Old' ''} 'Reject absent system drive'
    Expect-V122Failure {Run-LabelTest 'Windows' 'NTFS' 'Old' 'D:\path'} 'Reject malformed system drive'
} finally { $env:SystemDrive=$originalDrive }

$powerText=Get-Content -LiteralPath (Join-Path $testRoot 'UltimatePerformance.ps1') -Raw
# Replace only the executable adapter; execute the complete template logic with mocked powercfg.
$powerText=$powerText.Replace('$powercfg = Join-Path $env:windir ''System32/powercfg.exe''','$powercfg = { Invoke-TestPowercfg @args }')
$powerBlock=[scriptblock]::Create($powerText)
$power=@{Exists=$false;Collision=$false;Calls=@();Failure='';Name='UnattendLocal Ultimate Performance'}
function Invoke-TestPowercfg {
    $cli=@($args);$power.Calls+=,$cli;$global:LASTEXITCODE=0
    if($power.Failure -ceq $cli[0]) {$global:LASTEXITCODE=5;return 'Simulated unsupported operation'}
    switch($cli[0]) {
        '/LIST' { if($power.Exists){"电源方案 GUID: c68ca72b-9c7f-4ad3-931b-67e6ff7aeaa2 ($($power.Name))"};return }
        '/DUPLICATESCHEME' {
            Assert-V122 ($cli[1] -ceq 'e9a42b02-d5df-448d-aa00-03f14749eb61' -and $cli[2] -ceq 'c68ca72b-9c7f-4ad3-931b-67e6ff7aeaa2') 'Duplicate only Ultimate source to managed GUID'
            $power.Exists=$true;return 'Localized output must not be parsed for the new GUID'
        }
        '/CHANGENAME' { Assert-V122 ($cli[2] -ceq 'UnattendLocal Ultimate Performance') 'Name managed plan';return }
        '/SETACTIVE' { Assert-V122 ($cli[1] -ceq 'c68ca72b-9c7f-4ad3-931b-67e6ff7aeaa2') 'Activate only managed plan';return }
        default {throw 'Unexpected powercfg operation'}
    }
}
& $powerBlock
& $powerBlock
Assert-V122 (@($power.Calls|Where-Object {$_[0] -eq '/DUPLICATESCHEME'}).Count -eq 1) 'Repeat execution does not duplicate plans'
Assert-V122 (@($power.Calls|Where-Object {$_[0] -eq '/SETACTIVE'}).Count -eq 2) 'Repeat execution activates managed plan'
$power.Exists=$true;$power.Name='Other user plan';$power.Calls=@()
Expect-V122Failure {& $powerBlock} 'GUID collision stops before modifying another plan'
Assert-V122 ($power.Calls.Count -eq 1) 'Collision permits only listing'
foreach($failure in '/LIST','/DUPLICATESCHEME','/CHANGENAME','/SETACTIVE') {
    $power.Exists=$false;$power.Name='UnattendLocal Ultimate Performance';$power.Calls=@();$power.Failure=$failure
    Expect-V122Failure {& $powerBlock} 'Failed command must stop and report the error'
    if($failure -ne '/SETACTIVE') {Assert-V122 (@($power.Calls|Where-Object {$_[0] -eq '/SETACTIVE'}).Count -eq 0) 'Failure must not silently activate a fallback'}
}
$global:LASTEXITCODE=0
Write-Host "V1.22 templates: $($results.Checks) simulated checks passed. No volumes or power plans were changed."
