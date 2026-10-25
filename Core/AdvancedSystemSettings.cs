using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class AdvancedSystemSettings
{
    public record Item(string Property,string Label,string[] Commands,string[] Markers);
    public static IReadOnlyList<Item> Items {get;} = new Item[]
    {
        new(nameof(InstallPlan.EnableRemoteDesktop), global::UnattendLocal.Localization.UiText.Source("text.c255841f6809"), [
            "netsh.exe advfirewall firewall set rule group=\"@FirewallAPI.dll,-28752\" new enable=Yes;",
            "reg.exe add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Terminal Server\" /v fDenyTSConnections /t REG_DWORD /d 0 /f;"
        ], ["@FirewallAPI.dll,-28752", "fDenyTSConnections"]),
        new(nameof(InstallPlan.AllowPowerShellScripts), global::UnattendLocal.Localization.UiText.Source("text.ee932fe20821"), [
            "Set-ExecutionPolicy -Scope 'LocalMachine' -ExecutionPolicy 'RemoteSigned' -Force;"
        ], ["Set-ExecutionPolicy"])
    };
    public static readonly string RdpDescription=global::UnattendLocal.Localization.UiText.Source("text.3a2583497b13");
    public static readonly string ScriptDescription=global::UnattendLocal.Localization.UiText.Source("text.a06d19a5d87e");
    private const string FilePath=@"C:\Windows\Setup\Scripts\Specialize.ps1";
    private const string BlockPattern=@"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n";
    private static string Normal(string body)=>global::UnattendLocal.Localization.UiText.Join('\n', body.Replace("\r\n","\n").Split('\n').Select(l=>l.Trim()).Where(l=>l.Length>0), false);
    private static string Skeleton(string body)=>Normal(Regex.Replace(body.Replace("\r\n","\n"),BlockPattern,"",RegexOptions.None,TimeSpan.FromSeconds(1)));
    private static readonly Lazy<string> Wrapper=new(()=>{
        var g=new UnattendGenerator();var doc=g.GenerateXml(Configuration.Default with {EnableRemoteDesktop=true,AllowPowerShellScripts=true});
        var f=doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path")==FilePath);return Skeleton(f.InnerText);
    });
    public record State(bool Enabled,bool Ambiguous);
    public static Dictionary<string,State> Inspect(XmlDocument doc)
    {
        var files=doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var stage=files.Where(f=>f.GetAttribute("path").Equals(FilePath,StringComparison.OrdinalIgnoreCase)).ToArray();
        var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u",XmlMerge.Namespace);
        var paths=doc.SelectNodes("//u:settings[@pass='specialize']//u:Path",ns)!.Cast<XmlElement>();
        bool active=paths.Count(p=>Regex.IsMatch(p.InnerText,"^powershell\\.exe -WindowStyle \"(?:Hidden|Normal)\" -ExecutionPolicy \"Unrestricted\" -NoProfile -File \""+Regex.Escape(FilePath)+"\"$",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1)))==1;
        bool knownStage=stage.Length==1&&active&&Skeleton(stage[0].InnerText)==Wrapper.Value;
        var blocks=knownStage?Regex.Matches(stage[0].InnerText.Replace("\r\n","\n"),BlockPattern,RegexOptions.None,TimeSpan.FromSeconds(1)).Select(m=>Normal(m.Groups["body"].Value)).ToArray():[];
        var result=new Dictionary<string,State>();
        foreach(var item in Items)
        {
            var relevant=files.SelectMany(f=>Normal(f.InnerText).Split('\n')).Where(line=>item.Markers.Any(marker=>line.Contains(marker,StringComparison.OrdinalIgnoreCase))).ToArray();
            string command=Normal(global::UnattendLocal.Localization.UiText.Join('\n', item.Commands, false));
            bool enabled=knownStage&&blocks.Count(b=>b==command)==1&&relevant.Length==item.Commands.Length&&item.Commands.All(c=>relevant.Count(line=>line==Normal(c))==1);
            result[item.Property]=new(enabled,relevant.Length>0&&!enabled);
        }
        return result;
    }
    public static void ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes)
    {
        var states=Inspect(doc);
        foreach(var item in Items){typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,states[item.Property].Enabled);if(states[item.Property].Ambiguous)notes.Add(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.65e8c89d5b55") },new bool[]{false,false}));}
    }
    public static void ValidateImported(InstallPlan p,XmlDocument doc)
    {
        var states=Inspect(doc);
        foreach(var item in Items)if(states[item.Property].Ambiguous&&(bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.d2242cae892f") },new bool[]{false,false}));
    }
    public static string Summary(InstallPlan p)=>global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.37eaea3ad7cc"), (p.EnableRemoteDesktop?global::UnattendLocal.Localization.UiText.Source("text.025c59714fb0"):global::UnattendLocal.Localization.UiText.Source("text.4e9901953ce4")), global::UnattendLocal.Localization.UiText.Source("text.1a0cf9f076c8"), (p.AllowPowerShellScripts?global::UnattendLocal.Localization.UiText.Source("text.97ad43103c26"):global::UnattendLocal.Localization.UiText.Source("text.763d9dd8002b")) },new bool[]{false,false,false,false});
    public static void NormalizeLoaded(InstallPlan p,JsonElement json)
    {
        if(p.ImportedXml is not {OriginalOmitted:false} data||data.SourceXml.Length==0||data.BaselineXml.Length==0)return;
        var missing=Items.Where(i=>!json.TryGetProperty(i.Property,out _)).ToArray();if(missing.Length==0)return;
        var states=Inspect(XmlMerge.Read(data.SourceXml));
        var before=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!;before.ImportedXml=null;before.ConfirmWipeDisk=before.ConfirmFormatPartition=before.ConfirmDiskScript=true;
        foreach(var item in missing){typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,states[item.Property].Enabled);if(states[item.Property].Ambiguous)data.Notices=data.Notices.Append(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.f7e4eb4cca4c") },new bool[]{false,false})).Distinct().ToArray();}
        if(!missing.Any(i=>states[i.Property].Enabled))return;
        var after=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;
        foreach(var item in missing)typeof(InstallPlan).GetProperty(item.Property)!.SetValue(after,states[item.Property].Enabled);
        try {var g=new GeneratorService();data.BaselineXml=XmlMerge.Apply(data.BaselineXml,Encoding.UTF8.GetString(g.GenerateImportBaseline(before)),XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml;}
        catch(Exception e)when(e is ArgumentException or ConfigurationException or XmlException){data.Blockers=data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.03e937b4c0ec")).Distinct().ToArray();}
    }
}
