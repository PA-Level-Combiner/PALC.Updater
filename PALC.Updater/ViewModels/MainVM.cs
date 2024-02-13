using CommunityToolkit.Mvvm.ComponentModel;
using Octokit;
using Semver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Microsoft.VisualBasic;
using NLog.Fluent;
using System.Text;

namespace PALC.Updater.ViewModels;



public partial class MainVM : ViewModelBase
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();


    public ObservableCollection<ExistingVersionVM> ExistingVersions { get; } = [];


    public event AsyncEventHandler<DisplayGeneralErrorArgs>? LoadExistingFailed;

    public async Task LoadExistingReleases()
    {
        throw new Exception("test");
        _logger.Info("Loading existing releases...");
        ExistingVersions.Clear();

        if (!Directory.Exists(Globals.versionsFolder))
        {
            _logger.Info("Versions folder doesn't exist. Creating...");
            Directory.CreateDirectory(Globals.versionsFolder);
            _logger.Info("Versions folder created.");

            return;
        }


        _logger.Trace("Getting directories inside folder...");

        string[] directories;
        try
        {
            directories = Directory.GetDirectories(Globals.versionsFolder);
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException ||
            ex is PathTooLongException
        )
        {
            _logger.Warn(ex, "Cannot access directory {directory}.", Globals.versionsFolder);
            await AEHHelper.RunAEH(
                LoadExistingFailed, this,
                new(
                    $"Can't access the version folder {Globals.versionsFolder}. " + AdditionalErrors.noAccessHelp,
                    ex
                )
            );
            return;
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.Fatal(ex, "Cannot find versions directory {directory}.", Globals.versionsFolder);
            throw;
        }
        
        
        foreach (var directory in directories) 
        {
            _logger.Info("Loading version from directory {directory}...", directory);

            if (!File.Exists(Path.Join(directory, Globals.exeName)))
            {
                _logger.Warn("{exeName} is missing from directory {directory}.", Globals.exeName, directory);
                await AEHHelper.RunAEH(LoadExistingFailed, this,
                    new($"The {Globals.exeName} file is missing from the folder \"{directory}\".", null)
                );

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
                _logger.Warn(ex, "{directory} has an invalid version number.", directory);

                await AEHHelper.RunAEH(LoadExistingFailed, this,
                    new(
                        $"Can't deduce version from folder name. " +
                        $"Please use a valid semantic version as the folder name if you're manually installing.\n" +
                        $"{ex.Message}",
                        ex
                    )
                );

                version = null;
            }

            ExistingVersionVM existingVersionVM = new() { FolderPath = directory, ReleaseVersion = version };
            existingVersionVM.LaunchFailed += OnLaunchFailed;
            existingVersionVM.Launched += OnLaunched;

            ExistingVersions.Add(existingVersionVM);
        }


        _logger.Trace("Sorting existing versions...");

        var sorted = ExistingVersions.ToList().OrderByDescending(x => x.ReleaseVersion);
        ExistingVersions.Clear();
        foreach (var item in sorted)
            ExistingVersions.Add(item);

        _logger.Info("Finished loading existing releases.");
    }

    public event AsyncEventHandler<DisplayGeneralErrorArgs>? LaunchFailed;
    private async Task OnLaunchFailed(object? sender, DisplayGeneralErrorArgs e)
    {
        await AEHHelper.RunAEH(LaunchFailed, this, e);
    }

    public event AsyncEventHandler<object>? Launched;
    private async Task OnLaunched(object? sender, object e)
    {
        await AEHHelper.RunAEH(Launched, this, e);
    }



    public event AsyncEventHandler<DisplayGeneralErrorArgs>? LoadGithubFailed;
    public event AsyncEventHandler? LoadGithubFinished;

    [ObservableProperty]
    public bool isGithubReleasesLoaded = false;

    public ObservableCollection<GithubReleaseVM> GithubReleases { get; } = [];
    public async Task LoadGithubReleases()
    {
        _logger.Info("Loading Github releases...");

        if (isGithubReleasesLoaded)
        {
            _logger.Info("Github releases already loaded. Loading cached...");
            return;
        };


        IReadOnlyList<Release> releases;
        try
        {
            releases = await Globals.client.Repository.Release.GetAll(GithubInfo.owner, GithubInfo.mainName);
        }
        catch (ApiException ex)
        {
            _logger.Error(ex, "Failed to get releases from {owner} / {name}.", GithubInfo.owner, GithubInfo.mainName);
            await AEHHelper.RunAEH(LoadGithubFailed, this, new("Failed to get releases.", ex));
            return;
        }

        foreach (var release in releases)
        {
            _logger.Info("Loading release with tag {tag}...", release.TagName);

            SemVersion? version;
            try
            {
                version = SemVersion.Parse(release.TagName, SemVersionStyles.Any);
            }
            catch (FormatException ex)
            {
                _logger.Warn(ex, "Tag name {tag} cannot be parsed.", release.TagName);
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

        IsGithubReleasesLoaded = true;
        await AEHHelper.RunAEH(LoadGithubFinished, this);

        _logger.Info("Finished getting Github releases.");
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



    public void DeleteExistingVersion(ExistingVersionVM vm)
    {
        vm.DeleteFailed += OnDeleteFailed;
        vm.Delete();

        _logger.Debug("Deleting existing version with path {vmPath} from list...", vm.FolderPath);
        ExistingVersions.Remove(vm);
        _logger.Debug("Deleted.");
    }

    public event AsyncEventHandler<DisplayGeneralErrorArgs>? DeleteFailed;
    private async Task OnDeleteFailed(object? sender, DisplayGeneralErrorArgs e)
    {
        await AEHHelper.RunAEH(DeleteFailed, this, e);
    }
}


public partial class MainVMDesign : MainVM
{
    public MainVMDesign() : base()
    {
        ExistingVersions.Add(new ExistingVersionVM());
    }
}
