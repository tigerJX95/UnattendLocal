# Set the installed system volume label; does not format or repartition disks.
$ErrorActionPreference = 'Stop'
$label = 'Windows'
if ([string]::IsNullOrWhiteSpace($label) -or $label.Length -gt 32 -or $label -match '[\\/:*?"<>|\x00-\x1f]') {
    throw 'Use a non-empty NTFS volume label of at most 32 characters without path or control characters.'
}
if ($env:SystemDrive -notmatch '^[A-Za-z]:$') { throw 'Cannot identify the installed Windows system drive.' }
$letter = $env:SystemDrive.Substring(0, 1)
$volumes = @(Get-Volume -DriveLetter $letter -ErrorAction Stop)
if ($volumes.Count -ne 1 -or $volumes[0].FileSystem -ne 'NTFS') {
    throw 'The Windows system drive must resolve to one NTFS volume.'
}
if ($volumes[0].FileSystemLabel -cne $label) {
    Set-Volume -DriveLetter $letter -NewFileSystemLabel $label -ErrorAction Stop
}
