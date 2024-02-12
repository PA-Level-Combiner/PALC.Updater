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
    private static readonly GitHubClient client = new(new ProductHeaderValue(Globals.userAgent));

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


    public event AsyncEventHandler<Exception>? GetReleasesFromRepoFailed;
    public event AsyncEventHandler<Exception>? StringToSemverFailed;

    public event AsyncEventHandler? EndLoad;

    public async Task LoadReleases()
    {
        GithubReleases.Clear();

        try
        {
            IReadOnlyList<Release> releases;
            try
            {
                releases = await client.Repository.Release.GetAll(GithubInfo.owner, GithubInfo.mainName);
            }
            catch (Exception ex)
            {
                if (GetReleasesFromRepoFailed != null)
                    await GetReleasesFromRepoFailed(this, ex);

                return;
            }

            foreach (var release in releases)
            {
                SemVersion? version;
                try
                {
                    version = SemVersion.Parse(release.TagName, SemVersionStyles.Any);
                }
                catch (Exception ex)
                {
                    if (StringToSemverFailed != null)
                        await StringToSemverFailed(this, ex);

                    version = null;
                }


                GithubReleaseVM githubRelease = new()
                {
                    githubRelease = release,
                    Name = release.Name,
                    ReleaseNotes = release.Body,
                    Url = release.Url,
                    CreatedAt = release.CreatedAt.UtcDateTime,
                    ReleaseVersion = version
                };
                githubRelease.DownloadFailed += OnDownloadFailed;
                githubRelease.DownloadFinished += OnDownloadFinished;

                GithubReleases.Add(githubRelease);
            }
        }
        finally
        {
            if (EndLoad != null) await EndLoad(this, new EventArgs());
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
