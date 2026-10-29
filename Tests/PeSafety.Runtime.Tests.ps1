# Execute the production VBScript with mocked disk/firmware I/O on Windows.
# No DiskPart, registry writes, disk changes or real WMI enumeration occur.
$ErrorActionPreference = 'Stop'
$engine = if ($env:SystemRoot) { Join-Path $env:SystemRoot 'System32/cscript.exe' } else { '' }
if (-not $engine -or -not (Test-Path -LiteralPath $engine)) {
    Write-Host 'PE selector runtime: NOT RUN (Windows cscript.exe is required). This is not a VM installation pass.'
    return
}
$source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../Core/Resources/PeTargetDisk.vbs'))
$root = Join-Path ([IO.Path]::GetTempPath()) ('UL-PeSafety-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -Path $root -ItemType Directory
$script:checks = 0
function Assert-Pe($value,$message) { if (-not $value) { throw $message }; $script:checks++ }
$mock = @'
Class TestWmi
  Public Function InstancesOf(name)
    InstancesOf = TestDrives
  End Function
End Class
Class TestDisk
  Public Index, Size, Pnp, Partitions, Media, Interface, HasSource, MissingSize, MapCount, VolumeId
  Public Function ReadField(name)
    Select Case name
      Case "Index": ReadField = Index
      Case "Size": If MissingSize Then ReadField = Null Else ReadField = Size
      Case "PNPDeviceID": ReadField = Pnp
      Case "Partitions": ReadField = Partitions
      Case "MediaType": ReadField = Media
      Case "InterfaceType": ReadField = Interface
      Case "Model": ReadField = "Simulated disk"
      Case Else: Err.Raise 5
    End Select
  End Function
  Public Function Associators_(name)
    Dim part, parts(), i
    If MapCount = 0 Then
      Associators_ = Array()
      Exit Function
    End If
    ReDim parts(MapCount - 1)
    For i = 0 To MapCount - 1
      Set part = New TestPartition
      part.HasSource = HasSource
      part.VolumeId = VolumeId
      Set parts(i) = part
    Next
    Associators_ = parts
  End Function
End Class
Class TestPartition
  Public HasSource, VolumeId
  Public Function Associators_(name)
    Dim logical
    If Not HasSource Then
      Associators_ = Array()
      Exit Function
    End If
    Set logical = New TestLogical
    logical.VolumeId = VolumeId
    Associators_ = Array(logical)
  End Function
End Class
Class TestLogical
  Public VolumeId
  Public Function ReadField(name)
    ReadField = VolumeId
  End Function
End Class
'@
function Invoke-Selector([string]$mode='fixed',[double[]]$sizes=@(137438953472),[string]$layout='GPT',[int]$firmware=2,[string]$inputText='0',[string]$custom='WScript.Echo 0',[bool]$verify=$false,[string]$identity='Disk',[int]$target=0,[bool]$hasSource=$false,[bool]$missingSize=$false,[int]$parts=0,[bool]$empty=$false,[int]$min=64,[int]$max=512,[int]$mapped=0,[string]$volume='Z:',[bool]$missingHost=$false) {
    $lines = New-Object 'System.Collections.Generic.List[string]'
    $vars = @()
    for($i=0;$i -lt $sizes.Count;$i++) {
        $vars += "d$i"
        $number = $sizes[$i].ToString('F0',[Globalization.CultureInfo]::InvariantCulture)
        $lines.Add("Set d$i = New TestDisk")
        $lines.Add("d$i.Index = $i : d$i.Size = $number.0 : d$i.Pnp = `"$identity$i`" : d$i.Partitions = $parts")
        $lines.Add("d$i.Media = `"Fixed hard disk media`" : d$i.Interface = `"SCSI`"")
        $lines.Add("d$i.MapCount = $mapped : d$i.VolumeId = `"$volume`"")
        $lines.Add("d$i.HasSource = $($hasSource.ToString()) : d$i.MissingSize = $($missingSize.ToString())")
    }
    $prelude = 'Dim TestDrives' + $(if($vars.Count){', ' + ($vars -join ', ')}else{''}) + "`r`n" + ($lines -join "`r`n") + "`r`nTestDrives = Array(" + ($vars -join ', ') + ")`r`n"
    $text = $source.Replace('Option Explicit',"Option Explicit`r`n$prelude").Replace('Set wmi = GetObject("winmgmts:\\.\root\cimv2")','Set wmi = New TestWmi')
    $text = $text.Replace('firmware = CLng(shell.RegRead("HKLM\System\CurrentControlSet\Control\PEFirmwareType"))',"firmware = $firmware")
    $text = $text.Replace('value = disk.Properties_.Item(name).Value','value = disk.ReadField(name)')
    $values = @{ MODE=$mode;FIXED='0';INDEX='-1';MIN="$min";MAX="$max";EMPTY=$empty.ToString();INTERFACE='False';MEDIA='False';BYTES='2684354560.0';LAYOUT=$layout }
    foreach($key in $values.Keys) { $text=$text.Replace("@@$key@@",$values[$key]) }
    $customLines = ($custom -split "`r?`n" | ForEach-Object { 'custom.WriteLine "' + $_.Replace('"','""') + '"' }) -join "`r`n"
    $text=$text.Replace('@@CUSTOM@@',$customLines).Replace('X:\UL-target.snapshot',(Join-Path $root 'snapshot.txt')).Replace('X:\UL-user-target.vbs',(Join-Path $root 'user.vbs'))
    if ($missingHost) { $text = $text.Replace('WScript.FullName', ('"' + (Join-Path $root 'missing-host.exe') + '"')) }
    Assert-Pe (-not $text.Contains('@@')) 'No unresolved template tokens'
    Assert-Pe (-not $text.Contains('GetObject("winmgmts:') -and -not $text.Contains('shell.RegRead(')) 'Real WMI and registry reads are replaced by test I/O'
    Assert-Pe (-not $text.Contains('disk.Properties_.Item(name).Value')) 'Disk properties come only from the mock' 
    $file = Join-Path $root 'selector.vbs'
    [IO.File]::WriteAllText($file,$text+"`r`n"+$mock,[Text.Encoding]::Unicode)
    $info=New-Object Diagnostics.ProcessStartInfo
    $info.FileName=$engine;$info.Arguments='//Nologo //E:vbscript "'+$file+'"'+$(if($verify){' --verify'}else{''})
    $info.UseShellExecute=$false;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true;$info.RedirectStandardInput=$true
    $info.EnvironmentVariables['IMAGE_FILE']='Z:\sources\install.wim';$info.EnvironmentVariables['XML_FILE']='Z:\autounattend.xml'
    $info.EnvironmentVariables['TARGET_DISK']="$target";$info.EnvironmentVariables['LAYOUT']=$(if($firmware -eq 1){'MBR'}else{'GPT'})
    $process=[Diagnostics.Process]::Start($info)
    $process.StandardInput.WriteLine($inputText);$process.StandardInput.Close()
    if(-not $process.WaitForExit(10000)){$process.Kill();throw 'Selector simulation timed out'}
    $stdout=$process.StandardOutput.ReadToEnd();$stderr=$process.StandardError.ReadToEnd();$code=$process.ExitCode;$process.Dispose()
    return @{Code=$code;Out=$stdout.Trim();Error=$stderr}
}
try {
    $s=Invoke-Selector -verify $true
    # OpenTextFile on a missing snapshot raises a host runtime error. cscript
    # can write that diagnostic to stdout and return a negative HRESULT.
    # The production recheck gates on an exact zero status, not empty stdout.
    $details = 'ExitCode={0}; stdout=[{1}]; stderr=[{2}]' -f $s.Code,$s.Out,$s.Error
    Assert-Pe ($s.Code -ne 0) ('A missing target snapshot fails the pre-partition recheck. ' + $details)
    Assert-Pe ($s.Out -notmatch '(?m)^[0-9]+\r?$') ('A missing snapshot must not emit a target disk number. ' + $details)
    Assert-Pe (-not [string]::IsNullOrWhiteSpace($s.Out + $s.Error)) ('A missing snapshot reports a diagnostic. ' + $details)
    $s=Invoke-Selector -mode script -missingHost $true
    Assert-Pe ($s.Code -ne 0 -and $s.Out -eq '') 'A missing nested script host stops selection'
    foreach($layout in @('GPT','MBR','Automatic')) {
        $fw=if($layout -eq 'MBR'){1}else{2};$s=Invoke-Selector -layout $layout -firmware $fw
        Assert-Pe ($s.Code -eq 0 -and $s.Out -eq '0') 'Fixed disk selection succeeds for valid firmware/layout'
        $s=Invoke-Selector -layout $layout -firmware $fw -verify $true
        Assert-Pe ($s.Code -eq 0) 'Unchanged target passes the immediate pre-partition recheck'
    }
    $s=Invoke-Selector -mode match -sizes @(137438953472,17179869184)
    Assert-Pe ($s.Code -eq 0 -and $s.Out -eq '0') 'One match among several disks'
    foreach($sizes in @(@(),@(17179869184),@(137438953472,137438953472),@(68719476735),@(549755813889))) {
        $s=Invoke-Selector -mode match -sizes $sizes
        Assert-Pe ($s.Code -ne 0 -and $s.Out -eq '') 'Zero/multiple matches and byte-exact capacity violations fail closed'
    }
    foreach($size in @(68719476736,549755813888)) {
        $s=Invoke-Selector -sizes @($size)
        Assert-Pe ($s.Code -eq 0) 'Inclusive capacity boundaries succeed without rounding'
    }
    foreach($flags in @(@{hasSource=$true;parts=1;mapped=1},@{missingSize=$true},@{parts=1;empty=$true},@{layout='GPT';firmware=1},@{layout='MBR';firmware=2},@{firmware=0})) {
        $s=Invoke-Selector @flags
        Assert-Pe ($s.Code -ne 0 -and $s.Out -eq '') 'Source disks, missing properties, partition restrictions and firmware conflicts stop selection'
    }
    foreach($flags in @(@{parts=2;mapped=1},@{parts=1;mapped=2},@{parts=1;mapped=1;hasSource=$true;volume='invalid'})) {
        $s=Invoke-Selector @flags
        Assert-Pe ($s.Code -ne 0 -and $s.Out -eq '') 'Incomplete, excessive or malformed volume mappings stop selection'
    }
    $s=Invoke-Selector -parts 2 -mapped 2
    Assert-Pe ($s.Code -eq 0) 'Complete partition mapping without a source volume remains eligible'
    $s=Invoke-Selector -mode interactive -inputText '0'
    Assert-Pe ($s.Code -eq 0 -and $s.Out -eq '0') 'Interactive valid selection'
    foreach($inputText in @('-1','0 & echo bad','999','')) {
        $s=Invoke-Selector -mode interactive -inputText $inputText
        Assert-Pe ($s.Code -ne 0) 'Invalid interactive selections cannot reach partitioning'
    }
    $s=Invoke-Selector -mode script
    Assert-Pe ($s.Code -eq 0 -and $s.Out -eq '0') 'Custom script with exactly one valid index'
    foreach($custom in @('WScript.Echo 99','WScript.Echo "0abc"','Err.Raise 53','WScript.StdOut.Write "0" & vbLf & vbLf','WScript.Quit 1','WScript.Echo "0 & echo bad"',"WScript.Echo 0`r`nWScript.Echo 1",'WScript.Echo -1','WScript.StdErr.WriteLine "warning"')) {
        $s=Invoke-Selector -mode script -custom $custom
        Assert-Pe ($s.Code -ne 0 -and $s.Out -eq '') 'Custom script errors and noncanonical output stop selection'
    }
    $s=Invoke-Selector
    Assert-Pe ($s.Code -eq 0) 'Seed identity snapshot'
    foreach($flags in @(@{identity='OtherDisk'},@{sizes=@(274877906944)},@{target=1},@{firmware=1;layout='Automatic'})) {
        $s=Invoke-Selector -verify $true @flags
        Assert-Pe ($s.Code -ne 0) 'Identity, size, target index or firmware changes invalidate the snapshot'
    }
    Write-Host ('PE selector runtime: {0} actual VBScript checks passed with simulated disk I/O. No installation was performed.' -f $script:checks)
} finally { Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue }
