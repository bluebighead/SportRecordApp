using SportRecordApp.Models;

namespace SportRecordApp.Pages;

public partial class EditProjectDialog : ContentPage
{
    public event EventHandler<(string ProjectName, string TargetTime)>? OnConfirm;
    public event EventHandler? OnCancel;

    private readonly List<string> _units = new() { "天" };
    private bool _isProjectNameValid = true;
    private bool _isTargetTimeValid = true;
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
        
        // 解析目标时间
        string targetTime = project.TargetTime;
        if (!string.IsNullOrEmpty(targetTime))
        {
            // 提取数字部分
            string numberPart = new string(targetTime.Where(char.IsDigit).ToArray());
            TargetTimeEntry.Text = numberPart;
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
        _isTargetTimeValid = !string.IsNullOrWhiteSpace(targetTimeValue) && double.TryParse(targetTimeValue, out _);

        UpdateInputBorders();

        if (!_isProjectNameValid || !_isTargetTimeValid)
        {
            return;
        }

        string targetTime = $"{targetTimeValue}{selectedUnit}";
        OnConfirm?.Invoke(this, (projectName, targetTime));
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
