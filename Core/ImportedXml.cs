namespace UnattendLocal.Core;
public class ImportedXml {
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
