using Avalonia.Controls;
using PALC.Updater.ViewModels;
using System.Threading.Tasks;

using PALC.Common.Views.Templates;
using static PALC.Updater.ViewModels.MainVM;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using System;
using MsBox.Avalonia;

namespace PALC.Updater.Views;

public partial class MainV : Window
{
    public MainVM vm;
    public MainV()
    {
        InitializeComponent();

        vm = new();
        DataContext = vm;

        vm.LoadReleaseFailed += OnLoadReleaseFailed;

        vm.LaunchFailed += OnLaunchFailed;
        vm.Launched += OnProgramLaunched;
    }



    private Task OnProgramLaunched(object? sender, object e)
    {
        Close();
        return Task.CompletedTask;
    }

    private async Task OnLaunchFailed(object? sender, Exception e)
    {
        await MessageBoxTools.CreateErrorMsgBox(
            $"An error occured while trying to launch PALC.",
            e
        ).ShowWindowDialogAsync(this);
    }



    private async Task OnLoadReleaseFailed(object? sender, LoadReleaseFailedArgs e)
    {
        await MessageBoxTools.CreateErrorMsgBox(
            $"An error occured while trying to open the release in folder {e.path}.",
            e.ex
        ).ShowWindowDialogAsync(this);
    }

    public async void OnLoaded(object? sender, RoutedEventArgs e)
        => await RefreshVersionList();


    public async void OnDelete(object? sender, RoutedEventArgs e)
    {
        Button button = (Button)(sender ?? throw new NullReferenceException("Missing button sender."));
        ExistingVersionVM existingVersionsVM = (ExistingVersionVM)(button.DataContext ?? throw new NullReferenceException("Missing button sender."));

        var result = await MessageBoxManager.GetMessageBoxStandard(
            "Delete?",
            $"Are you sure you want to move this version to the recycle bin?\n{existingVersionsVM.ReleaseVersion}",
            MsBox.Avalonia.Enums.ButtonEnum.YesNo,
            MsBox.Avalonia.Enums.Icon.Info
        ).ShowWindowDialogAsync(this);

        if (result == MsBox.Avalonia.Enums.ButtonResult.Yes) vm.Delete(existingVersionsVM);
    }


    public async void OnRefresh(object? sender, RoutedEventArgs e)
        => await RefreshVersionList();


    public async Task RefreshVersionList()
    {
        // TODO make it more formal
        Window display = new() { Content = "Refreshing version list...", SizeToContent = SizeToContent.WidthAndHeight };

        display.Show();
        IsEnabled = false;
        await Task.Delay(1000);

        await vm.LoadExistingReleases();

        IsEnabled = true;
        display.Close();
    }


    public async void OnDownloadVersions(object? sender, RoutedEventArgs e)
    {
        DownloaderV downloaderV = new(new() { mainVM = vm });
        await downloaderV.ShowDialog(this);
        await RefreshVersionList();
    }
}
