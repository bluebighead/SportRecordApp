using SportRecordApp.Models;
using System.IO;
using System.Collections.Generic;
using Microsoft.Maui.Controls.Shapes;

namespace SportRecordApp.Pages;

public partial class CheckInDetailPage : ContentPage
{
	private SportProject? _project;
	private bool _isCalendarView = false;
	private HashSet<string> _checkedDates = new HashSet<string>();
	private int _currentYear;
	private int _currentMonth;

	public CheckInDetailPage(SportProject project)
	{
		InitializeComponent();
		_project = project;
		// 初始化当前年月
		DateTime now = DateTime.Now;
		_currentYear = now.Year;
		_currentMonth = now.Month;
		UpdateUI();
		ExtractCheckedDates();
		LoadViewState();
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

	private void ExtractCheckedDates()
	{
		if (_project == null) return;
		
		_checkedDates.Clear();
		if (_project.CheckInTimes != null)
		{
			foreach (var checkInTime in _project.CheckInTimes)
			{
				// 从打卡时间中提取日期部分（格式：2026年03月20日）
				if (checkInTime != null && checkInTime.Length >= 11)
				{
					// 确保日期格式一致（例如：2026年03月20日）
					string datePart = checkInTime.Substring(0, 11);
					_checkedDates.Add(datePart);
				}
			}
		}
	}

	private void LoadViewState()
	{
		// 加载视图状态
		if (Preferences.ContainsKey($"view_state_{_project?.Name}"))
		{
			_isCalendarView = Preferences.Get($"view_state_{_project?.Name}", false);
			if (_isCalendarView)
			{
				CheckInTimesList.IsVisible = false;
				CalendarView.IsVisible = true;
				ViewToggleButton.Text = "切换到列表视图";
				ChartButton.IsVisible = false;
				GenerateCalendar();
			}
			else
			{
				CheckInTimesList.IsVisible = true;
				CalendarView.IsVisible = false;
				ViewToggleButton.Text = "切换到日历视图";
				ChartButton.IsVisible = _project?.Name == "平板支撑";
			}
		}
		else
		{
			// 默认显示列表视图
			CheckInTimesList.IsVisible = true;
			CalendarView.IsVisible = false;
			ViewToggleButton.Text = "切换到日历视图";
			ChartButton.IsVisible = _project?.Name == "平板支撑";
		}
	}

	private void SaveViewState()
	{
		// 保存视图状态
		if (_project != null)
		{
			Preferences.Set($"view_state_{_project.Name}", _isCalendarView);
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}

	private async void OnShareClicked(object? sender, EventArgs e)
	{
		string action = await DisplayActionSheetAsync("分享", "取消", null, "导出为CSV文件", "分享打卡截图");
		
		if (action == "导出为CSV文件")
		{
			await ExportToCsv();
		}
		else if (action == "分享打卡截图")
		{
			await ShareScreenshot();
		}
	}

	private async void OnViewToggleClicked(object? sender, EventArgs e)
	{
		_isCalendarView = !_isCalendarView;
		
		if (_isCalendarView)
		{
			CheckInTimesList.IsVisible = false;
			CalendarView.IsVisible = true;
			ViewToggleButton.Text = "切换到列表视图";
			ChartButton.IsVisible = false;
			GenerateCalendar();
		}
		else
		{
			CheckInTimesList.IsVisible = true;
			CalendarView.IsVisible = false;
			ViewToggleButton.Text = "切换到日历视图";
			ChartButton.IsVisible = _project?.Name == "平板支撑";
		}
		
		// 保存视图状态
		SaveViewState();
	}

	private void OnPrevYearClicked(object? sender, EventArgs e)
	{
		_currentYear--;
		GenerateCalendar();
	}

	private void OnPrevMonthClicked(object? sender, EventArgs e)
	{
		_currentMonth--;
		if (_currentMonth < 1)
		{
			_currentMonth = 12;
			_currentYear--;
		}
		GenerateCalendar();
	}

	private void OnNextMonthClicked(object? sender, EventArgs e)
	{
		_currentMonth++;
		if (_currentMonth > 12)
		{
			_currentMonth = 1;
			_currentYear++;
		}
		GenerateCalendar();
	}

	private void OnNextYearClicked(object? sender, EventArgs e)
	{
		_currentYear++;
		GenerateCalendar();
	}

	private void GenerateCalendar()
	{
		if (_project == null) return;
		
		// 提取打卡日期
		ExtractCheckedDates();
		
		// 更新当前年月标签
		CurrentMonthYearLabel.Text = $"{_currentYear}年{_currentMonth}月";
		
		// 清空日历网格
		CalendarGrid.Children.Clear();
		
		// 获取月份的第一天和最后一天
		DateTime firstDay = new DateTime(_currentYear, _currentMonth, 1);
		DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);
		
		// 获取第一天是星期几（0-6，0是星期日）
		int firstDayOfWeek = (int)firstDay.DayOfWeek;
		
		// 生成日历单元格
		int day = 1;
		int row = 1;
		
		for (int i = 0; i < 42; i++) // 6行7列
		{
			int col = i % 7;
			
			if (i < firstDayOfWeek || day > lastDay.Day)
			{
				// 空单元格
				Label emptyLabel = new Label
				{
					Text = "",
					HorizontalOptions = LayoutOptions.Center
				};
				Grid.SetRow(emptyLabel, row);
				Grid.SetColumn(emptyLabel, col);
				CalendarGrid.Children.Add(emptyLabel);
			}
			else
			{
				// 日期单元格 - 确保格式与打卡时间中的日期格式一致
				string dateStr = $"{_currentYear}年{_currentMonth.ToString("00")}月{day.ToString("00")}日";
				bool isChecked = _checkedDates.Contains(dateStr);
				
				Label dayLabel = new Label
				{
					Text = day.ToString(),
					FontSize = 16,
					TextColor = isChecked ? Colors.Black : Colors.White,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				};
				
				Border dayBorder = new Border
				{
					BackgroundColor = isChecked ? Colors.LightGreen : Colors.Transparent,
					Stroke = Colors.White,
					StrokeThickness = 1,
					StrokeShape = new RoundRectangle { CornerRadius = 8 },
					Padding = new Thickness(10),
					Margin = new Thickness(2)
				};
				dayBorder.Content = dayLabel;
				
				Grid.SetRow(dayBorder, row);
				Grid.SetColumn(dayBorder, col);
				CalendarGrid.Children.Add(dayBorder);
				
				day++;
			}
			
			if (col == 6)
			{
				row++;
			}
		}
	}

	private async Task ExportToCsv()
	{
		if (_project == null) return;

		try
		{
			var fileName = $"{_project.Name}_打卡记录_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
			var filePath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);

			using (var writer = new StreamWriter(filePath))
			{
				// 写入标题行
				writer.WriteLine("项目名称,目标时间,已打卡天数,完成进度");
				// 写入项目信息
				writer.WriteLine($"{_project.Name},{_project.TargetTime},{_project.CheckInDays},{_project.CompletionRate}");
				writer.WriteLine();
				writer.WriteLine("打卡时间记录");
				
				// 检查是否为平板支撑项目
				bool isPlankSupport = _project.Name == "平板支撑";
				
				if (isPlankSupport)
				{
					// 平板支撑项目添加表头
					writer.WriteLine("日期,当日具体打卡时间,支撑时间");
					
					// 写入打卡记录，拆分时间信息
					foreach (var time in _project.CheckInTimes)
					{
						// 解析打卡时间格式：2026年03月21日 17:50:00 (00:01:30)
						string date = "";
						string timeOfDay = "";
						string plankTime = "";
						
						if (!string.IsNullOrEmpty(time))
						{
							// 提取日期部分（前11个字符）
							if (time.Length >= 11)
							{
								date = time.Substring(0, 11);
							}
							
							// 提取具体时间部分
							int timeStartIndex = 12;
							int timeEndIndex = time.IndexOf(" (");
							if (timeEndIndex > timeStartIndex)
							{
								timeOfDay = time.Substring(timeStartIndex, timeEndIndex - timeStartIndex);
							}
							
							// 提取支撑时间部分
							int plankStartIndex = time.IndexOf("(") + 1;
							int plankEndIndex = time.IndexOf(")");
							if (plankEndIndex > plankStartIndex)
							{
								plankTime = time.Substring(plankStartIndex, plankEndIndex - plankStartIndex);
							}
						}
						
						writer.WriteLine($"{date},{timeOfDay},{plankTime}");
					}
				}
				else
				{
					// 其他项目保持原有逻辑
					foreach (var time in _project.CheckInTimes)
					{
						writer.WriteLine(time);
					}
				}
			}

			await ShareFile(filePath, $"{_project.Name}打卡记录.csv");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("导出失败", $"导出CSV时出错: {ex.Message}", "确定");
		}
	}

	private async Task ShareScreenshot()
	{
		try
		{
			var screenshot = await Screenshot.Default.CaptureAsync();
			
			if (screenshot == null)
			{
				await DisplayAlertAsync("截图失败", "无法获取屏幕截图", "确定");
				return;
			}

			var fileName = $"{_project?.Name}_打卡截图_{DateTime.Now:yyyyMMdd_HHmmss}.png";
			var filePath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);

			using (var stream = await screenshot.OpenReadAsync())
			using (var fileStream = File.Create(filePath))
			{
				await stream.CopyToAsync(fileStream);
			}

			await ShareFile(filePath, fileName);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("分享失败", $"分享截图时出错: {ex.Message}", "确定");
		}
	}

	private async Task ShareFile(string filePath, string title)
	{
		try
		{
			var file = new ShareFile(filePath);
			var request = new ShareFileRequest
			{
				Title = title,
				File = file
			};
			
			await Share.Default.RequestAsync(request);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("分享失败", $"分享文件时出错: {ex.Message}", "确定");
		}
	}

	private async void OnChartButtonClicked(object? sender, EventArgs e)
	{
		if (_project != null)
		{
			await Navigation.PushAsync(new ChartPage(_project));
		}
	}
}
