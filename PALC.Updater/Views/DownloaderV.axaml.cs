using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MsBox.Avalonia;
using PALC.Common.Views.Templates;
using PALC.Updater.ViewModels;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PALC.Updater.Views;

public partial class DownloaderV : Window
{
    public DownloaderVM vm;

    public DownloaderV(DownloaderVM vm)
    {
        InitializeComponent();

        this.vm = vm;
        DataContext = vm;

        vm.GetReleasesFromRepoFailed += OnGetReleasesFromRepoFailed;

        vm.DownloadFinished += OnDownloadFinished;
        vm.DownloadFailed += OnDownloadFailed;
    }


    private async Task OnGetReleasesFromRepoFailed(object? sender, System.Exception e)
    {
        await MessageBoxTools.CreateErrorMsgBox(
            "An error occurred while trying to get releases. If this is a rate-limit issue, just wait 10 or so minutes.",
            e
        ).ShowWindowDialogAsync(this);
    }

    public DownloaderV() : this(new()) { }


    public async void OnLoaded(object? sender, RoutedEventArgs e)
        => await Refresh();


    public async Task Refresh()
    {
        Window display = new() { Content = "Refreshing online version list...", SizeToContent = SizeToContent.WidthAndHeight };

        display.Show();
        IsEnabled = false;

        await vm.LoadReleases();

        IsEnabled = true;
        display.Close();
    }

    public async void OnRefresh(object? sender, RoutedEventArgs e)
    {
        await Refresh();
        await MessageBoxManager.GetMessageBoxStandard(
            "Heya!",
            "Note that if you refresh too fast, Github may rate-limit you and prevent you from refreshing for 10 minutes.\n" +
            "This message helps you to not do that."
        ).ShowWindowDialogAsync(this);
    }

    public async void OnClose(object? sender, RoutedEventArgs e)
        => await Dispatcher.UIThread.InvokeAsync(Close);


    public async void OnOpenReleasesPage(object? sender, RoutedEventArgs e)
        => await Task.Run(() => Process.Start(new ProcessStartInfo(GithubInfo.mainReleases) { UseShellExecute = true }));



    private async Task OnDownloadFinished(object? sender, object? e)
    {
        await MessageBoxManager.GetMessageBoxStandard(
            "Finished!",
            "Finished downloading.",
            icon: MsBox.Avalonia.Enums.Icon.Success
        ).ShowWindowDialogAsync(this);

        if (vm.mainVM != null)
            await vm.mainVM.LoadExistingReleases();
    }

    private async Task OnDownloadFailed(object? sender, Exception e)
    {
        await MessageBoxTools.CreateErrorMsgBox(
            "An error occurred while trying to download necessary files.",
            e
        ).ShowWindowDialogAsync(this);
    }

    public async void OnDownload(object? sender, RoutedEventArgs e)
    {
        Button button = (Button)(sender ?? throw new NullReferenceException("Missing button sender."));
        GithubReleaseVM grvm = (GithubReleaseVM)(button.DataContext ?? throw new NullReferenceException("Missing button sender."));

        if (grvm.ReleaseVersion != null && vm.IsReleaseExisting(grvm.ReleaseVersion))
        {
            var result = await MessageBoxManager.GetMessageBoxStandard(
                "Repeat Download?",
                "This version has already been download. This would overwrite the old version if you've done modifications to it.\n" +
                "Do you want to download it again?",
                MsBox.Avalonia.Enums.ButtonEnum.YesNo,
                MsBox.Avalonia.Enums.Icon.Info
            ).ShowWindowDialogAsync(this);

            if (result == MsBox.Avalonia.Enums.ButtonResult.No) return;
        }
        else
        {
            var result = await MessageBoxManager.GetMessageBoxStandard(
                "Download?",
                "Do you want to download this version?",
                MsBox.Avalonia.Enums.ButtonEnum.YesNo,
                MsBox.Avalonia.Enums.Icon.Info
            ).ShowWindowDialogAsync(this);

            if (result == MsBox.Avalonia.Enums.ButtonResult.No) return;
        }



        Window display = new() { Content = "Downloading...", SizeToContent = SizeToContent.WidthAndHeight };
        IsEnabled = false;
        display.Show();

        await grvm.Download();

        display.Close();
        IsEnabled = true;
    }
}
