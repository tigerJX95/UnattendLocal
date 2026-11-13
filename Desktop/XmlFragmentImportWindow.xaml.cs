using System.IO;
using System.Windows;
using System.Windows.Controls;
using UnattendLocal.Core;
using UnattendLocal.Localization;

namespace UnattendLocal.Desktop;

public partial class XmlFragmentImportWindow : Window
{
    private readonly string path;
    public XmlComponentEntry? Draft { get; private set; }

    public XmlFragmentImportWindow(string path, XmlComponentEntry? selected)
    {
        this.path = path;
        InitializeComponent();
        FileNameText.Text = Path.GetFileName(path);
        ComponentPicker.ItemsSource = AdditionalXmlSettings.Components;
        var initial = selected ?? new XmlComponentEntry();
        ComponentPicker.SelectedItem = initial.Component;
        if (PassPicker.ItemsSource is AdditionalXmlSettings.PhaseChoice[] passes &&
            passes.Any(pass => pass.Id == initial.Pass))
            PassPicker.SelectedValue = initial.Pass;
        SourceInitialized += (_, _) => UiTheme.UpdateTitleBar(this);
        Loaded += (_, _) => ComponentPicker.Focus();
    }

    private void ComponentChanged(object sender, SelectionChangedEventArgs e)
    {
        var previous = PassPicker.SelectedValue as string;
        var passes = AdditionalXmlSettings.PassesForComponent(ComponentPicker.SelectedItem as string);
        PassPicker.ItemsSource = passes;
        // Preserve a valid phase, or preselect it only when there is no alternative.
        PassPicker.SelectedValue = passes.Any(pass => pass.Id == previous) ? previous :
            passes.Length == 1 ? passes[0].Id : null;
    }

    private void PassChanged(object sender, SelectionChangedEventArgs e) =>
        ImportButton.IsEnabled = PassPicker.SelectedValue is string;

    private void ImportClick(object sender, RoutedEventArgs e)
    {
        if (ComponentPicker.SelectedItem is not string component || PassPicker.SelectedValue is not string pass) return;
        try
        {
            Draft = AdditionalXmlSettings.ReadDraft(path, new XmlComponentEntry { Component = component, Pass = pass });
            DialogResult = true;
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException)
        {
            ErrorDialog.Show(this, error, UiText.Source("xml.importDraft"), importing: true);
        }
    }
}
