using System.Text.RegularExpressions;
using UnattendLocal.Core;

// Synthetic fixtures only: generated test accounts/settings, never a user's input XML.
static class LegacyRemovalFixture
{
    public static string FromCurrent(string xml)
    {
        var doc=XmlMerge.Read(xml);
        foreach(System.Xml.XmlComment c in doc.SelectNodes("//comment()")!.Cast<System.Xml.XmlComment>().ToArray()) c.ParentNode!.RemoveChild(c);
        foreach(System.Xml.XmlElement file in doc.SelectNodes("//*[local-name()='File']")!)
        {
            string path=file.GetAttribute("path");
            string plural=path.Replace("RemovePackage.ps1","RemovePackages.ps1").Replace("RemoveCapability.ps1","RemoveCapabilities.ps1").Replace("RemoveFeature.ps1","RemoveFeatures.ps1");
            string body=file.InnerText.Replace("\r\n","\n");
            if(plural!=path)
            {
                int start=body.IndexOf("$installed = & $getCommand;",StringComparison.Ordinal);
                body=body[..start]+"$logfile = '"+plural[..^4]+".log';\n"+Loop+"\n";
                file.SetAttribute("path",plural);
            }
            body=body.Replace("RemovePackage.ps1","RemovePackages.ps1").Replace("RemoveCapability.ps1","RemoveCapabilities.ps1").Replace("RemoveFeature.ps1","RemoveFeatures.ps1");
            body=Regex.Replace(body,@"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n",m=>m.Value.Contains("OneDrive.lnk")?"\t{\n\t\tRemove-Item -LiteralPath 'C:\\Users\\Default\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\OneDrive.lnk', 'C:\\Windows\\System32\\OneDriveSetup.exe', 'C:\\Windows\\SysWOW64\\OneDriveSetup.exe' -ErrorAction 'Continue';\n\t};\n":m.Value);
            file.InnerText=body;
        }
        return doc.OuterXml;
    }
    private const string Loop="""
& {
    $installed = & $getCommand;
    foreach( $selector in $selectors ) {
        $result = [ordered] @{
            Selector = $selector;
        };
        $found = $installed | Where-Object -FilterScript $filterCommand;
        if( $found ) {
            $result.Output = $found | & $removeCommand;
            if( $? ) {
                $result.Message = "$type removed.";
            } else {
                $result.Message = "$type not removed.";
                $result.Error = $Error[0];
            }
        } else {
            $result.Message = "$type not installed.";
        }
        $result | ConvertTo-Json -Depth 3 -Compress;
    }
} *>&1 | Out-String -Width 1KB -Stream >> $logfile;
""";
}
