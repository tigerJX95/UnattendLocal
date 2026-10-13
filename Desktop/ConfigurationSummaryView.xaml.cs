using System.Windows;
using System.Windows.Controls;

namespace UnattendLocal.Desktop;

public partial class ConfigurationSummaryView : UserControl
{
    public static readonly DependencyProperty ShowDetailsProperty = DependencyProperty.Register(nameof(ShowDetails), typeof(bool), typeof(ConfigurationSummaryView), new PropertyMetadata(true));
    public bool ShowDetails { get => (bool)GetValue(ShowDetailsProperty); set => SetValue(ShowDetailsProperty, value); }
    public static readonly DependencyProperty SummaryProperty = DependencyProperty.Register(nameof(Summary), typeof(string), typeof(ConfigurationSummaryView), new PropertyMetadata("", SummaryChanged));
    public string Summary { get => (string)GetValue(SummaryProperty); set => SetValue(SummaryProperty, value); }
    public static readonly DependencyProperty IsDestructiveDiskProperty = DependencyProperty.Register(nameof(IsDestructiveDisk), typeof(bool), typeof(ConfigurationSummaryView), new PropertyMetadata(false));
    public bool IsDestructiveDisk { get => (bool)GetValue(IsDestructiveDiskProperty); set => SetValue(IsDestructiveDiskProperty, value); }
    private static readonly DependencyPropertyKey PresentationPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Presentation), typeof(SummaryPresentation), typeof(ConfigurationSummaryView), new PropertyMetadata(SummaryPresentation.FromText("")));
    public static readonly DependencyProperty PresentationProperty = PresentationPropertyKey.DependencyProperty;
    public SummaryPresentation Presentation => (SummaryPresentation)GetValue(PresentationProperty);
    private static void SummaryChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) => ((ConfigurationSummaryView)sender).SetValue(PresentationPropertyKey, SummaryPresentation.FromText(e.NewValue as string));
    public ConfigurationSummaryView() { InitializeComponent(); }
}
