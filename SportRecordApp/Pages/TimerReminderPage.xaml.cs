using SportRecordApp.Models;
using SportRecordApp.Services;
using Microsoft.Maui.ApplicationModel;

namespace SportRecordApp.Pages;

public partial class TimerReminderPage : ContentPage
{
	private SportProject? _project;

	public TimerReminderPage(SportProject project)
	{
		InitializeComponent();
		_project = project;
		ProjectNameLabel.Text = _project.Name;

		UpdateUIState();
		DrinkWaterReminderManager.ReminderStatusChanged += OnReminderStatusChanged;
	}

	private void OnReminderStatusChanged(object? sender, EventArgs e)
	{
		UpdateUIState();
	}

	private void UpdateUIState()
	{
		if (DrinkWaterReminderManager.IsReminderActive)
		{
			DrinkWaterSwitch.IsToggled = true;
			int totalSeconds = DrinkWaterReminderManager.ReminderInterval / 1000;
			string timeText;
			if (totalSeconds >= 3600 && totalSeconds % 3600 == 0)
			{
				timeText = $"每{totalSeconds / 3600}小时提醒";
			}
			else
			{
				timeText = $"每{totalSeconds / 60}分钟提醒";
			}
			ReminderStatusLabel.Text = $"已开启 ({timeText})";
		}
		else
		{
			DrinkWaterSwitch.IsToggled = false;
			ReminderStatusLabel.Text = "已关闭";
		}
	}

	private async void OnDrinkWaterSwitchToggled(object? sender, ToggledEventArgs e)
	{
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

		if (e.Value)
		{
			bool hasPermission = await CheckNotificationPermission();
			if (hasPermission && _project != null)
			{
				await DrinkWaterReminderManager.StartReminder(_project);
			}
			else
			{
				DrinkWaterSwitch.IsToggled = false;
			}
		}
		else
		{
			DrinkWaterReminderManager.StopReminder();
		}
	}

	private async void OnMenuButtonClicked(object? sender, EventArgs e)
	{
		var action = await DisplayActionSheet(
			"定时设置",
			"取消",
			null,
			"定时设置"
		);

		if (action == "定时设置")
		{
			await ShowTimeIntervalDialog();
		}
	}

	private async Task ShowTimeIntervalDialog()
	{
		int totalSeconds = DrinkWaterReminderManager.ReminderInterval / 1000;
		int defaultInterval = 2 * 3600;
		
		if (totalSeconds == defaultInterval)
		{
			UnitPicker.SelectedIndex = 0;
			TimeEntry.Text = "2";
			TimeEntry.IsEnabled = false;
		}
		else if (totalSeconds >= 3600 && totalSeconds % 3600 == 0)
		{
			TimeEntry.Text = (totalSeconds / 3600).ToString();
			UnitPicker.SelectedIndex = 2;
			TimeEntry.IsEnabled = true;
		}
		else
		{
			TimeEntry.Text = (totalSeconds / 60).ToString();
			UnitPicker.SelectedIndex = 1;
			TimeEntry.IsEnabled = true;
		}
		
		DialogOverlay.IsVisible = true;
	}

	private void OnUnitPickerSelectedIndexChanged(object? sender, EventArgs e)
	{
		if (UnitPicker.SelectedIndex == 0)
		{
			TimeEntry.Text = "2";
			TimeEntry.IsEnabled = false;
		}
		else
		{
			TimeEntry.IsEnabled = true;
			if (string.IsNullOrEmpty(TimeEntry.Text) || TimeEntry.Text == "2")
			{
				TimeEntry.Text = "";
			}
		}
	}

	private void OnDialogCancelClicked(object? sender, EventArgs e)
	{
		DialogOverlay.IsVisible = false;
	}

	private async void OnDialogConfirmClicked(object? sender, EventArgs e)
	{
		int milliseconds = 0;
		
		if (UnitPicker.SelectedIndex == 0)
		{
			milliseconds = 2 * 3600 * 1000;
		}
		else if (int.TryParse(TimeEntry.Text, out int time) && time > 0)
		{
			if (UnitPicker.SelectedIndex == 2)
			{
				milliseconds = time * 3600 * 1000;
			}
			else
			{
				milliseconds = time * 60 * 1000;
			}
		}
		
		if (milliseconds > 0)
		{
			DrinkWaterReminderManager.ReminderInterval = milliseconds;
			UpdateUIState();
		}
		
		DialogOverlay.IsVisible = false;
	}

	private async Task<bool> CheckNotificationPermission()
	{
		try
		{
#if ANDROID
			var currentActivity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
			if (currentActivity != null)
			{
				Platforms.Android.NotificationHelper.CreateNotificationChannel(currentActivity);
				bool hasPermission = Platforms.Android.NotificationHelper.CheckNotificationPermission(currentActivity);
				if (!hasPermission)
				{
					bool result = await DisplayAlertAsync("权限提示", "定时喝水功能需要通知权限来发送提醒", "去设置", "取消");
					if (result)
					{
						AppInfo.ShowSettingsUI();
					}
					return false;
				}
			}
#endif
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"检查通知权限失败: {ex.Message}");
			return false;
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.Navigation.PopAsync();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		DrinkWaterReminderManager.ReminderStatusChanged -= OnReminderStatusChanged;
	}
}