using System.Text.Json;
using System.Text.Json.Serialization;

namespace SportRecordApp.Services;

public static class UpdateService
{
    private const string GitHubOwner = "bluebighead";
    private const string GitHubRepo = "SportRecordApp";
    private const string GitHubApiUrl = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
    private const string CurrentVersion = "1.0.3";

    public static async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "SportRecordApp");
            
            var response = await httpClient.GetAsync(GitHubApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"获取版本信息失败: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var releaseInfo = JsonSerializer.Deserialize<GitHubReleaseInfo>(json);
            
            if (releaseInfo == null || string.IsNullOrEmpty(releaseInfo.TagName))
            {
                Console.WriteLine("解析版本信息失败");
                return null;
            }

            var latestVersion = releaseInfo.TagName.TrimStart('v');
            if (CompareVersions(latestVersion, CurrentVersion) > 0)
            {
                var apkAsset = releaseInfo.Assets?.FirstOrDefault(a => a.Name.EndsWith(".apk"));
                if (apkAsset != null)
                {
                    return new UpdateInfo
                    {
                        LatestVersion = latestVersion,
                        CurrentVersion = CurrentVersion,
                        DownloadUrl = apkAsset.BrowserDownloadUrl,
                        ReleaseNotes = releaseInfo.Body
                    };
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"检查更新失败: {ex.Message}");
            return null;
        }
    }

    public static async Task<string?> DownloadApkAsync(string downloadUrl, IProgress<double>? progress = null)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"下载APK失败: {response.StatusCode}");
                return null;
            }

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            await using var fileStream = File.Create(filePath);
            await using var stream = await response.Content.ReadAsStreamAsync();
            
            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;
                
                if (totalBytes > 0 && progress != null)
                {
                    var percent = (double)totalRead / totalBytes * 100;
                    progress.Report(percent);
                }
            }

            return filePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载APK失败: {ex.Message}");
            return null;
        }
    }

    public static void InstallApk(string apkPath)
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var file = new Java.IO.File(apkPath);
            
            if (!file.Exists())
            {
                Console.WriteLine("APK文件不存在");
                return;
            }

            var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, context.PackageName + ".fileprovider", file);
            var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
            intent.SetDataAndType(uri, "application/vnd.android.package-archive");
            intent.SetFlags(Android.Content.ActivityFlags.GrantReadUriPermission | Android.Content.ActivityFlags.NewTask);
            
            context.StartActivity(intent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"安装APK失败: {ex.Message}");
        }
#endif
    }

    private static int CompareVersions(string version1, string version2)
    {
        var parts1 = version1.Split('.');
        var parts2 = version2.Split('.');

        for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
        {
            int v1 = i < parts1.Length ? int.Parse(parts1[i]) : 0;
            int v2 = i < parts2.Length ? int.Parse(parts2[i]) : 0;

            if (v1 != v2)
            {
                return v1.CompareTo(v2);
            }
        }

        return 0;
    }
}

public class UpdateInfo
{
    public string LatestVersion { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
}

public class GitHubReleaseInfo
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubAsset>? Assets { get; set; }
}

public class GitHubAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
}