using System.Text;
using System.Text.RegularExpressions;
namespace UnattendLocal.Desktop;

// Search labels and identifiers without changing the underlying configuration value.
public static class SearchTextMatcher {
 private static readonly Regex UtcOffset = new(@"(?<!\S)UTC([+\-−])([0-9]{1,2})(?::([0-9]{2}))?(?!\S)",
  RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
 public static string SearchText(string label,string identifier){
  string aliases=identifier.Equals("zh-CN",StringComparison.OrdinalIgnoreCase)||identifier.Equals("zh-Hans-CN",StringComparison.OrdinalIgnoreCase)||identifier.StartsWith("0804:",StringComparison.OrdinalIgnoreCase)||identifier.Equals("00000804",StringComparison.OrdinalIgnoreCase)?global::UnattendLocal.Localization.UiText.Source("text.1d6c894a034b"):"";
  if(identifier.Equals("China Standard Time",StringComparison.OrdinalIgnoreCase))aliases+=global::UnattendLocal.Localization.UiText.Source("text.f9cc90981df9");
  return label+" "+UnattendLocal.Localization.UiText.EnglishText(label)+" "+identifier+aliases;
 }
 public static bool Matches(string text,string? query){
  var haystack=Normalize(text+" "+UnattendLocal.Localization.UiText.EnglishText(text));
  return Normalize(query??"").Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries).All(word=>haystack.Contains(word,StringComparison.OrdinalIgnoreCase));
 }
 public static bool MatchesChoice(string text, string label, string query)
 {
  if (!label.StartsWith("(UTC", StringComparison.Ordinal))
   return Matches(text, query);

  string normalized = UtcOffset.Replace(Normalize(query), match =>
  {
   string hours = match.Groups[2].Value.PadLeft(2, '0');
   string minutes = match.Groups[3].Success ? match.Groups[3].Value : "00";
   bool zero = hours == "00" && minutes == "00";
   if (zero && label.StartsWith("(UTC)", StringComparison.Ordinal))
    return "UTC)";
   string sign = zero || match.Groups[1].Value == "+" ? "+" : "-";
   // The closing parenthesis distinguishes an exact offset from a partial match.
   return $"UTC{sign}{hours}:{minutes})";
  });
  return Matches(text, normalized);
 }
 public static bool Exact(string text,string? query)=>Normalize(text).Equals(Normalize(query??""),StringComparison.OrdinalIgnoreCase);
 private static string Normalize(string value)=>value.Normalize(NormalizationForm.FormKC).Trim();
}
