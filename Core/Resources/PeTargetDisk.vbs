Option Explicit
Const Mode = "@@MODE@@"
Const FixedIndex = @@FIXED@@
Const MatchIndex = @@INDEX@@
Const MinGiB = @@MIN@@
Const MaxGiB = @@MAX@@
Const RequireEmpty = @@EMPTY@@
Const RequireInterface = @@INTERFACE@@
Const RequireMedia = @@MEDIA@@
Const RequiredBytes = @@BYTES@@
Const RequestedLayout = "@@LAYOUT@@"
Const Snapshot = "X:\UL-target.snapshot"
Dim shell, fso, wmi, drives, accepted, drive, selected, index, verify, saved, file, firmware, layout, imageDrive, xmlDrive
' UnattendLocal runtime boundary start
Dim runtimeError, runtimeDescription
On Error Resume Next
RunSelector
runtimeError = Err.Number
runtimeDescription = Err.Description
On Error GoTo 0
If runtimeError <> 0 Then Fail "Target disk safety check failed (" & CStr(runtimeError) & "): " & runtimeDescription
Fail "Target disk safety check did not complete."

Sub RunSelector()
' UnattendLocal runtime boundary end
Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
Set wmi = GetObject("winmgmts:\\.\root\cimv2")
firmware = CLng(shell.RegRead("HKLM\System\CurrentControlSet\Control\PEFirmwareType"))
If firmware = 1 Then
  layout = "MBR"
ElseIf firmware = 2 Then
  layout = "GPT"
Else
  Fail "Unknown firmware mode. No disk was formatted."
End If
If RequestedLayout <> "Automatic" And RequestedLayout <> layout Then Fail "Firmware and partition layout do not match."
imageDrive = UCase(Left(shell.Environment("PROCESS")("IMAGE_FILE"), 2))
xmlDrive = UCase(Left(shell.Environment("PROCESS")("XML_FILE"), 2))
If Not Matches("^[A-Z]:$", imageDrive) Or Not Matches("^[A-Z]:$", xmlDrive) Then Fail "Installation media could not be identified."
verify = False
If WScript.Arguments.Count = 1 Then verify = (WScript.Arguments(0) = "--verify")
If WScript.Arguments.Count > 1 Then Fail "Invalid verifier arguments."
If verify Then
  Set file = fso.OpenTextFile(Snapshot, 1, False, -1)
  saved = Split(file.ReadAll, vbCrLf)
  file.Close
  If UBound(saved) <> 4 Then Fail "Invalid target identity record."
  If saved(4) <> "" Then Fail "Invalid target identity record terminator."
  index = ParseIndex(saved(0))
  If shell.Environment("PROCESS")("TARGET_DISK") <> CStr(index) Then Fail "Target disk number changed."
  If shell.Environment("PROCESS")("LAYOUT") <> layout Then Fail "Detected layout changed."
ElseIf Mode = "fixed" Then
  index = FixedIndex
ElseIf Mode = "interactive" Then
  For Each drive In wmi.InstancesOf("Win32_DiskDrive")
    WScript.StdErr.WriteLine "Disk " & Number(drive, "Index") & ": " & CStr(Number(drive, "Size") / 1073741824) & " GiB; " & Field(drive, "Model")
  Next
  WScript.StdErr.Write "Enter the target disk number (all data on it will be erased): "
  If WScript.StdIn.AtEndOfStream Then Fail "No target disk was selected."
  index = ParseIndex(WScript.StdIn.ReadLine)
ElseIf Mode = "script" Then
  Dim custom, process, started, output, errors, command, quote
  Set custom = fso.CreateTextFile("X:\UL-user-target.vbs", True, True)
@@CUSTOM@@
  custom.Close
  quote = Chr(34)
  If Not fso.FileExists(WScript.FullName) Then Fail "The current script host is unavailable."
  command = quote & WScript.FullName
  command = command & quote & " //Nologo //E:vbscript "
  command = command & quote & "X:\UL-user-target.vbs" & quote
  Set process = shell.Exec(command)
  started = Timer
  Do While process.Status = 0
    If ((Timer - started + 86400) Mod 86400) >= 60 Then
      process.Terminate
      Fail "Target script timed out."
    End If
    WScript.Sleep 100
  Loop
  If process.ExitCode <> 0 Then Fail "Target script failed."
  output = process.StdOut.Read(256)
  errors = process.StdErr.Read(256)
  If Not process.StdOut.AtEndOfStream Or Not process.StdErr.AtEndOfStream Or Len(errors) <> 0 Then Fail "Unexpected target script output."
  If Not Matches("^[0-9]{1,10}(\r\n|\n)?$", output) Then Fail "Target script must output exactly one unsigned disk number."
  index = ParseIndex(Replace(Replace(output, vbCr, ""), vbLf, ""))
ElseIf Mode <> "match" Then
  Fail "Invalid disk selection mode."
