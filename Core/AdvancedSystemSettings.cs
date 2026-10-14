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
        new(nameof(InstallPlan.EnableRemoteDesktop), "远程桌面", [
            "netsh.exe advfirewall firewall set rule group=\"@FirewallAPI.dll,-28752\" new enable=Yes;",
            "reg.exe add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Terminal Server\" /v fDenyTSConnections /t REG_DWORD /d 0 /f;"
        ], ["@FirewallAPI.dll,-28752", "fDenyTSConnections"]),
        new(nameof(InstallPlan.AllowPowerShellScripts), "PowerShell 执行策略", [
            "Set-ExecutionPolicy -Scope 'LocalMachine' -ExecutionPolicy 'RemoteSigned' -Force;"
        ], ["Set-ExecutionPolicy"])
    };
    public const string RdpDescription="在目标 Windows 安装阶段启用远程桌面接入，并开启内置远程桌面防火墙规则组。家庭版不提供远程桌面主机能力，此选项不会绕过版本限制；其他版本也需支持该功能。会允许网络上的其他设备尝试连接，须使用可信网络、有效账户与强密码；不自动添加允许用户，不关闭网络级别身份验证，不创建路由器端口转发。未勾选时不写入覆盖；取消只影响新生成的配置，不关闭已安装系统上的远程桌面。不修改当前电脑。";
    public const string ScriptDescription="在目标 Windows 安装阶段把 Windows PowerShell 的 LocalMachine 执行策略设为 RemoteSigned：允许本地脚本，通常要求带互联网来源标记的脚本具备可信签名。组织策略、当前用户和会话策略可能覆盖此值；执行策略不替代脚本内容审查。与隐藏 PowerShell 窗口、自定义脚本的启用和安装脚本调用参数独立，不开启 WinRM 或 PowerShell 远程连接。未勾选时不写入策略覆盖；取消只影响新生成的配置，不撤销已安装系统的策略。本工具不在本机执行脚本。";
    private const string FilePath=@"C:\Windows\Setup\Scripts\Specialize.ps1";
    private const string BlockPattern=@"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n";
    private static string Normal(string body)=>string.Join('\n',body.Replace("\r\n","\n").Split('\n').Select(l=>l.Trim()).Where(l=>l.Length>0));
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
            string command=Normal(string.Join('\n',item.Commands));
            bool enabled=knownStage&&blocks.Count(b=>b==command)==1&&relevant.Length==item.Commands.Length&&item.Commands.All(c=>relevant.Count(line=>line==Normal(c))==1);
            result[item.Property]=new(enabled,relevant.Length>0&&!enabled);
        }
        return result;
    }
    public static void ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes)
    {
        var states=Inspect(doc);
        foreach(var item in Items){typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,states[item.Property].Enabled);if(states[item.Property].Ambiguous)notes.Add(item.Label+"脚本未完整识别（可能修改、重复、停用或位于其他阶段），原文保留；不能用此开关覆盖。");}
    }
    public static void ValidateImported(InstallPlan p,XmlDocument doc)
    {
        var states=Inspect(doc);
        foreach(var item in Items)if(states[item.Property].Ambiguous&&(bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!)throw new ArgumentException(item.Label+"与原 XML 中未完整识别的脚本重叠，请用原工具调整后重新导入。");
    }
    public static string Summary(InstallPlan p)=>"远程桌面："+(p.EnableRemoteDesktop?"启用接入及内置防火墙规则（需目标版本支持）":"遵循 Windows 默认行为")+"\nPowerShell 执行策略："+(p.AllowPowerShellScripts?"Windows PowerShell / LocalMachine / RemoteSigned（可被更高优先级策略覆盖）":"不写入执行策略覆盖");
    public static void NormalizeLoaded(InstallPlan p,JsonElement json)
    {
        if(p.ImportedXml is not {OriginalOmitted:false} data||data.SourceXml.Length==0||data.BaselineXml.Length==0)return;
        var missing=Items.Where(i=>!json.TryGetProperty(i.Property,out _)).ToArray();if(missing.Length==0)return;
        var states=Inspect(XmlMerge.Read(data.SourceXml));
        var before=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!;before.ImportedXml=null;before.ConfirmWipeDisk=before.ConfirmFormatPartition=before.ConfirmDiskScript=true;
        foreach(var item in missing){typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,states[item.Property].Enabled);if(states[item.Property].Ambiguous)data.Notices=data.Notices.Append(item.Label+"脚本未完整识别，原文保留，不能用此开关覆盖。").Distinct().ToArray();}
        if(!missing.Any(i=>states[i.Property].Enabled))return;
        var after=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;
        foreach(var item in missing)typeof(InstallPlan).GetProperty(item.Property)!.SetValue(after,states[item.Property].Enabled);
        try {var g=new GeneratorService();data.BaselineXml=XmlMerge.Apply(data.BaselineXml,Encoding.UTF8.GetString(g.GenerateStandalone(before)),XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateStandalone(after)))).OuterXml;}
        catch(Exception e)when(e is ArgumentException or ConfigurationException or XmlException){data.Blockers=data.Blockers.Append("旧方案的远程桌面/执行策略编辑基准无法可靠迁移，请重新导入原 XML 后再导出。").Distinct().ToArray();}
    }
}
