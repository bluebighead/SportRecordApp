using SportRecordApp.Models;
using SportRecordApp.Services;
using Microsoft.Maui.Devices;

namespace SportRecordApp.Pages;

public partial class ProjectDetailPage : ContentPage
{
	private SportProject? _project;

	public ProjectDetailPage(SportProject project)
	{
		InitializeComponent();
		_project = project;
		UpdateUI();
	}

	private void UpdateUI()
	{
		if (_project != null)
		{
			ProjectNameLabel.Text = _project.Name;
			TargetTimeLabel.Text = _project.TargetTime;
			CheckInDaysLabel.Text = $"{_project.CheckInDays} 天";
			
			if (_project.IsCompleted)
			{
				CompletionLabel.Text = "🎉 目标打卡完成！太棒啦！";
			}
			else
			{
				CompletionLabel.Text = "";
			}
			
			if (_project.CheckInTimes.Count > 0)
			{
				LastCheckInTimeLabel.Text = _project.CheckInTimes.Last();
			}
			else
			{
				LastCheckInTimeLabel.Text = "";
			}
		}
	}

	private async void OnCheckInClicked(object? sender, EventArgs e)
	{
		if (_project != null)
		{
			// 检查是否开启了每天只可打卡一次的功能
			if (SettingsService.GetDailyCheckInLimit())
			{
				// 检查今天是否已经打卡过
				string today = DateTime.Now.ToString("yyyy年MM月dd日");
				bool hasCheckedInToday = _project.CheckInTimes.Any(time => time.StartsWith(today));
				
				if (hasCheckedInToday)
				{
					await DisplayAlertAsync("提示", "你今天已经打卡过了", "确定");
					return;
				}
			}
			
			try
			{
				// 短震动反馈
				if (Vibration.Default.IsSupported)
				{
					Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"震动失败: {ex.Message}");
			}
			
			// 记录打卡前的状态
			bool wasCompleted = _project.IsCompleted;
			
			_project.CheckInDays++;
			string checkInTime = DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒");
			_project.CheckInTimes.Add(checkInTime);
			UpdateUI();
			SaveData();
			
			try
			{
				// 首次完成时的长震动
				if (_project.IsCompleted && !wasCompleted && Vibration.Default.IsSupported)
				{
					Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"震动失败: {ex.Message}");
			}
		}
	}

	private void SaveData()
	{
		// 获取主页面的项目列表并保存
		if (Shell.Current.Navigation.NavigationStack.FirstOrDefault() is MainPage mainPage)
		{
			// 主页面会自动保存数据
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		// 当返回时保存数据
		SaveData();
		await Shell.Current.Navigation.PopAsync();
	}

	private async void OnMenuClicked(object? sender, EventArgs e)
	{
		string action = await DisplayActionSheetAsync("菜单", "取消", null, "回退打卡记录", "打卡时间详情");
		
		if (action == "回退打卡记录")
		{
			UndoCheckIn();
		}
		else if (action == "打卡时间详情")
		{
			ShowCheckInDetails();
		}
	}

	private void UndoCheckIn()
	{
		if (_project != null && _project.CheckInDays > 0)
		{
			_project.CheckInDays--;
			if (_project.CheckInTimes.Count > 0)
			{
				_project.CheckInTimes.RemoveAt(_project.CheckInTimes.Count - 1);
			}
			UpdateUI();
			SaveData();
		}
	}

	private async void ShowCheckInDetails()
	{
		if (_project != null)
		{
			await Shell.Current.Navigation.PushAsync(new CheckInDetailPage(_project));
		}
	}
}
