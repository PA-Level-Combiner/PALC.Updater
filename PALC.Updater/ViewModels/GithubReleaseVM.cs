using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

using Octokit;
using Semver;

namespace PALC.Updater.ViewModels;

public partial class GithubReleaseVM : ViewModelBase
{
    public Release? githubRelease = null;

    public required string Name { get; set; }
    public required string ReleaseNotes { get; set; }
    public required string Url { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required SemVersion? ReleaseVersion { get; set; }


    public event AsyncEventHandler<Exception>? DownloadFailed;

    public event AsyncEventHandler? StartLoad;
    public event AsyncEventHandler? EndLoad;

    [RelayCommand]
    public async Task Download()
    {
        if (githubRelease == null)
            if (DownloadFailed != null) await DownloadFailed(this, new Exception("No github release supplied. This is a fatal error and should be reported."));
    }
}
