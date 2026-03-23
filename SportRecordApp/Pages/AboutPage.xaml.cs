using SportRecordApp.Services;
using System.Linq;

namespace SportRecordApp.Pages;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnInstructionsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new InstructionsPage());
    }

    private async void OnCheckUpdateClicked(object? sender, EventArgs e)
    {
        var updateInfo = await UpdateService.CheckForUpdateAsync();
        
        if (updateInfo == null)
        {
            await DisplayAlertAsync("检查更新", "当前已是最新版本", "确定");
        }
        else
        {
            var message = $"发现新版本 {updateInfo.LatestVersion}\n\n当前版本: {updateInfo.CurrentVersion}";
            if (!string.IsNullOrEmpty(updateInfo.ReleaseNotes))
            {
                message += $"\n\n更新内容:\n{updateInfo.ReleaseNotes}";
            }
            
            var result = await DisplayAlertAsync("发现新版本", message, "立即更新", "取消");
            
            if (result)
            {
                await DownloadAndInstallUpdate(updateInfo);
            }
        }
    }

    private async Task DownloadAndInstallUpdate(UpdateInfo updateInfo)
    {
        try
        {
            var loadingPage = new ContentPage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new ActivityIndicator { IsRunning = true, Color = Colors.White },
                        new Label 
                        { 
                            Text = "正在下载更新...", 
                            TextColor = Colors.White,
                            Margin = new Thickness(0, 10, 0, 0)
                        }
                    }
                },
                BackgroundColor = new Color(0, 0, 0, 0.7f)
            };

            await Navigation.PushModalAsync(loadingPage);

            var progress = new Progress<double>(percent =>
            {
                if (loadingPage.Content is StackLayout stackLayout)
                {
                    var label = stackLayout.Children.OfType<Label>().FirstOrDefault();
                    if (label != null)
                    {
                        label.Text = $"正在下载更新... {percent:F0}%";
                    }
                }
            });

            var apkPath = await UpdateService.DownloadApkAsync(updateInfo.DownloadUrl, progress);
            
            await Navigation.PopModalAsync();

            if (!string.IsNullOrEmpty(apkPath))
            {
                var result = await DisplayAlertAsync("下载完成", "更新已下载完成，是否立即安装？", "安装", "稍后");
                
                if (result)
                {
                    UpdateService.InstallApk(apkPath);
                }
            }
            else
            {
                await DisplayAlertAsync("下载失败", "下载更新文件失败，请稍后重试", "确定");
            }
        }
        catch (Exception ex)
        {
            await Navigation.PopModalAsync();
            await DisplayAlertAsync("更新失败", $"更新过程中出错: {ex.Message}", "确定");
        }
    }
}