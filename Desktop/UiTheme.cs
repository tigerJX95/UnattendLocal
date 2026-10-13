using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace UnattendLocal.Desktop;

public static class UiTheme
{
    public static string Current { get; private set; } = "Light";
    public static bool Apply(string theme, bool save = true)
    {
        Current = theme == "Dark" ? "Dark" : "Light";
        Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
        { Source = new Uri($"/UnattendLocal;component/Themes/{Current}.xaml", UriKind.Relative) };
        foreach (Window window in Application.Current.Windows) UpdateTitleBar(window);
        return !save || UiPreferences.Save(Current);
    }
    public static void UpdateTitleBar(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;
        int dark = Current == "Dark" ? 1 : 0;
        try { _ = DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int)); }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
    }
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
