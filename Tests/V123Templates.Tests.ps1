$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '../Core/Resources/Templates'
$results = @{ Checks = 0 }
function Assert-V123($value, $message) { if (-not $value) { throw $message }; $results.Checks++ }
foreach ($name in 'InstallLocalExe','InstallWinGet','FirewallDroppedLog') {
    $tokens = $null; $errors = $null
    [void][Management.Automation.Language.Parser]::ParseFile((Join-Path $root "$name.ps1"),[ref]$tokens,[ref]$errors)
    Assert-V123 ($errors.Count -eq 0) 'Templates must parse'
}
$exeText = Get-Content -LiteralPath (Join-Path $root 'InstallLocalExe.ps1') -Raw
$exeText = $exeText.Replace("`$installerPath = ''", '$installerPath = $testPath').Replace("`$expectedSha256 = ''", '$expectedSha256 = $testHash').Replace("`$silentArguments = ''", '$silentArguments = $testArguments')
$exe = [scriptblock]::Create($exeText)
function Run-Exe([string]$testPath='D:\Install\Setup [1].exe', [string]$testHash=('a'*64), [string]$actualHash=('a'*64), [string]$testArguments='/S', [int]$exitCode=0, [bool]$available=$true, [bool]$copyFail=$false) {
    $state=@{Installs=0;Copies=0;Cleanups=0;Failed=$false;Error=''}
    function Test-Path { param($LiteralPath,$PathType)
        Assert-V123 ($LiteralPath -ceq $testPath -and $PathType -eq 'Leaf') 'Check configured literal file path'
        return $available
    }
    function New-Item { [CmdletBinding()]param($Path,$ItemType) }
    function Copy-Item { [CmdletBinding()]param($LiteralPath,$Destination)
        Assert-V123 ($LiteralPath -ceq $testPath) 'Copy exactly configured source, including spaces and brackets'
        $state.Copies++;if($copyFail){throw 'simulated copy failure'}
    }
    function Get-FileHash { [CmdletBinding()]param($LiteralPath,$Algorithm)
        Assert-V123 ($Algorithm -ceq 'SHA256' -and $LiteralPath -match 'UnattendLocal-EXE-') 'Hash private copy with SHA256'
        [pscustomobject]@{Hash=$actualHash}
    }
    function Start-Process { [CmdletBinding()]param($FilePath,$ArgumentList,[switch]$Wait,[switch]$PassThru)
        Assert-V123 ($Wait -and $PassThru -and $ArgumentList -ceq $testArguments) 'Pass vendor arguments intact and wait'
        Assert-V123 ($FilePath -match 'UnattendLocal-EXE-[a-f0-9]{32}') 'Run only private copy'
        $state.Installs++;[pscustomobject]@{ExitCode=$exitCode}
    }
    function Remove-Item { [CmdletBinding()]param($LiteralPath,[switch]$Recurse,[switch]$Force)
        Assert-V123 ($LiteralPath -match 'UnattendLocal-EXE-[a-f0-9]{32}$') 'Cleanup is scoped'
        $state.Cleanups++
    }
    try { & $exe 3>$null 6>$null } catch { $state.Failed=$true;$state.Error=$_.Exception.Message }
    return $state
}
foreach($arg in @('/S','/quiet /norestart','/VERYSILENT /NORESTART /DIR="C:\Example App"')) {
    $s=Run-Exe -testArguments $arg
    Assert-V123 (-not $s.Failed -and $s.Installs -eq 1 -and $s.Cleanups -eq 1) 'Generic EXE supports vendor-specific argument forms'
}
$s=Run-Exe -testHash ('A'*64)
Assert-V123 (-not $s.Failed -and $s.Installs -eq 1) 'Hash comparison is case-insensitive'
foreach($hash in @('', ' ', ('a'*63), ('g'*64), ('a'*65))) {
    $s=Run-Exe -testHash $hash
    Assert-V123 ($s.Failed -and $s.Installs -eq 0 -and $s.Copies -eq 0) 'Invalid hash stops before file operations'
}
foreach($path in @('', 'installer.exe','D:\Setup.msi','\\server\share\Setup.exe')) {
    $s=Run-Exe -testPath $path
    Assert-V123 ($s.Failed -and $s.Installs -eq 0 -and $s.Copies -eq 0) 'Require configured local EXE path'
}
foreach($arg in @('', ' ')) {
    $s=Run-Exe -testArguments $arg
    Assert-V123 ($s.Failed -and $s.Installs -eq 0 -and $s.Copies -eq 0) 'Do not launch an interactive installer with absent arguments'
}
$s=Run-Exe -available $false
Assert-V123 ($s.Failed -and $s.Installs -eq 0) 'Missing source never executes'
$s=Run-Exe -actualHash ('b'*64)
Assert-V123 ($s.Failed -and $s.Installs -eq 0 -and $s.Cleanups -eq 1) 'Hash mismatch never executes and cleans copy'
$s=Run-Exe -exitCode 3010
Assert-V123 (-not $s.Failed -and $s.Cleanups -eq 1) 'Restart-required status does not reboot'
$s=Run-Exe -exitCode 5
Assert-V123 ($s.Failed -and $s.Installs -eq 1 -and $s.Cleanups -eq 1) 'Installation errors surface and clean up'
$s=Run-Exe -copyFail $true
Assert-V123 ($s.Failed -and $s.Installs -eq 0 -and $s.Cleanups -eq 1) 'Incomplete copy never executes'

