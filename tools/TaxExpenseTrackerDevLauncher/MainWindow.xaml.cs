using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Web.WebView2.Core;
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

            if (_viewModel.FrontendOutputAutoScroll && FrontendOutputList.Items.Count > 0)
                FrontendOutputList.ScrollIntoView(FrontendOutputList.Items[^1]);
        };
        _viewModel.ApiLogFileLines.CollectionChanged += (_, _) =>
        {
            if (_viewModel.ApiLogFileAutoScroll && ApiLogFileList.Items.Count > 0)
                ApiLogFileList.ScrollIntoView(ApiLogFileList.Items[^1]);
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

    private async void AppWebView_Loaded(object sender, RoutedEventArgs args)
    {
        try
        {
            await AppWebView.EnsureCoreWebView2Async();
        }
        catch (Exception exception)
        {
            _viewModel.ReportBrowserStatus($"WebView2 initialization failed: {exception.Message}", isError: true);
        }
    }

    private void AppWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        _viewModel.ReportBrowserStatus(args.IsSuccess
            ? "Frontend loaded."
            : $"Frontend navigation failed: {args.WebErrorStatus}",
            isError: !args.IsSuccess);
    }

    private void ReloadWebView_Click(object sender, RoutedEventArgs args)
    {
        try
        {
            if (AppWebView.Source is not null)
            {
                _viewModel.ReportBrowserStatus("Reloading the frontend...");
                AppWebView.Reload();
            }
        }
        catch (Exception exception)
        {
            _viewModel.ReportBrowserStatus($"Frontend reload failed: {exception.Message}", isError: true);
        }
    }
}