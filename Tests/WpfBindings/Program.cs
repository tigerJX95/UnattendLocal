using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

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
        CheckLocalAccountsListTheme();
    }

    private static void CheckLocalAccountsListTheme()
    {
        var app = new App();
        app.InitializeComponent();
        var window = new MainWindow();
        try
        {
            var list = (ListBox)window.FindName("LocalUsersList");
            var accountMode = (ComboBox)window.FindName("SettingAnchor070");
            var pages = (TabControl)window.FindName("Pages");
            pages.SelectedItem = pages.Items.OfType<TabItem>()
                .Single(page => Equals(page.Header, UiText.Source("text.4e21977cff0f")));
            var plan = ((MainViewModel)window.DataContext).Plan;
            var automatic = UiText.Source("text.cdaa9a6700f6");
            var modes = accountMode.Items.Cast<ComboBoxItem>().Select(item => (string)item.Content).ToArray();
            foreach (var theme in new[] { "Dark", "Light", "Dark" })
            {
                UiTheme.Apply(theme, save: false);
                foreach (var mode in modes)
                {
                    plan.AccountMode = mode;
                    DrainDispatcher();
                    list.ApplyTemplate();
                    var border = list.Template.FindName("ListBorder", list) as Border;
                    var inputBrush = (SolidColorBrush)app.FindResource("InputBrush");
                    if (list.IsEnabled != (mode == automatic)
                        || border?.Background is not SolidColorBrush background
                        || background.Color != inputBrush.Color)
                        throw new Exception($"{theme}/{mode}: the account list must use the current theme in both enabled and disabled states.");
                }
            }
            Console.WriteLine("PASS local account list theme across account modes and theme switches.");
            CheckSidebarLayout(window);
            CheckButtonToolTips(window);
            CheckTimeZoneBindings(window);
            CheckAdvancedXmlFormThemes(window);
            CheckSetupLogTheme(window);
            CheckDeploymentReviewWindows(window);
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckButtonToolTips(MainWindow window)
    {
        var style = (Style)window.FindResource(typeof(Button));
        string previousLanguage = UiText.Language;
        try
        {
            foreach (string culture in new[] { "zh-CN", "en-US" })
            {
                UiText.SetLanguage(culture);
                foreach (string key in new[] { "text.b5141d3d19e9", "text.0c70665b6eb6", "text.fac2a67ad878",
                    "text.2cd0f3be8738", "text.3fd47edce45b", "review.diagnostic.export" })
                {
                    string label = UiText.Get(key);
                    var button = new Button { Style = style, Content = label, ContentTemplate = null };
                    DrainDispatcher();
                    if (button.ToolTip != null || AutomationProperties.GetName(button) != label)
                        throw new Exception($"{culture}/{key}: text buttons must keep their accessible name without an automatic tooltip.");
                }

                var helpButton = new Button { Style = style, Content = UiText.Source("plan.open") };
                var titleOnlyButton = new Button { Style = style, Content = UiText.Source("v122.copyScriptContent") };
                var explicitTip = new ToolTip { Content = UiText.Get("plan.open.help") };
                var textWithHelp = new Button { Style = style, Content = UiText.Get("plan.open"), ContentTemplate = null, ToolTip = explicitTip };
                DrainDispatcher();
                if (helpButton.ToolTip is not HelpTopic { Sections.Count: > 0 } help || UiText.Translate(help.Title) != UiText.Get("plan.open"))
                    throw new Exception($"{culture}: the icon button must retain its translated name and instructions.");
                if (titleOnlyButton.ToolTip is not HelpTopic { Sections.Count: 0 } titleOnly || UiText.Translate(titleOnly.Title) != UiText.Get("v122.copyScriptContent"))
                    throw new Exception($"{culture}: icon help that only names an action must display the name once.");
                if (!ReferenceEquals(textWithHelp.ToolTip, explicitTip))
                    throw new Exception($"{culture}: an explicit explanatory tooltip must override the text-button default.");
            }
            Console.WriteLine("Button tooltip WPF bindings passed in both interface languages.");
        }
        finally { UiText.SetLanguage(previousLanguage); }
    }

    private static void CheckTimeZoneBindings(MainWindow window)
    {
        var pages = (TabControl)window.FindName("Pages");
        var viewModel = (MainViewModel)window.DataContext;
        var mode = (ComboBox)window.FindName("SettingAnchor021");
        var zone = (ComboBox)window.FindName("SettingAnchor022");
        var originalPlan = viewModel.Plan;
        var originalPage = pages.SelectedItem;
        string language = UiText.Language;
        string automatic = UiText.Source("text.499ff33c1078");
        string specified = UiText.Source("text.980f34c4425d");
        try
        {
            pages.SelectedItem = pages.Items.OfType<TabItem>()
                .Single(page => Equals(page.Header, UiText.Source("text.692873ddd8f4")));
            foreach (string culture in new[] { "en-US", "zh-CN" })
            {
                UiText.SetLanguage(culture);
                viewModel.Plan = InstallPlan.New();
                DrainDispatcher();
                if (!Equals(mode.SelectedValue, automatic) || zone.IsEnabled || zone.SelectedIndex != -1)
                    throw new Exception($"{culture}: new plans must show automatic time zone with no active selection.");
                mode.SetCurrentValue(Selector.SelectedValueProperty, specified);
                DrainDispatcher();
                if (!zone.IsEnabled || viewModel.Plan.TimeZoneMode != specified)
                    throw new Exception($"{culture}: choosing an explicit mode must enable the time zone picker.");
                zone.SetCurrentValue(Selector.SelectedValueProperty, "India Standard Time");
                UiText.SetLanguage(culture == "en-US" ? "zh-CN" : "en-US");
                DrainDispatcher();
                if (!Equals(mode.SelectedValue, specified) || !Equals(zone.SelectedValue, "India Standard Time") ||
                    viewModel.Plan.TimeZone != "India Standard Time")
                    throw new Exception($"{culture}: changing language must preserve an explicit time zone.");
            }
            Console.WriteLine("Time zone WPF bindings passed in both interface languages.");
        }
        finally
        {
            UiText.SetLanguage(language);
            viewModel.Plan = originalPlan;
            pages.SelectedItem = originalPage;
            DrainDispatcher();
        }
    }

    private static void CheckAdvancedXmlFormThemes(MainWindow window)
    {
        var pages = (TabControl)window.FindName("Pages");
        object selectedPage = pages.SelectedItem;
        string language = UiText.Language;
        string themeBeforeTest = UiTheme.Current;
        var section = (StackPanel)window.FindName("AdditionalXmlSection");
        var card = (Border)section.Parent;
        var offlinePaths = (TextBox)window.FindName("OfflineDriverPaths");
        var offlineLabel = (Label)window.FindName("OfflineDriverPathsLabel");
        var offlineAdd = (Button)window.FindName("AddOfflineDriverButton");
        var xmlEntries = (ListBox)window.FindName("AdditionalXmlEntries");
        var forms = new[]
        {
            (Form: (Expander)window.FindName("OemSupportForm"), HeaderKey: "xml.oem.form", HelpKey: "xml.oem.help"),
            (Form: (Expander)window.FindName("OfflineDriverForm"), HeaderKey: "xml.offlineDrivers.form", HelpKey: "xml.offlineDrivers.help")
        };
        try
        {
            pages.SelectedItem = pages.Items.OfType<TabItem>()
                .Single(page => Equals(page.Header, UiText.Source("text.2f8dd7280e0a")));
            foreach (string culture in new[] { "zh-CN", "en-US" })
            {
                UiText.SetLanguage(culture);
                foreach (string theme in new[] { "Dark", "Light", "Dark" })
                {
                    UiTheme.Apply(theme, save: false);
                    Color expectedText = theme == "Dark" ? Colors.White : Colors.Black;
                    foreach (var (form, headerKey, helpKey) in forms)
                    foreach (bool expanded in new[] { false, true })
                    {
                        string context = $"{culture}/{theme}/{form.Name}/{(expanded ? "expanded" : "collapsed")}";
                        form.IsExpanded = expanded;
                        DrainDispatcher();
                        window.UpdateLayout();
                        var toggle = VisualDescendants<ToggleButton>(form).FirstOrDefault();
                        var body = (StackPanel)form.Content;
                        if (toggle is null || !toggle.Focusable || !toggle.IsEnabled || toggle.IsChecked != expanded || body.IsVisible != expanded)
                            throw new Exception($"{context}: the header toggle and form content must follow the expansion state.");

                        CheckThemeText(toggle, UiText.Get(headerKey), card.Background, expectedText, context);
                        if (!expanded) continue;

                        CheckThemeText(body, UiText.Get(helpKey), card.Background, expectedText, context);
                        foreach (var label in VisualDescendants<Label>(body))
                            CheckThemeText(label, (string)label.Content, card.Background, expectedText, context);
                        foreach (var input in body.Children.OfType<TextBox>())
                            CheckTextContrast(input.Foreground, input.Background, $"{context}/{input.Name}");
                        if (form.Name == "OfflineDriverForm")
                        {
                            Point inputLeft = offlinePaths.TranslatePoint(new Point(0, 0), section);
                            Point listLeft = xmlEntries.TranslatePoint(new Point(0, 0), section);
                            if (Math.Abs(inputLeft.X - listLeft.X) > 1 || Math.Abs(offlinePaths.ActualWidth - xmlEntries.ActualWidth) > 1)
                                throw new Exception($"{context}: the driver paths editor and XML list must align.");
                            Point labelCenter = offlineLabel.TranslatePoint(new Point(0, offlineLabel.ActualHeight / 2), section);
                            Point actionCenter = offlineAdd.TranslatePoint(new Point(offlineAdd.ActualWidth, offlineAdd.ActualHeight / 2), section);
                            if (Math.Abs(labelCenter.Y - actionCenter.Y) > 1 || Math.Abs(actionCenter.X - inputLeft.X - offlinePaths.ActualWidth) > 1)
                                throw new Exception($"{context}: the add button must share the label row and align to the editor's right edge.");
                        }
                    }
                }
            }
            Console.WriteLine("PASS advanced XML form text contrast, expansion and driver layout in both languages and themes.");
        }
        finally
        {
            foreach (var (form, _, _) in forms) form.IsExpanded = false;
            pages.SelectedItem = selectedPage;
            UiText.SetLanguage(language);
            UiTheme.Apply(themeBeforeTest, save: false);
        }
    }

    private static void CheckThemeText(DependencyObject root, string expected, Brush background, Color expectedColor, string context)
    {
        var text = VisualDescendants<TextBlock>(root).FirstOrDefault(item => item.IsVisible && item.Text == expected);
        if (text?.Foreground is not SolidColorBrush foreground || foreground.Color != expectedColor)
            throw new Exception($"{context}: '{expected}' must render with the current theme text color {expectedColor}.");
        CheckTextContrast(foreground, background, $"{context}/{expected}");
    }

    private static void CheckTextContrast(Brush foreground, Brush background, string context)
    {
        if (foreground is not SolidColorBrush text || background is not SolidColorBrush surface)
            throw new Exception($"{context}: text contrast requires solid foreground and background colors.");

        static double Luminance(Color color) => 0.2126 * color.ScR + 0.7152 * color.ScG + 0.0722 * color.ScB;
        double first = Luminance(text.Color);
        double second = Luminance(surface.Color);
        double contrast = (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
        if (contrast < 4.5)
            throw new Exception($"{context}: text contrast is {contrast:F2}:1, expected at least 4.5:1.");
    }

    private static IEnumerable<T> VisualDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var descendant in VisualDescendants<T>(child)) yield return descendant;
        }
    }

    private static void CheckSetupLogTheme(MainWindow owner)
    {
        string language = UiText.Language;
        string themeBeforeTest = UiTheme.Current;
        var log = new SetupLogWindow { Owner = owner };
        log.Width = log.MinWidth;
        try
        {
            log.Show();
            var filterLabel = (Label)log.FindName("LogFilterLabel");
            var logFilter = (TextBox)log.FindName("LogFilter");
            var openButton = (Button)log.FindName("OpenButton");
            var reloadButton = (Button)log.FindName("ReloadButton");
            var logName = (TextBlock)log.FindName("LogName");
            var logText = (TextBox)log.FindName("LogText");
            var status = (TextBlock)log.FindName("LogStatus");
            var intro = (TextBlock)owner.FindName("ExportIntro");
            var warning = (TextBlock)owner.FindName("ExportImportWarning");
            foreach (string culture in new[] { "zh-CN", "en-US" })
            {
                UiText.SetLanguage(culture);
                foreach (string theme in new[] { "Dark", "Light", "Dark" })
                {
                    UiTheme.Apply(theme, save: false);
                    DrainDispatcher();
                    log.UpdateLayout();
                    string context = $"{culture}/{theme}/SetupLogWindow";
                    CheckDialogTheme(log, context);
                    CheckThemeText(filterLabel, UiText.Get("setupLogs.filter"), log.Background,
                        ((SolidColorBrush)log.Foreground).Color, context);
                    CheckTextContrast(status.Foreground, log.Background, $"{context}/footer");
                    CheckTextContrast(logText.Foreground, logText.Background, $"{context}/contents");
                    if (!logText.IsReadOnly || logFilter.IsEnabled || reloadButton.IsEnabled || !ReferenceEquals(filterLabel.Target, logFilter))
                        throw new Exception($"{context}: preview and keyword filtering must retain their initial state.");
                    var label = filterLabel.TranslatePoint(new Point(filterLabel.ActualWidth, filterLabel.ActualHeight / 2), log);
                    var filter = logFilter.TranslatePoint(new Point(0, logFilter.ActualHeight / 2), log);
                    var reload = reloadButton.TranslatePoint(new Point(0, reloadButton.ActualHeight / 2), log);
                    var open = openButton.TranslatePoint(new Point(0, openButton.ActualHeight / 2), log);
                    if (logFilter.ActualWidth <= 0 || openButton.ActualWidth <= 0 || reloadButton.ActualWidth <= 0
                        || label.X > filter.X + 1 || filter.X + logFilter.ActualWidth > reload.X
                        || reload.X + reloadButton.ActualWidth > open.X || Math.Abs(label.Y - filter.Y) > 1
                        || Math.Abs(filter.Y - reload.Y) > 1 || Math.Abs(filter.Y - open.Y) > 1
                        || logName.Visibility != Visibility.Collapsed)
                        throw new Exception($"{context}: the filter and log actions must share the top row, with Open on the right.");
                    foreach (var paragraph in new[] { intro, warning, status })
                        if (string.IsNullOrEmpty(paragraph.Text) || char.IsWhiteSpace(paragraph.Text[0])
                            || paragraph.TextAlignment != TextAlignment.Left)
                            throw new Exception($"{context}/{paragraph.Name}: the explanation must be left aligned without a first-line indent.");
                }
            }
            if (VisualDescendants<Button>(log).Any(button => button.IsCancel)
                || !log.InputBindings.OfType<KeyBinding>().Any(binding => binding.Key == Key.Escape && binding.Command == ApplicationCommands.Close))
                throw new Exception("The setup log dialog must retain Escape without a second close button.");
            ApplicationCommands.Close.Execute(null, log);
            if (log.IsVisible)
                throw new Exception("The setup log dialog's Escape command must close the window.");
            Console.WriteLine("PASS Windows Setup log dialog layout, themes, Escape, and unindented export instructions.");
        }
        finally
        {
            if (log.IsVisible) log.Close();
            UiText.SetLanguage(language);
            UiTheme.Apply(themeBeforeTest, save: false);
        }
    }

    private static void CheckDialogTheme(Window dialog, string context)
    {
        Color expectedBackground = ((SolidColorBrush)dialog.FindResource("CanvasBrush")).Color;
        Color expectedText = ((SolidColorBrush)dialog.FindResource("ForegroundBrush")).Color;
        if (dialog.Background is not SolidColorBrush background || background.Color != expectedBackground
            || dialog.Foreground is not SolidColorBrush foreground || foreground.Color != expectedText)
            throw new Exception($"{context}: the entire dialog must use the active theme colors.");
        CheckTextContrast(foreground, background, context);
    }

    private static void CheckDialogTextButton(Button button, string caption, string context)
    {
        if (!button.IsVisible || !Equals(button.Content, caption) || button.ActualWidth < 94)
            throw new Exception($"{context}: the full '{caption}' button caption must fit in the dialog.");
        CheckTextContrast(button.Foreground, button.Background, $"{context}/{caption}");
    }

    private static void CheckDeploymentReviewWindows(MainWindow owner)
    {
        var vm = (MainViewModel)owner.DataContext;
        var plan = vm.Plan;
        string before = PlanStore.Serialize(plan, includeSecrets: true);
        string language = UiText.Language;
        if (!plan.UseAppLocker && !((Button)owner.FindName("AppLockerEventsButton")).IsEnabled)
            throw new Exception("Event review must be available without enabling AppLocker in the plan.");
        try
        {
            foreach (string culture in new[] { "zh-CN", "en-US" })
            foreach (string theme in new[] { "Light", "Dark" })
            foreach (var kind in Enum.GetValues<DeploymentReviewKind>())
            {
                UiText.SetLanguage(culture);
                UiTheme.Apply(theme, save: false);
                DrainDispatcher();
                // Preview text is localized. Compare it within the same language and theme.
                string preview = vm.Preview;
                var review = new DeploymentReviewWindow(kind, plan) { Owner = owner };
                try
                {
                    review.Show();
                    DrainDispatcher();
                    string context = $"{culture}/{theme}/{kind}";
                    CheckDialogTheme(review, context);
                    var text = (TextBox)review.FindName("ReportText");
                    var open = (Button)review.FindName("OpenButton");
                    var close = (Button)review.FindName("CloseButton");
                    string prefix = kind == DeploymentReviewKind.ConfigurationSet ? "oemReview" : "eventReview";
                    CheckDialogTextButton(open, UiText.Get(prefix + ".choose"), context);
                    CheckDialogTextButton(close, UiText.Get("text.3fd47edce45b"), context);
                    if (review.Title != UiText.Get(prefix + ".title") || !Equals(open.Content, UiText.Get(prefix + ".choose")) ||
                        !text.IsReadOnly || text.FontSize != 16 || text.Background is not SolidColorBrush brush ||
                        brush.Color != ((SolidColorBrush)Application.Current.FindResource("InputBrush")).Color)
                        throw new Exception($"{culture}/{theme}/{kind}: review window localization, readability, or theme mismatch.");
                    string scope = ((TextBlock)review.FindName("ScopeText")).Text;
                    string expectedScope = UiText.Get(prefix + ".hint");
                    if (kind == DeploymentReviewKind.ConfigurationSet)
                        expectedScope += "\n" + UiText.Translate(ConfigurationSetReview.PlanScope(plan));
                    if (scope != expectedScope)
                        throw new Exception($"{culture}/{theme}/{kind}: unexpected plan scope.");
                }
                finally { review.Close(); }
                DrainDispatcher();
                if (!ReferenceEquals(vm.Plan, plan) || PlanStore.Serialize(plan, includeSecrets: true) != before)
                    throw new Exception($"{culture}/{theme}/{kind}: Opening and closing a review window must preserve the plan.");
                if (vm.Preview != preview)
                    throw new Exception($"{culture}/{theme}/{kind}: Opening and closing a review window must preserve the preview.");
            }
            Console.WriteLine("PASS deployment review window localization, themes, and unchanged configuration.");
        }
        finally { UiText.SetLanguage(language); }
    }

    private static void CheckSidebarLayout(MainWindow window)
    {
        var pages = (TabControl)window.FindName("Pages");
        var viewModel = (MainViewModel)window.DataContext;
        object selectedPage = pages.SelectedItem;
        window.Show();
        DrainDispatcher();
        window.UpdateLayout();
        string savedPlan = PlanStore.Serialize(viewModel.Plan, includeSecrets: true);
        var pane = (Border)pages.Template.FindName("NavigationPane", pages);
        var toggle = (Button)pages.Template.FindName("SidebarToggleButton", pages);
        var edge = (Button)pages.Template.FindName("SidebarRevealEdge", pages);
        var content = (ContentPresenter)pages.Template.FindName("PART_SelectedContentHost", pages);
        foreach (var theme in new[] { "Light", "Dark" })
        {
            UiTheme.Apply(theme, save: false);
            DrainDispatcher();
            window.UpdateLayout();
            double expandedWidth = content.ActualWidth;
            if (!viewModel.IsSidebarPinned || !viewModel.IsSidebarVisible || !pane.IsVisible ||
                pane.ActualWidth <= 0 || !toggle.IsDescendantOf(pane))
                throw new Exception("The navigation sidebar must initially be visible.");

            toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            DrainDispatcher();
            window.UpdateLayout();
            if (viewModel.IsSidebarPinned || viewModel.IsSidebarVisible || pane.Visibility != Visibility.Collapsed ||
                content.ActualWidth <= expandedWidth + 50 || toggle.IsVisible || !edge.IsVisible ||
                !ReferenceEquals(selectedPage, pages.SelectedItem))
                throw new Exception("Unpinning must widen the selected page and expose its left-edge reveal control.");

            double hiddenWidth = content.ActualWidth;
            edge.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            DrainDispatcher();
            window.UpdateLayout();
            if (viewModel.IsSidebarPinned || !viewModel.IsSidebarVisible || !pane.IsVisible ||
                !toggle.IsVisible || Math.Abs(content.ActualWidth - hiddenWidth) > 1 ||
                !ReferenceEquals(selectedPage, pages.SelectedItem))
                throw new Exception("A temporary sidebar must overlay the content without changing its width.");

            viewModel.HideSidebarPreview();
            DrainDispatcher();
            window.UpdateLayout();
            if (pane.IsVisible || !edge.IsVisible || Math.Abs(content.ActualWidth - hiddenWidth) > 1)
                throw new Exception("Dismissing a temporary sidebar must preserve the expanded content width.");
            edge.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            DrainDispatcher();
            window.UpdateLayout();
            if (!viewModel.IsSidebarPinned || !viewModel.IsSidebarVisible || !pane.IsVisible ||
                Math.Abs(content.ActualWidth - expandedWidth) > 1 ||
                !ReferenceEquals(selectedPage, pages.SelectedItem))
                throw new Exception("Restoring the sidebar must preserve the selected page and restore its width.");
        }
        if (PlanStore.Serialize(viewModel.Plan, includeSecrets: true) != savedPlan)
            throw new Exception("Changing sidebar visibility must preserve the installation plan.");
        Console.WriteLine("PASS fixed, hidden, and overlay sidebars preserve the plan and selected page in both themes.");
    }
}
