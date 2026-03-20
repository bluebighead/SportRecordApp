using SportRecordApp.Models;
using SportRecordApp.Services;
using Microsoft.Maui.Devices;
#if ANDROID
using Android.App;
using Android.Appwidget;
using Android.Content;
#endif

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
					SoundService.PlayErrorSound();
					await AnimationService.PlayShakeAnimationAsync(CheckInButton);
					await DisplayAlertAsync("提示", "你今天已经打卡过了", "确定");
					return;
				}
			}
			
			// 播放打卡动画和音效
			SoundService.PlayCheckInSound();
			await AnimationService.PlayCheckInAnimationAsync(CheckInButton);
			
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
			
			string checkInTime = DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss");
			_project.CheckInTimes.Add(checkInTime);
			UpdateUI();
			SaveData();
			
			// 如果首次完成目标，播放成功动画和音效
			if (_project.IsCompleted && !wasCompleted)
			{
				SoundService.PlaySuccessSound();
				await AnimationService.PlaySuccessAnimationAsync(CheckInButton);
				
				try
				{
					if (Vibration.Default.IsSupported)
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
	}

	private void SaveData()
	{
		if (_project != null)
		{
			var allProjects = DataService.LoadProjects();
			var existingProject = allProjects.FirstOrDefault(p => p.Name == _project.Name);
			if (existingProject != null)
			{
				existingProject.CheckInTimes = _project.CheckInTimes;
				existingProject.TargetTime = _project.TargetTime;
				existingProject.IsCompletedBefore = _project.IsCompletedBefore;
				existingProject.IsUnlimited = _project.IsUnlimited;
			}
			DataService.SaveProjects(allProjects);
			
			// 通知小组件更新
			UpdateWidget();
		}
	}

	private void UpdateWidget()
	{
#if ANDROID
		try
		{
			var context = Android.App.Application.Context;
			var appWidgetManager = AppWidgetManager.GetInstance(context);
			if (appWidgetManager != null)
			{
				var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(Platforms.Android.CheckInWidgetLargeProvider)));
				var appWidgetIds = appWidgetManager.GetAppWidgetIds(componentName);
				if (appWidgetIds != null && appWidgetIds.Length > 0)
				{
					var intent = new Intent(context, typeof(Platforms.Android.CheckInWidgetLargeProvider));
					intent.SetAction("android.appwidget.action.APPWIDGET_UPDATE");
					intent.PutExtra("appWidgetIds", appWidgetIds);
					context.SendBroadcast(intent);
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"更新小组件失败: {ex.Message}");
		}
#endif
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		// 当返回时保存数据
		SaveData();
		await Shell.Current.Navigation.PopAsync();
	}

	private async void OnMenuClicked(object? sender, EventArgs e)
	{
		string action = await DisplayActionSheetAsync("菜单", "取消", null, "回退打卡记录", "打卡时间详情", "日历提醒");
		
		if (action == "回退打卡记录")
		{
			UndoCheckIn();
		}
		else if (action == "打卡时间详情")
		{
			ShowCheckInDetails();
		}
		else if (action == "日历提醒")
		{
			await SetCalendarReminder();
		}
	}

	private async Task SetCalendarReminder()
	{
		if (_project == null) return;
		
		try
		{
			await CalendarService.CreateReminderEventAsync(_project.Name, _project.TargetTime);
			await DisplayAlertAsync("成功", "已添加日历提醒", "确定");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("错误", $"添加日历提醒失败: {ex.Message}", "确定");
		}
	}

	private void UndoCheckIn()
	{
		if (_project != null && _project.CheckInTimes.Count > 0)
		{
			_project.CheckInTimes.RemoveAt(_project.CheckInTimes.Count - 1);
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
