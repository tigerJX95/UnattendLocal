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
            throw new ArgumentException(label + "加载脚本必须填写，且不超过 2 Mi 字符、不含 XML 非法字符。");
        return script;
    }
    public static bool IsLockScript(InstallPlan plan) => plan.LockScreenMode == LockScript;
    public static ILockScreenSettings BuildLockScreen(InstallPlan plan) => plan.LockScreenMode switch
    {
        LockDefault => new DefaultLockScreenSettings(),
        LockScript => new ScriptLockScreenSettings(ValidateScript(plan.LockScreenImageScript, "锁屏图片")),
        _ => throw new ArgumentException("请选择有效的锁屏图片设置方式。")
    };
    public static string LockSummary(InstallPlan p) => p.LockScreenMode switch
    {
        LockDefault => "Windows 默认锁屏（不写入图片覆盖）",
        LockScript => string.IsNullOrWhiteSpace(p.LockScreenImageScript) ? "锁屏图片：尚未填写加载脚本" : "脚本加载锁屏图片（效果取决于目标版本及策略）",
        _ => "锁屏设置方式无效，请重新选择"
    };
    private static string Normal(string value) => string.Join('\n', value.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
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
        bool known = loaders.Length == 1 && !string.IsNullOrWhiteSpace(loaders[0].InnerText) && setup.Contains(Normal(string.Join('\n', expected)), StringComparison.Ordinal) && Occurrences(setup, getter) == 1;
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
        if (desktop.Ambiguous) notes.Add("桌面图片加载或应用脚本未完整识别，原文保留；暂不能用桌面背景设置覆盖该脚本。");
        if (locked.Ambiguous) notes.Add("锁屏图片加载或应用脚本未完整识别，原文保留；暂不能用锁屏图片设置覆盖该脚本。");
    }
    public static void ValidateImported(InstallPlan plan, XmlDocument source)
    {
        if (plan.WallpaperMode != WallpaperSettings.Default && Inspect(source, false).Ambiguous)
            throw new ArgumentException("桌面背景与原 XML 中未完整识别的图片脚本重叠，请用原工具调整后重新导入。");
        if (IsLockScript(plan) && Inspect(source, true).Ambiguous)
            throw new ArgumentException("锁屏图片与原 XML 中未完整识别的脚本重叠，请用原工具调整后重新导入。");
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
            data.Blockers = data.Blockers.Append("旧方案的图片设置无法可靠迁移编辑基准，请重新导入原 XML 后再导出。").Distinct().ToArray();
        }
    }
}
