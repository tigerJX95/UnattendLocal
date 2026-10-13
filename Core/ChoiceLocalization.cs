using System.Text.Json;

namespace UnattendLocal.Core;

// Display-only labels for the fixed upstream resource IDs. Selection and XML always use the original IDs.
public static class ChoiceLocalization {
 private static readonly Lazy<Dictionary<string,Dictionary<string,string>>> Labels=new(()=>{
  using var stream=typeof(ChoiceLocalization).Assembly.GetManifestResourceStream("UnattendLocal.Core.Resources.ChoiceLabels.zh-CN.json")
   ??throw new InvalidOperationException("中文选项资源缺失。");
  return JsonSerializer.Deserialize<Dictionary<string,Dictionary<string,string>>>(stream)
   ??throw new InvalidOperationException("中文选项资源无效。");
 });
 public static string Get(string group,string id,string fallback)=>
  Labels.Value.TryGetValue(group,out var items)&&items.TryGetValue(id,out var label)?label:fallback;
 public static string Architecture(string id)=>id switch {
  "amd64"=>"x64（Intel / AMD 64 位）","arm64"=>"ARM64（ARM 64 位）","x86"=>"x86（32 位）",_=>id
 };
}
