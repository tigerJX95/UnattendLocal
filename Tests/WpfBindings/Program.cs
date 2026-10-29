using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xml.Linq;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var xaml = XDocument.Load(args[0]);
        XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            checks++;
        }
        foreach (var culture in new[] { "en-US", "zh-CN" })
        foreach (var mode in new[] { "fixed", "match", "interactive", "script" })
        foreach (var field in new[] { "PeMinCapacityEditor", "PeMaxCapacityEditor" })
        {
            // Use the production XAML binding, including its converter and validation settings.
            var original = xaml.Descendants(ns + "TextBox").Single(e => (string?)e.Attribute(x + "Name") == field);
            var host = (StackPanel)XamlReader.Parse(
                "<StackPanel xmlns=\"" + ns + "\" xmlns:x=\"" + x + "\" xmlns:local=\"clr-namespace:UnattendLocal.Desktop;assembly=UnattendLocal\">" +
                "<StackPanel.Resources><local:OptionalIntegerConverter x:Key=\"OptionalInteger\"/></StackPanel.Resources>" + original + "</StackPanel>");
            var editor = (TextBox)host.Children[0];
            var plan = new InstallPlan { PeTargetMode = mode, PeTargetScript = "WScript.Echo 0" };
            host.Language = XmlLanguage.GetLanguage(culture);
            host.DataContext = new MainViewModel { Plan = plan };
            var expression = editor.GetBindingExpression(TextBox.TextProperty)!;
            int? Value() => field == "PeMinCapacityEditor" ? plan.PeMinDiskGiB : plan.PeMaxDiskGiB;
            void Enter(string text) { editor.SetCurrentValue(TextBox.TextProperty, text); expression.UpdateSource(); }
            Check(editor.Text == "" && !Validation.GetHasError(editor), "Initial null must display blank without an error.");
            Enter("64");
            Check(Value() == 64 && !Validation.GetHasError(editor), "Valid input must reach the plan.");
            plan.ConfirmWipeDisk = true;
            Enter("");
            Check(Value() is null && !Validation.GetHasError(editor) && !plan.ConfirmWipeDisk, "Clearing must write null and revoke disk consent.");
            Enter(" \t ");
            Check(Value() is null && !Validation.GetHasError(editor), "Whitespace must mean no capacity limit.");
            Enter("128");
            foreach (var invalid in new[] { "abc", "64abc", "64.5", "99999999999999999999" })
            {
                Enter(invalid);
                Check(Validation.GetHasError(editor) && Value() == 128, "Invalid input must remain a blocking binding error.");
            }
            Enter("");
            Check(Value() is null && !Validation.GetHasError(editor), "Clearing invalid input must recover the binding.");
        }
        Console.WriteLine($"Optional capacity WPF bindings: {checks} checks passed.");
    }
}
