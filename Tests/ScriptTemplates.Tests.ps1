$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '../Core/Resources/Templates'
$testState = @{ Checks = 0 }
function Assert($condition, $message) {
    if (-not $condition) { throw $message }
    $testState.Checks++
}
foreach ($file in Get-ChildItem -LiteralPath $root -Filter '*.ps1') {
    $tokens = $null; $errors = $null
    $null = [System.Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$errors)
    Assert ($errors.Count -eq 0) ('PowerShell parse error: ' + $file.Name)
}
# All OS/download/install operations are replaced in this local scope.
Write-Host 'Script template simulation: expected installation warnings are captured; no software is installed.'
function Test-VcRedist([string]$architecture, [string]$status = 'Valid', [string]$subject = 'CN=Microsoft Corporation, O=Microsoft Corporation, C=US', [int]$exitCode = 0, [bool]$downloadFailure = $false) {
    $state = @{ Downloads = @(); Installs = @(); Cleanups = 0 }
    function New-Item { param($Path, $ItemType) }
    function Invoke-WebRequest { param($Uri, $OutFile, [switch]$UseBasicParsing, $TimeoutSec)
        $state.Downloads += $Uri
        if ($downloadFailure) { throw 'Simulated download failure' }
    }
    function Get-AuthenticodeSignature { param($LiteralPath)
        [pscustomobject]@{ Status = $status; SignerCertificate = [pscustomobject]@{ Subject = $subject } }
    }
    function Start-Process { param($FilePath, $ArgumentList, [switch]$Wait, [switch]$PassThru)
        Assert ($Wait -and $PassThru -and $ArgumentList -eq '/install /quiet /norestart') 'Installer must wait and avoid restarting'
        $state.Installs += $FilePath
        [pscustomobject]@{ ExitCode = $exitCode }
    }
    function Remove-Item { param($LiteralPath, [switch]$Recurse, [switch]$Force, $ErrorAction)
        Assert ($LiteralPath -match 'UnattendLocal-VC-[a-f0-9]{32}$') 'Cleanup must only target the private installer directory'
        $state.Cleanups++
    }
    $oldArchitecture = $env:PROCESSOR_ARCHITECTURE; $oldWow64 = $env:PROCESSOR_ARCHITEW6432
    try {
        $env:PROCESSOR_ARCHITECTURE = 'x86'; $env:PROCESSOR_ARCHITEW6432 = $architecture
        $failed = $false
        try { & (Join-Path $root 'VcRedist.ps1') 3>$null 6>$null } catch { $failed = $true ; $caught = $_.Exception.Message }
        $expectedFailure = $status -ne 'Valid' -or $subject -notmatch 'O=Microsoft Corporation,' -or $exitCode -notin @(0, 3010, 1638) -or $downloadFailure -or $architecture -eq 'unknown'
        Assert ($failed -eq $expectedFailure) ('Unexpected VC++ result: ' + $architecture + '/' + $status + '/' + $exitCode)
        if ($architecture -ne 'unknown') { Assert ($state.Cleanups -eq 1) 'Installer files must be cleaned after success and failure' }
        if ($status -ne 'Valid' -or $subject -notmatch 'O=Microsoft Corporation,' -or $downloadFailure) {
            Assert ($state.Installs.Count -eq 0) 'Unverified or incomplete download must never run'
        } elseif (-not $expectedFailure) {
            $expectedPackages = switch ($architecture) { 'AMD64' { 'x86'; 'x64' } 'ARM64' { 'x86'; 'arm64' } 'x86' { 'x86' } }
            Assert ($state.Installs.Count -eq @($expectedPackages).Count) 'Wrong installer count'
            foreach ($package in $expectedPackages) {
                Assert ($state.Downloads -contains ('https://aka.ms/vc14/vc_redist.' + $package + '.exe')) 'Incorrect Microsoft package URL'
            }
        }
    } finally {
        $env:PROCESSOR_ARCHITECTURE = $oldArchitecture; $env:PROCESSOR_ARCHITEW6432 = $oldWow64
    }
}
foreach ($architecture in 'AMD64', 'ARM64', 'x86') { Test-VcRedist $architecture }
Test-VcRedist 'AMD64' -exitCode 3010
Test-VcRedist 'AMD64' -exitCode 1638
Test-VcRedist 'AMD64' -exitCode 1603
Test-VcRedist 'AMD64' -status 'NotSigned'
Test-VcRedist 'AMD64' -subject 'CN=Other, O=Other, C=US'
Test-VcRedist 'AMD64' -downloadFailure $true
Test-VcRedist 'unknown'

