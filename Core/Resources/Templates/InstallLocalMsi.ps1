# Generic single-file MSI installation. Configure both values before enabling.
$ErrorActionPreference = 'Stop'
$installerPath = '' # Absolute local path on the target Windows computer, including .msi.
$expectedSha256 = '' # SHA256 of the MSI obtained from a trusted source.
if ($installerPath -notmatch '^[A-Za-z]:[\\/][^<>:"|?*\x00-\x1F]+\.msi$') {
    throw 'Set installerPath to an absolute local Windows MSI path.'
}
if ($expectedSha256 -notmatch '^[a-fA-F0-9]{64}$') {
    throw 'Set expectedSha256 to the SHA256 of your trusted MSI.'
}
if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    throw 'The configured MSI is unavailable on the target Windows computer.'
}
if ([string]::IsNullOrWhiteSpace($env:SystemRoot)) { throw 'The Windows system directory is unavailable.' }
$windowsInstaller = Join-Path $env:SystemRoot 'System32\msiexec.exe'
if (-not (Test-Path -LiteralPath $windowsInstaller -PathType Leaf)) {
    throw 'Windows Installer is unavailable on the target Windows computer.'
}
$directory = Join-Path ([IO.Path]::GetTempPath()) ('UnattendLocal-MSI-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -Path $directory -ItemType Directory -ErrorAction Stop
try {
    $file = Join-Path $directory 'installer.msi'
    Copy-Item -LiteralPath $installerPath -Destination $file -ErrorAction Stop
    $actual = (Get-FileHash -LiteralPath $file -Algorithm SHA256 -ErrorAction Stop).Hash
    if ($actual -ine $expectedSha256) { throw 'MSI SHA256 does not match. Nothing was executed.' }
    # Only the verified private copy is passed as a quoted argument. No shell is used.
    $arguments = '/i "{0}" /qn /norestart REBOOT=ReallySuppress' -f $file
    $process = Start-Process -FilePath $windowsInstaller -ArgumentList $arguments -Wait -PassThru -ErrorAction Stop
    if ($process.ExitCode -notin @(0, 3010)) { throw ('MSI installation failed, exit code {0}.' -f $process.ExitCode) }
    if ($process.ExitCode -eq 3010) { Write-Warning 'Installation requires a later restart.' }
} finally {
    Remove-Item -LiteralPath $directory -Recurse -Force -ErrorAction SilentlyContinue
}
