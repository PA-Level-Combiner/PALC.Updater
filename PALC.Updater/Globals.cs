using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PALC.Updater;

public static class Globals
{
    public static readonly string userAgent = "PALC-Updater";

    public static readonly string versionsFolder = "PALCVersions";
    public static readonly string exeName = "PALC.Main.Desktop.exe";

    public static readonly string releaseFileToDownload = "PALC.Desktop.zip";

    public static readonly GitHubClient client = new(new ProductHeaderValue(userAgent));


    public static readonly string programName = "PALC Updater";

    public static readonly string githubLink = @"https://github.com/PA-Level-Combiner/PALC.Updater";
    public static readonly string githubIssuesLink = $"{githubLink}/issues";
    public static readonly string githubReleasesLink = "${githubLink}/releases";

    public static readonly string logsPath = $"{AppDomain.CurrentDomain.BaseDirectory}logs";
}
