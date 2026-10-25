$ErrorActionPreference = 'Stop'
$templateRoot = Join-Path $PSScriptRoot '../Core/Resources/Templates'
$results = @{ Checks = 0 }
function Assert-Deployment($condition, $message) {
    if (-not $condition) { throw $message }
    $results.Checks++
}

function Test-WinGetBrowser([int]$build = 26100, [int]$readyAfter = 0, [int]$exitCode = 0, [bool]$launchFailure = $false, [string]$package = 'Google.Chrome', [switch]$UseDefault) {
    $state = @{ Starts = 0; Probes = 0; Sleeps = 0; Clock = [datetime]'2026-01-01T00:00:00Z' }
    function Get-CimInstance { param($ClassName, $Property)
        Assert-Deployment ($ClassName -eq 'Win32_OperatingSystem' -and $Property -eq 'BuildNumber') 'Target Windows build must be queried'
        [pscustomobject]@{ BuildNumber = [string]$build }
    }
    function Get-Date { $state.Clock }
    function Test-Path { param($LiteralPath, $PathType)
        Assert-Deployment ($LiteralPath.EndsWith('winget.exe') -and $PathType -eq 'Leaf') 'Probe must select the user WinGet executable'
        $state.Probes++
        return $state.Probes -gt $readyAfter
    }
    function Start-Sleep { param($Seconds)
        $state.Sleeps++
        $state.Clock = $state.Clock.AddSeconds($Seconds)
    }
    function Start-Process { param($FilePath, $ArgumentList, [switch]$Wait, [switch]$PassThru, [switch]$NoNewWindow)
        Assert-Deployment ($Wait -and $PassThru -and $NoNewWindow) 'Install must wait and expose the exit code'
        Assert-Deployment (($ArgumentList -join ' ') -eq ('install --exact --id ' + $package + ' --source winget --scope machine --silent --no-upgrade --disable-interactivity --accept-package-agreements --accept-source-agreements')) 'Installation arguments must preserve exact source, scope, security checks and agreement handling'
        $state.Starts++
        if ($launchFailure) { throw 'Simulated WinGet launch failure' }
        [pscustomobject]@{ ExitCode = $exitCode }
    }
    $oldLocalAppData = $env:LOCALAPPDATA
    try {
        $env:LOCALAPPDATA = [IO.Path]::GetTempPath()
        $failed = $false
        try {
            $options = @{ ReadyTimeoutSeconds = 2 }
            if (-not $UseDefault) { $options.PackageId = $package }
            & (Join-Path $templateRoot 'InstallChromeWinGet.ps1') @options 3>$null
        } catch { $failed = $true }
        $invalidPackage = $package -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$'
        $expectedFailure = $build -lt 26100 -or $readyAfter -gt 2 -or $launchFailure -or $exitCode -notin @(0, -1978335189) -or $invalidPackage
        Assert-Deployment ($failed -eq $expectedFailure) 'Unexpected WinGet template outcome'
        if ($build -lt 26100 -or $readyAfter -gt 2 -or $invalidPackage) {
            Assert-Deployment ($state.Starts -eq 0) 'Unsupported, timed out or invalid configuration must not run an installer'
        } else {
            Assert-Deployment ($state.Starts -eq 1) 'Installer must run exactly once'
        }
        Assert-Deployment ($state.Sleeps -le 2) 'Readiness wait must remain bounded'
    } finally { $env:LOCALAPPDATA = $oldLocalAppData }
}
Test-WinGetBrowser -UseDefault
Test-WinGetBrowser -readyAfter 1
Test-WinGetBrowser -readyAfter 99
Test-WinGetBrowser -build 26000
Test-WinGetBrowser -exitCode -1978335189
Test-WinGetBrowser -exitCode -1978335230
Test-WinGetBrowser -launchFailure $true
Test-WinGetBrowser -package 'Microsoft.PowerToys'
Test-WinGetBrowser -package 'Google.Chrome --ignore-security-hash'

