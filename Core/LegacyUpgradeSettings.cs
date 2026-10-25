using System.Text;
using System.Text.Json;
using System.Xml;
namespace UnattendLocal.Core;

// Recognize the complete historical MoSetup command independently of PE LabConfig.
public static class LegacyUpgradeSettings {
 public const string Marker="AllowUpgradesWithUnsupportedTPMOrCPU";
 public const string Command="reg.exe add \"HKLM\\SYSTEM\\Setup\\MoSetup\" /v AllowUpgradesWithUnsupportedTPMOrCPU /t REG_DWORD /d 1 /f;";
 public static readonly string Description=global::UnattendLocal.Localization.UiText.Source("text.dea4bc8d950a");
 public record State(bool Enabled,bool Ambiguous);
 public static State Inspect(XmlDocument source){
  var doc=XmlImportPaths.Inspection(source);
  var lines=doc.SelectNodes("//*[local-name()='File' or local-name()='Path' or local-name()='CommandLine']")!.Cast<XmlElement>()
   .SelectMany(e=>e.InnerText.Replace("\r\n","\n").Split('\n')).Where(l=>l.Contains(Marker,StringComparison.OrdinalIgnoreCase)).ToArray();
  bool enabled=lines.Length==1&&lines[0].Trim()==Command&&AdditionalSetupSettings.KnownBlocks(doc,"Specialize.ps1").Count(b=>b==Command)==1;
  return new(enabled,lines.Length>0&&!enabled);
 }
 public static void ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes){
  var state=Inspect(doc);p.AllowUnsupportedUpgrade=state.Enabled;
  if(state.Ambiguous)notes.Add(global::UnattendLocal.Localization.UiText.Source("text.7de995fe5021"));
 }
 public static void Apply(XmlDocument doc,InstallPlan p){
  if(!p.AllowUnsupportedUpgrade)return;
  var file=doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path")==@"C:\Windows\Setup\Scripts\Specialize.ps1");
  const string start="$scripts = @(\n";string text=file.InnerText.Replace("\r\n","\n");
  int at=text.IndexOf(start,StringComparison.Ordinal);
  if(at<0)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.f0c0c7c6e18f"));
  file.InnerText=text.Insert(at+start.Length,"\t{\n\t\t"+Command+"\n\t};\n");
 }
 public static void ValidateImported(InstallPlan p,XmlDocument source){
  if(Inspect(source).Ambiguous&&p.AllowUnsupportedUpgrade)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.96b43f6459bf"));
 }
 public static void NormalizeLoaded(InstallPlan p,JsonElement json){
  if(p.ImportedXml is not {OriginalOmitted:false} data||data.SourceXml.Length==0||data.BaselineXml.Length==0)return;
  var state=Inspect(XmlMerge.Read(data.SourceXml));data.LegacyUpgradeReadOnly=state.Ambiguous;
  if(json.TryGetProperty(nameof(InstallPlan.AllowUnsupportedUpgrade),out _))return;
  if(!state.Enabled)return;
  var before=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!;
  before.ImportedXml=null;before.ConfirmWipeDisk=before.ConfirmFormatPartition=before.ConfirmDiskScript=true;
  var after=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;after.AllowUnsupportedUpgrade=true;
  try{
   var g=new GeneratorService();
   data.BaselineXml=XmlMerge.Apply(data.BaselineXml,Encoding.UTF8.GetString(g.GenerateImportBaseline(before)),XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml;
   p.AllowUnsupportedUpgrade=true;
  }catch(Exception e)when(e is ArgumentException or Schneegans.Unattend.ConfigurationException or XmlException){
   data.Blockers=data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.74a2a90c23a1")).Distinct().ToArray();
  }
 }
}
