namespace UnattendLocal.Core;
public class ImportedXml {
 public int NativePreferencesImportVersion {get;set;}
 public bool PowerPlanReadOnly {get;set;}
 public bool ErrorReportingReadOnly {get;set;}
 public bool LegacyUpgradeReadOnly {get;set;}
 public int WorkgroupImportVersion {get;set;}
 public bool WorkgroupReadOnly {get;set;}
 public int PeLogImportVersion {get;set;}
 public bool PeLogReadOnly {get;set;}

 public int RegisteredInformationImportVersion {get;set;}
 public bool RegisteredInformationReadOnly {get;set;}
 public int SetupDynamicUpdateImportVersion {get;set;}
 public bool SetupDynamicUpdateReadOnly {get;set;}

 public int SetupDriversImportVersion {get;set;}
 public bool SetupDriversReadOnly {get;set;}
 public bool AutoPartitionReadOnly {get;set;}
 public bool GeneratedPeEditable {get;set;}
 public int StaticIPv4ImportVersion {get;set;}
 public bool StaticIPv4ReadOnly {get;set;}
 public int FirstLogonImportVersion {get;set;}
 public bool FirstLogonReadOnly {get;set;}
 public string FirstLogonSourceAccountMode {get;set;}="";
 public bool FirstLogonSourceAutoLogon {get;set;}
 public bool FirstLogonSourceBuiltin {get;set;}

 public int AppRemovalImportVersion {get;set;}
 public bool AppRemovalReadOnly {get;set;}
 public bool CompactOsScriptControlled {get;set;}
 public string CompactOsSourceMode {get;set;}=CompactOsSettings.Default;
 public bool CompactOsSourceKnown {get;set;}
 public string SourceXml {get;set;}="";
 public string BaselineXml {get;set;}="";
 public string[] Notices {get;set;}=[];
 public string[] Blockers {get;set;}=[];
 public bool RequiresDiskReview {get;set;}
 public bool OriginalOmitted {get;set;}
}
