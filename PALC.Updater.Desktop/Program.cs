using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Avalonia;
using NLog;

namespace PALC.Updater.Desktop;

class Program
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            string crashHandlerPath = "CrashHandler\\PALC.CrashHandler.exe";

            List<string> crashArgs = new() {
                Globals.programName,
                ProgramInfo.GetProgramVersion()?.ToString() ?? "Unknown version",
                Globals.logsPath,
                Globals.githubIssuesLink,
                ex.Message,
                ex.StackTrace ?? "No stack trace available"
            };


            _logger.Fatal(
                "A fatal error occurred.\n" +
                $"{ex.StackTrace}\n" +
                $"\n" +
                $"{ex.Message}"
            );

            _logger.Info("Running crash handler...");
            Process.Start(crashHandlerPath, crashArgs);
            _logger.Info("Exiting with exception...");
            ExceptionDispatchInfo.Capture(ex).Throw();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

}
