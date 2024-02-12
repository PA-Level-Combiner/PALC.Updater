using Semver;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace PALC.Updater.ViewModels;



public partial class MainVM : ViewModelBase
{
    public ObservableCollection<ExistingVersionVM> ExistingVersions { get; } = [];


    public event AsyncEventHandler<Exception>? LoadExistingReleasesFailed;

    public async Task LoadExistingReleases()
    {
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
                if (LoadExistingReleasesFailed != null)
                    await LoadExistingReleasesFailed(this, new Exception($"The .exe for path {directory} is missing."));

                continue;
            }

            var folderName = Path.GetFileName(Path.GetDirectoryName(directory));
            SemVersion? version;
            try
            {
                version = SemVersion.Parse(folderName, SemVersionStyles.Any);
            }
            catch (FormatException ex)
            {
                if (LoadExistingReleasesFailed != null)
                    await LoadExistingReleasesFailed(this, ex);

                version = null;
            }

            ExistingVersions.Add(new ExistingVersionVM { Path = directory, ReleaseVersion = version });
        }
    }
}
