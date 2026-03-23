using SportRecordApp.Models;
using SportRecordApp.Services;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SportRecordApp.Pages;

public partial class TimerReminderPage : ContentPage
{
	private SportProject? _project;
	private bool _isUpdatingUI = false;
	private bool _isProcessingHeartRateSwitch = false;
	private ObservableCollection<BleDeviceInfo> _discoveredDevices = new();
	private ObservableCollection<HistoryDeviceDisplayItem> _historyDevices = new();
	private BleDeviceInfo? _selectedDevice = null;
	private HistoryDeviceDisplayItem? _selectedHistoryDevice = null;

	public ICommand DeleteDeviceCommand { get; }

	public TimerReminderPage(SportProject project)
	{
		InitializeComponent();
		_project = project;
		ProjectNameLabel.Text = _project.Name;

		DeleteDeviceCommand = new Command<HistoryDeviceDisplayItem>(OnDeleteDeviceClicked);
		BindingContext = this;

		DeviceListView.ItemsSource = _discoveredDevices;
		HistoryDeviceListView.ItemsSource = _historyDevices;

		UpdateUIState();
		DrinkWaterReminderManager.ReminderStatusChanged += OnReminderStatusChanged;
		HeartRateService.ConnectionStateChanged += OnHeartRateConnectionStateChanged;
		HeartRateService.DeviceDiscovered += OnDeviceDiscovered;
		HeartRateService.ScanError += OnScanError;
	}

