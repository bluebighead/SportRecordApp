namespace SportRecordApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Loaded += OnLoaded;
	}

	private async void OnLoaded(object? sender, EventArgs e)
	{
		Loaded -= OnLoaded;
		await CheckForUpdateAsync();
	}

	private async Task CheckForUpdateAsync()
	{
		try
		{
			var updateInfo = await Services.UpdateService.CheckForUpdateAsync();
			if (updateInfo != null)
			{
				var message = $"发现新版本 {updateInfo.LatestVersion}\n\n" +
					$"当前版本: {updateInfo.CurrentVersion}\n\n" +
					$"更新内容:\n{updateInfo.ReleaseNotes}";

				var result = await Application.Current.MainPage.DisplayAlert("软件更新", message, "立即更新", "稍后提醒");
				if (result)
				{
					await DownloadAndInstallUpdateAsync(updateInfo.DownloadUrl);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"检查更新失败: {ex.Message}");
		}
	}

	private async Task DownloadAndInstallUpdateAsync(string downloadUrl)
	{
		try
		{
			await Application.Current.MainPage.DisplayAlert("下载中", "正在下载更新，请稍候...", "确定");

			var progress = new Progress<double>(percent =>
			{
				Console.WriteLine($"下载进度: {percent:F1}%");
			});

			var apkPath = await Services.UpdateService.DownloadApkAsync(downloadUrl, progress);
			if (!string.IsNullOrEmpty(apkPath))
			{
				await Application.Current.MainPage.DisplayAlert("下载完成", "即将安装更新", "确定");
				Services.UpdateService.InstallApk(apkPath);
			}
			else
			{
				await Application.Current.MainPage.DisplayAlert("下载失败", "无法下载更新文件", "确定");
			}
		}
		catch (Exception ex)
		{
			await Application.Current.MainPage.DisplayAlert("错误", $"下载更新失败: {ex.Message}", "确定");
		}
	}
}
