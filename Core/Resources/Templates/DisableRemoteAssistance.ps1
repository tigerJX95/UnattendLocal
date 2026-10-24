# UnattendLocal template: disable solicited Windows Remote Assistance.
$ErrorActionPreference = 'Stop'
$path = 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Remote Assistance'
$null = New-Item -Path $path -Force
$null = New-ItemProperty -LiteralPath $path -Name fAllowToGetHelp -PropertyType DWord -Value 0 -Force
