using CommunityToolkit.Mvvm.ComponentModel;
using Octokit;
using Semver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PALC.Updater.ViewModels;



public partial class MainVM : ViewModelBase
{
    public ObservableCollection<ExistingVersionVM> ExistingVersions { get; } = [];


    public class LoadReleaseFailedArgs
    {
        public required Exception ex;
        public required string path;
    }
    public event AsyncEventHandler<LoadReleaseFailedArgs>? LoadExistingFailed;

    public async Task LoadExistingReleases()
    {
        ExistingVersions.Clear();

        if (!Directory.Exists(Globals.versionsFolder))
        {
            Directory.CreateDirectory(Globals.versionsFolder);
            return;
        }

        var directories = Directory.GetDirectories(Globals.versionsFolder);
        foreach (var directory in directories) 
        {
            if (!File.Exists(Path.Join(directory, Globals.exeName)))
            {
                if (LoadExistingFailed != null)
                    await LoadExistingFailed(this, new LoadReleaseFailedArgs { ex = new Exception($"{Globals.exeName} is missing."), path = directory });

                continue;
            }

            var folderName = Path.GetFileName(directory);
            SemVersion? version;
            try
            {
                version = SemVersion.Parse(folderName, SemVersionStyles.Any);
            }
            catch (FormatException ex)
            {
                if (LoadExistingFailed != null)
                    await LoadExistingFailed(this, new LoadReleaseFailedArgs {
                        ex = new Exception(
                            $"Can't deduce version from folder name. " +
                            $"Please use a valid semantic version as the folder name if you're manually installing.\n" +
                            $"{ex.Message}",
                            ex
                        ),
                        path = directory
                    });

                version = null;
            }


            ExistingVersionVM existingVersionVM = new() { FolderPath = directory, ReleaseVersion = version };
            existingVersionVM.LaunchFailed += OnLaunchFailed;
            existingVersionVM.Launched += OnLaunched;

            ExistingVersions.Add(existingVersionVM);
        }

        var sorted = ExistingVersions.ToList().OrderByDescending(x => x.ReleaseVersion);
        ExistingVersions.Clear();
        foreach (var item in sorted)
            ExistingVersions.Add(item);
    }

    public event AsyncEventHandler<Exception>? LaunchFailed;
    private async Task OnLaunchFailed(object? sender, Exception e)
    {
        if (LaunchFailed != null) await LaunchFailed(this, e);
    }

    public event AsyncEventHandler<object>? Launched;
    private async Task OnLaunched(object? sender, object e)
    {
        if (Launched != null) await Launched(this, e);
    }



    public event AsyncEventHandler<Exception>? LoadGithubFailed;
    public event AsyncEventHandler? LoadGithubFinished;

    [ObservableProperty]
    public bool isGithubReleasesLoaded = false;

    public ObservableCollection<GithubReleaseVM> GithubReleases { get; } = [];
    public async Task LoadGithubReleases()
    {
        if (isGithubReleasesLoaded) return;

        try
        {
            IReadOnlyList<Release> releases;
            try
            {
                releases = await Globals.client.Repository.Release.GetAll(GithubInfo.owner, GithubInfo.mainName);
            }
            catch (Exception ex)
            {
                if (LoadGithubFailed != null)
                    await LoadGithubFailed(this, ex);

                return;
            }

            foreach (var release in releases)
            {
                SemVersion? version;
                try
                {
                    version = SemVersion.Parse(release.TagName, SemVersionStyles.Any);
                }
                catch
                {
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

                GithubReleases.Add(githubRelease);
            }
        }
        finally
        {
            if (LoadGithubFinished != null) await LoadGithubFinished(this, new EventArgs());
        }

        IsGithubReleasesLoaded = true;
    }


    public GithubReleaseVM? GetHighestGithubRelease()
    {
        return GithubReleases.MaxBy(x => x.ReleaseVersion ?? new SemVersion(0, 0, 0));
    }

    public bool HasNewUpdates()
    {
        if (!IsGithubReleasesLoaded) throw new Exception("Github releases have not been loaded yet.");

        var existingHighest = ExistingVersions.Select(x => x.ReleaseVersion).MaxBy(x => x ?? new SemVersion(0, 0, 0));
        var githubHighest = GithubReleases.Select(x => x.ReleaseVersion).MaxBy(x => x ?? new SemVersion(0, 0, 0));

        return githubHighest?.CompareSortOrderTo(existingHighest) == 1;
    }


    public void Delete(ExistingVersionVM vm)
    {
        vm.Delete();
        ExistingVersions.Remove(vm);
    }
}


public partial class MainVMDesign : MainVM
{
    public MainVMDesign() : base()
    {
        ExistingVersions.Add(new ExistingVersionVM());
    }
}
