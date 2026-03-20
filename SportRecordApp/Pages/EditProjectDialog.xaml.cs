using SportRecordApp.Models;

namespace SportRecordApp.Pages;

public partial class EditProjectDialog : ContentPage
{
    public event EventHandler<(string ProjectName, string TargetTime, bool IsUnlimited)>? OnConfirm;
    public event EventHandler? OnCancel;

    private readonly List<string> _units = new() { "天" };
    private bool _isProjectNameValid = true;
    private bool _isTargetTimeValid = true;
    private bool _isUnlimited = false;
    private readonly SportProject _project;

    public EditProjectDialog(SportProject project)
    {
        InitializeComponent();
        _project = project;
        
        // 初始化单位列表
        UnitPicker.ItemsSource = _units;
        UnitPicker.SelectedIndex = 0;
        
        // 填充现有项目信息
        ProjectNameEntry.Text = project.Name;
        
        // 设置无限模式状态
        _isUnlimited = project.IsUnlimited;
        UnlimitedCheckBox.IsChecked = _isUnlimited;
        
        // 解析目标时间
        string targetTime = project.TargetTime;
        if (!string.IsNullOrEmpty(targetTime) && targetTime != "无限")
        {
            // 提取数字部分
            string numberPart = new string(targetTime.Where(char.IsDigit).ToArray());
            TargetTimeEntry.Text = numberPart;
        }
        
        // 更新输入状态
        UpdateTimeInputState();
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
            TargetTimeBorder.Stroke = Color.FromArgb("#CCCCCC");
            UnitPickerBorder.BackgroundColor = Color.FromArgb("#E0E0E0");
            UnitPickerBorder.Stroke = Color.FromArgb("#CCCCCC");
            _isTargetTimeValid = true;
        }
        else
        {
            TargetTimeBorder.BackgroundColor = Color.FromArgb("#FFFFFF");
            TargetTimeBorder.Stroke = Color.FromArgb("#E0E0E0");
            UnitPickerBorder.BackgroundColor = Color.FromArgb("#FFFFFF");
            UnitPickerBorder.Stroke = Color.FromArgb("#E0E0E0");
            _isTargetTimeValid = !string.IsNullOrWhiteSpace(TargetTimeEntry.Text) && double.TryParse(TargetTimeEntry.Text, out _);
        }
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        OnCancel?.Invoke(this, EventArgs.Empty);
        Navigation.PopModalAsync();
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
        Navigation.PopModalAsync();
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
        TargetTimeBorder.BackgroundColor = _isTargetTimeValid ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#FFCCCC");
        TargetTimeBorder.Stroke = _isTargetTimeValid ? Color.FromArgb("#E0E0E0") : Color.FromArgb("#FF0000");
    }
}
