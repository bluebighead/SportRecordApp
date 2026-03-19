namespace SportRecordApp.Pages;

public partial class AddProjectDialog : ContentPage
{
	public event EventHandler<(string ProjectName, string TargetTime)>? OnConfirm;
	public event EventHandler? OnCancel;

	private readonly List<string> _units = new() { "天" };
	private bool _isProjectNameValid = true;
	private bool _isTargetTimeValid = true;

	public AddProjectDialog()
	{
		InitializeComponent();
		UnitPicker.ItemsSource = _units;
		UnitPicker.SelectedIndex = 0;
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
		_isTargetTimeValid = !string.IsNullOrWhiteSpace(targetTimeValue) && double.TryParse(targetTimeValue, out _);

		UpdateInputBorders();

		if (!_isProjectNameValid || !_isTargetTimeValid)
		{
			return;
		}

		string targetTime = $"{targetTimeValue}{selectedUnit}";
		OnConfirm?.Invoke(this, (projectName, targetTime));
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
}
