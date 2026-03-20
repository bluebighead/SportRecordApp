using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using System.Text.Json;
using SportRecordApp.Models;
using SportRecordApp.Services;
using System.IO;
using Microsoft.Maui.Storage;
using Android.OS;

namespace SportRecordApp.Platforms.Android;

[BroadcastReceiver(Exported = true)]
[IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
[MetaData("android.appwidget.provider", Resource = "@xml/checkin_widget_small_info")]
public class CheckInWidgetSmallProvider : AppWidgetProvider
{
    private const string ActionCheckIn = "com.companyname.sportrecordapp.CHECKIN_SMALL";
    private const string ActionRefresh = "com.companyname.sportrecordapp.REFRESH_SMALL";
    private const string PreferencesKey = "widget_small_selected_project";

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
        var views = new RemoteViews(context.PackageName ?? "", Resource.Layout.checkin_widget_small);
        
        var projects = LoadProjects(context);
        var selectedProjectName = GetSelectedProject(context, appWidgetId);
        
        SportProject? selectedProject = null;
        
        if (!string.IsNullOrEmpty(selectedProjectName))
        {
            selectedProject = projects.FirstOrDefault(p => p.Name == selectedProjectName);
        }
        
        if (selectedProject == null && projects.Count > 0)
        {
            selectedProject = projects[0];
            SaveSelectedProject(context, appWidgetId, selectedProject.Name ?? "");
        }
        
        if (selectedProject != null)
        {
            views.SetTextViewText(Resource.Id.widget_project_name, selectedProject.Name ?? "未命名");
            
            var checkInIntent = new Intent(context, typeof(CheckInWidgetSmallProvider));
            checkInIntent.SetAction(ActionCheckIn);
            checkInIntent.PutExtra("appWidgetId", appWidgetId);
            checkInIntent.PutExtra("projectName", selectedProject.Name ?? "");
            var checkInPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, checkInIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            views.SetOnClickPendingIntent(Resource.Id.widget_checkin_button, checkInPendingIntent);
        }
        else
        {
            views.SetTextViewText(Resource.Id.widget_project_name, "暂无项目");
        }
        
        var openAppIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? "");
        if (openAppIntent != null)
        {
            var openAppPendingIntent = PendingIntent.GetActivity(context, appWidgetId + 2000, openAppIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            views.SetOnClickPendingIntent(Resource.Id.widget_container, openAppPendingIntent);
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
                    ShowToast(context, "今天已经打卡过了！");
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

    private string GetSelectedProject(Context context, int appWidgetId)
    {
        var prefs = context.GetSharedPreferences("widget_small_prefs", FileCreationMode.Private);
        return prefs?.GetString($"{PreferencesKey}_{appWidgetId}", string.Empty) ?? string.Empty;
    }

    private void SaveSelectedProject(Context context, int appWidgetId, string projectName)
    {
        var prefs = context.GetSharedPreferences("widget_small_prefs", FileCreationMode.Private);
        var editor = prefs?.Edit();
        editor?.PutString($"{PreferencesKey}_{appWidgetId}", projectName);
        editor?.Apply();
    }

    private class DataContainer
    {
        public string Version { get; set; } = "1.0";
        public List<SportProject> Projects { get; set; } = new List<SportProject>();
    }
}
