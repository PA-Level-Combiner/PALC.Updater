using CommunityToolkit.Mvvm.Input;
using Semver;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using NLog;
using ShimSkiaSharp;

namespace PALC.Updater.ViewModels;

public partial class ExistingVersionVM : ViewModelBase
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();


    public SemVersion? ReleaseVersion { get; set; } = SemVersion.Parse("3.0.0-test1", SemVersionStyles.AllowV);
    public string FolderPath { get; set; } = "path/to/folder";


    public event AsyncEventHandler<DisplayGeneralErrorArgs>? LaunchFailed;
    public event AsyncEventHandler? Launched;

    [RelayCommand]
    public async Task Launch()
    {
        string exePath = Path.Join(FolderPath, Globals.exeName);
        _logger.Info("Launching {exePath}...", exePath);

        try
        {
            Process.Start(new ProcessStartInfo(exePath));
        }
        catch (Exception ex)
        {
            await AEHHelper.RunAEH(LaunchFailed, this, new(
                $"An error occurred while trying to launch the program at \"{exePath}\".",
                ex
            ));
            return;
        }


        _logger.Info("Launched.");
        await AEHHelper.RunAEH(Launched, this);
    }


    public event AsyncEventHandler<DisplayGeneralErrorArgs>? DeleteFailed;

    public async void Delete()
    {
        _logger.Info("Deleting version at {folderPath}...", FolderPath);

        try
        {
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                FolderPath,
                Microsoft.VisualBasic.FileIO.UIOption.AllDialogs,
                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin
            );
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException ||
            ex is PathTooLongException
        )
        {
            _logger.Error("Cannot access files in {folderPath}.", FolderPath);
            await AEHHelper.RunAEH(DeleteFailed, this, new(
                $"The program cannot access the folder \"{FolderPath}\".\n" +
                AdditionalErrors.noAccessHelp,
                ex
            ));
            return;
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.Error(ex, "Cannot find {folderPath}.", FolderPath);
            await AEHHelper.RunAEH(DeleteFailed, this, new(
                $"The folder \"{FolderPath}\" doesn't exist.",
                ex
            ));
            return;
        }


        _logger.Info("Deleted.");
    }
}
