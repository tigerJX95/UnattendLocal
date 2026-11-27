namespace UnattendLocal.Core;

internal static class PeMediaDiscovery
{
    internal static string Bind(string script, bool usesVirtIo)
    {
        const string scan = "for %%d in (";
        const string endMarker = "set \"OS_VERSION=11\"";
        const string copy = @"copy %XML_FILE% W:\Windows\Panther\unattend.xml";
        int start = script.IndexOf(scan, StringComparison.Ordinal);
        int end = script.IndexOf(endMarker, StringComparison.Ordinal);
        int headerEnd = start < 0 ? -1 : script.IndexOf(") do (", start, StringComparison.Ordinal);
        if (start < 0 || headerEnd < 0 || end <= headerEnd ||
            !script[start..end].Contains(@"%%d:\sources\install.swm", StringComparison.Ordinal) ||
            script.Split(copy, StringSplitOptions.None).Length != 2)
            throw new InvalidOperationException("Unexpected PE media discovery structure.");

        string drives = script[(start + scan.Length)..headerEnd];
        if (drives.Split(' ').Any(drive => drive is not [>= 'A' and <= 'Z']))
            throw new InvalidOperationException("Unexpected PE media drive list.");
        string media = $$"""
            set "IMAGE_FILE="
            set "SWM_PARAM="
            set "XML_FILE="
            set "OEM_FOLDER="
            set "PEDRIVERS_FOLDER="
            set "VIRTIO_DRIVE="
            set "UL_IMAGE_DRIVE="
            set "UL_MEDIA_MULTIPLE="
            for %%d in ({{drives}}) do (
                for %%e in (wim esd swm) do if exist "%%d:\sources\install.%%e" (
                    echo:Installation image candidate: %%d:\sources\install.%%e
                    if defined IMAGE_FILE set "UL_MEDIA_MULTIPLE=1"
                    set "IMAGE_FILE=%%d:\sources\install.%%e"
                    set "UL_IMAGE_DRIVE=%%d:"
                    if /I "%%e"=="swm" set "SWM_PARAM=/SWMFile:%%d:\sources\install*.swm"
                )
            )
            if defined UL_MEDIA_MULTIPLE call :fail "Several installation images found. Leave only the intended image connected. No disk was formatted."
            if not defined IMAGE_FILE call :fail "Could not locate install.wim, install.esd or install.swm."
            for /f "tokens=2,*" %%t in ('reg.exe query HKLM\System\Setup /v UnattendFile 2^>nul') do if /I "%%t"=="REG_SZ" if exist "%%u" set "XML_FILE=%%u"
            if not defined XML_FILE if exist "%UL_IMAGE_DRIVE%\autounattend.xml" set "XML_FILE=%UL_IMAGE_DRIVE%\autounattend.xml"
            if not defined XML_FILE (
                for %%d in ({{drives}}) do if exist "%%d:\autounattend.xml" (
                    if defined XML_FILE set "UL_MEDIA_MULTIPLE=1"
                    set "XML_FILE=%%d:\autounattend.xml"
                )
            )
            if defined UL_MEDIA_MULTIPLE call :fail "Several answer files found. Leave only the intended answer file connected. No disk was formatted."
            if not defined XML_FILE call :fail "Could not locate autounattend.xml."
            if exist "%UL_IMAGE_DRIVE%\$OEM$\" set "OEM_FOLDER=%UL_IMAGE_DRIVE%\$OEM$"
            if exist "%UL_IMAGE_DRIVE%\$WinPEDriver$\" set "PEDRIVERS_FOLDER=%UL_IMAGE_DRIVE%\$WinPEDriver$"
            """;
        // VirtIO documents a separate guest-tools ISO and remains an explicit editor option.
        if (usesVirtIo)
            media += "\r\n" + $$"""
                for %%d in ({{drives}}) do if exist "%%d:\virtio-win-guest-tools.exe" (
                    if defined VIRTIO_DRIVE set "UL_MEDIA_MULTIPLE=1"
                    set "VIRTIO_DRIVE=%%d:"
                )
                if defined UL_MEDIA_MULTIPLE call :fail "Several VirtIO media found. No disk was formatted."
                """;

        return (script[..start] + media.ReplaceLineEndings("\r\n") + "\r\n\r\n" + script[end..])
            .Replace(copy, "copy \"%XML_FILE%\" W:\\Windows\\Panther\\unattend.xml", StringComparison.Ordinal);
    }
}
