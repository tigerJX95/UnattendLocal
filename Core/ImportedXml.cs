namespace UnattendLocal.Core;
public class ImportedXml {
 public string SourceXml {get;set;}="";
 public string BaselineXml {get;set;}="";
 public string[] Notices {get;set;}=[];
 public string[] Blockers {get;set;}=[];
 public bool RequiresDiskReview {get;set;}
 public bool OriginalOmitted {get;set;}
}
