using System.Text.Json;

namespace UnattendLocal.Core;

// Display-only labels for the fixed upstream resource IDs. Selection and XML always use the original IDs.
public static class ChoiceLocalization {
 private static readonly Lazy<Dictionary<string,Dictionary<string,string>>> Labels=new(()=>{
  using var stream=typeof(ChoiceLocalization).Assembly.GetManifestResourceStream("UnattendLocal.Core.Resources.ChoiceLabels.zh-CN.json")
   ??throw new InvalidOperationException(global::UnattendLocal.Localization.UiText.Source("text.b3808911aa25"));
  return JsonSerializer.Deserialize<Dictionary<string,Dictionary<string,string>>>(stream)
   ??throw new InvalidOperationException(global::UnattendLocal.Localization.UiText.Source("text.1fc90d202e7e"));
 });
 public static string Get(string group,string id,string fallback)=>
  Labels.Value.TryGetValue(group,out var items)&&items.TryGetValue(id,out var label)?label:fallback;
 public static string Architecture(string id)=>id switch {
  "amd64"=>global::UnattendLocal.Localization.UiText.Source("text.75db507b76f1"),"arm64"=>global::UnattendLocal.Localization.UiText.Source("text.2bb29b0a25c7"),"x86"=>global::UnattendLocal.Localization.UiText.Source("text.cb3db7d4b84b"),_=>id
 };
}
