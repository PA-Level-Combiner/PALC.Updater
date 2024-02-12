using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Octokit;
using Semver;
using System.Collections.Generic;

namespace PALC.Updater.ViewModels;


public partial class DownloaderVM : ViewModelBase
{
    private static readonly GitHubClient client = new(new ProductHeaderValue("PALC-Updater"));

    public ObservableCollection<ExistingVersionVM> ExistingReleases { get; } = [];


    public ObservableCollection<GithubReleaseVM> GithubReleases { get; } = [];


    public event AsyncEventHandler<Exception>? GetReleasesFromRepoFailed;
    public event AsyncEventHandler<Exception>? StringToSemverFailed;

    public event AsyncEventHandler? StartLoad;
    public event AsyncEventHandler? EndLoad;

    public async Task LoadReleases()
    {
        if (StartLoad != null) await StartLoad(this, new EventArgs());
        
        try
        {
            IReadOnlyList<Release> releases;
            try
            {
                releases = await client.Repository.Release.GetAll("26F-Studio", "Techmino");
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

                GithubReleases.Add(new GithubReleaseVM {
                    githubRelease = release,
                    Name = release.Name,
                    ReleaseNotes = release.Body,
                    Url = release.Url,
                    CreatedAt = release.CreatedAt.UtcDateTime,
                    ReleaseVersion = version
                });
            }
        }
        finally
        {
            if (EndLoad != null) await EndLoad(this, new EventArgs());
        }
    }
}


public class DownloaderVMDesign : DownloaderVM
{
    public DownloaderVMDesign() : base()
    {
        GithubReleases.Add(new GithubReleaseVM {
            Name = "Sample",
            ReleaseNotes = "**This is *Markdown*!**\n> Wut Da Hell",
            Url = "https://github.com/sample/sample/releases/69.69.69-quack1",
            CreatedAt = DateTime.Now,
            ReleaseVersion = SemVersion.Parse("69.69.69-quack1", SemVersionStyles.Any)
        });
    }

}
