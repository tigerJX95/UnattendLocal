# Simulate file operations and Windows Installer. Never install software.
$ErrorActionPreference = 'Stop'
$results = @{ Checks = 0 }
function Assert-Msi($value, $message) { if (-not $value) { throw $message }; $results.Checks++ }
$path = Join-Path $PSScriptRoot '../Core/Resources/Templates/InstallLocalMsi.ps1'
$tokens = $null; $errors = $null
[void][Management.Automation.Language.Parser]::ParseFile($path,[ref]$tokens,[ref]$errors)
Assert-Msi ($errors.Count -eq 0) 'MSI template parses'
$text = Get-Content -LiteralPath $path -Raw
$script = [scriptblock]::Create($text.Replace("`$installerPath = ''", '$installerPath = $testPath').Replace("`$expectedSha256 = ''", '$expectedSha256 = $testHash'))
$oldSystemRoot = $env:SystemRoot
# A POSIX path lets the cross-platform simulator use Join-Path without a C: drive.
$env:SystemRoot = [IO.Path]::GetTempPath()
try {
    function Run-Msi([string]$testPath='D:\Install\Package [1] 中文.msi', [string]$testHash=('a'*64), [string]$actualHash=('a'*64), [int]$exitCode=0, [bool]$available=$true, [bool]$engine=$true, [bool]$copyFail=$false, [bool]$hashFail=$false, [bool]$launchFail=$false) {
        $state=@{Copies=0;Installs=0;Cleanups=0;Failed=$false;Message=''}
        function Test-Path { param($LiteralPath,$PathType)
            Assert-Msi ($PathType -eq 'Leaf') 'Only files accepted'
            if ($LiteralPath -ceq $testPath) { return $available }
            Assert-Msi ($LiteralPath -match 'System32[\\/]msiexec\.exe$') 'Check system Windows Installer'
            return $engine
        }
        function New-Item { [CmdletBinding()]param($Path,$ItemType)
            Assert-Msi ($Path -match 'UnattendLocal-MSI-[a-f0-9]{32}$' -and $ItemType -eq 'Directory') 'Unique private staging directory'
        }
        function Copy-Item { [CmdletBinding()]param($LiteralPath,$Destination)
            Assert-Msi ($LiteralPath -ceq $testPath -and $Destination -match 'installer\.msi$') 'Copy literal source only'
            $state.Copies++;if($copyFail){throw 'simulated copy failure'}
        }
        function Get-FileHash { [CmdletBinding()]param($LiteralPath,$Algorithm)
            Assert-Msi ($LiteralPath -match 'UnattendLocal-MSI-.+[\\/]installer\.msi$' -and $Algorithm -eq 'SHA256') 'Hash copied MSI before launch'
            if($hashFail){throw 'simulated hash failure'}
            [pscustomobject]@{Hash=$actualHash}
        }
        function Start-Process { [CmdletBinding()]param($FilePath,$ArgumentList,[switch]$Wait,[switch]$PassThru)
            Assert-Msi ($FilePath -match 'System32[\\/]msiexec\.exe$') 'Use the system Windows Installer, never the MSI as executable'
            Assert-Msi ($Wait -and $PassThru) 'Wait and inspect exit status'
            Assert-Msi ($ArgumentList -match '^/i "[^"]+UnattendLocal-MSI-[a-f0-9]{32}[\\/]installer\.msi" /qn /norestart REBOOT=ReallySuppress$') 'Quote private MSI and suppress UI and restarts'
            $state.Installs++;if($launchFail){throw 'simulated launch failure'}
            [pscustomobject]@{ExitCode=$exitCode}
        }
        function Remove-Item { [CmdletBinding()]param($LiteralPath,[switch]$Recurse,[switch]$Force)
            Assert-Msi ($LiteralPath -match 'UnattendLocal-MSI-[a-f0-9]{32}$' -and $Recurse -and $Force) 'Cleanup only private staging directory'
            $state.Cleanups++
        }
        try { & $script 3>$null } catch { $state.Failed=$true;$state.Message=$_.Exception.Message }
        return $state
    }
    foreach($p in @('D:\Install\Package [1] 中文.msi',"D:\Install\O'Brien.msi",'C:\Packages\A.MSI')) {
        $s=Run-Msi -testPath $p
        Assert-Msi (-not $s.Failed -and $s.Installs -eq 1 -and $s.Cleanups -eq 1) 'Spaces, brackets, Unicode, apostrophes and extension case remain literal'
    }
    $s=Run-Msi -testHash ('A'*64)
    Assert-Msi (-not $s.Failed) 'SHA256 is case insensitive'
    foreach($p in @('', 'setup.msi','D:\Setup.exe','\\server\share\Setup.msi','D:\Setup.msi:other.msi','D:\Bad" /x bad.msi',"D:\Bad`nname.msi")) {
        $s=Run-Msi -testPath $p
        Assert-Msi ($s.Failed -and $s.Copies -eq 0 -and $s.Installs -eq 0) 'Reject unsupported or malformed paths before file operations'
    }
    foreach($h in @('',('a'*63),('a'*65),('g'*64))) {
        $s=Run-Msi -testHash $h
        Assert-Msi ($s.Failed -and $s.Copies -eq 0 -and $s.Installs -eq 0) 'Require trusted complete SHA256'
    }
    foreach($flags in @(@{available=$false},@{engine=$false})) {
        $s=Run-Msi @flags
        Assert-Msi ($s.Failed -and $s.Copies -eq 0 -and $s.Installs -eq 0) 'Missing package or engine never executes'
    }
    foreach($flags in @(@{actualHash=('b'*64)},@{copyFail=$true},@{hashFail=$true})) {
        $s=Run-Msi @flags
        Assert-Msi ($s.Failed -and $s.Installs -eq 0 -and $s.Cleanups -eq 1) 'Copy/hash failures never execute and clean staging'
    }
    $s=Run-Msi -exitCode 3010
    Assert-Msi (-not $s.Failed -and $s.Installs -eq 1 -and $s.Cleanups -eq 1) 'Restart-required status does not reboot'
    foreach($code in @(1603,1618,1619,1641)) {
        $s=Run-Msi -exitCode $code
        Assert-Msi ($s.Failed -and $s.Installs -eq 1 -and $s.Cleanups -eq 1 -and $s.Message.Contains("$code")) 'Nonstandard/error results surface without retry'
    }
    $s=Run-Msi -launchFail $true
    Assert-Msi ($s.Failed -and $s.Cleanups -eq 1) 'Launch failure cleans staging'
    Write-Host ('V1.26 MSI: {0} simulated checks passed. No software was installed.' -f $results.Checks)
} finally { $env:SystemRoot = $oldSystemRoot }