	private void OnScanError(object? sender, string error)
	{
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			await DisplayAlertAsync("连接错误", error, "确定");
			UpdateHeartRateUIState();
		});
	}

	private void OnDeviceDiscovered(object? sender, BleDeviceInfo device)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			var existingDevice = _discoveredDevices.FirstOrDefault(d => d.Address == device.Address);
			if (existingDevice == null)
			{
				_discoveredDevices.Add(device);
			}
		});
	}

	private void OnReminderStatusChanged(object? sender, EventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			if (!_isProcessingHeartRateSwitch)
			{
				UpdateUIState();
			}
		});
	}

	private void OnHeartRateConnectionStateChanged(object? sender, EventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			if (!_isProcessingHeartRateSwitch)
			{
				UpdateHeartRateUIState();
			}
		});
	}

	private void UpdateUIState()
	{
		_isUpdatingUI = true;
		try
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
		finally
		{
			_isUpdatingUI = false;
		}

		UpdateHeartRateUIState();
	}

	private void UpdateHeartRateUIState()
	{
		_isUpdatingUI = true;
		try
		{
			bool heartRateEnabled = SettingsService.GetHeartRateBroadcastEnabled();
			string connectedDevice = HeartRateService.ConnectedDeviceName;
			
			if (!string.IsNullOrEmpty(connectedDevice))
			{
				HeartRateSwitch.IsToggled = true;
				HeartRateStatusLabel.Text = $"已连接 {connectedDevice}";
			}
			else if (HeartRateService.IsConnecting)
			{
				HeartRateSwitch.IsToggled = true;
				HeartRateStatusLabel.Text = "正在连接...";
			}
			else if (heartRateEnabled && HeartRateService.IsScanning)
			{
				HeartRateSwitch.IsToggled = true;
				HeartRateStatusLabel.Text = "搜索设备中...";
			}
			else if (heartRateEnabled)
			{
				HeartRateSwitch.IsToggled = true;
				HeartRateStatusLabel.Text = "已开启";
			}
			else
			{
				HeartRateSwitch.IsToggled = false;
				HeartRateStatusLabel.Text = "已关闭";
			}
		}
		finally
		{
			_isUpdatingUI = false;
		}
	}

	private async void OnDrinkWaterSwitchToggled(object? sender, ToggledEventArgs e)
	{
		if (_isUpdatingUI)
		{
			return;
		}

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
				_isUpdatingUI = true;
				try
				{
					DrinkWaterSwitch.IsToggled = false;
				}
				finally
				{
					_isUpdatingUI = false;
				}
			}
		}
		else
		{
			DrinkWaterReminderManager.StopReminder();
		}
	}

	private async void OnHeartRateSwitchToggled(object? sender, ToggledEventArgs e)
	{
		if (_isUpdatingUI || _isProcessingHeartRateSwitch)
		{
			return;
		}

		_isProcessingHeartRateSwitch = true;
		try
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
				bool hasPermission = await CheckBluetoothPermission();
				if (!hasPermission)
				{
					_isUpdatingUI = true;
					try
					{
						HeartRateSwitch.IsToggled = false;
						HeartRateStatusLabel.Text = "已关闭";
					}
					finally
					{
						_isUpdatingUI = false;
					}
					return;
				}

				SettingsService.SetHeartRateBroadcastEnabled(true);
				
				if (HeartRateService.HasLastConnectedDevice())
				{
					string lastName = HeartRateService.GetLastConnectedDeviceName();
					HeartRateStatusLabel.Text = $"正在连接 {lastName}...";
					
					bool reconnectSuccess = HeartRateService.TryAutoReconnect();
					if (!reconnectSuccess)
					{
						await DisplayAlertAsync("提示", $"无法连接到上次设备 \"{lastName}\"，请手动选择设备", "确定");
						HeartRateStatusLabel.Text = "已开启";
					}
				}
				else
				{
					HeartRateStatusLabel.Text = "已开启";
				}
			}
			else
			{
				HeartRateService.Disconnect();
				SettingsService.SetHeartRateBroadcastEnabled(false);
				HeartRateStatusLabel.Text = "已关闭";
			}
		}
		finally
		{
			_isProcessingHeartRateSwitch = false;
		}
	}

	private async Task<bool> CheckBluetoothPermission()
	{
		try
		{
			bool hasPermission = await HeartRateService.RequestBluetoothPermissionAsync();
			if (!hasPermission)
			{
				bool result = await DisplayAlertAsync("权限提示", "心率广播功能需要蓝牙权限来连接心率设备，请在设置中允许", "去设置", "取消");
				if (result)
				{
					AppInfo.ShowSettingsUI();
				}
			}
			return hasPermission;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"检查蓝牙权限失败: {ex.Message}");
			return false;
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

	private void OnHeartRateMenuButtonClicked(object? sender, EventArgs e)
	{
		HeartRateMenuOverlay.IsVisible = true;
	}

	private void OnHeartRateMenuOverlayTapped(object? sender, TappedEventArgs e)
	{
		HeartRateMenuOverlay.IsVisible = false;
	}

	private void OnSearchDeviceMenuClicked(object? sender, EventArgs e)
	{
		HeartRateMenuOverlay.IsVisible = false;
		_ = ShowDeviceScanDialog();
	}

	private void OnHistoryDeviceMenuClicked(object? sender, EventArgs e)
	{
		HeartRateMenuOverlay.IsVisible = false;
		_ = ShowHistoryDeviceDialog();
	}

	private async Task ShowHistoryDeviceDialog()
	{
		var history = SettingsService.GetDeviceHistory();
		_historyDevices.Clear();
		_selectedHistoryDevice = null;

		foreach (var device in history)
		{
			_historyDevices.Add(new HistoryDeviceDisplayItem(device.DeviceName, device.DeviceAddress, device.ConnectCount));
		}

		HistoryEmptyLabel.IsVisible = _historyDevices.Count == 0;
		HistoryDeviceOverlay.IsVisible = true;
	}

	private void OnHistoryDeviceListViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is HistoryDeviceDisplayItem device)
		{
			foreach (var d in _historyDevices)
			{
				d.IsSelected = d.Address == device.Address;
			}
			_selectedHistoryDevice = device;
		}
	}

	private void OnHistoryDeviceCancelClicked(object? sender, EventArgs e)
	{
		HistoryDeviceOverlay.IsVisible = false;
	}

	private async void OnHistoryDeviceConnectClicked(object? sender, EventArgs e)
	{
		if (_selectedHistoryDevice == null)
		{
			await DisplayAlertAsync("提示", "请选择一个设备", "确定");
			return;
		}

		HistoryDeviceOverlay.IsVisible = false;

		bool heartRateEnabled = SettingsService.GetHeartRateBroadcastEnabled();
		if (!heartRateEnabled)
		{
			await DisplayAlertAsync("提示", "请先开启心率广播功能", "确定");
			return;
		}

		bool hasPermission = await CheckBluetoothPermission();
		if (!hasPermission)
		{
			return;
		}

		HeartRateStatusLabel.Text = $"正在连接 {_selectedHistoryDevice.DeviceName}...";

		bool success = HeartRateService.ConnectToDeviceByAddress(_selectedHistoryDevice.Address, _selectedHistoryDevice.DeviceName);
		if (!success)
		{
			await DisplayAlertAsync("提示", "连接设备失败", "确定");
			HeartRateStatusLabel.Text = "已开启";
		}
	}

	private async void OnDeleteDeviceClicked(HistoryDeviceDisplayItem? device)
	{
		if (device == null) return;

		bool confirm = await DisplayAlertAsync("确认删除", $"确定要删除设备 \"{device.DeviceName}\" 吗？", "删除", "取消");
		if (confirm)
		{
			SettingsService.RemoveDeviceFromHistory(device.Address);
			_historyDevices.Remove(device);
			HistoryEmptyLabel.IsVisible = _historyDevices.Count == 0;
		}
	}

	private async Task ShowDeviceScanDialog()
	{
		bool heartRateEnabled = SettingsService.GetHeartRateBroadcastEnabled();
		if (!heartRateEnabled)
		{
			await DisplayAlertAsync("提示", "请先开启心率广播功能", "确定");
			return;
		}

		bool hasPermission = await CheckBluetoothPermission();
		if (!hasPermission)
		{
			return;
		}

		_discoveredDevices.Clear();
		_selectedDevice = null;
		ScanningStatusLabel.Text = "正在搜索...";

		DeviceDialogOverlay.IsVisible = true;

		bool success = await HeartRateService.StartScanForDevicesAsync();
		if (!success)
		{
			ScanningStatusLabel.Text = "搜索失败";
		}
	}

	private void OnDeviceDialogCancelClicked(object? sender, EventArgs e)
	{
		HeartRateService.StopScanForDevices();
		DeviceDialogOverlay.IsVisible = false;
	}

	private async void OnDeviceDialogConfirmClicked(object? sender, EventArgs e)
	{
		HeartRateService.StopScanForDevices();

		if (_selectedDevice == null)
		{
			await DisplayAlertAsync("提示", "请选择一个设备", "确定");
			return;
		}

		DeviceDialogOverlay.IsVisible = false;
		HeartRateStatusLabel.Text = $"正在连接 {_selectedDevice.Name}...";

		bool success = HeartRateService.ConnectToDevice(_selectedDevice);
		if (!success)
		{
			await DisplayAlertAsync("提示", "连接设备失败", "确定");
			HeartRateStatusLabel.Text = "已开启";
		}
	}

	private void OnDeviceListViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is BleDeviceInfo device)
		{
			foreach (var d in _discoveredDevices)
			{
				d.IsSelected = d.Address == device.Address;
			}
			_selectedDevice = device;
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
		HeartRateService.ConnectionStateChanged -= OnHeartRateConnectionStateChanged;
		HeartRateService.DeviceDiscovered -= OnDeviceDiscovered;
		HeartRateService.ScanError -= OnScanError;
	}
}