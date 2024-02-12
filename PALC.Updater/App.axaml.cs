using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using PALC.Updater.ViewModels;
using PALC.Updater.Views;
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace PALC.Updater;

public partial class App : Application
{
    public override void Initialize()
    {
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        AvaloniaXamlLoader.Load(this);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ExceptionDispatchInfo.Capture(e.Exception?.InnerException ?? e.Exception ?? new Exception("wut")).Throw();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainV();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainV();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
