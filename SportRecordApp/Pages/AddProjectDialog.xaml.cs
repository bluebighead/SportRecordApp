namespace SportRecordApp.Pages;

public partial class AddProjectDialog : ContentPage
{
	public event EventHandler<(string ProjectName, string TargetTime, bool IsUnlimited)>? OnConfirm;
	public event EventHandler? OnCancel;

	private readonly List<string> _units = new() { "天" };
	private readonly List<string> _presets = new() { "无", "平板支撑", "哑铃弯举" };
	private bool _isProjectNameValid = true;
	private bool _isTargetTimeValid = true;
	private bool _isUnlimited = false;

	public AddProjectDialog()
	{
		InitializeComponent();
		UnitPicker.ItemsSource = _units;
		UnitPicker.SelectedIndex = 0;
		PresetPicker.ItemsSource = _presets;
		PresetPicker.SelectedIndex = 0;
	}

	private void OnUnlimitedCheckBoxChanged(object? sender, CheckedChangedEventArgs e)
	{
		_isUnlimited = e.Value;
		UpdateTimeInputState();
	}

	private void OnUnlimitedLabelTapped(object? sender, EventArgs e)
	{
		UnlimitedCheckBox.IsChecked = !UnlimitedCheckBox.IsChecked;
	}

	private void UpdateTimeInputState()
	{
		bool isEnabled = !_isUnlimited;
		
		TargetTimeEntry.IsEnabled = isEnabled;
		UnitPicker.IsEnabled = isEnabled;
		
		if (_isUnlimited)
		{
			TargetTimeBorder.BackgroundColor = Color.FromArgb("#E0E0E0");
			UnitPickerBorder.BackgroundColor = Color.FromArgb("#E0E0E0");
			_isTargetTimeValid = true;
		}
		else
		{
			TargetTimeBorder.BackgroundColor = Color.FromArgb("#F5F5F5");
			UnitPickerBorder.BackgroundColor = Color.FromArgb("#F5F5F5");
			_isTargetTimeValid = !string.IsNullOrWhiteSpace(TargetTimeEntry.Text) && double.TryParse(TargetTimeEntry.Text, out _);
		}
	}

	private void OnCancelClicked(object? sender, EventArgs e)
	{
		OnCancel?.Invoke(this, EventArgs.Empty);
	}

	private void OnConfirmClicked(object? sender, EventArgs e)
	{
		string projectName = ProjectNameEntry.Text ?? string.Empty;
		string targetTimeValue = TargetTimeEntry.Text ?? string.Empty;
		string selectedUnit = UnitPicker.SelectedItem?.ToString() ?? string.Empty;

		_isProjectNameValid = !string.IsNullOrWhiteSpace(projectName);
		
		if (!_isUnlimited)
		{
			_isTargetTimeValid = !string.IsNullOrWhiteSpace(targetTimeValue) && double.TryParse(targetTimeValue, out _);
		}

		UpdateInputBorders();

		if (!_isProjectNameValid || !_isTargetTimeValid)
		{
			return;
		}

		string targetTime = _isUnlimited ? "无限" : $"{targetTimeValue}{selectedUnit}";
		OnConfirm?.Invoke(this, (projectName, targetTime, _isUnlimited));
	}

	private void OnProjectNameTextChanged(object? sender, TextChangedEventArgs e)
	{
		if (!_isProjectNameValid)
		{
			_isProjectNameValid = !string.IsNullOrWhiteSpace(e.NewTextValue);
			UpdateInputBorders();
		}
	}

	private async void OnTargetTimeTextChanged(object? sender, TextChangedEventArgs e)
	{
		string newText = e.NewTextValue ?? string.Empty;

		if (!string.IsNullOrWhiteSpace(newText))
		{
			if (!double.TryParse(newText, out _))
			{
				TargetTimeEntry.Text = string.Empty;
				await DisplayAlertAsync("提示", "请输入正确的时间", "确定");
				_isTargetTimeValid = false;
			}
			else
			{
				_isTargetTimeValid = true;
			}
		}
		else
		{
			_isTargetTimeValid = false;
		}

		UpdateInputBorders();
	}

	private void UpdateInputBorders()
	{
		ProjectNameBorder.BackgroundColor = _isProjectNameValid ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#FFCCCC");
		TargetTimeBorder.BackgroundColor = _isTargetTimeValid ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#FFCCCC");
	}

	private void OnPresetPickerSelectedIndexChanged(object? sender, EventArgs e)
	{
		if (PresetPicker.SelectedItem == null) return;

		string selectedPreset = PresetPicker.SelectedItem.ToString() ?? "无";

		if (selectedPreset == "平板支撑")
		{
			ProjectNameEntry.Text = "平板支撑";
			ProjectNameEntry.IsEnabled = false;
			ProjectNameBorder.BackgroundColor = Color.FromArgb("#E0E0E0");
			_isProjectNameValid = true;
		}
		else if (selectedPreset == "哑铃弯举")
		{
			ProjectNameEntry.Text = "哑铃弯举";
			ProjectNameEntry.IsEnabled = false;
			ProjectNameBorder.BackgroundColor = Color.FromArgb("#E0E0E0");
			_isProjectNameValid = true;
		}
		else
		{
			ProjectNameEntry.Text = string.Empty;
			ProjectNameEntry.IsEnabled = true;
			ProjectNameBorder.BackgroundColor = Color.FromArgb("#F5F5F5");
		}
	}
}