function Test-PublicShortcuts {
    $directory = Join-Path ([IO.Path]::GetTempPath()) ('unattend-shortcut-tests-' + [guid]::NewGuid().ToString('N'))
    $state = @{ Saves = 0; FailSave = $false }
    function New-Object { param($ComObject)
        Assert-Deployment ($ComObject -eq 'WScript.Shell') 'Only Windows shortcut automation may be requested'
        $shell = [pscustomobject]@{}
        $shell | Add-Member -MemberType ScriptMethod -Name CreateShortcut -Value {
            param($path)
            $link = [pscustomobject]@{ Path = $path; TargetPath = '' }
            $link | Add-Member -MemberType ScriptMethod -Name Save -Value {
                $state.Saves++
                [IO.File]::WriteAllText($this.Path, 'FAKE LINK: ' + $this.TargetPath)
                if ($state.FailSave) { throw 'Simulated shortcut save failure' }
            }
            return $link
        }
        return $shell
    }
    $script = Join-Path $templateRoot 'PublicDesktopShortcuts.ps1'
    try {
        $target = Join-Path $directory 'Example App.exe'
        $items = @(@{ Name = 'Example.lnk'; Target = $target }, @{ Name = 'Website.url'; Target = 'https://example.test/path?a=1&b=2' })
        & $script -DesktopDirectory $directory -Shortcuts $items
        Assert-Deployment ($state.Saves -eq 1) 'Program shortcut must be saved through COM exactly once'
        Assert-Deployment ([IO.File]::ReadAllText((Join-Path $directory 'Example.lnk')) -eq ('FAKE LINK: ' + $target)) 'Link target must remain an absolute literal path'
        Assert-Deployment ([IO.File]::ReadAllText((Join-Path $directory 'Website.url')) -eq "[InternetShortcut]`r`nURL=https://example.test/path?a=1&b=2`r`n") 'Web link must retain URL and INI framing'
        [IO.File]::WriteAllText((Join-Path $directory 'Example.lnk'), 'KEEP EXISTING')
        & $script -DesktopDirectory $directory -Shortcuts $items 3>$null
        Assert-Deployment ($state.Saves -eq 1 -and [IO.File]::ReadAllText((Join-Path $directory 'Example.lnk')) -eq 'KEEP EXISTING') 'Rerun must preserve existing items without creating a replacement link'
        $folderCollision = Join-Path $directory 'Folder.lnk'
        $null = [IO.Directory]::CreateDirectory($folderCollision)
        & $script -DesktopDirectory $directory -Shortcuts @(@{ Name = 'Folder.lnk'; Target = $target }) 3>$null
        Assert-Deployment ([IO.Directory]::Exists($folderCollision) -and $state.Saves -eq 1) 'Existing directories must be preserved'
        foreach ($bad in @(
            @{ Name = '../escape.lnk'; Target = $target },
            @{ Name = 'CON.lnk'; Target = $target },
            @{ Name = 'Bad.lnk '; Target = $target },
            @{ Name = 'Bad.exe'; Target = $target },
            @{ Name = 'Bad.url'; Target = 'javascript:alert(1)' },
            @{ Name = 'Bad.url'; Target = "https://example.test/`r`nIconFile=bad" },
            @{ Name = 'Bad.lnk'; Target = 'relative.exe' },
            @{ Name = 'Bad.lnk'; Target = 'C:relative.exe' },
            @{ Name = 'Bad.lnk'; Target = '\rooted.exe' }
        )) {
            $failed = $false
            try { & $script -DesktopDirectory $directory -Shortcuts @(@{ Name = 'MustNotExist.url'; Target = 'https://example.test' }, $bad) } catch { $failed = $true }
            Assert-Deployment ($failed -and -not [IO.File]::Exists((Join-Path $directory 'MustNotExist.url'))) 'Invalid list must be rejected before writing earlier entries'
        }
        $failed = $false
        try { & $script -DesktopDirectory $directory -Shortcuts @(@{ Name = 'Same.url'; Target = 'https://example.test' }, @{ Name = 'SAME.url'; Target = 'https://example.test' }) } catch { $failed = $true }
        Assert-Deployment ($failed -and -not [IO.File]::Exists((Join-Path $directory 'Same.url'))) 'Case-insensitive duplicate names must fail before file creation'
        $failed = $false
        try { & $script -DesktopDirectory 'C:relative' -Shortcuts $items } catch { $failed = $true }
        Assert-Deployment $failed 'Drive-relative desktop directories must be rejected'
        $state.FailSave = $true
        $failed = $false
        try { & $script -DesktopDirectory $directory -Shortcuts @(@{ Name = 'Failed.lnk'; Target = $target }) } catch { $failed = $true }
        Assert-Deployment ($failed -and -not [IO.File]::Exists((Join-Path $directory 'Failed.lnk'))) 'COM failure must not install an incomplete shortcut'
        Assert-Deployment (@([IO.Directory]::GetFiles($directory, '.unattend-shortcut-*')).Count -eq 0) 'Temporary shortcut files must be cleaned after success and failure'
    } finally {
        if ([IO.Directory]::Exists($directory)) { [IO.Directory]::Delete($directory, $true) }
    }
}
Test-PublicShortcuts
Write-Host "Deployment templates: $($results.Checks) simulated checks passed. No applications were installed or actual desktop shortcuts changed."
