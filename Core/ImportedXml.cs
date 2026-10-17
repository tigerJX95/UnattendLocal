namespace UnattendLocal.Core;
public class ImportedXml {
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
