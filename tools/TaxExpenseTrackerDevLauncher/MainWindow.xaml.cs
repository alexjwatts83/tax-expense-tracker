using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using TaxExpenseTrackerDevLauncher.ViewModels;

namespace TaxExpenseTrackerDevLauncher;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _viewModel.LogLines.CollectionChanged += (_, _) =>
        {
            if (_viewModel.AutoScroll && LauncherLogList.Items.Count > 0)
                LauncherLogList.ScrollIntoView(LauncherLogList.Items[^1]);
        };
    }

    private async void Window_Closed(object? sender, EventArgs args)
    {
        try
        {
            await _viewModel.ShutdownAsync();
            Application.Current.Shutdown(0);
        }
        catch
        {
            Application.Current.Shutdown(1);
        }
    }

    private void LocalLink_RequestNavigate(object sender, RequestNavigateEventArgs args)
    {
        Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri) { UseShellExecute = true });
        args.Handled = true;
    }

    private void ReloadWebView_Click(object sender, RoutedEventArgs args)
    {
        if (AppWebView.Source is not null)
            AppWebView.Reload();
    }
}