# UnattendLocal template: disable AutoRun for every drive type and drive letter.
$ErrorActionPreference = 'Stop'
$path = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer'
$null = New-Item -Path $path -Force
$null = New-ItemProperty -LiteralPath $path -Name NoDriveAutoRun -PropertyType DWord -Value 0x03ffffff -Force
$null = New-ItemProperty -LiteralPath $path -Name NoDriveTypeAutoRun -PropertyType DWord -Value 0xff -Force
