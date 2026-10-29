using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Localization;

static class PePreflightTests
{
 public static int Run(GeneratorService generator){
  int count=0;
  void Check(bool ok,string name){if(!ok)throw new Exception("PE preflight: "+name);count++;}
  string Script(byte[] bytes)=>XmlMerge.Read(Encoding.UTF8.GetString(bytes)).SelectSingleNode("//*[local-name()='PEScriptCopy']")!.InnerText;
  foreach(string layout in new[]{"UEFI / GPT","BIOS / MBR",PeDiskSafety.AutomaticLayout}){
   var p=QuickPresets.Create(QuickPresets.LocalAccount);p.UseGeneratedPe=true;p.Architecture="amd64";p.KeepSensitiveFiles=true;p.DiskMode=InstallationSettings.Wipe;p.DiskLayout=layout;p.TargetDiskId=0;p.ImageSelection="按镜像索引";p.ImageIndex=3;p.EditionMode=UiText.Source("text.5056d73b13ec");p.BypassHardware=p.BypassTPMCheck=p.BypassSecureBootCheck=p.BypassRAMCheck=false;p.PauseBeforeFormatting=true;p.ConfirmWipeDisk=true;
   string locale=p.Locale,keyboard=p.Keyboard;
   var bytes=generator.Generate(p);var doc=XmlMerge.Read(Encoding.UTF8.GetString(bytes));string script=Script(bytes);
   Check(!script.Contains("findstr.exe",StringComparison.OrdinalIgnoreCase),"no findstr dependency");
   int architecture=script.IndexOf("cscript.exe //E:vbscript //Nologo X:\\UL-imagecheck.vbs",StringComparison.Ordinal);
   int target=script.IndexOf("call :print \"Determining target disk\"",StringComparison.Ordinal);
   int gate=script.IndexOf("rem UnattendLocal destructive gate v2",StringComparison.Ordinal);
   int disk=script.IndexOf("diskpart.exe /s",StringComparison.Ordinal);
   Check(architecture>=0 && target>architecture && gate>target && disk>gate,"architecture, selection, recheck and partition ordering");
   Check(script.Contains("if errorlevel 1 (")&&script.Contains("Cannot read selected image."),"DISM failure remains distinct");
   Check(script.Contains("set \"UL_IMAGE_CHECK_EXIT=%errorlevel%\"")&&script.Contains("\"42\" call :fail \"Selected image is not x64.")&&script.Contains("Cannot verify selected image information."),"checker exit status is captured and classified");
   Check(script.Contains("/Index:3")&&script.Contains("/CheckIntegrity /Verify"),"image index and integrity checks retained");
   Check(script.LastIndexOf("\npause",gate,StringComparison.Ordinal)<gate&&script.LastIndexOf("\npause",gate,StringComparison.Ordinal)>target,"destructive recheck remains after pause");
   Check(script.Contains("if %LAYOUT% == GPT (")&&script.Contains("PNPDeviceID"),"boot and target identity guards retained");
   foreach(XmlNode command in doc.SelectNodes("//*[local-name()='RunSynchronousCommand']/*[local-name()='Path']")!)Check(command.InnerText.Length<=255,"generated command respects PE length limit");
   Check(p.Locale==locale&&p.Keyboard==keyboard,"installed locale and input method stay unchanged");
   var imported=XmlPlanImport.Import(Encoding.UTF8.GetString(bytes),generator);imported.ConfirmWipeDisk=true;imported.ConfirmImportedXmlRisk=true;
   Check(Script(generator.Generate(imported))==script,"updated pipeline restores and regenerates consistently");
  }
  string ordinary="@echo off\r\nwpeutil.exe SetKeyboardLayout 0409:00000409\r\nrem end";
  string modified=PePreflight.Keyboard(ordinary);
  Check(modified.Contains("SetKeyboardLayout 0409:00000409 || call :print")&&modified.Contains("Warning:"),"basic keyboard layout retains request and reports failure");
  string ime=PePreflight.Keyboard("wpeutil.exe SetKeyboardLayout 1000:0804:{GUID}{GUID}\r\n");
  Check(!ime.Contains("SetKeyboardLayout")&&ime.Contains("keeping the current PE layout"),"IME identifiers do not reach the PE keyboard command");
  Check(PePreflight.Keyboard("@echo off\r\n")=="@echo off\r\n","interactive-language path adds no keyboard command");
  string checker=PePreflight.ImageCheckerSource();
  Check(checker.Contains("If matches <> 1")&&checker.Contains("WScript.Quit 42")&&checker.Contains("WScript.Quit 1"),"parser rejects missing duplicate and unknown architecture separately");
  Check(!checker.Contains("shell.Exec")&&!checker.Contains("CreateObject(\"WScript.Shell\")"),"DISM text is parsed without command execution");
  return count;
 }
}
