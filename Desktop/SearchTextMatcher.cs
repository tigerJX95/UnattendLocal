using System.Text;
namespace UnattendLocal.Desktop;

// Search labels and identifiers without changing the underlying configuration value.
public static class SearchTextMatcher {
 public static string SearchText(string label,string identifier){
  string aliases=identifier.Equals("zh-CN",StringComparison.OrdinalIgnoreCase)||identifier.Equals("zh-Hans-CN",StringComparison.OrdinalIgnoreCase)||identifier.StartsWith("0804:",StringComparison.OrdinalIgnoreCase)||identifier.Equals("00000804",StringComparison.OrdinalIgnoreCase)?global::UnattendLocal.Localization.UiText.Source("text.1d6c894a034b"):"";
  if(identifier.Equals("China Standard Time",StringComparison.OrdinalIgnoreCase))aliases+=global::UnattendLocal.Localization.UiText.Source("text.f9cc90981df9");
  return label+" "+UnattendLocal.Localization.UiText.EnglishText(label)+" "+identifier+aliases;
 }
 public static bool Matches(string text,string? query){
  var haystack=Normalize(text+" "+UnattendLocal.Localization.UiText.EnglishText(text));
  return Normalize(query??"").Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries).All(word=>haystack.Contains(word,StringComparison.OrdinalIgnoreCase));
 }
 public static bool Exact(string text,string? query)=>Normalize(text).Equals(Normalize(query??""),StringComparison.OrdinalIgnoreCase);
 private static string Normalize(string value)=>value.Normalize(NormalizationForm.FormKC).Trim();
}
