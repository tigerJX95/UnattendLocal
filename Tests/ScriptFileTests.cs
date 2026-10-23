using System.Text;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class ScriptFileTests {
 public static int Run(GeneratorService generator){
  int count=0;
  void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("PASS V1.2 "+message);}
  void Reject(Action action,string message){try{action();}catch(ArgumentException){Check(true,message);return;}throw new Exception("Expected rejection: "+message);}
  const string content="# 中文与 Unicode 😀\r\nWrite-Output 'test';\n\t# 保留空白\r\n";
  Encoding[] encodings=[new UTF8Encoding(false,true),new UTF8Encoding(true,true),new UnicodeEncoding(false,true,true),new UnicodeEncoding(true,true,true)];
  foreach(string extension in new[]{".ps1",".CMD",".Reg",".vbs"})foreach(var encoding in encodings){
   byte[] bytes=[..encoding.GetPreamble(),..encoding.GetBytes(content)];
   var row=ScriptFiles.Decode("中文名称"+extension,bytes);
   Check(row.Type==ScriptFiles.TypeForFile("f"+extension)&&row.Content==content&&!row.Enabled&&row.Phase=="System","text, whitespace and safe initial state "+extension+encoding.WebName+encoding.GetPreamble().Length);
  }
  foreach(string name in new[]{"a.js","a.bat","a.txt","a.ps1.exe","noextension","a.ps1 ",".exe"})Reject(()=>ScriptFiles.Decode(name,Encoding.UTF8.GetBytes(content)),"unsupported extension "+name);
  foreach(byte[] invalid in new byte[][]{[0xc3,0x28],[0xff,0xfe,0x41],[0xef,0xbb,0xbf,0xc3],[0xff,0xfe,0,0,0x41,0,0,0],[0,0,0xfe,0xff,0,0,0,0x41],[0x41,0,0x42,0],[0xff,0xfe,0x00,0xd8]})Reject(()=>ScriptFiles.Decode("x.ps1",invalid),"invalid or unsupported encoding "+Convert.ToHexString(invalid));
  foreach(string invalid in new[]{""," \r\n\t","a\0b","a\u0001b","\ud800","\udfff","a\ufffeb"})Reject(()=>ScriptFiles.ValidateContent(invalid),"invalid text "+Convert.ToHexString(Encoding.Unicode.GetBytes(invalid)));
  Reject(()=>ScriptFiles.Decode("x.ps1",new byte[ScriptFiles.MaxBytes+1]),"byte limit enforced before decoding");
  string maximum=new('a',ScriptFiles.MaxCharacters);Check(ScriptFiles.Decode("x.cmd",Encoding.UTF8.GetBytes(maximum)).Content.Length==ScriptFiles.MaxCharacters,"character boundary accepted");Reject(()=>ScriptFiles.Decode("x.cmd",Encoding.UTF8.GetBytes(maximum+"a")),"character limit enforced after decoding");
  foreach(string type in new[]{"Ps1","Cmd","Reg","Vbs"}){
   var row=new ScriptEntry{Type=type,Content=content,Phase="UserOnce",Enabled=true};
   byte[] saved=ScriptFiles.Encode(row,"output"+ScriptFiles.Extension(type));var read=ScriptFiles.Decode("output"+ScriptFiles.Extension(type),saved);
   Check(read.Content==(type=="Reg"?"Windows Registry Editor Version 5.00\r\n\r\n"+content:content)&&row.Content==content&&row.Phase=="UserOnce"&&row.Enabled,"standalone save preserves editor and reimported text "+type);
   Check(type is "Reg" or "Vbs"?saved.AsSpan().StartsWith(new byte[]{0xff,0xfe}):type=="Ps1"?saved.AsSpan().StartsWith(new byte[]{0xef,0xbb,0xbf}):saved.AsSpan().StartsWith(Encoding.UTF8.GetBytes("# 中文")),"appropriate encoding "+type);
   Reject(()=>ScriptFiles.Encode(row,"wrong.txt"),"wrong extension cannot disguise type "+type);
  }
  var reg=new ScriptEntry{Type="Reg",Content="Windows Registry Editor Version 5.00\n\n[HKEY_CURRENT_USER\\Test]\n"};Check(ScriptFiles.Decode("x.reg",ScriptFiles.Encode(reg,"x.reg")).Content==reg.Content,"existing REG header and line endings retained");
  Reject(()=>ScriptFiles.Encode(new ScriptEntry{Type="Reg",Content=maximum},"x.reg"),"REG header expansion cannot exceed import limit");Reject(()=>ScriptFiles.Encode(new ScriptEntry{Type="Js",Content="test"},"x.js"),"removed JScript cannot be exported");
  string folder=Path.Combine(Path.GetTempPath(),"UnattendLocal-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
  try{
   string path=Path.Combine(folder,"中文脚本.PS1");File.WriteAllBytes(path,Encoding.UTF8.GetBytes(content));
   var vm=new MainViewModel();var existing=new ScriptEntry{Phase="FirstLogon",Content="Write-Output 'existing'"};vm.Plan.Scripts.Add(existing);vm.SelectedScript=existing;vm.Preview="old preview";
   var imported=vm.ImportScriptFile(path);Check(vm.Plan.Scripts.Count==2&&ReferenceEquals(vm.Plan.Scripts[0],existing)&&ReferenceEquals(vm.SelectedScript,imported)&&imported.Content==content&&!imported.Enabled&&vm.HasSelectedScript,"file import appends and selects without replacing existing row");
   Check(vm.Preview!="old preview","file import invalidates generated preview");var candidate=ScriptFiles.Read(path);var added=ScriptFiles.Append(vm.Plan,candidate);candidate.Content="changed";Check(added.Content==content&&!ReferenceEquals(added,candidate),"committed entry independent of candidate");
   foreach(string invalid in new[]{"bad encoding","empty","missing","wrong extension"}){
    string bad=Path.Combine(folder,"bad"+(invalid=="wrong extension"?".js":".ps1"));if(invalid=="bad encoding")File.WriteAllBytes(bad,[0xff]);else if(invalid=="empty")File.WriteAllText(bad,"");else if(invalid=="wrong extension")File.WriteAllText(bad,"test");else File.Delete(bad);
    var before=vm.Plan.Scripts.ToArray();var selected=vm.SelectedScript;vm.Preview="keep preview";
    try{vm.ImportScriptFile(bad);throw new Exception("Should fail");}catch(Exception ex)when(ex is ArgumentException or IOException){Check(vm.Plan.Scripts.SequenceEqual(before)&&ReferenceEquals(vm.SelectedScript,selected)&&vm.Preview=="keep preview","failure is atomic "+invalid);}
   }
   while(vm.Plan.Scripts.Count<60)vm.Plan.Scripts.Add(new ScriptEntry{Enabled=false});var selection=vm.SelectedScript;Reject(()=>vm.ImportScriptFile(path),"full list rejects file import");Check(vm.Plan.Scripts.Count==60&&ReferenceEquals(selection,vm.SelectedScript),"full list retains selection and entries");
   var plan=InstallPlan.New();plan.Users[0].Password="SyntheticFileTest1!";var row=ScriptFiles.Append(plan,ScriptFiles.Read(path));Check(!XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(plan))).SelectNodes("//*[local-name()='File']")!.Cast<System.Xml.XmlElement>().Any(e=>e.InnerText.Contains("中文与 Unicode")),"disabled file contributes no generated command");
   row.Enabled=true;var xml=Encoding.UTF8.GetString(generator.Generate(plan));Check(XmlMerge.Read(xml).InnerText.Contains("中文与 Unicode 😀"),"valid supplementary Unicode generates XML");var reimport=XmlPlanImport.Import(xml,generator);Check(reimport.Scripts.Count==1&&reimport.Scripts[0].Content==content&&reimport.Scripts[0].Enabled&&reimport.Scripts[0].Phase=="System","enabled imported file survives XML roundtrip");
   foreach(bool secrets in new[]{false,true}){row.Enabled=false;row.Phase="UserOnce";var stored=PlanStore.Deserialize(PlanStore.Serialize(plan,secrets));Check(stored.Scripts[0].Content==content&&!stored.Scripts[0].Enabled&&stored.Scripts[0].Phase=="UserOnce","JSON saves text, stage and disabled state "+secrets);}
   byte[] beforeSource=File.ReadAllBytes(path);string output=Path.Combine(folder,"saved.ps1");File.WriteAllText(output,"old destination");ScriptFiles.Write(row,output);Check(ScriptFiles.Read(output).Content==content&&File.ReadAllBytes(path).SequenceEqual(beforeSource)&&row.Content==content,"save replaces only chosen destination without mutating source/editor");
   row.Content="bad\0text";Reject(()=>ScriptFiles.Write(row,output),"invalid save rejected before touching destination");Check(ScriptFiles.Read(output).Content==content&&Directory.GetFiles(folder,".unattend-*.tmp").Length==0,"failed save leaves destination and no temporary files");
   row.Content=content;
   string directoryTarget=Path.Combine(folder,"folder.ps1");Directory.CreateDirectory(directoryTarget);
   string sentinel=Path.Combine(directoryTarget,"keep.txt");File.WriteAllText(sentinel,"preserve directory contents");
   try{ScriptFiles.Write(row,directoryTarget);throw new Exception("Expected file-system error");}
   // Existing directory destinations produce different exception types on Windows and Unix.
   catch(Exception error)when(error is IOException or UnauthorizedAccessException){
    Check(Directory.Exists(directoryTarget)&&File.ReadAllText(sentinel)=="preserve directory contents"&&Directory.GetFiles(folder,".unattend-*.tmp").Length==0,"write failure cleans temporary and preserves directory");
   }
   int notifications=0;vm.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(vm.HasSelectedScript))notifications++;};vm.SelectedScript=null;Check(!vm.HasSelectedScript&&notifications>0,"save-button selection notification updates");
  }finally{Directory.Delete(folder,true);}
  foreach(string query in new[]{"导入脚本文件","另存选中脚本","export script file","GBK"})Check(SettingsSearchCatalog.Search(query).Any(r=>r.Target is "ImportScriptFileButton" or "SaveScriptFileButton"),"file actions searchable "+query);
  return count;
 }
}
