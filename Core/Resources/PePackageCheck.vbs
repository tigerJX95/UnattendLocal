Option Explicit
Dim errorNumber
On Error Resume Next
RunChecks
errorNumber = Err.Number
On Error GoTo 0
Fail "Could not complete offline package checks (" & errorNumber & ")."

Sub RunChecks
    Dim fso, shell, root, packages, package, file, folder, name, suffix, mode
    If WScript.Arguments.Count <> 2 Then Fail "Invalid offline package check arguments."
    root = WScript.Arguments(0)
    mode = WScript.Arguments(1)
    If mode <> "read" And mode <> "list" Then Fail "Invalid offline package check mode."
    Set fso = CreateObject("Scripting.FileSystemObject")
    Set shell = CreateObject("WScript.Shell")
    Set packages = CreateObject("Scripting.Dictionary")
    packages.CompareMode = vbTextCompare
@@PACKAGES@@
    If Not fso.FolderExists(root) Then Fail "Offline package folder is missing."
    Set folder = fso.GetFolder(root)
    For Each file In folder.Files
        suffix = LCase(fso.GetExtensionName(file.Name))
        If suffix = "msu" Or suffix = "cab" Then
            If Not packages.Exists(file.Name) Then Fail "An update file in WinPEUpdates was not listed."
        End If
    Next
    For Each name In packages.Keys
        package = fso.BuildPath(root, name)
        If Not fso.FileExists(package) Then Fail "Missing update file: " & name
        Set file = fso.GetFile(package)
        If file.Size <= 0 Then Fail "Empty update file: " & name
        If Len(packages(name)) > 0 Then
            WScript.Echo "Verifying SHA-256: " & name
            CheckHash shell, fso, package, name, packages(name)
        ElseIf mode = "read" Then
            WScript.Echo "Checking offline package: " & name
            Dim copyCommand
            copyCommand = Quote(shell.ExpandEnvironmentStrings("%ComSpec%"))
            copyCommand = copyCommand & " /d /c copy /b " & Quote(package) & " NUL >nul 2>&1"
            If shell.Run(copyCommand, 0, True) <> 0 Then Fail "Unreadable update file: " & name
        End If
    Next
    WScript.Quit 0
End Sub

Sub CheckHash(shell, fso, package, name, expected)
    Dim certutil, process, result, line, candidate, found, checked, expression
    certutil = shell.ExpandEnvironmentStrings("%SystemRoot%") & "\System32\certutil.exe"
    If Not fso.FileExists(certutil) Then Fail "SHA-256 check requires certutil.exe in the Windows PE image."
    Set process = shell.Exec(Quote(certutil) & " -hashfile " & Quote(package) & " SHA256")
    result = process.StdOut.ReadAll
    If process.ExitCode <> 0 Then Fail "Could not read update file for SHA-256: " & name
    Set expression = New RegExp
    expression.Pattern = "^[0-9a-fA-F]{64}$"
    found = 0
    checked = ""
    For Each line In Split(result, vbLf)
        candidate = Replace(Replace(Replace(line, vbCr, ""), " ", ""), vbTab, "")
        If expression.Test(candidate) Then
            found = found + 1
            checked = candidate
        End If
    Next
    If found <> 1 Or LCase(checked) <> LCase(expected) Then Fail "SHA-256 mismatch: " & name
End Sub

Function Quote(value)
    Quote = Chr(34) & value & Chr(34)
End Function

Sub Fail(message)
    WScript.StdErr.WriteLine message
    WScript.Quit 1
End Sub
