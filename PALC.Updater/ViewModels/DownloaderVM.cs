using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Octokit;
using Semver;
using System.Collections.Generic;
using System.Linq;

namespace PALC.Updater.ViewModels;


public partial class DownloaderVM : ViewModelBase
{
    public MainVM? mainVM;

    public bool IsReleaseExisting(SemVersion semVersion)
    {
        if (mainVM == null) return false;

        return mainVM.ExistingVersions.Any(x => {
            if (x.ReleaseVersion == null) return false;
            return x.ReleaseVersion.ToString() == semVersion.ToString();
        });
    }


    public ObservableCollection<GithubReleaseVM> GithubReleases { get; } = [];

    public void PopulateGithubReleases(IEnumerable<GithubReleaseVM> githubReleases)
    {
        List<GithubReleaseVM> copy = githubReleases.ToList();
        foreach (var githubRelease in copy)
        {
            githubRelease.DownloadFinished += OnDownloadFinished;
            githubRelease.DownloadFailed += OnDownloadFailed;
            GithubReleases.Add(githubRelease);
        }
    }

    public event AsyncEventHandler<object?>? DownloadFinished;
    private async Task OnDownloadFinished(object? sender, object? e)
    {
        if (DownloadFinished != null) await DownloadFinished(sender, e);
    }

    public event AsyncEventHandler<Exception>? DownloadFailed;
    private async Task OnDownloadFailed(object? sender, Exception e)
    {
        if (DownloadFailed != null) await DownloadFailed(sender, e);
    }
}


public class DownloaderVMDesign : DownloaderVM
{
    public DownloaderVMDesign() : base()
    {
        GithubReleases.Add(new GithubReleaseVM {
            Name = "Sample",
            ReleaseNotes = "**This is *Markdown*!**\n> Wut Da Hell\n" + string.Join("", Enumerable.Repeat("Lots\n\n", 10)),
            Url = "https://github.com/sample/sample/releases/69.69.69-quack1",
            CreatedAt = DateTime.Now,
            ReleaseVersion = SemVersion.Parse("69.69.69-quack1", SemVersionStyles.Any)
        });
    }

}
