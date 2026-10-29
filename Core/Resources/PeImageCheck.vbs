Option Explicit
Dim fso, file, contents, line, colon, field, architecture, matches, failed
Dim editionId, editionCount, imageIndex, indexCount, expectedIndex, expression
If WScript.Arguments.Count <> 2 Then StopCheck "Invalid image check arguments."
expectedIndex = WScript.Arguments(1)
If Not ValidIndex(expectedIndex) Then StopCheck "Invalid expected image index."
On Error Resume Next
Set fso = CreateObject("Scripting.FileSystemObject")
If Err.Number <> 0 Then StopCheck "Cannot access the image information file."
Set file = fso.GetFile(WScript.Arguments(0))
If Err.Number <> 0 Then StopCheck "Cannot read the image information file."
If file.Size > 1048576 Then StopCheck "Image information is too large."
Set file = fso.OpenTextFile(WScript.Arguments(0), 1, False)
If Err.Number <> 0 Then StopCheck "Cannot open the image information file."
contents = file.ReadAll
failed = Err.Number
file.Close
If failed <> 0 Or Err.Number <> 0 Then StopCheck "Cannot read image information."
If InStr(contents, Chr(0)) > 0 Then
  Err.Clear
  Set file = fso.OpenTextFile(WScript.Arguments(0), 1, False, -1)
  If Err.Number <> 0 Then StopCheck "Cannot open Unicode image information."
  contents = file.ReadAll
  failed = Err.Number
  file.Close
  If failed <> 0 Or Err.Number <> 0 Then StopCheck "Cannot read Unicode data."
End If
On Error GoTo 0
matches = 0
architecture = ""
editionId = ""
editionCount = 0
imageIndex = ""
indexCount = 0
contents = Replace(Replace(contents, vbCrLf, vbLf), vbCr, vbLf)
For Each line In Split(contents, vbLf)
  line = Replace(line, vbTab, " ")
  colon = InStr(line, ":")
  If colon > 0 Then
    field = LCase(Trim(Left(line, colon - 1)))
    If field = "architecture" Then
      matches = matches + 1
      architecture = LCase(Trim(Mid(line, colon + 1)))
    ElseIf field = "edition" Then
      editionCount = editionCount + 1
      editionId = Trim(Mid(line, colon + 1))
    ElseIf field = "index" Then
      indexCount = indexCount + 1
      imageIndex = Trim(Mid(line, colon + 1))
    End If
  End If
Next
If matches <> 1 Then StopCheck "Architecture field is missing or duplicated."
If indexCount <> 1 Then StopCheck "Image index is missing or duplicated."
If Not ValidIndex(imageIndex) Then StopCheck "Image index is invalid."
If CDbl(imageIndex) <> CDbl(expectedIndex) Then
  StopCheck "Reported image index does not match the selected index."
End If
Set expression = New RegExp
expression.Pattern = "^[A-Za-z][A-Za-z0-9_-]{0,127}$"
WScript.Echo "Selected image index: " & imageIndex
If editionCount = 1 And expression.Test(editionId) Then
  WScript.Echo "Selected image edition/SKU: " & editionId
Else
  WScript.Echo "Edition/SKU unavailable; using the explicitly selected index."
End If
Select Case architecture
  Case "x64"
    WScript.Echo "Selected image architecture verified: x64."
    WScript.Quit 0
  Case "x86", "arm", "arm64", "ia64"
    WScript.Echo "Selected image architecture is incompatible with x64 PE."
    WScript.Quit 42
  Case Else
    StopCheck "Selected image architecture could not be recognized."
End Select
Sub StopCheck(message)
  WScript.Echo message
  WScript.Quit 1
End Sub

Function ValidIndex(value)
  Dim i, character
  ValidIndex = False
  If Len(value) < 1 Or Len(value) > 10 Then Exit Function
  For i = 1 To Len(value)
    character = AscW(Mid(value, i, 1))
    If character < 48 Or character > 57 Then Exit Function
  Next
  If CDbl(value) < 1 Or CDbl(value) > 2147483647 Then Exit Function
  ValidIndex = True
End Function
