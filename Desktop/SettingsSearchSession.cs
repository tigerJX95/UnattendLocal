namespace UnattendLocal.Desktop;

// A pending query must never expose an old selection for navigation.
public sealed class SettingsSearchSession
{
    public string Query { get; private set; } = "";
    public int? PageIndex { get; private set; }
    public bool IsPending { get; private set; }
    public IReadOnlyList<SettingSearchResult> Results { get; private set; } = [];
    public SettingSearchResult? Selected { get; private set; }

    public void Request(string? query, int? pageIndex)
    {
        Query = query ?? "";
        PageIndex = SettingsSearchCatalog.NormalizeCategory(pageIndex);
        IsPending = true;
        Selected = null;
    }

    public void Refresh()
    {
        if (!IsPending) return;
        Results = SettingsSearchCatalog.Search(Query, PageIndex);
        Selected = Results.FirstOrDefault();
        IsPending = false;
    }

    public void Select(SettingSearchResult? result) => Selected = !IsPending && result != null && Results.Contains(result) ? result : null;
}
