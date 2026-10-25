namespace UnattendLocal.Desktop;

// 只重排现有摘要，值仍由原有配置联动生成；未知字段继续显示。
public record SummaryRow(string SourceLabel, string Label, string Value)
{
    public string DisplayValue => Value;
    public bool IsError => DiagnosticText.IsErrorSummary(SourceLabel + "：" + Value);
}
public record SummarySection(string Kind, string Title, IReadOnlyList<SummaryRow> Rows);
public record SummaryReview(string Title, string Value, bool NeedsAttention)
{
    public bool IsError => Title == global::UnattendLocal.Localization.UiText.Source("text.58386516447e") && NeedsAttention;
}
public record SummaryPresentation(IReadOnlyList<SummarySection> Sections, IReadOnlyList<SummaryReview> Reviews, IReadOnlyList<string> Notices)
{
    private static readonly (string Kind, string Title, string[] Labels)[] Categories =
    [
        ("basic", global::UnattendLocal.Localization.UiText.Source("text.4bbdce0e924a"), [global::UnattendLocal.Localization.UiText.Source("text.5d922bf0452e"), global::UnattendLocal.Localization.UiText.Source("text.920a6b8d44bd"), global::UnattendLocal.Localization.UiText.Source("text.f9432ecb8e0a"), global::UnattendLocal.Localization.UiText.Source("text.735b6028db41"), global::UnattendLocal.Localization.UiText.Source("text.061246bd31e0"), global::UnattendLocal.Localization.UiText.Source("text.fcb62ca5dfa1"), global::UnattendLocal.Localization.UiText.Source("text.89c3759ef74f"), global::UnattendLocal.Localization.UiText.Source("text.c8617322079b"), global::UnattendLocal.Localization.UiText.Source("text.fa67952632ed"), global::UnattendLocal.Localization.UiText.Source("text.9328734717b0")]),
        ("protection", global::UnattendLocal.Localization.UiText.Source("text.166f2bb1e37a"), [global::UnattendLocal.Localization.UiText.Source("text.98bee575873a"), global::UnattendLocal.Localization.UiText.Source("text.4e152ba8ef23"), global::UnattendLocal.Localization.UiText.Source("text.452494b412fd"), global::UnattendLocal.Localization.UiText.Source("text.42568d12154e"), global::UnattendLocal.Localization.UiText.Source("text.1ee43d4aa992"), global::UnattendLocal.Localization.UiText.Source("text.cc267fdab07e"), "CompactOS", global::UnattendLocal.Localization.UiText.Source("text.d1616a31a5ab"), global::UnattendLocal.Localization.UiText.Source("text.532817a272a9"), global::UnattendLocal.Localization.UiText.Source("text.0a17ec6f46cc"), "SmartScreen", global::UnattendLocal.Localization.UiText.Source("text.081fc746e8e3"), global::UnattendLocal.Localization.UiText.Source("text.bdbc575db6bc"), "AppLocker", global::UnattendLocal.Localization.UiText.Source("text.a0c25e67053c"), global::UnattendLocal.Localization.UiText.Source("text.e107f7886578"), global::UnattendLocal.Localization.UiText.Source("text.1fa60cde8f71"), global::UnattendLocal.Localization.UiText.Source("text.13b8559bcaa2")]),
        ("network", global::UnattendLocal.Localization.UiText.Source("text.0015e8fc1722"), [global::UnattendLocal.Localization.UiText.Source("text.97b31b5d63f5"), global::UnattendLocal.Localization.UiText.Source("text.7f7db73c07f1"), global::UnattendLocal.Localization.UiText.Source("text.69050783cafb"), global::UnattendLocal.Localization.UiText.Source("text.fe924a910aed"), global::UnattendLocal.Localization.UiText.Source("text.86651d17a401"), global::UnattendLocal.Localization.UiText.Source("text.5d4f709636ee"), global::UnattendLocal.Localization.UiText.Source("text.2f8dd7280e0a"), global::UnattendLocal.Localization.UiText.Source("text.98ee4c3f8e53"), global::UnattendLocal.Localization.UiText.Source("text.91ebd1a7c347"), global::UnattendLocal.Localization.UiText.Source("text.c255841f6809"), global::UnattendLocal.Localization.UiText.Source("text.ee932fe20821"), global::UnattendLocal.Localization.UiText.Source("text.8190df8a26b0"), global::UnattendLocal.Localization.UiText.Source("text.cc1c0c4c9120")]),
        ("shell", global::UnattendLocal.Localization.UiText.Source("text.c004454831c1"), [global::UnattendLocal.Localization.UiText.Source("text.80ecd8a38be4"), global::UnattendLocal.Localization.UiText.Source("text.ae07401578d9"), global::UnattendLocal.Localization.UiText.Source("text.bfa59c81c311"), global::UnattendLocal.Localization.UiText.Source("text.5f8fc718797a"), global::UnattendLocal.Localization.UiText.Source("text.aee448a5470c"), global::UnattendLocal.Localization.UiText.Source("text.413dd530fbcd")]),
        ("explorer", global::UnattendLocal.Localization.UiText.Source("text.3263416e27c1"), [global::UnattendLocal.Localization.UiText.Source("text.17abd349b511"), global::UnattendLocal.Localization.UiText.Source("text.433d82edd2f0"), global::UnattendLocal.Localization.UiText.Source("text.d49ecddf530b"), global::UnattendLocal.Localization.UiText.Source("text.aad16d71c52c")]),
        ("appearance", global::UnattendLocal.Localization.UiText.Source("text.10e5543abf28"), [global::UnattendLocal.Localization.UiText.Source("text.0ab315b8cb23"), global::UnattendLocal.Localization.UiText.Source("text.fa6a7f591b47"), global::UnattendLocal.Localization.UiText.Source("text.fbd77c61ce36"), global::UnattendLocal.Localization.UiText.Source("text.c7a90b8fdf58")])
    ];
    private static readonly Dictionary<string, string> ShortLabels = new()
    {
        [global::UnattendLocal.Localization.UiText.Source("text.98bee575873a")] = global::UnattendLocal.Localization.UiText.Source("text.6466d71d3666"), [global::UnattendLocal.Localization.UiText.Source("text.532817a272a9")] = global::UnattendLocal.Localization.UiText.Source("text.1edd3e890c20"),
        [global::UnattendLocal.Localization.UiText.Source("text.aee448a5470c")] = global::UnattendLocal.Localization.UiText.Source("text.0567d054b058"), [global::UnattendLocal.Localization.UiText.Source("text.413dd530fbcd")] = global::UnattendLocal.Localization.UiText.Source("text.107bc35f5536"),
        [global::UnattendLocal.Localization.UiText.Source("text.433d82edd2f0")] = global::UnattendLocal.Localization.UiText.Source("text.c2abe223ded2"), [global::UnattendLocal.Localization.UiText.Source("text.d49ecddf530b")] = global::UnattendLocal.Localization.UiText.Source("text.cf47f00a10c4"), [global::UnattendLocal.Localization.UiText.Source("text.aad16d71c52c")] = global::UnattendLocal.Localization.UiText.Source("text.3646c57ef407")
    };
    public static SummaryPresentation FromText(string? summary)
    {
        var buckets = Categories.ToDictionary(c => c.Kind, _ => new List<SummaryRow>());
        var extra = new List<SummaryRow>(); var reviews = new List<SummaryReview>(); var notices = new List<string>();
        foreach (var line in (summary ?? "").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            if (line.StartsWith(global::UnattendLocal.Localization.UiText.Source("text.b9e9c34b2173"), StringComparison.Ordinal)) { notices.Add(line); continue; }
            UnattendLocal.Localization.UiText.RememberSummaryParts(line);
            int colon = line.IndexOf('：');
            string label = colon >= 0 ? line[..colon] : global::UnattendLocal.Localization.UiText.Source("text.6f3ebd2d3e87"), value = colon >= 0 ? line[(colon + 1)..].Trim() : line;
            if (label is "生成前待填" or "安装时仍需操作" or "安装时可能需要操作")
            {
                bool needsAttention = label == global::UnattendLocal.Localization.UiText.Source("text.58386516447e") ? !value.StartsWith(global::UnattendLocal.Localization.UiText.Source("text.d5f6aad4d287"), StringComparison.Ordinal) : !value.StartsWith(global::UnattendLocal.Localization.UiText.Source("text.868e36f81b24"), StringComparison.Ordinal);
                reviews.Add(new(label, value, needsAttention)); continue;
            }
            var category = Categories.FirstOrDefault(c => c.Labels.Contains(label));
            var row = new SummaryRow(label, ShortLabels.GetValueOrDefault(label, label), value);
            if (category.Kind != null) buckets[category.Kind].Add(row); else extra.Add(row);
        }
        var sections = Categories.Where(c => buckets[c.Kind].Count > 0).Select(c => new SummarySection(c.Kind, c.Title, buckets[c.Kind])).ToList();
        if (extra.Count > 0) sections.Add(new("other", global::UnattendLocal.Localization.UiText.Source("text.73b126a0aabe"), extra));
        return new(sections, reviews, notices);
    }
}
