using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Schneegans.Unattend;

namespace UnattendLocal.Core;

public static class ImageBackgroundSettings
{
    public const string LockDefault = "使用 Windows 默认锁屏", LockScript = "使用锁屏图片（脚本加载）";
    public const string DesktopExample = """
        foreach ($drive in [System.IO.DriveInfo]::GetDrives()) {
            if (-not $drive.IsReady) { continue; }
            $path = Join-Path -Path $drive.RootDirectory -ChildPath 'wallpaper.jpg';
            if (Test-Path -LiteralPath $path -PathType Leaf) {
                return [System.IO.File]::ReadAllBytes($path);
            }
        }
        throw 'Cannot find wallpaper.jpg on an accessible drive root.';
        """;
    public const string LockExample = """
        foreach ($drive in [System.IO.DriveInfo]::GetDrives()) {
            if (-not $drive.IsReady) { continue; }
            $path = Join-Path -Path $drive.RootDirectory -ChildPath 'lockscreen.jpg';
            if (Test-Path -LiteralPath $path -PathType Leaf) {
                return [System.IO.File]::ReadAllBytes($path);
            }
        }
        throw 'Cannot find lockscreen.jpg on an accessible drive root.';
        """;
    public static string ValidateScript(string? script, string label)
    {
        if (string.IsNullOrWhiteSpace(script) || script.Length > 2 * 1024 * 1024 || script.Any(c => !XmlConvert.IsXmlChar(c)))
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { label, global::UnattendLocal.Localization.UiText.Source("text.2cfa3c7f4a76") },new bool[]{false,false}));
        return script;
    }
    public static bool IsLockScript(InstallPlan plan) => plan.LockScreenMode == LockScript;
    public static ILockScreenSettings BuildLockScreen(InstallPlan plan) => plan.LockScreenMode switch
    {
        LockDefault => new DefaultLockScreenSettings(),
        LockScript => new ScriptLockScreenSettings(ValidateScript(plan.LockScreenImageScript, global::UnattendLocal.Localization.UiText.Source("text.c7a90b8fdf58"))),
        _ => throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.766afc1b8793"))
    };
    public static string LockSummary(InstallPlan p) => p.LockScreenMode switch
    {
        LockDefault => global::UnattendLocal.Localization.UiText.Source("text.a584fc9be0b8"),
        LockScript => string.IsNullOrWhiteSpace(p.LockScreenImageScript) ? global::UnattendLocal.Localization.UiText.Source("text.bb8a66a606ef") : global::UnattendLocal.Localization.UiText.Source("text.9877e78554d2"),
        _ => global::UnattendLocal.Localization.UiText.Source("text.3ea77b88ac04")
    };
    private static string Normal(string value) => global::UnattendLocal.Localization.UiText.Join('\n', value.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0), true);
    private static int Occurrences(string body, string value) => Regex.Matches(body, Regex.Escape(value), RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)).Count;
    public record Source(string? Script, bool Ambiguous);
    public static Source Inspect(XmlDocument doc, bool locked)
    {
        string getter = "C:\\Windows\\Setup\\Scripts\\" + (locked ? "GetLockScreenImage.ps1" : "GetWallpaper.ps1");
        string image = "C:\\Windows\\Setup\\Scripts\\" + (locked ? "LockScreenImage" : "Wallpaper");
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        XmlElement[] At(string path) => files.Where(f => f.GetAttribute("path").Equals(path, StringComparison.OrdinalIgnoreCase)).ToArray();
        string Content(string name) => At("C:\\Windows\\Setup\\Scripts\\" + name) is { Length: 1 } f ? Normal(f[0].InnerText) : "";
        var loaders = At(getter); string setup = Content("Specialize.ps1");
        bool any = loaders.Length > 0 || setup.Contains(getter, StringComparison.OrdinalIgnoreCase) || (locked && setup.Contains("LockScreenImagePath", StringComparison.OrdinalIgnoreCase));
        if (!any) return new(null, false);
        var expected = new List<string> { "try {", "$bytes = & '" + getter + "';", "[System.IO.File]::WriteAllBytes( '" + image + "', $bytes );" };
        if (locked) expected.Add("reg.exe add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\PersonalizationCSP\" /v LockScreenImagePath /t REG_SZ /d \"" + image + "\" /f;");
        expected.AddRange(["} catch {", "$_;", "}"]);
        bool known = loaders.Length == 1 && !string.IsNullOrWhiteSpace(loaders[0].InnerText) && setup.Contains(Normal(global::UnattendLocal.Localization.UiText.Join('\n', expected, false)), StringComparison.Ordinal) && Occurrences(setup, getter) == 1;
        if (locked) known &= Occurrences(setup, "LockScreenImagePath") == 1;
        else
        {
            using var resource = typeof(UnattendGenerator).Assembly.GetManifestResourceStream("Schneegans.Unattend.resource.SetWallpaper.ps1")!;
            using var reader = new StreamReader(resource);
            string setter = "C:\\Windows\\Setup\\Scripts\\SetWallpaper.ps1";
            string helper = Normal(reader.ReadToEnd() + "\nSet-WallpaperImage -LiteralPath '" + image + "';");
            string user = Content("UserOnce.ps1");
            known &= Content("SetWallpaper.ps1") == helper && user.Contains("& '" + setter + "';", StringComparison.Ordinal) && Occurrences(user, setter) == 1;
        }
        return known ? new(loaders[0].InnerText.TrimEnd('\r', '\n'), false) : new(null, true);
    }
    public static void ReadIntoPlan(XmlDocument source, InstallPlan plan, List<string> notes)
    {
        var desktop = Inspect(source, false); var locked = Inspect(source, true);
        plan.WallpaperImageScript = desktop.Script ?? "";
        if (desktop.Script != null) plan.WallpaperMode = WallpaperSettings.Script;
        else if (desktop.Ambiguous || WallpaperSettings.IsScript(plan)) plan.WallpaperMode = WallpaperSettings.Default;
        plan.LockScreenImageScript = locked.Script ?? "";
        plan.LockScreenMode = locked.Script != null ? LockScript : LockDefault;
        if (desktop.Ambiguous) notes.Add(global::UnattendLocal.Localization.UiText.Source("text.5239d06b6f71"));
        if (locked.Ambiguous) notes.Add(global::UnattendLocal.Localization.UiText.Source("text.ed371be2247d"));
    }
    public static void ValidateImported(InstallPlan plan, XmlDocument source)
    {
        if (plan.WallpaperMode != WallpaperSettings.Default && Inspect(source, false).Ambiguous)
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.6e9f955e6645"));
        if (IsLockScript(plan) && Inspect(source, true).Ambiguous)
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.edcabf7d9574"));
    }
    public static void NormalizeLoaded(InstallPlan plan, JsonElement json)
    {
        if (plan.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0) return;
        bool desktopMissing = !json.TryGetProperty(nameof(InstallPlan.WallpaperImageScript), out _);
        bool lockMissing = !json.TryGetProperty(nameof(InstallPlan.LockScreenMode), out _);
        if (!desktopMissing && !lockMissing) return;
        var source = XmlMerge.Read(data.SourceXml); var desktop = Inspect(source, false); var locked = Inspect(source, true);
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(plan))!;
        before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        if (desktopMissing && desktop.Script != null) { plan.WallpaperImageScript = desktop.Script; if (plan.WallpaperMode == WallpaperSettings.Default) plan.WallpaperMode = WallpaperSettings.Script; }
        if (lockMissing && locked.Script != null) { plan.LockScreenMode = LockScript; plan.LockScreenImageScript = locked.Script; }
        if (!(desktopMissing && desktop.Script != null) && !(lockMissing && locked.Script != null)) return;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;
        if (desktopMissing && desktop.Script != null) { after.WallpaperMode = WallpaperSettings.Script; after.WallpaperImageScript = desktop.Script; }
        if (lockMissing && locked.Script != null) { after.LockScreenMode = LockScript; after.LockScreenImageScript = locked.Script; }
        try
        {
            var service = new GeneratorService();
            data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(service.GenerateImportBaseline(before)), XmlMerge.Read(Encoding.UTF8.GetString(service.GenerateImportBaseline(after)))).OuterXml;
        }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException)
        {
            data.Blockers = data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.6df4da77ed66")).Distinct().ToArray();
        }
    }
}
