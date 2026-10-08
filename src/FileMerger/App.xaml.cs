using System.Windows;
using FileMerger.Core;

namespace FileMerger;

public partial class App : Application
{
    private MainViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeService.Start(this);

        var store = SettingsStore.NextToExecutable();
        _viewModel = new MainViewModel(store, store.Load());
        var window = new MainWindow(_viewModel);
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ThemeService.Stop();
        _viewModel?.SaveSettingsNow();
        base.OnExit(e);
    }
}
