# Generic application installation through the WinGet community source.
param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$')]
    [string]$PackageId = '',
    [ValidateRange(1, 900)]
    [int]$ReadyTimeoutSeconds = 300
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($PackageId)) {
    throw 'Set PackageId to the verified WinGet package ID before enabling this template.'
}
$build = [int](Get-CimInstance -ClassName Win32_OperatingSystem -Property BuildNumber).BuildNumber
if ($build -lt 26100) {
    throw 'This template requires Windows 11 24H2 (build 26100) or later.'
}
if ([string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
    throw 'A signed-in user profile is required. Run this script at FirstLogon.'
}
$winget = Join-Path $env:LOCALAPPDATA 'Microsoft\WindowsApps\winget.exe'
$deadline = (Get-Date).AddSeconds($ReadyTimeoutSeconds)
while (-not (Test-Path -LiteralPath $winget -PathType Leaf)) {
    if ((Get-Date) -ge $deadline) {
        throw 'WinGet did not become available before the timeout. Check App Installer and organization policy.'
    }
    Start-Sleep -Seconds 1
}
# Enabling this template accepts the package and source agreements.
# Keep exact ID/source matching, security checks and non-interactive operation.
$arguments = @('install', '--exact', '--id', $PackageId, '--source', 'winget',
    '--scope', 'machine', '--silent', '--no-upgrade', '--disable-interactivity',
    '--accept-package-agreements', '--accept-source-agreements')
$process = Start-Process -FilePath $winget -ArgumentList $arguments -Wait -PassThru -NoNewWindow
if ($process.ExitCode -eq -1978335189) {
    Write-Warning 'WinGet found no applicable update. Check the already installed application if needed.'
} elseif ($process.ExitCode -ne 0) {
    throw ('WinGet installation failed for {0}, exit code {1}. See the WinGet diagnostic logs.' -f $PackageId, $process.ExitCode)
}
