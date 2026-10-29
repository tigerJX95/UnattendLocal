using UnattendLocal.Core;
namespace UnattendLocal.Desktop;
public class AppRemovalChoice(AppRemovalSettings.Item item, Func<InstallPlan> current) : Observable {
 public string Id=>item.Id;public string Label=>item.Label;public string Group=>item.Group;public string Description=>item.Description;
 public string TooltipDescription=>HelpText.RemovalDescription(Description);
 public bool IsSelected {get=>current().RemovedApps.Contains(Id);set{var list=current().RemovedApps;if(value&&!list.Contains(Id))list.Add(Id);else if(!value)list.Remove(Id);Notify(nameof(IsSelected));}}
 public void Refresh()=>Notify(nameof(IsSelected));
}
public class MainViewModel : Observable {
 private System.Collections.ObjectModel.ObservableCollection<string>? observedApps;
 public List<AppRemovalChoice> AppRemovalChoices {get;private set;}=new();
 private string appRemovalFilter="";public string AppRemovalFilter {get=>appRemovalFilter;set{Set(ref appRemovalFilter,value);Notify(nameof(FilteredAppRemovalChoices));Notify(nameof(AppRemovalFilterSummary));}}
 public List<AppRemovalChoice> FilteredAppRemovalChoices=>AppRemovalChoices.Where(i=>SearchTextMatcher.Matches(i.Label+" "+i.Id+" "+i.Group+" "+i.Description+" "+UnattendLocal.Localization.UiText.EnglishText(i.Label)+" "+UnattendLocal.Localization.UiText.EnglishText(i.Description),AppRemovalFilter)).ToList();
 public string AppRemovalFilterSummary=>global::UnattendLocal.Localization.UiText.Format("message.f9d13e1e985e", new string[] { global::UnattendLocal.Localization.UiText.Arg((FilteredAppRemovalChoices.Count), 0, null), global::UnattendLocal.Localization.UiText.Arg((AppRemovalChoices.Count), 0, null) });
 public IReadOnlyList<string> FirstLogonModes=>HasReadonlyFirstLogon?[FirstLogonSettings.Summary(Plan)]:FirstLogonSettings.Modes;
 public string SelectedFirstLogon {get=>HasReadonlyFirstLogon?FirstLogonSettings.Summary(Plan):FirstLogonSettings.Mode(Plan);set{if(updating||!CanEditFirstLogon||!FirstLogonSettings.Modes.Contains(value)||SelectedFirstLogon==value)return;updating=true;try{Plan.UseBuiltinAdministrator=value==FirstLogonSettings.Builtin;Plan.AutoLogon=value!=FirstLogonSettings.None;}finally{updating=false;}RefreshAll();InvalidatePreview();}}
 public bool CanEditFirstLogon=>Plan.AccountMode==global::UnattendLocal.Localization.UiText.Source("text.cdaa9a6700f6")&&Plan.ImportedXml?.FirstLogonReadOnly!=true;
 public bool IsBuiltinAdministrator=>FirstLogonSettings.IsBuiltin(Plan);
 public bool HasReadonlyFirstLogon=>Plan.ImportedXml?.FirstLogonReadOnly==true;
 public bool CanEditAppRemoval=>Plan.ImportedXml?.AppRemovalReadOnly!=true;
 public void SelectFilteredApps(bool selected){if(!CanEditAppRemoval)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.9a41b899ad94"));var ids=FilteredAppRemovalChoices.Select(i=>i.Id).ToHashSet();var next=selected?Plan.RemovedApps.Concat(ids).Distinct().ToArray():Plan.RemovedApps.Where(id=>!ids.Contains(id)).ToArray();if(!Plan.RemovedApps.SequenceEqual(next))Plan.RemovedApps=new(next);}
 public IReadOnlyList<ScriptTemplate> ScriptTemplateChoices=>ScriptTemplates.All;
 private ScriptTemplate selectedScriptTemplate=ScriptTemplates.All[0];
 public ScriptTemplate SelectedScriptTemplate {get=>selectedScriptTemplate;set{if(value!=null)Set(ref selectedScriptTemplate,value);}}
 public void AddSelectedScriptTemplate(){SelectedScript=ScriptTemplates.Add(Plan,SelectedScriptTemplate.Id);Status=global::UnattendLocal.Localization.UiText.Source("text.0da763fde8c8");}
 public ScriptEntry? DuplicateSelectedScript(){if(SelectedScript is not {} source)return null;var copy=CustomScripts.Duplicate(Plan,source);SelectedScript=copy;return copy;}
 public string AppRemovalSummary=>AppRemovalSettings.Selection(Plan);
 private void ObserveApps(){if(ReferenceEquals(observedApps,Plan.RemovedApps))return;if(observedApps!=null)observedApps.CollectionChanged-=AppsChanged;observedApps=Plan.RemovedApps;observedApps.CollectionChanged+=AppsChanged;}
 private void RefreshApps(){ObserveApps();foreach(var choice in AppRemovalChoices)choice.Refresh();Notify(nameof(AppRemovalSummary));}
 private void AppsChanged(object? sender,System.Collections.Specialized.NotifyCollectionChangedEventArgs e){RefreshApps();RefreshAll();InvalidatePreview();}

 public bool HasReadonlyLegacyUpgrade=>Plan.ImportedXml?.LegacyUpgradeReadOnly==true;
 public bool CanEditLegacyUpgrade=>Plan.ImportedXml?.LegacyUpgradeReadOnly!=true;
 public bool ImportJson(string json,Func<bool> confirm){
  InstallPlan candidate;try{candidate=PlanStore.Deserialize(json);PlanFileImport.ValidateForDisplay(candidate);}catch{Status=global::UnattendLocal.Localization.UiText.Source("text.a7aa2377538a");throw;}
  if(!confirm()){Status=global::UnattendLocal.Localization.UiText.Source("text.5e736d1a599c");return false;}
  Plan=candidate;Status=global::UnattendLocal.Localization.UiText.Source("text.33c8b312a91e");return true;
 }
 public bool ImportXml(string xml,Func<bool> confirm){
  InstallPlan candidate;try{candidate=XmlPlanImport.Import(xml,Service);PlanFileImport.ValidateForDisplay(candidate);}catch{Status=global::UnattendLocal.Localization.UiText.Source("text.96325785aa0d");throw;}
  if(!confirm()){Status=global::UnattendLocal.Localization.UiText.Source("text.90c07a228f7e");return false;}
  Plan=candidate;Status=candidate.ImportedXml!.Blockers.Length>0?global::UnattendLocal.Localization.UiText.Source("text.b27d37e9013c"):global::UnattendLocal.Localization.UiText.Source("text.f4804d7eaefb");return true;
 }
 private InstallPlan plan=InstallPlan.New();
 private readonly HashSet<UserEntry> observedUsers=new();
 private readonly HashSet<ScriptEntry> observedScripts=new();
 private ScriptEntry? selectedScript;public ScriptEntry? SelectedScript {get=>selectedScript;set{Set(ref selectedScript,value);Notify(nameof(HasSelectedScript));}}
 public bool HasSelectedScript=>SelectedScript!=null;
 public ScriptEntry ImportScriptFile(string path){var row=ScriptFiles.Append(Plan,ScriptFiles.Read(path));SelectedScript=row;return row;}
 private void ObserveScripts(){foreach(var row in observedScripts.Where(s=>!Plan.Scripts.Contains(s)).ToArray()){row.PropertyChanged-=HelpSettingChanged;observedScripts.Remove(row);}foreach(var row in Plan.Scripts)if(observedScripts.Add(row))row.PropertyChanged+=HelpSettingChanged;}
 private void ScriptsChanged(object? sender,System.Collections.Specialized.NotifyCollectionChangedEventArgs e){ObserveScripts();if(SelectedScript==null||!Plan.Scripts.Contains(SelectedScript))SelectedScript=Plan.Scripts.FirstOrDefault();Notify(nameof(HasSelectedScript));RefreshAll();InvalidatePreview();}
 public IReadOnlyList<string> XmlComponentNames=>AdditionalXmlSettings.Components;
 public AdditionalXmlSettings.PhaseChoice[] XmlComponentPasses=>AdditionalXmlSettings.Passes;
 private readonly HashSet<XmlComponentEntry> observedXmlComponents=new();
 private XmlComponentEntry? selectedXmlComponent;
 public XmlComponentEntry? SelectedXmlComponent {get=>selectedXmlComponent;set{Set(ref selectedXmlComponent,value);Notify(nameof(HasSelectedXmlComponent));}}
 public bool HasSelectedXmlComponent=>SelectedXmlComponent!=null;
 private void ObserveXmlComponents(){foreach(var row in observedXmlComponents.Where(row=>!Plan.XmlComponents.Contains(row)).ToArray()){row.PropertyChanged-=XmlComponentChanged;observedXmlComponents.Remove(row);}foreach(var row in Plan.XmlComponents)if(observedXmlComponents.Add(row))row.PropertyChanged+=XmlComponentChanged;}
 private void XmlComponentChanged(object? sender,System.ComponentModel.PropertyChangedEventArgs e){if(e.PropertyName==nameof(XmlComponentEntry.Label))return;Plan.ClearDiskConsent();RefreshAll();InvalidatePreview();}
 private void XmlComponentsChanged(object? sender,System.Collections.Specialized.NotifyCollectionChangedEventArgs e){ObserveXmlComponents();if(SelectedXmlComponent==null||!Plan.XmlComponents.Contains(SelectedXmlComponent))SelectedXmlComponent=Plan.XmlComponents.FirstOrDefault();Plan.ClearDiskConsent();RefreshAll();InvalidatePreview();}
 private bool updating;
 public bool IsLoadingPlan {get;private set;}
 public InstallPlan Plan {get=>plan;set{
  if(ReferenceEquals(plan,value))return;
  if(IsLoadingPlan)throw new InvalidOperationException(global::UnattendLocal.Localization.UiText.Source("text.ae62636bb75e"));
  IsLoadingPlan=true;
  try{
  HardwareSettings.NormalizeLegacy(value);
  plan.XmlComponents.CollectionChanged-=XmlComponentsChanged;foreach(var row in observedXmlComponents)row.PropertyChanged-=XmlComponentChanged;observedXmlComponents.Clear();
  plan.Scripts.CollectionChanged-=ScriptsChanged;foreach(var row in observedScripts)row.PropertyChanged-=HelpSettingChanged;observedScripts.Clear();
  plan.PropertyChanged-=HelpSettingChanged;plan.Users.CollectionChanged-=UsersChanged;
  foreach(var user in observedUsers)user.PropertyChanged-=HelpSettingChanged;observedUsers.Clear();
  Set(ref plan,value);plan.PropertyChanged+=HelpSettingChanged;plan.Users.CollectionChanged+=UsersChanged;
  plan.XmlComponents.CollectionChanged+=XmlComponentsChanged;ObserveXmlComponents();SelectedXmlComponent=plan.XmlComponents.FirstOrDefault();
  plan.Scripts.CollectionChanged+=ScriptsChanged;ObserveScripts();SelectedScript=plan.Scripts.FirstOrDefault();Notify(nameof(HasSelectedScript));
  ObserveUsers();SelectedUser=plan.Users.FirstOrDefault();RefreshAll();InvalidatePreview();
  }finally{IsLoadingPlan=false;}
 }}
 private UserEntry? selected;public UserEntry? SelectedUser {get=>selected;set{Set(ref selected,value);RefreshHelp();}}
 private string preview=global::UnattendLocal.Localization.UiText.Source("text.fae7e313af33"),status=ProductIdentity.ReadyStatus;
 // XML is already redacted and localized at its authored boundaries. Never translate its body.
 private string? englishXmlPreview;
 public string Preview {get=>englishXmlPreview is null?global::UnattendLocal.Localization.UiText.Translate(preview):global::UnattendLocal.Localization.UiText.IsEnglish?englishXmlPreview:preview;set{englishXmlPreview=null;Set(ref preview,value);}}
 public void ShowXmlPreview(byte[] bytes){
  var summary=InstallationReview.ExportSummary(bytes);
  preview=summary+"\n\n"+GeneratorService.Preview(bytes,Plan);
  englishXmlPreview=global::UnattendLocal.Localization.UiText.EnglishText(summary)+"\n\n"+GeneratorService.Preview(bytes,Plan,english:true);
  Notify(nameof(Preview));
 }
 public void RefreshPreviewLanguage()=>Notify(nameof(Preview)); public string Status {get=>status;set=>Set(ref status,value);}
 public string ExportXmlButtonText=>global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.199c1f379826"), AnswerFileExport.FileName(Plan.ExportNonAutomaticFilename) },new bool[]{false,true});
 public string ExportUsage=>AnswerFileExport.Usage(Plan.ExportNonAutomaticFilename);
 public string ExportIsoDescription=>global::UnattendLocal.Localization.UiText.Source("text.61d6ce2fd717");
 public GeneratorService Service {get;}=new();
 public List<GeneratorService.Choice> Editions=>Service.Editions;
 public IReadOnlyList<string> ProcessAuditModes=>ProcessAuditSettings.Modes;
 public bool IsAuditCommandLine=>Plan.ProcessAuditMode==ProcessAuditSettings.WithCommandLine;
 private bool isGenericEdition,isCustomEdition,isPresetWifi;private string selectedEditionKey="",editionHint="";
 public bool IsPresetWifi {get=>isPresetWifi;private set=>Set(ref isPresetWifi,value);}
 public bool IsFixedComputerName=>Plan.ComputerNameMode==ComputerNamingSettings.Existing;
 public bool IsPromptComputerName=>Plan.ComputerNameMode==ComputerNamingSettings.Prompt;
 public bool IsMacComputerName=>Plan.ComputerNameMode==ComputerNamingSettings.Mac;
 public bool IsScriptComputerName=>Plan.ComputerNameMode==ComputerNamingSettings.Script;
 public string ComputerNameExample=>ComputerNamingSettings.Example;
 public string ComputerNameSummary=>ComputerNamingSettings.Summary(Plan);
 public string LanguagePreferencesSummary=>LanguagePreferences.Summary(Plan);
 public bool IsSecondLanguage=>Plan.UseSecondLanguage&&!Plan.InteractiveLanguage;
 public bool IsThirdLanguage=>Plan.UseThirdLanguage&&!Plan.InteractiveLanguage;
 public bool IsPasswordWifi=>WifiSettings.NeedsPassword(Plan);
 public bool IsOpenWifi=>Plan.WifiMode==WifiSettings.Open;
 public bool IsXmlWifi=>Plan.WifiMode==WifiSettings.Profile;
 public string WifiSummary=>WifiSettings.Summary(Plan);
 public string[] PowerPlanModes=>NativePreferences.PowerModes;
 public string[] ErrorReportingModes=>NativePreferences.WerModes;
 public bool CanEditPowerPlan=>NativePreferences.CanEdit(Plan,NativePreferences.Power);
 public bool IsCustomPowerPlan=>CanEditPowerPlan&&Plan.PowerPlanMode==NativePreferences.Custom;
 public bool CanEditErrorReporting=>NativePreferences.CanEdit(Plan,NativePreferences.Wer);
 public string PowerPlanSummary=>NativePreferences.Summary(Plan,NativePreferences.Power);
 public string ErrorReportingSummary=>NativePreferences.Summary(Plan,NativePreferences.Wer);
 public bool CanEditWorkgroup=>WorkgroupSettings.CanEdit(Plan);
 public bool IsWorkgroup=>CanEditWorkgroup&&Plan.UseWorkgroup;
 public bool HasReadonlyWorkgroup=>!CanEditWorkgroup;
 public string WorkgroupSummary=>WorkgroupSettings.Summary(Plan);
 public bool CanEditPeLog=>PeLogSettings.CanEdit(Plan);
 public bool IsPeLog=>CanEditPeLog&&Plan.UsePeLogPath;
 public bool HasReadonlyPeLog=>!CanEditPeLog;
 public string PeLogSummary=>PeLogSettings.Summary(Plan);
 public bool CanEditRegisteredInformation=>RegisteredInformationSettings.CanEdit(Plan);
 public bool IsRegisteredOwner=>CanEditRegisteredInformation&&Plan.UseRegisteredOwner;
 public bool IsRegisteredOrganization=>CanEditRegisteredInformation&&Plan.UseRegisteredOrganization;
 public bool HasReadonlyRegisteredInformation=>!CanEditRegisteredInformation;
 public string RegisteredInformationSummary=>RegisteredInformationSettings.Summary(Plan);
 public string[] SetupDynamicUpdateModes=>SetupDynamicUpdateSettings.Modes;
 public string[] SetupDynamicUpdateUiModes=>SetupDynamicUpdateSettings.UiModes;
 public bool CanEditSetupDynamicUpdate=>SetupDynamicUpdateSettings.CanEdit(Plan);
 public bool HasReadonlySetupDynamicUpdate=>!CanEditSetupDynamicUpdate;
 public string SetupDynamicUpdateSummary=>SetupDynamicUpdateSettings.Summary(Plan);
 public bool CanEditSetupDrivers=>SetupDriverSettings.CanEdit(Plan);
 public bool IsSetupDrivers=>Plan.UseSetupDrivers&&CanEditSetupDrivers;
 public bool HasReadonlySetupDrivers=>!CanEditSetupDrivers;
 public string SetupDriversSummary=>SetupDriverSettings.Summary(Plan);
 public bool CanEditStaticIPv4=>StaticIPv4Settings.CanEdit(Plan);
 public bool IsStaticIPv4=>Plan.UseStaticIPv4&&CanEditStaticIPv4;
 public string StaticIPv4Summary=>StaticIPv4Settings.Summary(Plan);
 public bool IsStaticIPv6=>Plan.UseStaticIPv6&&CanEditStaticIPv4;
 public bool IsStaticIP=>CanEditStaticIPv4&&(Plan.UseStaticIPv4||Plan.UseStaticIPv6);
 public string StaticIPv6Summary=>StaticIPv4Settings.SummaryIPv6(Plan);
 public string StaticIPDeploymentWarning=>StaticIPv4Settings.DeploymentWarning(Plan);
 public bool HasStaticIPDeploymentWarning=>StaticIPDeploymentWarning.Length>0;
 public string StaticIPv4SubnetPreview=>StaticIPv4Settings.SubnetPreview(Plan);
 public string StaticIPv4SubnetInput {
  get=>Plan.UseStaticIPv4SubnetMask?(Plan.StaticIPv4SubnetMask??""):"/"+Plan.StaticIPv4Prefix.ToString(System.Globalization.CultureInfo.InvariantCulture);
  set {
   value??="";
   if(updating||IsLoadingPlan||!IsStaticIPv4||value==StaticIPv4SubnetInput)return;
   updating=true;
   try {
    // Keep the raw text (including an incomplete edit) in the existing persisted text field.
    Plan.StaticIPv4SubnetMask=value;Plan.UseStaticIPv4SubnetMask=true;
    try{Plan.StaticIPv4Prefix=StaticIPv4Settings.PrefixFromInput(value);}catch(ArgumentException){ }
   }finally{updating=false;}
   RefreshAll();InvalidatePreview();
  }
 }

 private void RefreshNetwork(){IsPresetWifi=WifiSettings.IsPreset(Plan);Notify(nameof(IsPasswordWifi));Notify(nameof(IsOpenWifi));Notify(nameof(IsXmlWifi));Notify(nameof(WifiSummary));Notify(nameof(CanEditStaticIPv4));Notify(nameof(IsStaticIPv4));Notify(nameof(StaticIPv4Summary));Notify(nameof(StaticIPv4SubnetInput));Notify(nameof(IsStaticIPv6));Notify(nameof(IsStaticIP));Notify(nameof(StaticIPv6Summary));Notify(nameof(StaticIPDeploymentWarning));Notify(nameof(HasStaticIPDeploymentWarning));Notify(nameof(StaticIPv4SubnetPreview));}
 public bool IsGenericEdition {get=>isGenericEdition;private set=>Set(ref isGenericEdition,value);}
 public bool IsCustomEdition {get=>isCustomEdition;private set=>Set(ref isCustomEdition,value);}
 public string SelectedEditionKey {get=>selectedEditionKey;private set=>Set(ref selectedEditionKey,value);}
 public string EditionInstallationDescription=>global::UnattendLocal.Localization.UiText.Source(Plan.UseGeneratedPe?"v129.pe.editionDescription":"text.7f25b84adfed");
 public string ActivationKeyDescription=>global::UnattendLocal.Localization.UiText.Source(Plan.UseGeneratedPe?"v129.pe.activation":"text.e60a63390690");
 public string EditionHint {get=>editionHint;private set=>Set(ref editionHint,value);}
 private void RefreshEdition(){
  Notify(nameof(EditionInstallationDescription));Notify(nameof(ActivationKeyDescription));
  IsGenericEdition=Plan.EditionMode==global::UnattendLocal.Localization.UiText.Source("text.cc706914a2ad");IsCustomEdition=Plan.EditionMode==global::UnattendLocal.Localization.UiText.Source("text.2a5194b6a242");
  SelectedEditionKey=Service.GenericInstallationKey(Plan.EditionId);
  EditionHint=Plan.UseGeneratedPe?global::UnattendLocal.Localization.UiText.Source(Plan.EditionMode==global::UnattendLocal.Localization.UiText.Source("text.5056d73b13ec")?"v129.pe.ready":"v125.pe.edition"):Plan.EditionMode switch {
   "按 SKU 使用通用安装密钥"=>global::UnattendLocal.Localization.UiText.Source("text.786c6aba30d6"),
   "指定自己的产品密钥"=>global::UnattendLocal.Localization.UiText.Source("text.dcf1c7cef18c"),
   "使用 BIOS/UEFI 固件密钥"=>global::UnattendLocal.Localization.UiText.Source("text.35eb6aee2ff7"),
   _=>global::UnattendLocal.Localization.UiText.Source("text.ad3a30f3ada0")};
 }
 public List<GeneratorService.Choice> Languages=>Service.Languages;
 public List<GeneratorService.Choice> Locales=>Service.Locales;
 public List<GeneratorService.Choice> Keyboards=>Service.Keyboards;
 public List<GeneratorService.Choice> Regions=>Service.Regions;
 public List<GeneratorService.Choice> TimeZones=>Service.TimeZones;
 private IReadOnlyDictionary<string,HelpTopic> helpTips=new Dictionary<string,HelpTopic>();
 public IReadOnlyDictionary<string,HelpTopic> HelpTips {get=>helpTips;private set=>Set(ref helpTips,value);}
 private void HelpSettingChanged(object? sender,System.ComponentModel.PropertyChangedEventArgs e){
  if(updating)return;
  updating=true;
  try{
   // Only explicit edits normalize dependent choices; loading a saved plan preserves its values.
   if(ReferenceEquals(sender,Plan)){
    if(e.PropertyName==nameof(InstallPlan.AccountMode)){
     if(Plan.AccountMode==global::UnattendLocal.Localization.UiText.Source("text.5f78e95b6bd9")){Plan.BypassNetwork=false;Plan.HideOnlineAccount=false;}
     else if(Plan.AccountMode is "自动创建本地账户" or "安装时创建本地账户")Plan.HideOnlineAccount=true;
    }
    if(Plan.AccountMode==global::UnattendLocal.Localization.UiText.Source("text.5f78e95b6bd9")&&e.PropertyName is nameof(InstallPlan.BypassNetwork) or nameof(InstallPlan.HideOnlineAccount)){
     Plan.BypassNetwork=false;Plan.HideOnlineAccount=false;
    }
    if(e.PropertyName==nameof(InstallPlan.WifiMode)&&WifiSettings.IsPreset(Plan))Plan.ConnectWifi=true;
   }
  }finally{updating=false;}
  RefreshAll();InvalidatePreview();
 }
 private void RefreshAll(){foreach(var name in new[]{nameof(IsFixedComputerName),nameof(IsPromptComputerName),nameof(IsScriptComputerName),nameof(IsMacComputerName),nameof(ComputerNameSummary),nameof(LanguagePreferencesSummary),nameof(IsSecondLanguage),nameof(IsThirdLanguage)})Notify(name);Notify(nameof(IsAuditCommandLine));RefreshRecommendations();RefreshEdition();RefreshNetwork();RefreshShell();RefreshDesktop();RefreshVisualEffects();RefreshColors();RefreshWallpaper();RefreshLockKeys();RefreshStickyKeys();RefreshAccountLockout();RefreshXmlImport();RefreshCompactOs();RefreshHelp();}
 public bool IsCustomAccountLockout=>Plan.AccountLockoutMode==AccountLockoutPolicy.Custom;
 public bool IsAccountLockoutDisabled=>AccountLockoutPolicy.IsDisabled(Plan);
 public string AccountLockoutSummary=>AccountLockoutPolicy.ModeSummary(Plan);
 private void RefreshAccountLockout(){Notify(nameof(IsCustomAccountLockout));Notify(nameof(IsAccountLockoutDisabled));Notify(nameof(AccountLockoutSummary));}
 public bool CanEditCompactOs=>CompactOsSettings.CanEdit(Plan);
 public string CompactOsSummary=>CompactOsSettings.ModeSummary(Plan);
 private void RefreshCompactOs(){Notify(nameof(CanEditCompactOs));Notify(nameof(CompactOsSummary));}
 public bool HasImportedXml=>Plan.ImportedXml!=null;
 public bool HasImportedXmlRisk=>Plan.ImportedXml?.RequiresDiskReview==true;
 public string XmlImportSummary=>XmlPlanImport.Summary(Plan);
 public string XmlImportErrors=>Plan.ImportedXml is {} data?global::UnattendLocal.Localization.UiText.Join("\n", (data.OriginalOmitted?new[]{global::UnattendLocal.Localization.UiText.Source("text.7872b0c5f6cf")}:Array.Empty<string>()).Concat(data.Blockers), false):"";
 public string XmlImportNotices=>Plan.ImportedXml is {} data?global::UnattendLocal.Localization.UiText.Join("\n", data.Notices, false):"";
 public bool HasXmlImportErrors=>XmlImportErrors.Length>0;
 public string XmlImportDetails=>Plan.ImportedXml is {} data?global::UnattendLocal.Localization.UiText.Join("\n", data.Blockers.Concat(data.Notices), false):"";
 private void RefreshXmlImport(){Notify(nameof(CanEditLegacyUpgrade));Notify(nameof(HasReadonlyLegacyUpgrade));Notify(nameof(CanEditAppRemoval));Notify(nameof(HasImportedXml));Notify(nameof(HasImportedXmlRisk));Notify(nameof(XmlImportSummary));Notify(nameof(XmlImportDetails));Notify(nameof(XmlImportErrors));Notify(nameof(XmlImportNotices));Notify(nameof(HasXmlImportErrors));}
 public bool IsCustomStickyKeys=>Plan.StickyKeysMode==StickyKeySettings.Custom;
 public bool IsStickyKeysDisabled=>Plan.StickyKeysMode==StickyKeySettings.Disabled;
 public string StickyKeysSummary=>StickyKeySettings.ModeSummary(Plan);
 private void RefreshStickyKeys(){Notify(nameof(IsCustomStickyKeys));Notify(nameof(IsStickyKeysDisabled));Notify(nameof(StickyKeysSummary));}
 public bool IsCustomLockKeys=>Plan.LockKeyMode==KeyboardLockSettings.Custom;
 public string LockKeyStateSummary=>KeyboardLockSettings.ModeSummary(Plan);
 private void RefreshLockKeys(){Notify(nameof(IsCustomLockKeys));Notify(nameof(LockKeyStateSummary));}
 public bool IsSolidWallpaper=>WallpaperSettings.IsSolid(Plan);
 public bool IsScriptWallpaper=>WallpaperSettings.IsScript(Plan);
 public bool IsScriptLockScreen=>ImageBackgroundSettings.IsLockScript(Plan);
 public string LockScreenSummary=>ImageBackgroundSettings.LockSummary(Plan);
 public string DesktopImageExample=>ImageBackgroundSettings.DesktopExample;
 public string LockScreenImageExample=>ImageBackgroundSettings.LockExample;
 public string DesktopWallpaperSummary=>WallpaperSettings.ModeSummary(Plan);
 public List<GeneratorService.Choice> WallpaperColorChoices {get;}=new(){new("#000000",global::UnattendLocal.Localization.UiText.Source("text.ed7d2c54184b")),new("#202020",global::UnattendLocal.Localization.UiText.Source("text.79551187b83c")),new("#E5E5E5",global::UnattendLocal.Localization.UiText.Source("text.fe07a094f39e")),new("#FFFFFF",global::UnattendLocal.Localization.UiText.Source("text.f56e7eff58bf")),new("#003366",global::UnattendLocal.Localization.UiText.Source("text.648a0efd3584")),new("#0078D4",global::UnattendLocal.Localization.UiText.Source("text.67035e6b7f82")),new("#008272",global::UnattendLocal.Localization.UiText.Source("text.9005604ed49f")),new("#744DA9",global::UnattendLocal.Localization.UiText.Source("text.2ed9c6fabac2")),new("#6B4F3B",global::UnattendLocal.Localization.UiText.Source("text.c688f65ef209"))};
 public string? WallpaperColorPreset {get=>ColorSettings.TryNormalize(Plan.WallpaperColorHex,out var hex)&&WallpaperColorChoices.Any(c=>c.Id==hex)?hex:null;set{if(WallpaperColorChoices.Any(c=>c.Id==value))Plan.WallpaperColorHex=value!;}}
 public string WallpaperColorPreview=>ColorSettings.TryNormalize(Plan.WallpaperColorHex,out var hex)?hex:"Transparent";
 public string WallpaperColorHint=>ColorSettings.TryNormalize(Plan.WallpaperColorHex,out var hex)?global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.bdd701681619"), hex },new bool[]{false,false}):global::UnattendLocal.Localization.UiText.Source("text.4b9f73cd286c");
 private void RefreshWallpaper(){foreach(var name in new[]{nameof(IsSolidWallpaper),nameof(IsScriptWallpaper),nameof(IsScriptLockScreen),nameof(LockScreenSummary),nameof(DesktopWallpaperSummary),nameof(WallpaperColorPreset),nameof(WallpaperColorPreview),nameof(WallpaperColorHint)})Notify(name);}
 public bool IsCustomColors=>ColorSettings.IsCustom(Plan);
 public bool CanUseStartAccent=>IsCustomColors&&Plan.WindowsColorTheme==ColorSettings.Dark;
 public bool StartAccentChecked {get=>ColorSettings.StartAccentEnabled(Plan);set{if(CanUseStartAccent)Plan.AccentColorOnStart=value;}}
 public string WindowsColorsSummary=>ColorSettings.ModeSummary(Plan);
 public List<GeneratorService.Choice> AccentColorChoices {get;}=new(){new("#0078D4",global::UnattendLocal.Localization.UiText.Source("text.9aba037f8002")),new("#0099BC",global::UnattendLocal.Localization.UiText.Source("text.6119928332f1")),new("#008272",global::UnattendLocal.Localization.UiText.Source("text.9005604ed49f")),new("#107C10",global::UnattendLocal.Localization.UiText.Source("text.30c83dc25b12")),new("#FF8C00",global::UnattendLocal.Localization.UiText.Source("text.aa1e899dd357")),new("#E81123",global::UnattendLocal.Localization.UiText.Source("text.789f61dbfcc7")),new("#C239B3",global::UnattendLocal.Localization.UiText.Source("text.afae64f8320e")),new("#744DA9",global::UnattendLocal.Localization.UiText.Source("text.2ed9c6fabac2")),new("#767676",global::UnattendLocal.Localization.UiText.Source("text.051e67d913f4"))};
 public string? AccentColorPreset {get=>ColorSettings.TryNormalize(Plan.AccentColorHex,out var hex)&&AccentColorChoices.Any(c=>c.Id==hex)?hex:null;set{if(AccentColorChoices.Any(c=>c.Id==value))Plan.AccentColorHex=value!;}}
 public string AccentColorPreview=>ColorSettings.TryNormalize(Plan.AccentColorHex,out var hex)?hex:"Transparent";
 public string AccentColorHint=>ColorSettings.TryNormalize(Plan.AccentColorHex,out var hex)?global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.bdd701681619"), hex },new bool[]{false,false}):global::UnattendLocal.Localization.UiText.Source("text.8066acb0bbba");
 private void RefreshColors(){foreach(var name in new[]{nameof(IsCustomColors),nameof(CanUseStartAccent),nameof(StartAccentChecked),nameof(WindowsColorsSummary),nameof(AccentColorPreset),nameof(AccentColorPreview),nameof(AccentColorHint)})Notify(name);}
 public bool IsCustomVisualEffects=>Plan.VisualEffectsMode==VisualEffectsSettings.Custom;
 public string VisualEffectsSummary=>VisualEffectsSettings.ModeSummary(Plan);
 private void RefreshVisualEffects(){Notify(nameof(IsCustomVisualEffects));Notify(nameof(VisualEffectsSummary));}
 public bool IsCustomDesktopIcons=>Plan.DesktopIconsMode==DesktopSettings.CustomIcons;
 public bool IsCustomStartFolders=>Plan.StartFoldersMode==DesktopSettings.CustomFolders;
 public string DesktopIconSelection=>global::UnattendLocal.Localization.UiText.Format("message.9e06b6ef415c", new string[] { global::UnattendLocal.Localization.UiText.Arg((DesktopSettings.Icons.Count(item=>DesktopSettings.Selected(Plan,item))), 0, null), global::UnattendLocal.Localization.UiText.Arg((DesktopSettings.Icons.Count), 0, null), global::UnattendLocal.Localization.UiText.Arg((DesktopSettings.Selection(Plan,true)), 0, null) });
 public string StartFolderSelection=>global::UnattendLocal.Localization.UiText.Format("message.9e06b6ef415c", new string[] { global::UnattendLocal.Localization.UiText.Arg((DesktopSettings.Folders.Count(item=>DesktopSettings.Selected(Plan,item))), 0, null), global::UnattendLocal.Localization.UiText.Arg((DesktopSettings.Folders.Count), 0, null), global::UnattendLocal.Localization.UiText.Arg((DesktopSettings.Selection(Plan,false)), 0, null) });
 private void RefreshDesktop(){foreach(var name in new[]{nameof(IsCustomDesktopIcons),nameof(IsCustomStartFolders),nameof(DesktopIconSelection),nameof(StartFolderSelection)})Notify(name);}
 public bool IsCustomTaskbar=>Plan.TaskbarIconsMode==ShellSettings.CustomXml;
 public bool IsCustomTiles=>Plan.StartTilesMode==ShellSettings.CustomXml;
 public bool IsCustomPins=>Plan.StartPinsMode==ShellSettings.CustomJson;
 public bool ShowSearchPreview=>Plan.TaskbarSearch!=global::UnattendLocal.Localization.UiText.Source("text.145219e726b8");
 public bool ShowSearchText=>Plan.TaskbarSearch is "完整搜索框" or "图标和文字";
 public double SearchPreviewWidth=>Plan.TaskbarSearch switch {"完整搜索框"=>260,"图标和文字"=>120,_=>44};
 private void RefreshShell(){foreach(var name in new[]{nameof(IsCustomTaskbar),nameof(IsCustomTiles),nameof(IsCustomPins),nameof(ShowSearchPreview),nameof(ShowSearchText),nameof(SearchPreviewWidth)})Notify(name);}
 private void InvalidatePreview(){Preview=global::UnattendLocal.Localization.UiText.Source("text.92a3dbdf6833");Status=global::UnattendLocal.Localization.UiText.Source("text.aa9344791149");}
 private void ObserveUsers(){
  foreach(var user in observedUsers.Where(u=>!Plan.Users.Contains(u)).ToList()){user.PropertyChanged-=HelpSettingChanged;observedUsers.Remove(user);}
  foreach(var user in Plan.Users)if(observedUsers.Add(user))user.PropertyChanged+=HelpSettingChanged;
 }
 private void RefreshHelp(){RefreshApps();HelpTips=Enumerable.Range(0,HelpCatalog.PageCount).SelectMany(page=>HelpCatalog.ForPage(page).Select(topic=>HelpCatalog.Current(page,topic.Id,Plan,SelectedUser))).ToDictionary(topic=>topic.Id);}
 private bool isCustomConfiguration,isFixedLanguage,isFixedTimeZone,isAutomaticAccounts,isPasswordDays,canBypassNetwork,needsRetentionConsent;
 public bool NeedsRetentionConsent {get=>needsRetentionConsent;private set=>Set(ref needsRetentionConsent,value);}
 public bool IsAutomaticAccounts {get=>isAutomaticAccounts;private set=>Set(ref isAutomaticAccounts,value);}
 public bool IsPasswordDays {get=>isPasswordDays;private set=>Set(ref isPasswordDays,value);}
 public bool CanBypassNetwork {get=>canBypassNetwork;private set=>Set(ref canBypassNetwork,value);}
 public bool CanHideOnlineAccount=>CanBypassNetwork;
 public bool IsCustomConfiguration {get=>isCustomConfiguration;private set=>Set(ref isCustomConfiguration,value);}
 public bool IsFixedLanguage {get=>isFixedLanguage;private set=>Set(ref isFixedLanguage,value);}
 public bool IsFixedTimeZone {get=>isFixedTimeZone;private set=>Set(ref isFixedTimeZone,value);}
 private bool isScriptDisk,isPartitionTarget;
 public bool IsScriptDisk {get=>isScriptDisk;private set=>Set(ref isScriptDisk,value);} public bool IsPartitionTarget {get=>isPartitionTarget;private set=>Set(ref isPartitionTarget,value);}
 private bool isImageName,isImageIndex,isTargetDisk,isExistingDisk,isWipeDisk,isFormatDisk,isDestructiveDisk;
 public bool IsImageName {get=>isImageName;private set=>Set(ref isImageName,value);} public bool IsImageIndex {get=>isImageIndex;private set=>Set(ref isImageIndex,value);}
 public bool HasReadonlyAutoPartition=>Plan.ImportedXml?.AutoPartitionReadOnly==true;
 public bool IsPeMatch=>Plan.UseGeneratedPe&&Plan.PeTargetMode=="match";
 public bool IsPeScript=>Plan.UseGeneratedPe&&Plan.PeTargetMode=="script";
 public bool CanEditPeInstall=>Plan.ImportedXml==null||Plan.ImportedXml.GeneratedPeEditable;
 public bool CanEditAutoPartition=>IsWipeDisk&&AutoPartitionSettings.CanEdit(Plan);
 public string AutoPartitionSummary=>AutoPartitionSettings.Summary(Plan);
 public bool IsTargetDisk {get=>isTargetDisk;private set=>Set(ref isTargetDisk,value);} public bool IsExistingDisk {get=>isExistingDisk;private set=>Set(ref isExistingDisk,value);} public bool IsWipeDisk {get=>isWipeDisk;private set=>Set(ref isWipeDisk,value);} public bool IsFormatDisk {get=>isFormatDisk;private set=>Set(ref isFormatDisk,value);} public bool IsDestructiveDisk {get=>isDestructiveDisk;private set=>Set(ref isDestructiveDisk,value);}
 private string diskRisk="";public string DiskRisk {get=>diskRisk;private set=>Set(ref diskRisk,value);}
 private void RefreshInstallation(){foreach(var name in new[]{nameof(CanEditPowerPlan),nameof(IsCustomPowerPlan),nameof(CanEditErrorReporting),nameof(PowerPlanSummary),nameof(ErrorReportingSummary)})Notify(name);Notify(nameof(ExportXmlButtonText));Notify(nameof(ExportUsage));foreach(var name in new[]{nameof(CanEditWorkgroup),nameof(IsWorkgroup),nameof(HasReadonlyWorkgroup),nameof(WorkgroupSummary),nameof(CanEditPeLog),nameof(IsPeLog),nameof(HasReadonlyPeLog),nameof(PeLogSummary)})Notify(name);Notify(nameof(CanEditRegisteredInformation));Notify(nameof(IsRegisteredOwner));Notify(nameof(IsRegisteredOrganization));Notify(nameof(HasReadonlyRegisteredInformation));Notify(nameof(RegisteredInformationSummary));Notify(nameof(CanEditSetupDynamicUpdate));Notify(nameof(HasReadonlySetupDynamicUpdate));Notify(nameof(SetupDynamicUpdateSummary));Notify(nameof(CanEditSetupDrivers));Notify(nameof(IsSetupDrivers));Notify(nameof(HasReadonlySetupDrivers));Notify(nameof(SetupDriversSummary));Notify(nameof(FirstLogonModes));Notify(nameof(SelectedFirstLogon));Notify(nameof(CanEditFirstLogon));Notify(nameof(IsBuiltinAdministrator));Notify(nameof(HasReadonlyFirstLogon));IsAutomaticAccounts=Plan.AccountMode==global::UnattendLocal.Localization.UiText.Source("text.cdaa9a6700f6");NeedsRetentionConsent=IsAutomaticAccounts&&!Plan.AutoLogon&&!Plan.KeepSensitiveFiles;IsPasswordDays=IsAutomaticAccounts&&Plan.PasswordExpiration==global::UnattendLocal.Localization.UiText.Source("text.5fb01e314f6b");var wasEnabled=CanBypassNetwork;CanBypassNetwork=Plan.AccountMode!=global::UnattendLocal.Localization.UiText.Source("text.5f78e95b6bd9");if(wasEnabled!=CanBypassNetwork)Notify(nameof(CanHideOnlineAccount));IsCustomConfiguration=Plan.ConfigurationMode==global::UnattendLocal.Localization.UiText.Source("text.cb1e0e22c7de");IsFixedLanguage=!Plan.InteractiveLanguage;IsFixedTimeZone=Plan.TimeZoneMode==global::UnattendLocal.Localization.UiText.Source("text.980f34c4425d");IsImageName=Plan.ImageSelection==global::UnattendLocal.Localization.UiText.Source("text.5965e61aee4c");IsImageIndex=Plan.ImageSelection==global::UnattendLocal.Localization.UiText.Source("text.9cbbdbe44e5d");IsExistingDisk=Plan.DiskMode==InstallationSettings.Existing;IsWipeDisk=Plan.DiskMode==InstallationSettings.Wipe;Notify(nameof(CanEditPeInstall));Notify(nameof(IsPeMatch));Notify(nameof(IsPeScript));Notify(nameof(CanEditAutoPartition));Notify(nameof(HasReadonlyAutoPartition));Notify(nameof(AutoPartitionSummary));IsScriptDisk=Plan.DiskMode==DiskScriptSettings.Mode;IsPartitionTarget=IsExistingDisk||IsScriptDisk;IsTargetDisk=IsExistingDisk||(IsWipeDisk&&(!Plan.UseGeneratedPe||Plan.PeTargetMode=="fixed"))||IsScriptDisk;IsFormatDisk=IsExistingDisk&&Plan.FormatExistingPartition;IsDestructiveDisk=InstallationSettings.Destructive(Plan)||HasImportedXmlRisk;DiskRisk=global::UnattendLocal.Localization.UiText.Concat(new object?[] { InstallationSettings.DiskSummary(Plan), (HasImportedXmlRisk?global::UnattendLocal.Localization.UiText.Source("text.e0f733509d5c"):"") },new bool[] { false, false });}
 private string pendingRecommendation="wired";
 public string PendingRecommendation {get=>pendingRecommendation;set{
  if(pendingRecommendation==value||!Recommendations.Known(value))return;
  Set(ref pendingRecommendation,value);
  if(IsRecommended){
   updating=true;try{Recommendations.SelectNetworkScene(Plan,value);}finally{updating=false;}
   RefreshAll();InvalidatePreview();
  }
 }}
 private bool isRecommended;public bool IsRecommended {get=>isRecommended;private set=>Set(ref isRecommended,value);}
 private string recommendationState="",configurationSummary="",differenceHint="";
 public string RecommendationState {get=>recommendationState;private set=>Set(ref recommendationState,value);}
 public string ConfigurationSummary {get=>configurationSummary;private set=>Set(ref configurationSummary,value);}
 public string DifferenceHint {get=>differenceHint;private set=>Set(ref differenceHint,value);}
 private List<Recommendations.Difference> differences=new();
 public List<Recommendations.Difference> RecommendationDifferences {get=>differences;private set=>Set(ref differences,value);}
 private void UsersChanged(object? sender,System.Collections.Specialized.NotifyCollectionChangedEventArgs e){ObserveUsers();if(SelectedUser==null||!Plan.Users.Contains(SelectedUser))SelectedUser=Plan.Users.FirstOrDefault();RefreshAll();InvalidatePreview();}
 private void RefreshRecommendations(){if(Recommendations.Known(Plan.RecommendationId))Set(ref pendingRecommendation,Plan.RecommendationId,nameof(PendingRecommendation));RefreshInstallation();IsRecommended=Plan.ConfigurationMode==global::UnattendLocal.Localization.UiText.Source("text.933c2fd10345");RecommendationState=Recommendations.State(Plan);ConfigurationSummary=global::UnattendLocal.Localization.UiText.Concat(new object?[] { Recommendations.Summary(Plan), "\n", NativePreferences.Summary(Plan,NativePreferences.Power), "\n", NativePreferences.Summary(Plan,NativePreferences.Wer), (Plan.XmlComponents.Count>0?global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.08f742fd610d"), Plan.XmlComponents.Count(row=>row.Enabled), global::UnattendLocal.Localization.UiText.Source("text.e4abb182302b"), Plan.XmlComponents.Count(row=>!row.Enabled), global::UnattendLocal.Localization.UiText.Source("text.a2ecf815b7fb") },new bool[]{false,false,false,false,false}):"") },new bool[]{false,false,false,false,false,false});ConfigurationSummary+="\n"+DeploymentTuningSettings.Summary(Plan);RecommendationDifferences=Recommendations.Differences(Plan);DifferenceHint=!Recommendations.HasBaseline(Plan)?global::UnattendLocal.Localization.UiText.Source("text.a5bb13ae2448"):RecommendationDifferences.Count==0?global::UnattendLocal.Localization.UiText.Source("text.33c3799476bd"):global::UnattendLocal.Localization.UiText.Format("message.ae77a50e4a60", new string[] { global::UnattendLocal.Localization.UiText.Arg((RecommendationDifferences.Count), 0, null) });}
 public MainViewModel(){Plan.XmlComponents.CollectionChanged+=XmlComponentsChanged;ObserveXmlComponents();AppRemovalChoices=AppRemovalSettings.Items.Select(i=>new AppRemovalChoice(i,()=>Plan)).ToList();ObserveApps();Plan.Scripts.CollectionChanged+=ScriptsChanged;ObserveScripts();Plan.PropertyChanged+=HelpSettingChanged;Plan.Users.CollectionChanged+=UsersChanged;ObserveUsers();SelectedUser=Plan.Users.FirstOrDefault();RefreshAll();}
}
