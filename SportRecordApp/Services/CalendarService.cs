namespace SportRecordApp.Services;

public static class CalendarService
{
    public static async Task<bool> RequestCalendarPermissionAsync()
    {
#if ANDROID
        return await Platforms.Android.CalendarHelper.RequestCalendarPermissionAsync();
#else
        return await Task.FromResult(false);
#endif
    }

    public static async Task<bool> AddCalendarEventAsync(string title, string description, DateTime startDate, int totalDays)
    {
#if ANDROID
        return await Platforms.Android.CalendarHelper.AddCalendarEventAsync(title, description, startDate, totalDays);
#else
        return await Task.FromResult(false);
#endif
    }

    public static async Task<bool> CreateReminderEventAsync(string projectName, string targetTime)
    {
        var hasPermission = await RequestCalendarPermissionAsync();
        if (!hasPermission)
        {
            throw new Exception("未获取日历权限");
        }

        int totalDays = ParseTargetDays(targetTime);
        if (totalDays <= 0)
        {
            throw new Exception("无效的目标天数");
        }

        string title = $"【{projectName}】打卡提醒";
        string description = $"项目: {projectName}\n目标: {targetTime}\n请记得打卡！";
        
        return await AddCalendarEventAsync(title, description, DateTime.Today, totalDays);
    }

    private static int ParseTargetDays(string targetTime)
    {
        if (string.IsNullOrEmpty(targetTime) || targetTime == "无限")
        {
            return 30;
        }

        try
        {
            var match = System.Text.RegularExpressions.Regex.Match(targetTime, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int days))
            {
                return days;
            }
        }
        catch
        {
        }
        
        return 30;
    }
}