End If
Set accepted = CreateObject("Scripting.Dictionary")
For Each drive In wmi.InstancesOf("Win32_DiskDrive")
  Dim candidate
  candidate = Number(drive, "Index")
  If candidate < 0 Or candidate > 2147483647 Or Fix(candidate) <> candidate Then Fail "Invalid disk index returned by WMI."
  If Mode = "match" Then
    If Eligible(drive) Then accepted.Add CStr(candidate), drive
  ElseIf candidate = index Then
    If Not Eligible(drive) Then Fail "Selected disk does not satisfy the safety conditions."
    accepted.Add CStr(candidate), drive
  End If
Next
If accepted.Count = 0 Then Fail "No disk satisfies the conditions. No disk was formatted."
If accepted.Count <> 1 Then Fail "Several disks satisfy the conditions. Automatic partitioning stopped."
Dim keys, identity, size
keys = accepted.Keys
Set selected = accepted(keys(0))
index = CLng(keys(0))
identity = Field(selected, "PNPDeviceID")
size = Number(selected, "Size")
If Len(identity) = 0 Or InStr(identity, vbCr) > 0 Or InStr(identity, vbLf) > 0 Then Fail "Disk identity is unavailable."
If verify Then
  If saved(0) <> CStr(index) Then Fail "Target index changed."
  If saved(1) <> identity Then Fail "Target identity changed."
  If CDbl(saved(2)) <> size Then Fail "Target capacity changed."
  If CLng(saved(3)) <> firmware Then Fail "Firmware changed."
Else
  Set file = fso.CreateTextFile(Snapshot, True, True)
  file.WriteLine CStr(index)
  file.WriteLine identity
  file.WriteLine CStr(size)
  file.WriteLine CStr(firmware)
  file.Close
End If
WScript.Echo CStr(index)
WScript.Quit 0
End Sub ' UnattendLocal selector entry

Sub Fail(message)
  WScript.StdErr.WriteLine message
  WScript.Quit 1
End Sub
Function Matches(pattern, value)
  Dim expression, results
  Set expression = New RegExp
  expression.Pattern = pattern
  expression.MultiLine = False
  Set results = expression.Execute(value)
  Matches = False
  If results.Count = 1 Then
    Matches = (results(0).FirstIndex = 0 And results(0).Length = Len(value))
  End If
End Function
Function ParseIndex(value)
  If Not Matches("^[0-9]{1,10}$", value) Then Fail "Invalid target disk number."
  If CDbl(value) > 2147483647 Then Fail "Target disk number is out of range."
  ParseIndex = CLng(value)
End Function
Function Field(disk, name)
  Dim value
  value = disk.Properties_.Item(name).Value
  If IsNull(value) Or IsEmpty(value) Then Fail "Disk property is unavailable: " & name
  Field = CStr(value)
End Function
Function Number(disk, name)
  Dim value
  value = Field(disk, name)
  If Not IsNumeric(value) Then Fail "Disk property is not numeric: " & name
  Number = CDbl(value)
End Function
Function Eligible(disk)
  Dim bytes, partitions, partition, logical, device, seenPartitions
  Eligible = False
  bytes = Number(disk, "Size")
  If bytes <= 0 Or bytes >= 9007199254740992 Then Fail "Disk capacity is unavailable or outside the verifiable range."
  If bytes < RequiredBytes Then Exit Function
  If MinGiB >= 0 Then
    If bytes < CDbl(MinGiB) * 1073741824 Then Exit Function
  End If
  If MaxGiB >= 0 Then
    If bytes > CDbl(MaxGiB) * 1073741824 Then Exit Function
  End If
  If Mode = "match" And MatchIndex >= 0 Then
    If Number(disk, "Index") <> MatchIndex Then Exit Function
  End If
  partitions = Number(disk, "Partitions")
  If partitions < 0 Or Fix(partitions) <> partitions Then Fail "Invalid partition count."
  If RequireEmpty Then
    If partitions <> 0 Then Exit Function
  End If
  If RequireInterface Then
    device = UCase(Field(disk, "InterfaceType"))
    If device <> "IDE" And device <> "SCSI" Then Exit Function
  End If
  If RequireMedia Then
    If Field(disk, "MediaType") <> "Fixed hard disk media" Then Exit Function
  End If
  seenPartitions = 0
  For Each partition In disk.Associators_("Win32_DiskDriveToDiskPartition")
    seenPartitions = seenPartitions + 1
    For Each logical In partition.Associators_("Win32_LogicalDiskToPartition")
      device = UCase(Field(logical, "DeviceID"))
      If Not Matches("^[A-Z]:$", device) Then Fail "Cannot identify a mapped source volume."
      If device = imageDrive Or device = xmlDrive Or device = "X:" Then Exit Function
    Next
  Next
  If seenPartitions <> partitions Then Fail "Disk partition mapping is incomplete or changed."
  Eligible = True
End Function
