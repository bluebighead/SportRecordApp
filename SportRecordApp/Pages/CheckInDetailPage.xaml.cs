using SportRecordApp.Models;

namespace SportRecordApp.Pages;

public partial class CheckInDetailPage : ContentPage
{
	private SportProject? _project;

	public CheckInDetailPage(SportProject project)
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
			CheckInDaysLabel.Text = $"已打卡 {_project.CheckInDays} 天";
			CheckInTimesList.ItemsSource = _project.CheckInTimes;
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.Navigation.PopAsync();
	}
}
