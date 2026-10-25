# UnattendLocal template: editable public desktop shortcuts, keeping existing files.
param(
    [string]$DesktopDirectory = [Environment]::GetFolderPath('CommonDesktopDirectory'),
    [object[]]$Shortcuts = @(
        @{ Name = 'System Properties.lnk'; Target = (Join-Path $env:SystemRoot 'System32\SystemPropertiesAdvanced.exe') },
        @{ Name = 'UnattendLocal Releases.url'; Target = 'https://github.com/tigerJX95/UnattendLocal/releases/latest' }
    )
)
$ErrorActionPreference = 'Stop'
function Test-UnattendAbsolutePath([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return $false }
    if ([IO.Path]::DirectorySeparatorChar -eq '\') {
        # Reject drive-relative C:foo and current-drive-rooted \foo paths.
        return $Value -match '^(?:[A-Za-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+(?:[\\/]|$))'
    }
    return [IO.Path]::IsPathRooted($Value)
}
if (-not (Test-UnattendAbsolutePath $DesktopDirectory)) {
    throw 'An absolute public desktop directory is required.'
}
$names = @{}
# Validate the entire definition list before creating files.
foreach ($item in $Shortcuts) {
    $name = [string]$item.Name
    $target = [string]$item.Target
    if ([string]::IsNullOrWhiteSpace($name) -or $name.Length -gt 120 -or
        $name -match '[<>:"/\\|?*\x00-\x1f]' -or $name -match '[. ]$' -or
        $name -match '^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)' -or
        [IO.Path]::GetExtension($name).ToLowerInvariant() -notin @('.lnk', '.url')) {
        throw ('Invalid shortcut filename: ' + $name)
    }
    if ($names.ContainsKey($name)) { throw ('Duplicate shortcut filename: ' + $name) }
    $names[$name] = $true
    if ([string]::IsNullOrWhiteSpace($target) -or $target -match '[\x00-\x1f]') {
        throw ('Invalid shortcut target: ' + $name)
    }
    if ([IO.Path]::GetExtension($name) -ieq '.url') {
        $uri = $null
        if (-not [uri]::TryCreate($target, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -notin @('https', 'http')) {
            throw ('Web shortcuts require an absolute HTTP or HTTPS URL: ' + $name)
        }
    } elseif (-not (Test-UnattendAbsolutePath ([Environment]::ExpandEnvironmentVariables($target)))) {
        throw ('Program shortcuts require an absolute target path: ' + $name)
    }
}
$null = [IO.Directory]::CreateDirectory($DesktopDirectory)
$shell = $null
foreach ($item in $Shortcuts) {
    $destination = Join-Path $DesktopDirectory $item.Name
    if ([IO.File]::Exists($destination) -or [IO.Directory]::Exists($destination)) {
        Write-Warning ('Keeping existing desktop item: ' + $item.Name)
        continue
    }
    $extension = [IO.Path]::GetExtension($item.Name).ToLowerInvariant()
    $temporary = Join-Path $DesktopDirectory ('.unattend-shortcut-' + [guid]::NewGuid().ToString('N') + $extension)
    try {
        if ($extension -eq '.url') {
            [IO.File]::WriteAllText($temporary, "[InternetShortcut]`r`nURL=$($item.Target)`r`n", [Text.UTF8Encoding]::new($false))
        } else {
            if ($null -eq $shell) { $shell = New-Object -ComObject WScript.Shell }
            $shortcut = $shell.CreateShortcut($temporary)
            $shortcut.TargetPath = [Environment]::ExpandEnvironmentVariables($item.Target)
            $shortcut.Save()
        }
        # Two-argument Move never overwrites an existing destination.
        [IO.File]::Move($temporary, $destination)
    } finally {
        if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) }
    }
}
