using System.Text.Json;
using System.IO;

namespace UnattendLocal.Desktop;

// 工具自身的显示偏好独立于安装方案，不进入应答文件。
public static class UiPreferences
{
    private static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UnattendLocal", "ui-preferences.json");
    public static string Load(string? path = null)
    {
        try
        {
            path ??= DefaultPath;
            if (!File.Exists(path) || new FileInfo(path).Length > 4096) return "Light";
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("theme", out var theme) && theme.ValueKind == JsonValueKind.String && theme.GetString() == "Dark") return "Dark";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException) { }
        return "Light";
    }
    public static bool Save(string theme, string? path = null)
    {
        if (theme is not ("Light" or "Dark")) return false;
        string? temporary = null;
        try
        {
            path ??= DefaultPath;
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(new { theme }));
            File.Move(temporary, path, true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { return false; }
        finally
        {
            if (temporary != null) try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
