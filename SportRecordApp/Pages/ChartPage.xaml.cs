using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using SportRecordApp.Models;

namespace SportRecordApp.Pages;

public partial class ChartPage : ContentPage
{
    private SportProject _project;
    private List<(DateTime Date, TimeSpan Duration)> _chartData = new List<(DateTime Date, TimeSpan Duration)>();

    public ChartPage(SportProject project)
    {
        InitializeComponent();
        _project = project;
        try
        {
            LoadChartData();
            UpdateUI();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化图表页面失败: {ex.Message}");
            ChartInfoLabel.Text = "加载数据失败";
            ChartDataLabel.Text = "加载数据失败";
        }
    }

    private void LoadChartData()
    {
        _chartData = new List<(DateTime Date, TimeSpan Duration)>();

        try
        {
            if (_project != null && _project.CheckInTimes != null)
            {
                foreach (var time in _project.CheckInTimes)
                {
                    // 解析打卡时间格式：2026年03月21日 17:50:00 (00:01:30)
                    if (!string.IsNullOrEmpty(time))
                    {
                        try
                        {
                            // 提取日期部分
                            int dateEndIndex = time.IndexOf(' ');
                            if (dateEndIndex > 0 && dateEndIndex < time.Length)
                            {
                                string dateStr = time.Substring(0, dateEndIndex);
                                if (DateTime.TryParseExact(dateStr, "yyyy年MM月dd日", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                                {
                                    // 提取支撑时间部分
                                    int durationStartIndex = time.IndexOf('(') + 1;
                                    int durationEndIndex = time.IndexOf(')');
                                    if (durationStartIndex > 0 && durationEndIndex > durationStartIndex && durationEndIndex < time.Length)
                                    {
                                        string durationStr = time.Substring(durationStartIndex, durationEndIndex - durationStartIndex);
                                        if (TimeSpan.TryParse(durationStr, out TimeSpan duration))
                                        {
                                            _chartData.Add((date, duration));
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"解析打卡时间失败: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载图表数据失败: {ex.Message}");
        }
    }

    private void UpdateUI()
    {
        try
        {
            if (_project != null)
            {
                ProjectNameLabel.Text = $"{_project.Name} - 支撑时间趋势";
                GenerateChart();
            }
            else
            {
                ProjectNameLabel.Text = "支撑时间趋势";
                ChartInfoLabel.Text = "暂无数据";
                ChartDataLabel.Text = "暂无数据";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"更新UI失败: {ex.Message}");
            ChartInfoLabel.Text = "加载失败";
            ChartDataLabel.Text = "加载失败";
        }
    }

    private void GenerateChart()
    {
        try
        {
            if (_chartData == null || _chartData.Count == 0)
            {
                ChartInfoLabel.Text = "暂无数据";
                ChartDataLabel.Text = "暂无数据";
                return;
            }

            // 按日期排序
            var sortedData = _chartData.OrderBy(item => item.Date).ToList();

            // 生成文本格式的图表数据
            string chartText = "日期\t\t支撑时间\n";
            chartText += "------------------------------\n";
            
            foreach (var item in sortedData)
            {
                chartText += $"{item.Date.ToString("MM/dd")}\t\t{item.Duration.ToString(@"mm\:ss")}\n";
            }

            ChartDataLabel.Text = chartText;
            ChartInfoLabel.Text = $"共 {_chartData.Count} 条记录";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成图表失败: {ex.Message}");
            ChartInfoLabel.Text = "生成图表失败";
            ChartDataLabel.Text = "生成图表失败";
        }
    }

    private void OnBackClicked(object? sender, EventArgs e)
    {
        Navigation.PopAsync();
    }
}