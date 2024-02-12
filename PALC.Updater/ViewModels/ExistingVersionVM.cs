using CommunityToolkit.Mvvm.Input;
using Semver;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace PALC.Updater.ViewModels;

public partial class ExistingVersionVM : ViewModelBase
{
    public SemVersion? ReleaseVersion { get; set; } = SemVersion.Parse("3.0.0-test1", SemVersionStyles.AllowV);
    public string FolderPath { get; set; } = "path/to/folder";


    public event AsyncEventHandler<Exception>? LaunchFailed;
    public event AsyncEventHandler<object>? Launched;

    [RelayCommand]
    public async Task Launch()
    {
        try
        {
            Process.Start(new ProcessStartInfo(Path.Join(FolderPath, Globals.exeName)));
        }
        catch (Exception ex)
        {
            if (LaunchFailed != null) await LaunchFailed(this, ex);
            return;
        }
        
        if (Launched != null) await Launched(this, "");
    }


    public void Delete()
    {
        Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
            FolderPath,
            Microsoft.VisualBasic.FileIO.UIOption.AllDialogs,
            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin
        );
    }
}
