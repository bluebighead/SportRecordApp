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
	private bool _isTimerRunning = false;
	private DateTime _timerStartTime;
	private TimeSpan _elapsedTime = TimeSpan.Zero;
	private System.Timers.Timer? _timer;
	private System.Timers.Timer? _timeUpdateTimer;

	public ProjectDetailPage(SportProject project)
	{
		InitializeComponent();
		_project = project;
		UpdateUI();
		StartTimeUpdateTimer();
	}

	private void StartTimeUpdateTimer()
	{
		// 更新当前时间
		UpdateCurrentTime();
		
		// 启动定时器，每秒更新一次时间
		_timeUpdateTimer = new System.Timers.Timer(1000);
		_timeUpdateTimer.Elapsed += (s, e) =>
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				UpdateCurrentTime();
			});
		};
		_timeUpdateTimer.Start();
	}

	private void UpdateCurrentTime()
	{
		CurrentTimeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
	}

	private void UpdateUI()
	{
		if (_project != null)
		{
			ProjectNameLabel.Text = _project.Name;
			TargetTimeLabel.Text = _project.TargetTime;
			CheckInDaysLabel.Text = $"{_project.CheckInDays} 天";
			
			bool isPlankSupport = _project.Name == "平板支撑";
			TimerButton.IsVisible = isPlankSupport;
			
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
			
			// 平板支撑项目的按钮状态管理
			if (isPlankSupport)
			{
				// 检查今天是否已经打卡
				string today = DateTime.Now.ToString("yyyy年MM月dd日");
				bool hasCheckedInToday = _project.CheckInTimes.Any(time => time.StartsWith(today));
				
				if (hasCheckedInToday)
				{
					// 今天已经打卡，禁用所有按钮
					CheckInButton.IsEnabled = false;
					CheckInButton.BackgroundColor = Colors.Gray;
					TimerButton.IsEnabled = false;
					TimerButton.BackgroundColor = Colors.Gray;
				}
				else
				{
					// 今天未打卡，根据计时状态设置按钮状态
					if (_elapsedTime.TotalSeconds > 0)
					{
						// 已完成计时，启用打卡按钮，禁用计时按钮
						CheckInButton.IsEnabled = true;
						CheckInButton.BackgroundColor = Color.FromArgb("#8A2BE2");
						TimerButton.IsEnabled = false;
						TimerButton.BackgroundColor = Colors.Gray;
					}
					else
					{
						// 未进行计时，禁用打卡按钮，启用计时按钮
						CheckInButton.IsEnabled = false;
						CheckInButton.BackgroundColor = Colors.Gray;
						TimerButton.IsEnabled = true;
						TimerButton.BackgroundColor = Color.FromArgb("#4CAF50");
						if (_isTimerRunning)
						{
							TimerButton.BackgroundColor = Colors.Red;
						}
					}
				}
			}
		}
	}

	private async void OnCheckInClicked(object? sender, EventArgs e)
	{
		if (_project != null)
		{
			string timerDuration = string.Empty;
			
			if (_isTimerRunning)
			{
				StopTimer();
				timerDuration = FormatTime(_elapsedTime);
			}
			else if (_elapsedTime.TotalSeconds > 0)
			{
				// 如果计时器已停止但有计时数据，也使用该数据
				timerDuration = FormatTime(_elapsedTime);
			}
			
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
			if (!string.IsNullOrEmpty(timerDuration))
			{
				checkInTime += $" ({timerDuration})";
			}
			_project.CheckInTimes.Add(checkInTime);
			UpdateUI();
			SaveData();
			
			// 重置计时数据
			_elapsedTime = TimeSpan.Zero;
			TimerLabel.Text = "00:00:00";
			
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
		bool allowUndoCheckIn = SettingsService.GetAllowUndoCheckIn();
		
		string action;
		if (allowUndoCheckIn)
		{
			action = await DisplayActionSheetAsync("菜单", "取消", null, "回退打卡记录", "打卡时间详情", "日历提醒", "多功能设置");
		}
		else
		{
			action = await DisplayActionSheetAsync("菜单", "取消", null, "打卡时间详情", "日历提醒", "多功能设置");
		}
		
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
		else if (action == "多功能设置")
		{
			await ShowTimerReminder();
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

	private async Task ShowTimerReminder()
	{
		if (_project != null)
		{
			await Shell.Current.Navigation.PushAsync(new TimerReminderPage(_project));
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

	private async void OnTimerButtonClicked(object? sender, EventArgs e)
	{
		// 添加震动反馈
		try
		{
			if (Vibration.Default.IsSupported)
			{
				Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"震动失败: {ex.Message}");
		}
		
		if (_isTimerRunning)
		{
			StopTimer();
		}
		else
		{
			StartTimer();
		}
	}

	private void StartTimer()
	{
		_isTimerRunning = true;
		_timerStartTime = DateTime.Now;
		_elapsedTime = TimeSpan.Zero;
		
		TimerButton.Text = "停止计时";
		TimerButton.BackgroundColor = Colors.Red;
		TimerLabel.IsVisible = true;
		TimerLabel.Text = "00:00:00";
		
		_timer = new System.Timers.Timer(100);
		_timer.Elapsed += (s, e) =>
		{
			_elapsedTime = DateTime.Now - _timerStartTime;
			MainThread.BeginInvokeOnMainThread(() =>
			{
				TimerLabel.Text = FormatTime(_elapsedTime);
			});
		};
		_timer.Start();
		
		// 更新UI状态
		UpdateUI();
	}

	private void StopTimer()
	{
		_isTimerRunning = false;
		
		if (_timer != null)
		{
			_timer.Stop();
			_timer.Dispose();
			_timer = null;
		}
		
		TimerButton.Text = "开始计时";
		TimerButton.BackgroundColor = Color.FromArgb("#4CAF50");
		
		// 更新UI状态
		UpdateUI();
	}

	private string FormatTime(TimeSpan timeSpan)
	{
		int hours = timeSpan.Hours;
		int minutes = timeSpan.Minutes;
		int seconds = timeSpan.Seconds;
		
		return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		
		if (_isTimerRunning)
		{
			StopTimer();
		}
		
		// 停止时间更新定时器
		if (_timeUpdateTimer != null)
		{
			_timeUpdateTimer.Stop();
			_timeUpdateTimer.Dispose();
			_timeUpdateTimer = null;
		}

		// 取消订阅心率更新事件
		HeartRateService.HeartRateUpdated -= OnHeartRateUpdated;
		HeartRateService.ConnectionStateChanged -= OnHeartRateConnectionStateChanged;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		StartTimeUpdateTimer();

		// 检查心率广播是否开启
		bool heartRateEnabled = SettingsService.GetHeartRateBroadcastEnabled();
		HeartRateLayout.IsVisible = heartRateEnabled;

		if (heartRateEnabled)
		{
			// 订阅心率更新事件
			HeartRateService.HeartRateUpdated += OnHeartRateUpdated;
			HeartRateService.ConnectionStateChanged += OnHeartRateConnectionStateChanged;

			// 更新当前心率显示
			if (HeartRateService.IsScanning)
			{
				int currentRate = HeartRateService.CurrentHeartRate;
				HeartRateLabel.Text = currentRate > 0 ? $"{currentRate}" : "--";
			}
			else
			{
				HeartRateLabel.Text = "--";
			}
		}
	}

	private void OnHeartRateUpdated(object? sender, HeartRateEventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			HeartRateLabel.Text = $"{e.HeartRate}";
		});
	}

	private void OnHeartRateConnectionStateChanged(object? sender, EventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			if (!HeartRateService.IsScanning)
			{
				HeartRateLabel.Text = "--";
			}
		});
	}
}
