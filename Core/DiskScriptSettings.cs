using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class DiskScriptSettings {
 public const string Mode="自定义 DiskPart 脚本";
 // MIT upstream example: https://schneegans.de/windows/unattend-generator/
 public const string Example="""
SELECT DISK=0
CLEAN
CONVERT GPT
CREATE PARTITION EFI SIZE=300
FORMAT QUICK FS=FAT32 LABEL="System"
ASSIGN LETTER=S
CREATE PARTITION MSR SIZE=16
CREATE PARTITION PRIMARY
SHRINK MINIMUM=1000
FORMAT QUICK FS=NTFS LABEL="Windows"
ASSIGN LETTER=W
CREATE PARTITION PRIMARY
FORMAT QUICK FS=NTFS LABEL="Recovery"
ASSIGN LETTER=R
SET ID="de94bba4-06d1-4d40-a16a-bfd50179d6ac"
GPT ATTRIBUTES=0x8000000000000001
""";
 public static string[] Lines(string? script){
  if(string.IsNullOrWhiteSpace(script))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.d892729bd3fc"));
  if(script.Length>128*1024)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.f6319c7722ca"));
  if(script.Any(c=>c>126||(c<32&&c is not ('\r' or '\n' or '\t'))))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.6c38e09c2318"));
  if(script.IndexOfAny("&|<>%^!()".ToCharArray())>=0)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.a78863c0a263"));
  var lines=script.Replace("\r\n","\n").Replace('\r','\n').Split('\n').Select(s=>s.Trim()).Where(s=>s.Length>0).ToArray();
  if(lines.Length>512||lines.Any(s=>s.Length>160))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.a23c2641219e"));
  if(lines.Any(s=>!s.StartsWith("rem ",StringComparison.OrdinalIgnoreCase)&&System.Text.RegularExpressions.Regex.IsMatch(s,@"\bnoerr\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase)))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.56a9fbe3f3c5"));
  return lines;
 }
 public static IReadOnlyList<string> Commands(string script){
  var builder=new CommandBuilder(false);
  const string file=@"X:\UnattendLocal-diskpart.txt", runner=@"X:\UnattendLocal-diskpart.cmd";
  var lines=Lines(script);
  List<string> result=[builder.ShellCommand($"type nul > {file}"),builder.ShellCommand($"type nul > {runner}")];
  result.AddRange(builder.WriteToFilePE(file,lines));
  result.AddRange(builder.WriteToFilePE(runner,new[]{"@echo off",$@"diskpart.exe /s {file} > X:\UnattendLocal-diskpart.log 2>&1","if errorlevel 1 goto fail","exit /b 0",":fail",@"type X:\UnattendLocal-diskpart.log","echo DiskPart failed. Setup is halted. Check the target disk and script.","pause","goto fail"}));
  result.Add(builder.ShellCommand(runner));
  return result;
 }
}