$firewallText=Get-Content -LiteralPath (Join-Path $root 'FirewallDroppedLog.ps1') -Raw
$firewall=[scriptblock]::Create($firewallText.Replace('$logMaxSizeKilobytes = 16384', '$logMaxSizeKilobytes = $testSize'))
function Run-Firewall([int]$testSize=16384,[bool]$writeFail=$false,[bool]$readMismatch=$false,[bool]$missing=$false) {
    $state=@{Writes=0;Failed=$false;Error=''}
    function Set-NetFirewallProfile { [CmdletBinding()]param($Name,$PolicyStore,$LogBlocked,$LogMaxSizeKilobytes)
        Assert-V123 (($Name -join ',') -ceq 'Domain,Private,Public') 'Configure exactly all three profiles'
        Assert-V123 ($PolicyStore -ceq 'PersistentStore' -and $LogBlocked -eq 'True') 'Only write local logging'
        $state.Writes++;if($writeFail){throw 'simulated firewall failure'}
    }
    function Get-NetFirewallProfile { [CmdletBinding()]param($Name,$PolicyStore)
        foreach($n in $Name) {
            if($missing -and $n -eq 'Public'){continue}
            [pscustomobject]@{Name=$n;LogBlocked= $(if($readMismatch){'False'}else{'True'});LogMaxSizeKilobytes=$testSize}
        }
    }
    try { & $firewall 6>$null }catch{$state.Failed=$true;$state.Error=$_.Exception.Message}
    return $state
}
foreach($size in @(1,8192,16384,32767)) {
    $s=Run-Firewall -testSize $size
    Assert-V123 (-not $s.Failed -and $s.Writes -eq 1) 'Valid log size and successful readback'
}
foreach($size in @(0,-1,32768)) {
    $s=Run-Firewall -testSize $size
    Assert-V123 ($s.Failed -and $s.Writes -eq 0) 'Out-of-range size stops before writes'
}
$s=Run-Firewall -writeFail $true;Assert-V123 $s.Failed 'Firewall service failure surfaces'
$s=Run-Firewall -readMismatch $true;Assert-V123 $s.Failed 'Readback mismatch surfaces'
$s=Run-Firewall -missing $true;Assert-V123 $s.Failed 'Missing profile surfaces'
Write-Host "V1.23 templates: $($results.Checks) simulated checks passed. No installers or firewall commands were executed."
