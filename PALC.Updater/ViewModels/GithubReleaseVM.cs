using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

using Octokit;
using Semver;
using System.Net.Http;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NLog;

namespace PALC.Updater.ViewModels;

public partial class GithubReleaseVM : ViewModelBase
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();


    public Release? githubRelease = null;

    public required string Name { get; set; }
    public required string ReleaseNotes { get; set; }
    public required string Url { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required SemVersion? ReleaseVersion { get; set; }


    public event AsyncEventHandler<DisplayGeneralErrorArgs>? DownloadFailed;
    public event AsyncEventHandler? DownloadFinished;

    public async Task Download()
    {
        _logger.Info("Downloading release with name {name} and version {releaseVersion}...", Name, ReleaseVersion);


        if (githubRelease == null)
        {
            _logger.Fatal("No github release supplied.");
            throw new Exception("No github release supplied.");
        }


        _logger.Trace("Creating request...");
        var req = new HttpRequestMessage()
        {
            RequestUri = new Uri(new Uri(GithubInfo.main.Releases), $"download/{githubRelease.TagName}/{Globals.releaseFileToDownload}"),
            Method = HttpMethod.Get
        };
        req.Headers.Add("User-Agent", Globals.releaseFileToDownload);


        _logger.Info("Sending request...");
        HttpResponseMessage res;
        using (var httpClient = new HttpClient())
        {
            try
            {
                res = await httpClient.SendAsync(req);
            }
            catch (HttpRequestException ex)
            {
                _logger.Error(ex, "Cannot send HTTP request.");
                await AEHHelper.RunAEH(DownloadFailed, this, new("Cannot download release due to a network failure.", ex));
                return;
            }
        }

        _logger.Trace("Checking if the request is a success...");
        try
        {
            res.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "Downloading resulted in a {code} code.", res.StatusCode);
            await AEHHelper.RunAEH(DownloadFailed, this, new($"An invalid response was given during the download (code {res.StatusCode}).", ex));
            return;
        }



        _logger.Info("Moving downloaded content to zip...");
        string zipPath = Path.Combine(Globals.versionsFolder, "download.zip");

        byte[] content = await res.Content.ReadAsByteArrayAsync();

        try
        {
            File.WriteAllBytes(zipPath, content);
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException ||
            ex is PathTooLongException
        )
        {
            _logger.Error(ex, "Cannot write to zip at {zipPath}.", zipPath);
            await AEHHelper.RunAEH(DownloadFailed, this, new(
                $"The program doesn't have access to the extracted zip.\n",
                ex
            ));

            return;
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.Error(ex, "Path {filePath} can't be found for some reason.", zipPath);
            await AEHHelper.RunAEH(DownloadFailed, this, new(
                $"The zip path \"{zipPath}\" cannot be found.",
                ex
            ));
            return;
        }



        _logger.Info("Extracting to folder...");

        string folderPath = Path.Combine(Globals.versionsFolder, githubRelease.TagName);
        try
        {
            ZipFile.ExtractToDirectory(zipPath, folderPath, true);
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException ||
            ex is PathTooLongException
        )
        {
            _logger.Error(ex, "Cannot extract to directory {directory}.", folderPath);
            await AEHHelper.RunAEH(DownloadFailed, this, new(
                $"The program doesn't have access to the folder \"{folderPath}\" and can't extract to this folder.\n",
                ex
            ));

            return;
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.Error(ex, "Path {directory} can't be found for some reason.", folderPath);
            await AEHHelper.RunAEH(DownloadFailed, this, new(
                $"The directory path \"{folderPath}\" cannot be found.",
                ex
            ));
            return;
        }



        _logger.Info("Deleting old zip file...");
        try
        {
            File.Delete(zipPath);
        }
        catch (Exception ex) when(
            ex is UnauthorizedAccessException ||
            ex is PathTooLongException
        )
        {
            _logger.Error(ex, "Cannot delete zip {zipPath}.", zipPath);
            await AEHHelper.RunAEH(DownloadFailed, this, new(
                $"The program can't delete the zip at \"{zipPath}\".\n\n" +
                $"The release is still downloaded, just extract it to a folder named \"{githubRelease.TagName}\" inside \"{Globals.versionsFolder}\".",
                ex
            ));

            return;
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.Error(ex, "Zip {zipPath} can't be found.", zipPath);
            await AEHHelper.RunAEH(DownloadFailed, this, new(
                $"The zip file at \"{zipPath}\" cannot be found. The download likely failed.",
                ex
            ));
            return;
        }

        await AEHHelper.RunAEH(DownloadFinished, this);
    }
}
