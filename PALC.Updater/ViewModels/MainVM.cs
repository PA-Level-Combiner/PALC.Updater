using Semver;
using System;
using System.Collections.ObjectModel;
using System.IO;
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
    public event AsyncEventHandler<LoadReleaseFailedArgs>? LoadReleaseFailed;

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
                if (LoadReleaseFailed != null)
                    await LoadReleaseFailed(this, new LoadReleaseFailedArgs { ex = new Exception($"{Globals.exeName} is missing."), path = directory });

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
                if (LoadReleaseFailed != null)
                    await LoadReleaseFailed(this, new LoadReleaseFailedArgs {
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