function Test-AutoRun {
    $state = @{ Values = @{} }
    function New-Item { param($Path, [switch]$Force)
        Assert ($Path -eq 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer') 'Incorrect AutoRun policy path'
    }
    function New-ItemProperty { param($LiteralPath, $Name, $PropertyType, $Value, [switch]$Force)
        Assert ($PropertyType -eq 'DWord' -and $Force) 'Incorrect policy type'
        $state.Values[$Name] = $Value
    }
    & (Join-Path $root 'DisableAutoRun.ps1')
    Assert ($state.Values['NoDriveAutoRun'] -eq 0x03ffffff -and $state.Values['NoDriveTypeAutoRun'] -eq 0xff) 'AutoRun policy must cover all drives and types'
}
Test-AutoRun
function Test-NetFx3([bool]$enabled = $false, [bool]$media = $true, [bool]$installFailure = $false) {
    $state = @{ Installs = 0; Queries = 0 }
    function Get-WindowsOptionalFeature { param([switch]$Online, $FeatureName)
        $state.Queries++
        Assert ($Online -and $FeatureName -eq 'NetFx3') 'Incorrect feature query'
        [pscustomobject]@{ State = $(if ($enabled) { 'Enabled' } else { 'Disabled' }) }
    }
    function Test-Path { param($LiteralPath, $PathType) return $media }
    function Get-ChildItem { param($LiteralPath, $Filter, [switch]$File, $ErrorAction)
        Assert ($Filter -eq 'microsoft-windows-netfx3-ondemand-package~*.cab' -and $File) 'Local NetFx3 package must be present'
        [pscustomobject]@{ Name = 'matching.cab' }
    }
    function Enable-WindowsOptionalFeature { param([switch]$Online, $FeatureName, [switch]$All, $Source, [switch]$LimitAccess, [switch]$NoRestart, $ErrorAction)
        Assert ($Online -and $All -and $LimitAccess -and $NoRestart -and $FeatureName -eq 'NetFx3') 'Offline install must block Windows Update and avoid restart'
        $state.Installs++
        if ($installFailure) { throw 'Simulated source mismatch' }
        [pscustomobject]@{ RestartNeeded = $true }
    }
    $failed = $false
    try { & (Join-Path $root 'NetFx3.ps1') 3>$null 6>$null } catch { $failed = $true }
    if ([Environment]::OSVersion.Version.Build -ge 28000) {
        Assert ($failed -and $state.Queries -eq 0 -and $state.Installs -eq 0) 'Unsupported Windows build must stop before feature modification'
    } else {
        Assert ($failed -eq (-not $enabled -and (-not $media -or $installFailure))) 'Unexpected offline NetFx3 result'
        Assert ($(if ($enabled -or -not $media) { $state.Installs -eq 0 } else { $state.Installs -gt 0 })) 'Unexpected NetFx3 install count'
    }
}
Test-NetFx3
Test-NetFx3 -enabled $true
Test-NetFx3 -media $false
Test-NetFx3 -installFailure $true

function Test-LocalPing([string]$conflict = '', [bool]$failWrite = $false) {
    $state = @{ Rules = @{}; Creates = 0; Updates = 0 }
    if ($conflict) { $state.Rules[$conflict] = [pscustomobject]@{ Name = $conflict; Group = 'Other application' } }
    function Get-NetFirewallRule { param($PolicyStore, $Name, $ErrorAction)
        Assert ($PolicyStore -eq 'PersistentStore') 'Only local persistent rules may be queried'
        if ($state.Rules.ContainsKey($Name)) { $state.Rules[$Name] }
    }
    function Assert-PingOptions($options) {
        Assert ($options.PolicyStore -eq 'PersistentStore' -and $options.Direction -eq 'Inbound' -and $options.Action -eq 'Allow' -and $options.Enabled -eq 'True') 'Incorrect rule scope or direction'
        Assert ((@($options.Profile) -join ',') -eq 'Domain,Private') 'Ping rules must exclude Public profile'
        Assert ($options.RemoteAddress -eq 'LocalSubnet' -and $options.EdgeTraversalPolicy -eq 'Block') 'Ping sources must stay on the local subnet'
        Assert (($options.Protocol -eq 'ICMPv4' -and $options.IcmpType -eq '8' -and $options.Name -eq 'UnattendLocal-ICMPv4-Echo') -or ($options.Protocol -eq 'ICMPv6' -and $options.IcmpType -eq '128' -and $options.Name -eq 'UnattendLocal-ICMPv6-Echo')) 'Only echo-request types may be allowed'
    }
    function New-NetFirewallRule {
        param($PolicyStore, $Name, $Direction, $Action, $Enabled, $Profile, $Protocol, $IcmpType, $RemoteAddress, $EdgeTraversalPolicy, $DisplayName, $Group)
        Assert-PingOptions $PSBoundParameters
        Assert ($Group -eq 'UnattendLocal' -and -not $state.Rules.ContainsKey($Name)) 'New rule must have unique name and ownership group'
        if ($failWrite) { throw 'Simulated firewall write failure' }
        $state.Rules[$Name] = [pscustomobject]@{ Name = $Name; Group = $Group }
        $state.Creates++
    }
    function Set-NetFirewallRule {
        param($PolicyStore, $Name, $Direction, $Action, $Enabled, $Profile, $Protocol, $IcmpType, $RemoteAddress, $EdgeTraversalPolicy)
        Assert-PingOptions $PSBoundParameters
        Assert ($state.Rules[$Name].Group -eq 'UnattendLocal') 'Updates must be restricted to owned rules'
        $state.Updates++
    }
    $failed = $false
    try { & (Join-Path $root 'AllowLocalPing.ps1') } catch { $failed = $true }
    Assert ($failed -eq [bool]($conflict -or $failWrite)) 'Unexpected Ping template outcome'
    if ($conflict) {
        Assert ($state.Creates -eq 0 -and $state.Updates -eq 0 -and $state.Rules.Count -eq 1) 'All name collisions must be detected before changing rules'
    } elseif ($failWrite) {
        Assert ($state.Creates -eq 0 -and $state.Updates -eq 0) 'Write failure must stop template execution'
    } else {
        Assert ($state.Creates -eq 2 -and $state.Rules.Count -eq 2) 'IPv4 and IPv6 rules must both be created'
        & (Join-Path $root 'AllowLocalPing.ps1')
        Assert ($state.Creates -eq 2 -and $state.Updates -eq 2 -and $state.Rules.Count -eq 2) 'Rerun must update owned rules without duplicates'
    }
}
Test-LocalPing
Test-LocalPing -conflict 'UnattendLocal-ICMPv4-Echo'
Test-LocalPing -conflict 'UnattendLocal-ICMPv6-Echo'
Test-LocalPing -failWrite $true

function Test-RemoteAssistance {
    $state = @{ Writes = 0 }
    function New-Item { param($Path, [switch]$Force)
        Assert ($Path -eq 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Remote Assistance' -and $Force) 'Only the Remote Assistance key may be created'
    }
    function New-ItemProperty { param($LiteralPath, $Name, $PropertyType, $Value, [switch]$Force)
        Assert ($LiteralPath -eq 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Remote Assistance') 'Incorrect Remote Assistance path'
        Assert ($Name -eq 'fAllowToGetHelp' -and $PropertyType -eq 'DWord' -and $Value -eq 0 -and $Force) 'Only solicited Remote Assistance toggle may change'
        $state.Writes++
    }
    & (Join-Path $root 'DisableRemoteAssistance.ps1')
    Assert ($state.Writes -eq 1) 'Remote Assistance toggle must be written exactly once'
}
Test-RemoteAssistance
Write-Host "Script template PowerShell: $($testState.Checks) checks passed. No installers or Windows policies were changed."
