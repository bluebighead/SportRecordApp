using System.Collections.ObjectModel;
using SportRecordApp.Models;
using SportRecordApp.Pages;
using SportRecordApp.Services;
using Microsoft.Maui.Devices;

namespace SportRecordApp;

public partial class MainPage : ContentPage
{
	private ObservableCollection<SportProject> _projects = new();

	public MainPage()
	{
		InitializeComponent();
		SizeChanged += OnSizeChanged;
		LoadData();
		ProjectList.ItemsSource = _projects;
		_projects.CollectionChanged += (s, e) => 
		{
			SaveData();
			UpdateNoProjectsLabel();
		};
		UpdateNoProjectsLabel();
	}

	private void LoadData()
	{
		var loadedProjects = DataService.LoadProjects();
		foreach (var project in loadedProjects)
		{
			_projects.Add(project);
		}
	}

	private void UpdateNoProjectsLabel()
	{
		NoProjectsLabel.IsVisible = _projects.Count == 0;
		ProjectList.IsVisible = _projects.Count > 0;
	}

	private void SaveData()
	{
		DataService.SaveProjects(_projects.ToList());
	}

	private void OnSizeChanged(object? sender, EventArgs e)
	{
		if (Width > 0 && Height > 0)
		{
			AdaptToScreen();
		}
	}

	private void AdaptToScreen()
	{
		double screenWidth = Width;
		double screenHeight = Height;

		double baseWidth = 360;
		double baseHeight = 640;

		double scaleFactor = Math.Min(screenWidth / baseWidth, screenHeight / baseHeight);

		double buttonSize = Math.Max(50, 60 * scaleFactor);
		double fontSize = Math.Max(24, 36 * scaleFactor);

		MenuButton.WidthRequest = buttonSize;
		MenuButton.HeightRequest = buttonSize;
		MenuButton.CornerRadius = (int)(buttonSize / 2);
		MenuButton.FontSize = fontSize;

		AddButton.WidthRequest = buttonSize;
		AddButton.HeightRequest = buttonSize;
		AddButton.CornerRadius = (int)(buttonSize / 2);
		AddButton.FontSize = fontSize;

		AbsoluteLayout.SetLayoutBounds(MenuButton, new Rect(0.08, 0.92, buttonSize, buttonSize));
		AbsoluteLayout.SetLayoutBounds(AddButton, new Rect(0.92, 0.92, buttonSize, buttonSize));
	}

	private async void OnAddButtonClicked(object? sender, EventArgs e)
	{
		var dialog = new AddProjectDialog();
		
		dialog.OnConfirm += (s, args) =>
		{
			if (!string.IsNullOrWhiteSpace(args.ProjectName) && !string.IsNullOrWhiteSpace(args.TargetTime))
			{
				var newProject = new SportProject
				{
					Name = args.ProjectName,
					TargetTime = args.TargetTime
				};
				_projects.Add(newProject);
			}
			Navigation.PopModalAsync();
		};

		dialog.OnCancel += (s, args) =>
		{
			Navigation.PopModalAsync();
		};

		await Navigation.PushModalAsync(dialog);
	}

	private async void OnProjectSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is SportProject project)
		{
			ProjectList.SelectedItem = null;
			await Shell.Current.Navigation.PushAsync(new ProjectDetailPage(project));
		}
	}

	private async void OnMenuClicked(object? sender, EventArgs e)
	{
		string action = await DisplayActionSheetAsync("菜单", "取消", null, "统一打卡", "设置", "关于");
		
		if (action == "统一打卡")
		{
			DoUnifiedCheckIn();
		}
		else if (action == "设置")
		{
			await Shell.Current.Navigation.PushAsync(new SettingsPage());
		}
		else if (action == "关于")
		{
			await Shell.Current.Navigation.PushAsync(new AboutPage());
		}
	}

	private async void OnMenuButtonClicked(object? sender, EventArgs e)
	{
		if (sender is Button button && button.BindingContext is SportProject project)
		{
			string action = await DisplayActionSheetAsync("项目操作", "取消", null, "编辑", "删除");
			
			switch (action)
			{
				case "编辑":
					await EditProject(project);
					break;
				case "删除":
					await DeleteProject(project);
					break;
			}
		}
	}

	private async void DoUnifiedCheckIn()
	{
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
		
		bool hasCompleted = false;
		bool hasCheckedInToday = false;
		string today = DateTime.Now.ToString("yyyy年MM月dd日");
		
		foreach (var project in _projects)
		{
			// 检查是否开启了每天只可打卡一次的功能
			if (SettingsService.GetDailyCheckInLimit())
			{
				// 检查今天是否已经打卡过
				bool projectCheckedInToday = project.CheckInTimes.Any(time => time.StartsWith(today));
				if (projectCheckedInToday)
				{
					hasCheckedInToday = true;
					continue; // 跳过今天已经打卡的项目
				}
			}
			
			bool wasCompleted = project.IsCompleted;
			int oldDays = project.CheckInDays;
			project.CheckInDays++;
			string checkInTime = DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒");
			project.CheckInTimes.Add(checkInTime);
			
			// 检查是否有项目首次完成
			try
			{
				if (project.IsCompleted && !wasCompleted)
				{
					hasCompleted = true;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"解析目标时间失败: {ex.Message}");
			}
		}
		
		SaveData();
		
		// 如果有项目今天已经打卡过，显示提示
		if (hasCheckedInToday)
		{
			await DisplayAlertAsync("提示", "部分项目今天已经打卡过了", "确定");
		}
		
		try
		{
			// 如果有项目完成，触发长震动
			if (hasCompleted && Vibration.Default.IsSupported)
			{
				Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"震动失败: {ex.Message}");
		}
	}

	private async void OnProjectLongPressed(object? sender, EventArgs e)
	{
		if (sender is Border border && border.BindingContext is SportProject project)
		{
			string action = await DisplayActionSheetAsync("项目操作", "取消", null, "删除", "编辑");
			
			switch (action)
			{
				case "删除":
					await DeleteProject(project);
					break;
				case "编辑":
					await EditProject(project);
					break;
			}
		}
	}

	private async Task DeleteProject(SportProject project)
	{
		bool confirm = await DisplayAlertAsync("删除项目", $"确定要删除项目 '{project.Name}' 吗？", "确定", "取消");
		if (confirm)
		{
			_projects.Remove(project);
			SaveData();
		}
	}

	private async Task EditProject(SportProject project)
	{
		// 创建编辑对话框
		var editDialog = new EditProjectDialog(project);
		editDialog.OnConfirm += (sender, updatedProject) =>
		{
			// 更新项目信息
			project.Name = updatedProject.ProjectName;
			project.TargetTime = updatedProject.TargetTime;
			SaveData();
		};
		
		await Navigation.PushModalAsync(editDialog);
	}
}
