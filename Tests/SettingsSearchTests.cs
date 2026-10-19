using UnattendLocal.Desktop;

static class SettingsSearchTests
{
    public static int Run()
    {
        int count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; Console.WriteLine("PASS V1.6 " + message); }
        var categories = SettingsSearchCatalog.Categories;
        Check(categories.First().PageIndex == null && categories.Count(c => c.PageIndex == null) == 1, "all-categories option appears once and first");
        Check(categories.Skip(1).Select(c => c.PageIndex!.Value).SequenceEqual(Enumerable.Range(0, 15)), "categories follow all fifteen navigation pages");
        foreach (var category in categories.Skip(1))
        {
            var found = SettingsSearchCatalog.Search("", category.PageIndex);
            Check(found.Count > 0 && found.All(r => r.Page == category.Label && r.PageIndex == category.PageIndex), "blank query browses only " + category.Label);
            foreach (var query in new[] { "Windows", "$OEM$", "自动 登录", "PowerShell", "Ｗｉｎｄｏｗｓ" })
                Check(SettingsSearchCatalog.Search(query, category.PageIndex).SequenceEqual(SettingsSearchCatalog.Search(query).Where(r => r.PageIndex == category.PageIndex)), "category retains matching/ranking " + category.Label + " / " + query);
        }
        Check(SettingsSearchCatalog.Search(null).SequenceEqual(SettingsSearchCatalog.Search("")), "null query browses the full catalog");
        Check(SettingsSearchCatalog.Search("", -1).SequenceEqual(SettingsSearchCatalog.Search("")) && SettingsSearchCatalog.NormalizeCategory(999) == null, "invalid remembered category safely falls back to all");
        Check(SettingsSearchCatalog.Search("$OEM$", 3).Any(r => r.Aliases.Contains("UseConfigurationSet")) && SettingsSearchCatalog.Search("$OEM$", 4).All(r => r.PageIndex == 4), "matching does not escape the selected category");
        var appItems = SettingsSearchCatalog.Search("", 7).Where(r => r.AppId.Length > 0).ToArray();
        Check(appItems.Length == 60 && appItems.Select(r => r.AppId).Distinct().Count() == 60, "app-removal category retains sixty individually navigable items");
        Check(SettingsSearchCatalog.Search("BitLocker").Count > 0 && SettingsSearchCatalog.Search("BitLocker", 13).Count == 0, "scoped no-results does not fall back silently to global matches");
        Check(SearchTextMatcher.Exact("  Ｗｉｎｄｏｗｓ  ", "windows") && !SearchTextMatcher.Exact("Windows", "win"), "exact ranking uses the same width/case normalization as matching");
        foreach (var query in new[] { "Windows", " $OEM$ ", "PowerShell" })
        {
            string fullWidth = string.Concat(query.Select(c => c is >= '!' and <= '~' ? (char)(c + 0xfee0) : c));
            Check(SettingsSearchCatalog.Search(query).SequenceEqual(SettingsSearchCatalog.Search(fullWidth)), "full-width and ASCII searches have identical ranking: " + query);
        }

        var session = new SettingsSearchSession();
        session.Request("", 7); session.Refresh(); session.Select(appItems.Last());
        Check(session.Selected == appItems.Last(), "user selection can point to a non-first result");
        session.Refresh(); Check(session.Selected == appItems.Last(), "flush without a new query preserves the user's selected row");
        session.Request("DynamicUpdate", 3);
        Check(session.IsPending && session.Selected == null, "typing invalidates an old navigation selection immediately");
        session.Select(appItems.Last()); Check(session.Selected == null, "pending results cannot accept a stale mouse selection");
        session.Request("RegisteredOwner", 1); session.Refresh();
        Check(!session.IsPending && session.Query == "RegisteredOwner" && session.PageIndex == 1 && session.Results.Count > 0 && session.Selected == session.Results[0] && session.Results.All(r => r.PageIndex == 1), "fast replacement then Enter flushes only the latest query and category");
        session.Select(appItems[0]); Check(session.Selected == null, "a row from the previous category cannot be selected");
        session.Request("no-such-setting-v16", 1); session.Refresh();
        Check(!session.IsPending && session.Results.Count == 0 && session.Selected == null, "zero matches cannot expose a previous navigation target");
        session.Request("", null); session.Refresh();
        Check(session.PageIndex == null && session.Results.Count == SettingsSearchCatalog.All.Count && session.Selected != null, "reset restores the complete catalog and an actionable result");
        session.Request(null, int.MaxValue); session.Refresh();
        Check(session.Query == "" && session.PageIndex == null && session.Results.Count == SettingsSearchCatalog.All.Count, "session normalizes missing query and invalid category");
        return count;
    }
}
