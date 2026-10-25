using System.Text;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class FirstLogonSettings
{
    public const string Own="首个新建管理员账户（一次）", Builtin="内置 Administrator（一次）", None="不自动登录";
    public static IReadOnlyList<string> Modes {get;}=[Own,Builtin,None];
    public const string ResetCommand="Set-ItemProperty -LiteralPath 'Registry::HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon' -Name 'AutoLogonCount' -Type 'DWord' -Force -Value 0;";
    public static readonly string Description=global::UnattendLocal.Localization.UiText.Source("text.a60c808779b8");
    public static readonly string ReadOnlyDescription=global::UnattendLocal.Localization.UiText.Source("text.8bc6880c330d");
    public static bool IsBuiltin(InstallPlan p)=>p.AccountMode==global::UnattendLocal.Localization.UiText.Source("text.cdaa9a6700f6")&&p.AutoLogon&&p.UseBuiltinAdministrator;
    public static string Mode(InstallPlan p)=>!p.AutoLogon?None:p.UseBuiltinAdministrator?Builtin:Own;
    public static string Summary(InstallPlan p)=>p.ImportedXml?.FirstLogonReadOnly==true?global::UnattendLocal.Localization.UiText.Source("text.e1d48689fb89"):p.AccountMode!=global::UnattendLocal.Localization.UiText.Source("text.cdaa9a6700f6")?global::UnattendLocal.Localization.UiText.Source("text.18003f680464"):Mode(p);
    public static void Validate(InstallPlan p)
    {
        if(!IsBuiltin(p))return;
        if(p.NeedsBuiltinAdministratorPassword&&!(p.AllowEmptyBuiltinAdministratorPassword&&string.IsNullOrEmpty(p.BuiltinAdministratorPassword)))throw new ConfigurationValidationException(nameof(InstallPlan.BuiltinAdministratorPassword), global::UnattendLocal.Localization.UiText.Source("text.04bab1e44cc8"));
        if(string.IsNullOrEmpty(p.BuiltinAdministratorPassword)&&!p.AllowEmptyBuiltinAdministratorPassword)throw new ConfigurationValidationException(nameof(InstallPlan.BuiltinAdministratorPassword), global::UnattendLocal.Localization.UiText.Source("text.0ff711822b89"));
        if(p.AllowEmptyBuiltinAdministratorPassword&&!string.IsNullOrEmpty(p.BuiltinAdministratorPassword))throw new ConfigurationValidationException(nameof(InstallPlan.BuiltinAdministratorPassword), global::UnattendLocal.Localization.UiText.Source("text.7ae435730e23"));
        if(p.ComputerNameMode==ComputerNamingSettings.Existing&&string.Equals(p.ComputerName,"Administrator",StringComparison.OrdinalIgnoreCase))throw new ConfigurationValidationException(nameof(InstallPlan.ComputerName), global::UnattendLocal.Localization.UiText.Source("text.1b741b254d57"));
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
        if(value.Contains(global::UnattendLocal.Localization.UiText.Source("text.069d51fb67fa"),StringComparison.Ordinal))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.e2d66c4e5993"));
        if(plain is not ("true" or "false"))return null;
        if(plain=="false")
        {
            try{value=new UnicodeEncoding(false,false,true).GetString(Convert.FromBase64String(value));if(!value.EndsWith(name,StringComparison.Ordinal))throw new FormatException();value=value[..^name.Length];}
            catch(Exception e)when(e is FormatException or DecoderFallbackException){throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.29525bed0491"));}
        }
        return(value,plain=="false");
    }
    public static Source Inspect(XmlDocument doc)
    {
        doc=XmlImportPaths.Inspection(doc);
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
            p.AccountMode=global::UnattendLocal.Localization.UiText.Source("text.cdaa9a6700f6");p.AutoLogon=true;p.UseBuiltinAdministrator=true;p.BuiltinAdministratorPassword=source.Password;p.AllowEmptyBuiltinAdministratorPassword=source.Password.Length==0;p.ObscurePasswords=source.Obscure;
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
        if(state.Mentioned&&!state.Known&&p.ImportedXml is {} data&&(p.AccountMode!=data.FirstLogonSourceAccountMode||p.AutoLogon!=data.FirstLogonSourceAutoLogon||p.UseBuiltinAdministrator!=data.FirstLogonSourceBuiltin))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.98143b3e6d93"));
    }
    public static void ValidateGenerated(InstallPlan p,XmlDocument generated)
    {
        var data=p.ImportedXml!;var source=Inspect(XmlMerge.Read(data.SourceXml));
        if(!source.Mentioned||source.Known)return;
        var baseline=XmlMerge.Read(data.BaselineXml);
        var before=Nodes(baseline,"//u:AutoLogon");var after=Nodes(generated,"//u:AutoLogon");
        if(before.Length!=after.Length||before.Length>1||before.Length==1&&!XmlMerge.Equal(before[0],after[0]))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.538e348e1d3e"));
    }
    public static void RefreshLoaded(InstallPlan p)
    {
        if(p.ImportedXml is not {OriginalOmitted:false} data||data.SourceXml.Length==0)return;
        var source=Inspect(XmlMerge.Read(data.SourceXml));
        if(source.Mentioned&&data.FirstLogonImportVersion<1)
        {
            data.FirstLogonReadOnly=true;data.Blockers=data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.876d0ebe6e90")).Distinct().ToArray();return;
        }
        data.FirstLogonReadOnly=source.Mentioned&&!source.Known;
    }
}
