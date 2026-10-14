using System.Text.Json;
namespace UnattendLocal.Core;
public static class PlanStore {
 private static readonly JsonSerializerOptions Options=new(){WriteIndented=true};
 public static string Serialize(InstallPlan plan,bool includeSecrets) {
  var copy=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(plan,Options),Options)!;
  HardwareSettings.NormalizeLegacy(copy);
  copy.ClearDiskConsent();
  if(!includeSecrets&&copy.ImportedXml is {} imported){imported.SourceXml="";imported.BaselineXml="";imported.OriginalOmitted=true;imported.Blockers=["未保存 XML 原文（可能含凭据及其他敏感内容），请重新导入原 XML 后再导出。"];}
  if(!includeSecrets) {foreach(var u in copy.Users){u.NeedsPassword=u.NeedsPassword||!u.AllowEmptyPassword;u.Password="";}copy.NeedsWifiPassword=copy.NeedsWifiPassword||copy.WifiMode=="预设 WPA2 Wi-Fi";copy.WifiPassword="";copy.NeedsInstallationKey=copy.EditionMode=="指定自己的产品密钥";copy.NeedsActivationKey=copy.UseActivationKey;copy.CustomProductKey="";copy.ActivationProductKey="";}
  return JsonSerializer.Serialize(copy,Options);
 }
 public static InstallPlan Deserialize(string json) {
  var p=JsonSerializer.Deserialize<InstallPlan>(json,Options)??throw new ArgumentException("方案文件为空。");
  if(p.SchemaVersion!=1)throw new ArgumentException("不支持此方案版本。");
  if(p.Users==null||p.Users.Any(u=>u==null))throw new ArgumentException("账户列表无效。");
  if(p.Scripts==null||p.Scripts.Any(s=>s==null)||p.Scripts.Count>60)throw new ArgumentException("脚本列表无效。最多 60 条。 ");
  foreach(var script in p.Scripts.Where(s=>s.IsRemovedType))script.Enabled=false;
  HardwareSettings.NormalizeLegacy(p);
  AppRemovalSettings.Validate(p);
  if(p.ImportedXml is {} imported&&(imported.SourceXml==null||imported.BaselineXml==null||imported.SourceXml.Length>XmlMerge.MaxLength||imported.BaselineXml.Length>XmlMerge.MaxLength||imported.Notices==null||imported.Blockers==null))throw new ArgumentException("方案中的 XML 导入记录无效。");
  using(var document=JsonDocument.Parse(json)){
   CompactOsSettings.NormalizeLoaded(p,document.RootElement.TryGetProperty(nameof(InstallPlan.CompactOsMode),out _));
   CommonSystemSettings.NormalizeLoaded(p,document.RootElement);
   ImageBackgroundSettings.NormalizeLoaded(p,document.RootElement);
   AppRemovalSettings.NormalizeLoaded(p,document.RootElement);
   AdvancedSystemSettings.NormalizeLoaded(p,document.RootElement);
   SystemMaintenanceSettings.NormalizeLoaded(p,document.RootElement);
  }
  p.ClearDiskConsent();
  return p;
 }
}
