using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using Android.Views;
using System.Text.Json;
using SportRecordApp.Models;
using SportRecordApp.Services;
using System.IO;
using Microsoft.Maui.Storage;
using Android.OS;

namespace SportRecordApp.Platforms.Android;

[BroadcastReceiver(Exported = true)]
[IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
[MetaData("android.appwidget.provider", Resource = "@xml/checkin_widget_large_info")]
public class CheckInWidgetLargeProvider : AppWidgetProvider
{
    private const string ActionCheckIn = "com.companyname.sportrecordapp.CHECKIN_LARGE";
    private const string ActionCheckInAll = "com.companyname.sportrecordapp.CHECKIN_ALL_LARGE";
    private const string ActionRefresh = "com.companyname.sportrecordapp.REFRESH_LARGE";
    private const string ActionAutoRefresh = "com.companyname.sportrecordapp.AUTO_REFRESH_LARGE";
    private const long RefreshInterval = 2000; // 2秒

    public override void OnEnabled(Context? context)
    {
        base.OnEnabled(context);
        if (context != null)
        {
            StartAutoRefresh(context);
        }
    }

    public override void OnDisabled(Context? context)
    {
        base.OnDisabled(context);
        if (context != null)
        {
            StopAutoRefresh(context);
        }
    }

    private void StartAutoRefresh(Context context)
    {
        var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager != null)
        {
            var intent = new Intent(context, typeof(CheckInWidgetLargeProvider));
            intent.SetAction(ActionAutoRefresh);
            var pendingIntent = PendingIntent.GetBroadcast(context, 2, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            
            alarmManager.SetRepeating(
                AlarmType.Rtc,
                Java.Lang.JavaSystem.CurrentTimeMillis() + RefreshInterval,
                RefreshInterval,
                pendingIntent);
        }
    }

    private void StopAutoRefresh(Context context)
    {
        var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager != null)
        {
            var intent = new Intent(context, typeof(CheckInWidgetLargeProvider));
            intent.SetAction(ActionAutoRefresh);
            var pendingIntent = PendingIntent.GetBroadcast(context, 2, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            alarmManager.Cancel(pendingIntent);
        }
    }

    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context == null || appWidgetManager == null || appWidgetIds == null) return;
        
        foreach (int appWidgetId in appWidgetIds)
        {
            UpdateWidget(context, appWidgetManager, appWidgetId);
        }
    }

    private void UpdateWidget(Context context, AppWidgetManager appWidgetManager, int appWidgetId)
    {
        var views = new RemoteViews(context.PackageName ?? "", Resource.Layout.checkin_widget_large);
        
        var projects = LoadProjects(context);
        int totalCheckIns = projects.Sum(p => p.CheckInTimes.Count);
        
        views.SetTextViewText(Resource.Id.widget_total_count, $"共 {totalCheckIns} 次打卡");
        
        int[] projectRowIds = { Resource.Id.project_row_1, Resource.Id.project_row_2, Resource.Id.project_row_3, Resource.Id.project_row_4 };
        int[] projectNameIds = { Resource.Id.project_name_1, Resource.Id.project_name_2, Resource.Id.project_name_3, Resource.Id.project_name_4 };
        int[] projectCountIds = { Resource.Id.project_count_1, Resource.Id.project_count_2, Resource.Id.project_count_3, Resource.Id.project_count_4 };
        int[] projectButtonIds = { Resource.Id.project_button_1, Resource.Id.project_button_2, Resource.Id.project_button_3, Resource.Id.project_button_4 };
        
        for (int i = 0; i < 4; i++)
        {
            if (i < projects.Count)
            {
                var project = projects[i];
                views.SetTextViewText(projectNameIds[i], project.Name ?? "未命名");
                views.SetTextViewText(projectCountIds[i], $"{project.CheckInTimes.Count}次");
                views.SetViewVisibility(projectRowIds[i], ViewStates.Visible);
                
                var checkInIntent = new Intent(context, typeof(CheckInWidgetLargeProvider));
                checkInIntent.SetAction(ActionCheckIn);
                checkInIntent.PutExtra("appWidgetId", appWidgetId);
                checkInIntent.PutExtra("projectName", project.Name ?? "");
                checkInIntent.PutExtra("projectIndex", i);
                var checkInPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId * 10 + i, checkInIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(projectButtonIds[i], checkInPendingIntent);
            }
            else
            {
                views.SetViewVisibility(projectRowIds[i], ViewStates.Gone);
            }
        }
        
        if (projects.Count == 0)
        {
            views.SetViewVisibility(Resource.Id.widget_empty_text, ViewStates.Visible);
        }
        else
        {
            views.SetViewVisibility(Resource.Id.widget_empty_text, ViewStates.Gone);
        }
        
        var checkInAllIntent = new Intent(context, typeof(CheckInWidgetLargeProvider));
        checkInAllIntent.SetAction(ActionCheckInAll);
        checkInAllIntent.PutExtra("appWidgetId", appWidgetId);
        var checkInAllPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId + 5000, checkInAllIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        views.SetOnClickPendingIntent(Resource.Id.widget_checkin_all_button, checkInAllPendingIntent);
        
        var refreshIntent = new Intent(context, typeof(CheckInWidgetLargeProvider));
        refreshIntent.SetAction(ActionRefresh);
        refreshIntent.PutExtra("appWidgetId", appWidgetId);
        var refreshPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId + 7000, refreshIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        views.SetOnClickPendingIntent(Resource.Id.widget_refresh_button, refreshPendingIntent);
        
        var openAppIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? "");
        if (openAppIntent != null)
        {
            var openAppPendingIntent = PendingIntent.GetActivity(context, appWidgetId + 6000, openAppIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            views.SetOnClickPendingIntent(Resource.Id.widget_title, openAppPendingIntent);
        }
        
        appWidgetManager.UpdateAppWidget(appWidgetId, views);
    }

    public override void OnReceive(Context? context, Intent? intent)
    {
        base.OnReceive(context, intent);
        
        if (context == null || intent == null) return;
        
        if (intent.Action == ActionCheckIn)
        {
            var projectName = intent.GetStringExtra("projectName");
            var appWidgetId = intent.GetIntExtra("appWidgetId", AppWidgetManager.InvalidAppwidgetId);
            
            if (!string.IsNullOrEmpty(projectName) && appWidgetId != AppWidgetManager.InvalidAppwidgetId)
            {
                PerformCheckIn(context, projectName, appWidgetId);
            }
        }
        else if (intent.Action == ActionCheckInAll)
        {
            var appWidgetId = intent.GetIntExtra("appWidgetId", AppWidgetManager.InvalidAppwidgetId);
            if (appWidgetId != AppWidgetManager.InvalidAppwidgetId)
            {
                PerformCheckInAll(context, appWidgetId);
            }
        }
        else if (intent.Action == ActionRefresh)
        {
            var appWidgetId = intent.GetIntExtra("appWidgetId", AppWidgetManager.InvalidAppwidgetId);
            if (appWidgetId != AppWidgetManager.InvalidAppwidgetId)
            {
                var appWidgetManager = AppWidgetManager.GetInstance(context);
                if (appWidgetManager != null)
                {
                    UpdateWidget(context, appWidgetManager, appWidgetId);
                }
            }
        }
        else if (intent.Action == ActionAutoRefresh)
        {
            var appWidgetManager = AppWidgetManager.GetInstance(context);
            if (appWidgetManager != null)
            {
                var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(CheckInWidgetLargeProvider)));
                var appWidgetIds = appWidgetManager.GetAppWidgetIds(componentName);
                if (appWidgetIds != null && appWidgetIds.Length > 0)
                {
                    foreach (int appWidgetId in appWidgetIds)
                    {
                        UpdateWidget(context, appWidgetManager, appWidgetId);
                    }
                }
            }
        }
    }

    private void PerformCheckIn(Context context, string projectName, int appWidgetId)
    {
        var projects = LoadProjects(context);
        var project = projects.FirstOrDefault(p => p.Name == projectName);
        
        if (project != null)
        {
            var now = DateTime.Now;
            var today = now.ToString("yyyy年MM月dd日 HH:mm:ss");
            
            if (SettingsService.GetDailyCheckInLimit())
            {
                var todayDate = now.ToString("yyyy年MM月dd日");
                if (project.CheckInTimes.Any(t => t.StartsWith(todayDate)))
                {
                    ShowToast(context, $"{projectName} 今天已经打卡过了！");
                    return;
                }
            }
            
            project.CheckInTimes.Add(today);
            SaveProjects(context, projects);
            
            ShowToast(context, $"打卡成功！{projectName}");
            
            var appWidgetManager = AppWidgetManager.GetInstance(context);
            if (appWidgetManager != null)
            {
                UpdateWidget(context, appWidgetManager, appWidgetId);
            }
        }
    }

    private void PerformCheckInAll(Context context, int appWidgetId)
    {
        var projects = LoadProjects(context);
        var now = DateTime.Now;
        var todayDate = now.ToString("yyyy年MM月dd日");
        int checkedCount = 0;
        int skippedCount = 0;
        
        foreach (var project in projects)
        {
            if (SettingsService.GetDailyCheckInLimit())
            {
                if (project.CheckInTimes.Any(t => t.StartsWith(todayDate)))
                {
                    skippedCount++;
                    continue;
                }
            }
            
            var today = now.ToString("yyyy年MM月dd日 HH:mm:ss");
            project.CheckInTimes.Add(today);
            checkedCount++;
        }
        
        SaveProjects(context, projects);
        
        if (checkedCount > 0)
        {
            ShowToast(context, $"成功打卡 {checkedCount} 个项目！");
        }
        if (skippedCount > 0)
        {
            ShowToast(context, $"{skippedCount} 个项目今天已打卡");
        }
        
        var appWidgetManager = AppWidgetManager.GetInstance(context);
        if (appWidgetManager != null)
        {
            UpdateWidget(context, appWidgetManager, appWidgetId);
        }
    }

    private void ShowToast(Context context, string message)
    {
        var handler = new Handler(Looper.MainLooper ?? Looper.MyLooper()!);
        handler.Post(() =>
        {
            Toast.MakeText(context, message, ToastLength.Short)?.Show();
        });
    }

    private List<SportProject> LoadProjects(Context context)
    {
        try
        {
            var dataFile = Path.Combine(FileSystem.AppDataDirectory, "sport_projects.json");
            if (File.Exists(dataFile))
            {
                var json = File.ReadAllText(dataFile);
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var container = JsonSerializer.Deserialize<DataContainer>(json);
                        if (container != null)
                        {
                            return container.Projects;
                        }
                    }
                    catch
                    {
                        var projects = JsonSerializer.Deserialize<List<SportProject>>(json);
                        if (projects != null)
                        {
                            return projects;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载项目失败: {ex.Message}");
        }
        return new List<SportProject>();
    }

    private void SaveProjects(Context context, List<SportProject> projects)
    {
        try
        {
            var dataFile = Path.Combine(FileSystem.AppDataDirectory, "sport_projects.json");
            var container = new DataContainer { Projects = projects };
            var json = JsonSerializer.Serialize(container);
            File.WriteAllText(dataFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存项目失败: {ex.Message}");
        }
    }

    private class DataContainer
    {
        public string Version { get; set; } = "1.0";
        public List<SportProject> Projects { get; set; } = new List<SportProject>();
    }
}
