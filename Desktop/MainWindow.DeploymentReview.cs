using System.Windows;

namespace UnattendLocal.Desktop;

public partial class MainWindow
{
    private void ConfigurationSetReviewClick(object sender, RoutedEventArgs e) =>
        Run(() => new DeploymentReviewWindow(DeploymentReviewKind.ConfigurationSet, vm.Plan) { Owner = this }.ShowDialog());

    private void AppLockerEventsClick(object sender, RoutedEventArgs e) =>
        Run(() => new DeploymentReviewWindow(DeploymentReviewKind.AppLockerEvents) { Owner = this }.ShowDialog());
}
