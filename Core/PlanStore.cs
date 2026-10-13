using System.Text.Json;
namespace UnattendLocal.Core;
public static class PlanStore {
 private static readonly JsonSerializerOptions Options=new(){WriteIndented=true};
 public static string Serialize(InstallPlan plan,bool includeSecrets) {
  var copy=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(plan,Options),Options)!;
  HardwareSettings.NormalizeLegacy(copy);
  copy.ClearDiskConsent();
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
  p.ClearDiskConsent();
  return p;
 }
}
