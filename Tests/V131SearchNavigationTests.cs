using System.Xml.Linq;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class V131SearchNavigationTests
{
    public static int Run()
    {
        int count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception("Search navigation: " + message); count++; }
        string path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Desktop/MainWindow.xaml"));
        var doc = XDocument.Load(path);
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        XNamespace ui = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var pages = doc.Descendants(ui + "TabControl").Single(e => (string?)e.Attribute(x + "Name") == "Pages");
        var tabs = pages.Elements(ui + "TabItem").ToArray();
        foreach (var result in SettingsSearchCatalog.All)
        {
            var found = doc.Descendants().Where(e => (string?)e.Attribute(x + "Name") == result.Target).ToArray();
            Check(found.Length == 1, "target exists exactly once: " + result.Target);
            var target = found.Single();
            Check(target.Name != ui + "TextBlock", "target must be an editor or content section: " + result.Target);
            var tab = target.Ancestors(ui + "TabItem").Single(e => ReferenceEquals(e.Parent, pages));
            Check(Array.IndexOf(tabs, tab) == result.PageIndex, "target belongs to declared page: " + result.Target);
        }
        foreach (string query in new[] { "CompactOS", "CompactOS 系统文件压缩", "CompactOS 压缩方式" })
        {
            var found = SettingsSearchCatalog.Search(query);
            Check(found.Count == 1 && found[0].Target == "SettingAnchor059", "one actionable CompactOS result: " + query);
        }
        Check(doc.Descendants(ui + "ComboBox").Count(e => ((string?)e.Attribute("SelectedValue"))?.Contains("Plan.CompactOsMode") == true) == 1,
            "regular and PE installation share one CompactOS editor");
        Check(doc.Descendants(ui + "Button").Single(e => (string?)e.Attribute("Click") == "CompactOsSettingsClick")
            .Ancestors().Any(e => ((string?)e.Attribute("Visibility"))?.Contains("Plan.UseGeneratedPe") == true), "CompactOS navigation is available in PE area");
        var vm = new MainViewModel();
        vm.Plan.UseGeneratedPe = true;
        bool updated = false; vm.PropertyChanged += (_, e) => updated |= e.PropertyName == nameof(MainViewModel.PeCompactOsStatus);
        vm.Plan.CompactOsMode = CompactOsSettings.Enabled;
        Check(updated && vm.PeCompactOsStatus.Contains(UiText.Source("v131.compact.enabled")), "PE CompactOS status follows the shared setting");
        vm.Plan.CompactOsMode = CompactOsSettings.Disabled;
        Check(vm.PeCompactOsStatus.Contains(UiText.Source("v131.compact.disabled")), "PE CompactOS status follows disabled selection");
        return count;
    }
}
