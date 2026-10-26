# Generic local EXE installation. Configure all three values before enabling.
$ErrorActionPreference = 'Stop'
$installerPath = '' # Absolute path on the target Windows computer, including .exe.
$expectedSha256 = '' # SHA256 of the installer obtained from a trusted source.
$silentArguments = '' # Vendor-documented silent and no-restart arguments.
if ($installerPath -notmatch '^[A-Za-z]:[\\/].+\.exe$') {
    throw 'Set installerPath to an absolute local Windows EXE path.'
}
if ($expectedSha256 -notmatch '^[a-fA-F0-9]{64}$') {
    throw 'Set expectedSha256 to the SHA256 of your trusted installer.'
}
if ([string]::IsNullOrWhiteSpace($silentArguments)) {
    throw 'Set silentArguments to the silent installation arguments documented by the software vendor.'
}
if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    throw 'The configured EXE installer is unavailable on the target Windows computer.'
}
$directory = Join-Path ([IO.Path]::GetTempPath()) ('UnattendLocal-EXE-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -Path $directory -ItemType Directory -ErrorAction Stop
try {
    $file = Join-Path $directory 'installer.exe'
    Copy-Item -LiteralPath $installerPath -Destination $file -ErrorAction Stop
    $actual = (Get-FileHash -LiteralPath $file -Algorithm SHA256 -ErrorAction Stop).Hash
    if ($actual -ine $expectedSha256) { throw 'Installer SHA256 does not match. Nothing was executed.' }
    $process = Start-Process -FilePath $file -ArgumentList $silentArguments -Wait -PassThru -ErrorAction Stop
    if ($process.ExitCode -notin @(0, 3010)) { throw ('EXE installer failed, exit code {0}.' -f $process.ExitCode) }
    if ($process.ExitCode -eq 3010) { Write-Warning 'Installation requires a later restart.' }
} finally {
    Remove-Item -LiteralPath $directory -Recurse -Force -ErrorAction SilentlyContinue
}
