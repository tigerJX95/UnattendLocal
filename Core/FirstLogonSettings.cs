using System.Text;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class FirstLogonSettings
{
    public const string Own="首个新建管理员账户（一次）", Builtin="内置 Administrator（一次）", None="不自动登录";
    public static IReadOnlyList<string> Modes {get;}=[Own,Builtin,None];
    public const string ResetCommand="Set-ItemProperty -LiteralPath 'Registry::HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon' -Name 'AutoLogonCount' -Type 'DWord' -Force -Value 0;";
    public const string Description="仅在自动配置本地账户时生效。可自动登录首个新建管理员或内置 Administrator 一次，也可停留在登录界面。选择内置账户会启用它并设置其密码，不创建同名普通账户；首次登录完成后，本工具不会自动停用内置账户。请核对后续使用安排。Base64 只是可还原的编码，不是加密。取消此选项只影响新配置，不停用已安装系统中的账户。";
    public const string ReadOnlyDescription="内置 Administrator 登录设置或执行流程未完整识别，原文保留；首次登录方式暂不可编辑，请在原工具调整后重新导入。";
    public static bool IsBuiltin(InstallPlan p)=>p.AccountMode=="自动创建本地账户"&&p.AutoLogon&&p.UseBuiltinAdministrator;
    public static string Mode(InstallPlan p)=>!p.AutoLogon?None:p.UseBuiltinAdministrator?Builtin:Own;
    public static string Summary(InstallPlan p)=>p.ImportedXml?.FirstLogonReadOnly==true?"原 XML 登录设置保留（只读）":p.AccountMode!="自动创建本地账户"?"由安装时账户创建流程决定":Mode(p);
    public static void Validate(InstallPlan p)
    {
        if(!IsBuiltin(p))return;
        if(p.NeedsBuiltinAdministratorPassword&&!(p.AllowEmptyBuiltinAdministratorPassword&&string.IsNullOrEmpty(p.BuiltinAdministratorPassword)))throw new ArgumentException("请重新输入内置 Administrator 密码，或明确允许空密码。");
        if(string.IsNullOrEmpty(p.BuiltinAdministratorPassword)&&!p.AllowEmptyBuiltinAdministratorPassword)throw new ArgumentException("请设置内置 Administrator 密码；如需空密码，请明确勾选允许空密码。");
        if(p.AllowEmptyBuiltinAdministratorPassword&&!string.IsNullOrEmpty(p.BuiltinAdministratorPassword))throw new ArgumentException("内置 Administrator 同时设置了密码和允许空密码，请选择一种。");
        if(p.ComputerNameMode==ComputerNamingSettings.Existing&&string.Equals(p.ComputerName,"Administrator",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("计算机名不能与内置 Administrator 账户同名。");
    }
    public static IAutoLogonSettings Build(InstallPlan p)=>!p.AutoLogon?new NoneAutoLogonSettings():p.UseBuiltinAdministrator?new BuiltinAutoLogonSettings(p.BuiltinAdministratorPassword):new OwnAutoLogonSettings();
    public record Source(bool Mentioned,bool Known,string Password,bool Obscure);
    private static XmlNamespaceManager Ns(XmlDocument doc){var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u",XmlMerge.Namespace);return ns;}
    private static XmlElement[] Nodes(XmlDocument doc,string path)=>doc.SelectNodes(path,Ns(doc))!.Cast<XmlElement>().ToArray();
    private static string? Text(XmlElement? node,string name)=>node?.ChildNodes.OfType<XmlElement>().SingleOrDefault(n=>n.LocalName==name)?.InnerText;
    private static (string Value,bool Obscure)? Password(XmlElement? parent,string name)
    {
        var elements=parent?.ChildNodes.OfType<XmlElement>().Where(n=>n.LocalName==name).ToArray();
        if(elements is not {Length:1})return null;
        var node=elements[0];var children=node.ChildNodes.OfType<XmlElement>().ToArray();
        if(children.Length!=2||children.Count(n=>n.LocalName=="Value")!=1||children.Count(n=>n.LocalName=="PlainText")!=1)return null;
        string value=Text(node,"Value")!;string plain=Text(node,"PlainText")!;
        if(value.Contains("密码已遮蔽",StringComparison.Ordinal))throw new ArgumentException("此 XML 是遮蔽后的预览，不能恢复内置账户密码；请导入实际 XML。");
        if(plain is not ("true" or "false"))return null;
        if(plain=="false")
        {
            try{value=new UnicodeEncoding(false,false,true).GetString(Convert.FromBase64String(value));if(!value.EndsWith(name,StringComparison.Ordinal))throw new FormatException();value=value[..^name.Length];}
            catch(Exception e)when(e is FormatException or DecoderFallbackException){throw new ArgumentException("内置 Administrator 密码编码无法还原，未导入；请使用原始安装 XML。");}
        }
        return(value,plain=="false");
    }
    public static Source Inspect(XmlDocument doc)
    {
        var auto=Nodes(doc,"//u:AutoLogon");var accounts=Nodes(doc,"//u:UserAccounts");
        bool mentioned=Nodes(doc,"//u:AdministratorPassword").Length>0||auto.Any(n=>n.ChildNodes.OfType<XmlElement>().Any(c=>c.LocalName=="Username"&&c.InnerText.Equals("Administrator",StringComparison.OrdinalIgnoreCase)));
        if(!mentioned)return new(false,false,"",true);
        // Read actual password fields even when the surrounding flow is unsupported.
        var loginPassword=auto.Length==1?Password(auto[0],"Password"):null;
        var adminPassword=accounts.Length==1?Password(accounts[0],"AdministratorPassword"):null;
        bool known=auto.Length==1&&accounts.Length==1&&Nodes(doc,"//u:settings[@pass='oobeSystem']/u:component[@name='Microsoft-Windows-Shell-Setup']/u:AutoLogon").Length==1&&Nodes(doc,"//u:settings[@pass='oobeSystem']/u:component[@name='Microsoft-Windows-Shell-Setup']/u:UserAccounts/u:AdministratorPassword").Length==1;
        if(known)
        {
            var children=auto[0].ChildNodes.OfType<XmlElement>().Select(n=>n.LocalName).Order().ToArray();
            known=children.SequenceEqual(new[]{"Enabled","LogonCount","Password","Username"})&&Text(auto[0],"Enabled")=="true"&&Text(auto[0],"Username")=="Administrator"&&Text(auto[0],"LogonCount")=="1"&&loginPassword!=null&&adminPassword!=null&&loginPassword==adminPassword&&AdditionalSetupSettings.KnownBlocks(doc,"FirstLogon.ps1").Count(b=>b==ResetCommand)==1;
        }
        var references=doc.SelectNodes("//*[local-name()='File' or local-name()='Path' or local-name()='CommandLine']")!.Cast<XmlElement>().SelectMany(n=>n.InnerText.Replace("\r\n","\n").Split('\n')).Where(l=>l.Contains("AutoLogonCount",StringComparison.OrdinalIgnoreCase)).ToArray();
        known=known&&references.Length==1&&references[0].Trim()==ResetCommand;
        return new(true,known,known?adminPassword!.Value.Value:"",known&&adminPassword!.Value.Obscure);
    }
    public static Source ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes)
    {
        var source=Inspect(doc);p.UseBuiltinAdministrator=false;p.BuiltinAdministratorPassword="";p.AllowEmptyBuiltinAdministratorPassword=false;p.NeedsBuiltinAdministratorPassword=false;
        if(source.Known)
        {
            p.AccountMode="自动创建本地账户";p.AutoLogon=true;p.UseBuiltinAdministrator=true;p.BuiltinAdministratorPassword=source.Password;p.AllowEmptyBuiltinAdministratorPassword=source.Password.Length==0;p.ObscurePasswords=source.Obscure;
        }
        else if(source.Mentioned)notes.Add(ReadOnlyDescription);
        return source;
    }
    public static void UpdateImportState(ImportedXml data,InstallPlan p,Source source)
    {
        data.FirstLogonImportVersion=1;data.FirstLogonReadOnly=source.Mentioned&&!source.Known;data.FirstLogonSourceAccountMode=p.AccountMode;data.FirstLogonSourceAutoLogon=p.AutoLogon;data.FirstLogonSourceBuiltin=p.UseBuiltinAdministrator;
    }
    public static void ValidateImported(InstallPlan p,XmlDocument source)
    {
        var state=Inspect(source);
        if(state.Mentioned&&!state.Known&&p.ImportedXml is {} data&&(p.AccountMode!=data.FirstLogonSourceAccountMode||p.AutoLogon!=data.FirstLogonSourceAutoLogon||p.UseBuiltinAdministrator!=data.FirstLogonSourceBuiltin))throw new ArgumentException("原 XML 的内置账户登录流程未完整识别，不能通过首次登录设置覆盖；请重新导入原工具调整后的 XML。");
    }
    public static void ValidateGenerated(InstallPlan p,XmlDocument generated)
    {
        var data=p.ImportedXml!;var source=Inspect(XmlMerge.Read(data.SourceXml));
        if(!source.Mentioned||source.Known)return;
        var baseline=XmlMerge.Read(data.BaselineXml);
        var before=Nodes(baseline,"//u:AutoLogon");var after=Nodes(generated,"//u:AutoLogon");
        if(before.Length!=after.Length||before.Length>1||before.Length==1&&!XmlMerge.Equal(before[0],after[0]))throw new ArgumentException("账户修改会影响原 XML 中只读的自动登录凭据，不能可靠合并；请在原工具调整登录流程后重新导入。");
    }
    public static void RefreshLoaded(InstallPlan p)
    {
        if(p.ImportedXml is not {OriginalOmitted:false} data||data.SourceXml.Length==0)return;
        var source=Inspect(XmlMerge.Read(data.SourceXml));
        if(source.Mentioned&&data.FirstLogonImportVersion<1)
        {
            data.FirstLogonReadOnly=true;data.Blockers=data.Blockers.Append("旧方案含尚未还原的内置 Administrator 登录流程，请重新导入原 XML；当前方案的待处理修改保留。").Distinct().ToArray();return;
        }
        data.FirstLogonReadOnly=source.Mentioned&&!source.Known;
    }
}
