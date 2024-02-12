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

namespace PALC.Updater.ViewModels;

public partial class GithubReleaseVM : ViewModelBase
{
    public Release? githubRelease = null;

    public required string Name { get; set; }
    public required string ReleaseNotes { get; set; }
    public required string Url { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required SemVersion? ReleaseVersion { get; set; }


    public event AsyncEventHandler<Exception>? DownloadFailed;
    public event AsyncEventHandler<object?>? DownloadFinished;

    public async Task Download()
    {
        if (githubRelease == null)
        {
            if (DownloadFailed != null) await DownloadFailed(this, new Exception("No github release supplied. This is a fatal error and should be reported."));
            return;
        }

        try
        {
            var req = new HttpRequestMessage()
            {
                RequestUri = new Uri(new Uri(GithubInfo.mainReleases), $"download/{githubRelease.TagName}/{Globals.releaseFileToDownload}"),
                Method = HttpMethod.Get
            };
            req.Headers.Add("User-Agent", Globals.releaseFileToDownload);

            HttpResponseMessage res;
            using (var httpClient = new HttpClient())
            {
                res = await httpClient.SendAsync(req);
            }
            res.EnsureSuccessStatusCode();


            string zipPath = Path.Combine(Globals.versionsFolder, "download.zip");

            byte[] content = await res.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes(zipPath, content);

            string folderPath = Path.Combine(Globals.versionsFolder, githubRelease.TagName);
            ZipFile.ExtractToDirectory(zipPath, folderPath, true);
            File.Delete(zipPath);

            if (DownloadFinished != null) await DownloadFinished(this, null);
        }
        catch (Exception ex)
        {
            if (DownloadFailed != null) await DownloadFailed(this, ex);
            return;
        }
    }
}
