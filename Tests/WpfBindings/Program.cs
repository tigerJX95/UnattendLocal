using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml.Linq;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

internal static class Program
{
    // Flush inherited DataContext and binding attachment work before assertions.
    private static void DrainDispatcher() => Dispatcher.CurrentDispatcher.Invoke(
        () => { }, DispatcherPriority.ApplicationIdle, CancellationToken.None, TimeSpan.FromSeconds(5));

    [STAThread]
    private static void Main(string[] args)
    {
        var xaml = XDocument.Load(args[0]);
        XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        int checks = 0;
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
            var viewModel = new MainViewModel { Plan = plan };
            host.DataContext = viewModel;
            DrainDispatcher();
            var expression = editor.GetBindingExpression(TextBox.TextProperty)
                ?? throw new Exception($"{culture}/{mode}/{field}: production Text binding is missing.");
            int? Value() => field == "PeMinCapacityEditor" ? plan.PeMinDiskGiB : plan.PeMaxDiskGiB;
            void Check(bool condition, string message)
            {
                if (!condition)
                {
                    string errors = string.Join("; ", Validation.GetErrors(editor).Select(e => e.ErrorContent?.ToString()));
                    throw new Exception($"{culture}/{mode}/{field}: {message} Status={expression.Status}; Text=[{editor.Text}]; Plan={Value()?.ToString(CultureInfo.InvariantCulture) ?? "null"}; ResolvedProperty={expression.ResolvedSourcePropertyName}; Validation=[{errors}]");
                }
                checks++;
            }
            void Enter(string text)
            {
                editor.SetCurrentValue(TextBox.TextProperty, text);
                DrainDispatcher();
                Check(ReferenceEquals(expression, editor.GetBindingExpression(TextBox.TextProperty)), "Editing must retain the binding.");
            }
            string property = field == "PeMinCapacityEditor" ? nameof(InstallPlan.PeMinDiskGiB) : nameof(InstallPlan.PeMaxDiskGiB);
            Check(expression.Status == BindingStatus.Active && ReferenceEquals(expression.DataItem, viewModel)
                && ReferenceEquals(expression.ResolvedSource, plan) && expression.ResolvedSourcePropertyName == property,
                "Binding must resolve to the intended plan property before testing input.");
            Check(editor.Text == "" && !Validation.GetHasError(editor), "Initial null must display blank without an error.");
            // An empty editor alone cannot prove that a nullable binding is attached.
            if (field == "PeMinCapacityEditor") plan.PeMinDiskGiB = 32; else plan.PeMaxDiskGiB = 32;
            DrainDispatcher();
            Check(editor.Text == "32" && !Validation.GetHasError(editor), "Plan changes must reach the editor.");
            if (field == "PeMinCapacityEditor") plan.PeMinDiskGiB = null; else plan.PeMaxDiskGiB = null;
            DrainDispatcher();
            Check(editor.Text == "" && !Validation.GetHasError(editor), "Clearing the plan must clear the editor.");
            Enter("64");
            Check(Value() == 64 && !Validation.GetHasError(editor), "Valid input must reach the plan.");
            plan.ConfirmWipeDisk = true;
            Enter("");
            Check(Value() is null && !Validation.GetHasError(editor) && !plan.ConfirmWipeDisk, "Clearing must write null and revoke disk consent.");
            Enter(" \t ");
            Check(Value() is null && !Validation.GetHasError(editor), "Whitespace must mean no capacity limit.");
            Enter("128");
            Check(Value() == 128 && !Validation.GetHasError(editor), "A second valid value must reach the plan.");
            foreach (var invalid in new[] { "abc", "64abc", "64.5", "99999999999999999999" })
            {
                Enter(invalid);
                Check(Validation.GetHasError(editor) && Value() == 128, "Invalid input must remain a blocking binding error.");
            }
            Enter("");
            Check(Value() is null && !Validation.GetHasError(editor), "Clearing invalid input must recover the binding.");
            Enter("256");
            Check(Value() == 256 && !Validation.GetHasError(editor), "Recovered binding must accept valid input.");
            Console.WriteLine($"PASS {culture}/{mode}/{field}");
            BindingOperations.ClearAllBindings(editor);
            host.DataContext = null;
            DrainDispatcher();
        }
        Console.WriteLine($"Optional capacity WPF bindings: {checks} checks passed.");
    }
}
