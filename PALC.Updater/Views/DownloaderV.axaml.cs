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

        vm.DownloadFinished += OnDownloadFinished;
        vm.DownloadFailed += OnDownloadFailed;
    }
    public DownloaderV() : this(new()) { }



    public async void OnClose(object? sender, RoutedEventArgs e)
        => await Dispatcher.UIThread.InvokeAsync(Close);


    public async void OnOpenReleasesPage(object? sender, RoutedEventArgs e)
        => await Task.Run(() => Process.Start(new ProcessStartInfo(GithubInfo.main.Releases) { UseShellExecute = true }));



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

    private async Task OnDownloadFailed(object? sender, DisplayGeneralErrorArgs e)
    {
        await MessageBoxTools.CreateErrorMsgBox(e).ShowWindowDialogAsync(this);
    }

    public async void OnDownload(object? sender, RoutedEventArgs e)
    {
        Button button = (Button)(sender ?? throw new NullReferenceException("Missing button sender."));
        GithubReleaseVM grvm = (GithubReleaseVM)(button.DataContext ?? throw new NullReferenceException("Missing button sender."));

        if (grvm.ReleaseVersion != null && vm.IsReleaseExisting(grvm.ReleaseVersion))
        {
            var result = await MessageBoxManager.GetMessageBoxStandard(
                "Repeat Download?",
                "This version has already been downloaded. " +
                "This would overwrite the old version if you've done modifications to it.\n" +
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



        var display = MessageBoxTools.CreateProgressModalDialog("Downloading...");
        display.ShowFromWindow(this);

        await grvm.Download();

        display.CloseFromWindow(this);
    }
}
