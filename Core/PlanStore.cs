using System.Text.Json;
namespace UnattendLocal.Core;
public static class PlanStore {
 private static readonly JsonSerializerOptions Options=new(){WriteIndented=true};
 public static string Serialize(InstallPlan plan,bool includeSecrets) {
  var copy=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(plan,Options),Options)!;
  HardwareSettings.NormalizeLegacy(copy);
  copy.ClearDiskConsent();
  if(!includeSecrets)foreach(var row in copy.XmlComponents){row.Content="";}

  if(!includeSecrets&&copy.ImportedXml is {} imported){imported.SourceXml="";imported.BaselineXml="";imported.OriginalOmitted=true;imported.Blockers=[global::UnattendLocal.Localization.UiText.Source("text.63a1b3bd6a5c")];}
  if(!includeSecrets) {copy.NeedsBuiltinAdministratorPassword=copy.NeedsBuiltinAdministratorPassword||FirstLogonSettings.IsBuiltin(copy)&&!copy.AllowEmptyBuiltinAdministratorPassword;copy.BuiltinAdministratorPassword="";foreach(var u in copy.Users){u.NeedsPassword=u.NeedsPassword||!u.AllowEmptyPassword;u.Password="";}copy.NeedsWifiPassword=copy.NeedsWifiPassword||WifiSettings.NeedsPassword(copy);copy.WifiPassword="";copy.NeedsWifiProfile=copy.NeedsWifiProfile||copy.WifiMode==WifiSettings.Profile;copy.WifiProfileXml="";copy.NeedsInstallationKey=copy.EditionMode==global::UnattendLocal.Localization.UiText.Source("text.2a5194b6a242");copy.NeedsActivationKey=copy.UseActivationKey;copy.CustomProductKey="";copy.ActivationProductKey="";}
  return JsonSerializer.Serialize(copy,Options);
 }
 public static InstallPlan Deserialize(string json) {
  using var input=JsonDocument.Parse(json);
  PlanFileImport.ValidateJsonPlan(input.RootElement);
  var p=input.RootElement.Deserialize<InstallPlan>(Options)??throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.1fe246685916"));
  if(p.SchemaVersion!=1)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.b8956bfee619"));
  if(p.XmlComponents==null||p.XmlComponents.Count>AdditionalXmlSettings.MaxCount||p.XmlComponents.Any(row=>row==null))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.c21c4601cc0d"));
  if(p.Users==null||p.Users.Any(u=>u==null))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.29ec97c86b4a"));
  if(p.Scripts==null||p.Scripts.Any(s=>s==null)||p.Scripts.Count>60)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.604d5c4da0f2"));
  foreach(var script in p.Scripts.Where(s=>s.IsRemovedType))script.Enabled=false;
  HardwareSettings.NormalizeLegacy(p);
  AppRemovalSettings.Validate(p);
  if(p.ImportedXml is {} imported&&(imported.SourceXml==null||imported.BaselineXml==null||imported.SourceXml.Length>XmlMerge.MaxLength||imported.BaselineXml.Length>XmlMerge.MaxLength||imported.Notices==null||imported.Blockers==null))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.2f5f18e96a91"));
  if(p.ImportedXml is {} data&&(data.Notices.Any(n=>n==null)||data.Blockers.Any(n=>n==null)))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.65d971214b02"));
  PlanFileImport.ValidateForDisplay(p);
  // Reuse the already validated parse instead of building another JSON tree.
  {
   var document=input;
   // These migrations inspect the same original document. Baselines remain separate
   // because a migration can update the editable baseline for the next step.
   var parsedSource=p.ImportedXml is {OriginalOmitted:false,SourceXml.Length:>0} original
    ? XmlMerge.Read(original.SourceXml) : null;
   CompactOsSettings.NormalizeLoaded(p,document.RootElement.TryGetProperty(nameof(InstallPlan.CompactOsMode),out _), parsedSource);
   CommonSystemSettings.NormalizeLoaded(p,document.RootElement);
   ImageBackgroundSettings.NormalizeLoaded(p,document.RootElement);
   AppRemovalSettings.NormalizeLoaded(p,document.RootElement);
   AdvancedSystemSettings.NormalizeLoaded(p,document.RootElement);
   SystemMaintenanceSettings.NormalizeLoaded(p,document.RootElement);
   ProcessAuditSettings.NormalizeLoaded(p,document.RootElement);
   ProtectionSettings.NormalizeLoaded(p,document.RootElement);
   VmGuestToolsSettings.NormalizeLoaded(p,document.RootElement);
   AdditionalSetupSettings.NormalizeLoaded(p,document.RootElement);
   SecurityOptions.NormalizeLoaded(p,document.RootElement);
   JunctionSettings.NormalizeLoaded(p,document.RootElement);
   AppControlSettings.NormalizeLoaded(p,document.RootElement);
   ConfigurationSetSettings.NormalizeLoaded(p,document.RootElement);
   WifiSettings.NormalizeLoaded(p,document.RootElement);
   ComputerNamingSettings.NormalizeLoaded(p,document.RootElement);
   LanguagePreferences.NormalizeLoaded(p,document.RootElement);
   XmlImportParameters.RefreshLoaded(p);
   FirstLogonSettings.RefreshLoaded(p);
   StaticIPv4Settings.NormalizeLoaded(p,document.RootElement, parsedSource);
   AutoPartitionSettings.NormalizeLoaded(p,document.RootElement, parsedSource);
   SetupDriverSettings.NormalizeLoaded(p,document.RootElement, parsedSource);
   SetupDynamicUpdateSettings.NormalizeLoaded(p,document.RootElement, parsedSource);
   RegisteredInformationSettings.NormalizeLoaded(p,document.RootElement, parsedSource);
   WorkgroupSettings.NormalizeLoaded(p,document.RootElement, parsedSource);
   PeLogSettings.NormalizeLoaded(p,document.RootElement, parsedSource);
   NativePreferences.NormalizeLoaded(p,document.RootElement, parsedSource);
   LegacyUpgradeSettings.NormalizeLoaded(p,document.RootElement, parsedSource);
   XmlPlanImport.RefreshRemainder(p);
  }
  p.ClearDiskConsent();
  GeneratedPeSettings.NormalizeLoaded(p);
  return p;
 }
}
