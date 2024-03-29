﻿using Avalonia.Controls;
using PALC.Updater.ViewModels;
using System.Threading.Tasks;

using PALC.Common.Views.Templates;
using static PALC.Updater.ViewModels.MainVM;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using System;
using MsBox.Avalonia;
using Semver;
using System.Diagnostics;

namespace PALC.Updater.Views;

public partial class MainV : Window
{
    public MainVM vm;
    public MainV()
    {
        InitializeComponent();

        vm = new();
        DataContext = vm;

        vm.LoadExistingFailed += OnDisplayGeneralError;

        vm.LaunchFailed += OnDisplayGeneralError;
        vm.Launched += OnProgramLaunched;

        vm.LoadGithubFailed += OnDisplayGeneralError;
        vm.LoadGithubUpdaterFailed += OnDisplayGeneralError;

        vm.DeleteFailed += OnDisplayGeneralError;
    }


    private async Task OnDisplayGeneralError(object? sender, DisplayGeneralErrorArgs e)
        => await MessageBoxTools.CreateErrorMsgBox(e).ShowWindowDialogAsync(this);


    public async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        await RefreshVersionList();

        await vm.LoadGithubReleases();
        if (!vm.isGithubReleasesLoaded) return;

        SemVersion? version = await vm.GetNewerUpdaterVersion();
        if (version != null)
        {
            var result = await MessageBoxManager.GetMessageBoxStandard(
                "New update!",
                $"A new UPDATER update has been released: {version}\n" +
                $"\n" +
                $"Would you like to visit the download page now?",
                MsBox.Avalonia.Enums.ButtonEnum.YesNo,
                MsBox.Avalonia.Enums.Icon.Info
            ).ShowWindowDialogAsync(this);

            if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
                Process.Start(new ProcessStartInfo(GithubInfo.updater.Releases) { UseShellExecute = true });
        }


        if (vm.HasNewUpdates())
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "New update!",
                $"A new update has been released:\n" +
                $"{vm.GetHighestGithubRelease()?.ReleaseVersion?.ToString() ?? "wut unknown version"}\n" +
                $"Use the download button to download this update."
            ).ShowWindowDialogAsync(this);
        }
    }

    



    private Task OnProgramLaunched(object? sender, object e)
    {
        Close();
        return Task.CompletedTask;
    }


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

        if (result == MsBox.Avalonia.Enums.ButtonResult.Yes) vm.DeleteExistingVersion(existingVersionsVM);
    }




    public async void OnRefresh(object? sender, RoutedEventArgs e)
        => await RefreshVersionList();


    public async Task RefreshVersionList()
    {
        // TODO make it more formal
        var display = MessageBoxTools.CreateProgressModalDialog("Refreshing version list...");

        display.ShowFromWindow(this);
        await Task.Delay(1000);

        await vm.LoadExistingReleases();

        display.CloseFromWindow(this);
    }


    public async void OnDownloadVersions(object? sender, RoutedEventArgs e)
    {
        DownloaderVM downloaderVM = new() { mainVM = vm };
        downloaderVM.PopulateGithubReleases(vm.GithubReleases);

        DownloaderV downloaderV = new(downloaderVM);
        await downloaderV.ShowDialog(this);
    }
}
