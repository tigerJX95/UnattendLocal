using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UnattendLocal.Localization;

// Canonical source values never depend on the display language. Only presentation
// adapters call Translate/Get. This preserves existing plans and generated XML.
public static class UiText
{
    private sealed record Entry(string Source, string[] Contexts, bool[] RawArgs, bool Opaque = false, string[]? ProtectedTokens = null, string[]? FilterPatterns = null, bool StructuredLines = false);
    private sealed record Projection(string? Key, string[] Parts, bool[] Raw, string? Separator = null, int Line = -1, int Part = -1);
    public sealed class LanguageState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public int Revision { get; private set; }
        public string this[string key] => UiText.Get(key);
        internal void Changed() { Revision++; PropertyChanged?.Invoke(this, new(nameof(Revision))); PropertyChanged?.Invoke(this, new("Item[]")); }
    }
    public static LanguageState State { get; } = new();
    public static event EventHandler? LanguageChanged;
    public static string Language { get; private set; } = "zh-CN";
    public static bool IsEnglish => Language == "en-US";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    // Keep token matching linear for untrusted language packs. Boundary checks
    // below preserve escaped-brace behavior without unsupported lookarounds.
    private static readonly Regex PlaceholderPattern = new(@"\{\d+(?:,[^{}]+)?(?::[^{}]+)?\}", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(250));
    private static readonly Dictionary<string, Entry> Catalog = Read<Dictionary<string, Entry>>("catalog");
    private static readonly Dictionary<string, string> Chinese = Read<Dictionary<string, string>>("zh-CN");
    private static readonly Dictionary<string, string> English = Read<Dictionary<string, string>>("en-US");
    private static readonly Dictionary<string, string> SourceKeys = Catalog.GroupBy(p => p.Value.Source).ToDictionary(g => g.Key, g => g.First().Key, StringComparer.Ordinal);
    private static readonly Dictionary<string, string> NormalizedKeys = Catalog.Where(p => p.Value.Source.Any(c => c is >= '\u4e00' and <= '\u9fff')).GroupBy(p => Normalize(p.Value.Source)).ToDictionary(g => g.Key, g => g.First().Key, StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, Projection> Projections = new(StringComparer.Ordinal);
    // Application-authored help remains alive with its cached view models. Keep
    // that small immutable graph separate from bounded, transient diagnostics.
    private static readonly ConcurrentDictionary<string, Projection> Authored = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Dictionary<string, string>> Overrides = new();
    public static IReadOnlyDictionary<string, string> SourceResources => Chinese;
    public static IReadOnlyDictionary<string, string> EnglishResources => English;
    public sealed record UiLanguage(string Code,string Name);
    public static IReadOnlyList<UiLanguage> Languages {get;private set;} = Read<UiLanguage[]>("languages");
    public static bool IsSupported(string? code) => Languages.Any(l=>l.Code==code);
    public static bool LoadLanguageManifest(string path,out string? error){
        error=null;
        try{
            if(new FileInfo(path).Length>65536)throw new ArgumentException("Language manifest is too large.");
            var items=JsonSerializer.Deserialize<UiLanguage[]>(File.ReadAllText(path,new UTF8Encoding(false,true)),Json)??throw new ArgumentException("Invalid language manifest.");
            if(items.Any(i=>i is null)||items.Length>64||items.Select(i=>i.Code).Distinct().Count()!=items.Length||!items.Any(i=>i.Code=="zh-CN")||!items.Any(i=>i.Code=="en-US"))throw new ArgumentException("Required or duplicate language codes.");
            foreach(var item in items){if(!Regex.IsMatch(item.Code,@"^[a-z]{2,3}(?:-[A-Za-z0-9]{2,8})*$")||string.IsNullOrWhiteSpace(item.Name)||item.Name.Length>80)throw new ArgumentException("Invalid language identity.");_ = CultureInfo.GetCultureInfo(item.Code);}
            Languages=Array.AsReadOnly(items);return true;
        }catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or ArgumentException or JsonException){error=ex.Message;return false;}
    }
    private static T Read<T>(string name)
    {
        using var stream = typeof(UiText).Assembly.GetManifestResourceStream("UnattendLocal.Localization." + name + ".json") ?? throw new InvalidOperationException("Missing localization resource: " + name);
        return JsonSerializer.Deserialize<T>(stream, Json) ?? throw new InvalidOperationException("Invalid localization resource: " + name);
    }
    private static string Normalize(string text) => string.Concat(text.Where(c => !char.IsWhiteSpace(c)));
    public static string Source(string key) => Catalog.TryGetValue(key, out var value) ? value.Source : throw new ArgumentException("Unknown localization key: " + key);
    public static string Get(string key) => GetFor(Language,key);
    private static string GetFor(string code,string key, bool paragraphs = true)
    {
        if(Catalog.TryGetValue(key,out var entry)&&entry.Opaque)return entry.Source;
        if (Overrides.TryGetValue(code, out var pack) && pack.TryGetValue(key, out var text)) return paragraphs ? DisplayParagraphs(text,code) : text;
        string value = code != "zh-CN" && English.TryGetValue(key, out var translated) ? translated : Chinese.GetValueOrDefault(key,Source(key));
        return paragraphs ? DisplayParagraphs(value,code) : value;
    }
    private static string DisplayParagraphs(string text, string code)
    {
        if (text.Contains('\n')) return text;
        if (code != "zh-CN") return text.Length > 200 ? Regex.Replace(text, @"(?<=[.!?]) +(?=[A-Z])", "\n\n") : text;
        if (text.Length < 110) return text;
        var result = new StringBuilder(text.Length + 32);
        var closing = new Stack<char>();
        foreach (char value in text)
        {
            char end = value switch { '（' => '）', '(' => ')', '【' => '】', '[' => ']', '“' => '”', '「' => '」', _ => '\0' };
            if (end != '\0') closing.Push(end);
            else if (closing.TryPeek(out char expected) && value == expected) closing.Pop();
            result.Append(value);
            if (closing.Count == 0 && value is '。' or '；') result.Append("\n\n");
        }
        return result.ToString().TrimEnd('\n');
    }
    public static string EnglishText(string source) => Render(source,new HashSet<string>(StringComparer.Ordinal),0,"en-US");
    public static void SetLanguage(string code)
    {
        if (!IsSupported(code)) throw new ArgumentOutOfRangeException(nameof(code));
        if (Language == code) return;
        Language = code;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(code);
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(code);
        // Numeric parsing/formatting and installation language are deliberately independent.
        State.Changed(); LanguageChanged?.Invoke(null, EventArgs.Empty);
    }
    public static bool LoadExternalPack(string code, string path, out string? error)
    {
        error = null;
        try
        {
            if (!IsSupported(code)) throw new ArgumentException("Unsupported language pack.");
            var info = new FileInfo(path);
            if (info.Length > 4 * 1024 * 1024) throw new ArgumentException("Language pack is too large.");
            var raw = File.ReadAllText(path, new UTF8Encoding(false, true));
            using var parsed = JsonDocument.Parse(raw);
            if (parsed.RootElement.ValueKind != JsonValueKind.Object || parsed.RootElement.EnumerateObject().GroupBy(p => p.Name).Any(g => g.Count() > 1)) throw new ArgumentException("Invalid or duplicate language keys.");
            var pack = JsonSerializer.Deserialize<Dictionary<string, string>>(raw) ?? throw new ArgumentException("Invalid language pack.");
            foreach (var item in pack)
                if (!IsValidEntry(item.Key, item.Value)) throw new ArgumentException("Invalid language entry: " + item.Key);
            Overrides[code] = pack;
            if (Language == code) { State.Changed(); LanguageChanged?.Invoke(null, EventArgs.Empty); }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or JsonException or NotSupportedException)
        { error = ex.Message; return false; }
    }
    // Packs are data only. Never evaluate markup, commands or scripts from a pack.
    public static bool IsValidEntry(string key, string? value)
    {
        try { return ValidateEntry(key, value); }
        catch (RegexMatchTimeoutException) { return false; }
    }
    private static bool ValidateEntry(string key, string? value)
    {
        if (!Catalog.TryGetValue(key, out var entry) || string.IsNullOrWhiteSpace(value)) return false;
        if (Placeholders(Chinese[key]) != Placeholders(value)) return false;
        try { if (ArgumentCount(Chinese[key]) > 0) _ = string.Format(CultureInfo.InvariantCulture, value, Enumerable.Repeat<object>("test", ArgumentCount(Chinese[key])).ToArray()); }
        catch (FormatException) { return false; }
        if (entry.StructuredLines)
        {
            var originalLines = entry.Source.Split('\n'); var translatedLines = value.Split('\n');
            if (originalLines.Length != translatedLines.Length) return false;
            for (int i = 0; i < originalLines.Length; i++)
                if (Placeholders(originalLines[i]) != Placeholders(translatedLines[i]) || originalLines[i].Contains('：') && Colon(translatedLines[i]) < 0) return false;
        }
        if (entry.Opaque && value != entry.Source) return false;
        if (entry.ProtectedTokens?.Any(token => !value.Contains(token, StringComparison.Ordinal)) == true) return false;
        if (entry.FilterPatterns != null && !value.Split('|').Where((_, i) => i % 2 == 1).SequenceEqual(entry.FilterPatterns)) return false;
        return true;
    }
    private static IEnumerable<Match> PlaceholderMatches(string text) => PlaceholderPattern.Matches(text).Cast<Match>()
        .Where(m => (m.Index == 0 || text[m.Index - 1] != '{') && (m.Index + m.Length == text.Length || text[m.Index + m.Length] != '}'));
    private static int ArgumentCount(string value) => PlaceholderMatches(value).Select(m => {
        int end = 1; while (end < m.Value.Length && char.IsDigit(m.Value[end])) end++;
        return checked(int.Parse(m.Value.AsSpan(1, end - 1), CultureInfo.InvariantCulture) + 1);
    }).DefaultIfEmpty(0).Max();
    public static string Placeholders(string text) => string.Join("|", PlaceholderMatches(text).Select(m => m.Value).Order(StringComparer.Ordinal));
    private static void Remember(string source, Projection projection)
    {
        if (source.Length > 16384) return;
        if (Projections.Count > 12000) Projections.Clear();
        Projections[source] = projection;
    }
    public static void PinAuthoredText(string text)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        void Pin(string value, int depth)
        {
            if (depth > 16 || !visited.Add(value) || !Projections.TryGetValue(value, out var projection)) return;
            if (Authored.Count >= 4096 && !Authored.ContainsKey(value)) throw new InvalidOperationException("Authored text graph exceeds its limit.");
            Authored[value] = projection;
            for (int i = 0; i < projection.Parts.Length; i++)
                if (!(i < projection.Raw.Length && projection.Raw[i])) Pin(projection.Parts[i], depth + 1);
        }
        Pin(text, 0);
    }
    public static string Arg<T>(T value, int alignment = 0, string? format = null) =>
        string.Format(CultureInfo.CurrentCulture, "{0" + (alignment == 0 ? "" : "," + alignment.ToString(CultureInfo.InvariantCulture)) + (format == null ? "" : ":" + format) + "}", value);
    public static string Format(string key, params string[] arguments)
    {
        var source = string.Format(CultureInfo.CurrentCulture, Source(key), arguments.Cast<object?>().ToArray());
        RememberFragments(key, arguments);
        Remember(source, new(key, arguments, Catalog[key].RawArgs));
        return source;
    }
    public static string Alias(string canonical, string shaped)
    {
        if (canonical != shaped) {
            Remember(shaped, new(null, [canonical], [false]));
            if (SourceKeys.ContainsKey(canonical)) PinAuthoredText(shaped);
        }
        return shaped;
    }
    // Summaries are grouped using invariant labels. Keep the relationship to the
    // complete resource when the presentation layer separates label and value.
    public static void RememberSummaryParts(string line)
    {
        int colon = line.IndexOf('：');
        if (colon < 0) return;
        string label = line[..colon], value = line[(colon + 1)..].Trim();
        if (SourceKeys.TryGetValue(line, out var key))
        {
            if (!SourceKeys.ContainsKey(label)) Remember(label, new(key, [], [], Line: 0, Part: 0));
            if (!SourceKeys.ContainsKey(value) && !Projections.ContainsKey(value)) Remember(value, new(key, [], [], Line: 0, Part: 1));
        }
        else if (Projections.TryGetValue(line, out var parent))
        {
            if (parent.Key != null)
            {
                if (!SourceKeys.ContainsKey(label)) Remember(label, parent with { Line = Math.Max(0, parent.Line), Part = 0 });
                if (!SourceKeys.ContainsKey(value) && !Projections.ContainsKey(value)) Remember(value, parent with { Line = Math.Max(0, parent.Line), Part = 1 });
            }
            else if (parent.Separator == null && !SourceKeys.ContainsKey(value) && !Projections.ContainsKey(value))
            {
                int skip = colon + 1;
                var parts = new List<string>(); var raw = new List<bool>();
                for (int i = 0; i < parent.Parts.Length; i++)
                {
                    string part = parent.Parts[i];
                    if (skip >= part.Length) { skip -= part.Length; continue; }
                    string remainder = part[skip..];
                    if (skip > 0 && !(i < parent.Raw.Length && parent.Raw[i])) { RememberSummaryParts(part); Alias(remainder.Trim(), remainder); }
                    parts.Add(remainder); raw.Add(i < parent.Raw.Length && parent.Raw[i]); skip = 0;
                }
                Remember(value, new(null, parts.ToArray(), raw.ToArray()));
            }
        }
    }
    private static int Colon(string text) { int full = text.IndexOf('：'), ascii = text.IndexOf(':'); return full < 0 ? ascii : ascii < 0 ? full : Math.Min(full, ascii); }
    private static void RememberFragments(string key, string[] arguments)
    {
        var entry = Catalog[key];
        var lines = entry.Source.Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            string template = lines[index];
            void Add(string part, int section)
            {
                if (string.IsNullOrWhiteSpace(part) || Regex.IsMatch(part, @"^\s*\{\d+\}\s*$")) return;
                string value = string.Format(CultureInfo.CurrentCulture, part, arguments.Cast<object?>().ToArray());
                Remember(value.Trim(), new(key, arguments, entry.RawArgs, Line: index, Part: section));
            }
            if (lines.Length > 1) Add(template, -1);
            int colon = template.IndexOf('：');
            if (colon >= 0) { Add(template[..colon], 0); Add(template[(colon + 1)..], 1); }
        }
    }
    public static string Concat(object?[] parts, bool[] raw)
    {
        var values = parts.Select(p => p?.ToString() ?? "").ToArray();
        var source = string.Concat(values); Remember(source, new(null, values, raw)); return source;
    }
    public static string Join<T>(string separator, IEnumerable<T> items, bool raw = false)
    {
        var values = items.Select(p => p?.ToString() ?? "").ToArray();
        var source = string.Join(separator, values);
        Remember(source, new(null, values, Enumerable.Repeat(raw, values.Length).ToArray(), separator)); return source;
    }
    public static string Join<T>(char separator, IEnumerable<T> items, bool raw = false) => Join(separator.ToString(), items, raw);
    public static string Join(string separator, string item, bool raw = false) => item;
    public static string Join(char separator, string item, bool raw = false) => item;
    private static readonly ConcurrentDictionary<string,string> PresentedSources = new(StringComparer.Ordinal);
    public static string Original(string? displayed) => displayed != null && PresentedSources.TryGetValue(displayed,out var source) ? source : displayed ?? "";
    public static string Translate(string? source)
    {
        source??="";
        string value=Render(source,new HashSet<string>(StringComparer.Ordinal),0,Language);
        if(value!=source && source.Length<16384){if(PresentedSources.Count>12000)PresentedSources.Clear();PresentedSources[value]=source;}
        return value;
    }
    private static string Render(string source, HashSet<string> stack, int depth,string code)
    {
        if (source.Length == 0 || depth > 16) return source;
        if (SourceKeys.TryGetValue(source, out var key) && !key.StartsWith("message.", StringComparison.Ordinal)) return GetFor(code,key);
        if (!stack.Add(source)) return source;
        try
        {
            if (Authored.TryGetValue(source, out var p) || Projections.TryGetValue(source, out p))
            {
                var parts = p.Parts.Select((v, i) => i < p.Raw.Length && p.Raw[i] ? v : Render(v, stack, depth + 1,code)).ToArray();
                if (p.Key != null)
                {
                    try {
                        string template = GetFor(code,p.Key, false);
                        if (p.Line >= 0) {
                            var lines = template.Split('\n');
                            if (p.Line >= lines.Length) return source;
                            template = lines[p.Line];
                            if (p.Part >= 0) { int colon = Colon(template); if (colon < 0) return source; template = p.Part == 0 ? template[..colon] : template[(colon + 1)..].TrimStart(); }
                        }
                        return string.Format(CultureInfo.CurrentCulture, DisplayParagraphs(template,code), parts.Cast<object?>().ToArray());
                    }
                    catch (FormatException) { return source; }
                }
                return p.Separator == null ? string.Concat(parts) : string.Join(Render(p.Separator, stack, depth + 1,code), parts);
            }
            if (NormalizedKeys.TryGetValue(Normalize(source), out key)) return GetFor(code,key);
            if (source.Contains('\n')) return string.Join('\n', source.Split('\n').Select(line => Render(line, stack, depth + 1,code)));
            return source;
        }
        finally { stack.Remove(source); }
    }
}
