using System.Windows;
using TaxExpenseTrackerDevLauncher.Services;

namespace TaxExpenseTrackerDevLauncher;

public partial class App : Application
{
	private SingleInstanceCoordinator? _instance;

	protected override void OnStartup(StartupEventArgs args)
	{
		base.OnStartup(args);
		_instance = new SingleInstanceCoordinator(RepositoryLocator.FindRepositoryRoot());
		if (!_instance.IsPrimary)
		{
			_instance.RequestActivation();
			Shutdown(0);
			return;
		}

		_instance.ActivationRequested += () => Dispatcher.BeginInvoke(new Action(ActivateWindow));
		MainWindow = new MainWindow();
		MainWindow.Show();
	}

	protected override void OnExit(ExitEventArgs args)
	{
		_instance?.Dispose();
		base.OnExit(args);
	}

	private void ActivateWindow()
	{
		if (MainWindow is not { } window)
			return;

		if (window.WindowState == WindowState.Minimized)
			window.WindowState = WindowState.Normal;

		window.Show();
		window.Activate();
	}
}

