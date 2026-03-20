using Android.Content;
using Android.Provider;
using Android.Content.PM;
using Microsoft.Maui.ApplicationModel;
using Android.App;
using Java.Util;

namespace SportRecordApp.Platforms.Android;

public static class CalendarHelper
{
    public static async Task<bool> RequestCalendarPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.CalendarRead>();
        
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.CalendarRead>();
        }
        
        if (status != PermissionStatus.Granted)
        {
            return false;
        }
        
        status = await Permissions.CheckStatusAsync<Permissions.CalendarWrite>();
        
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.CalendarWrite>();
        }
        
        return status == PermissionStatus.Granted;
    }

    public static async Task<bool> AddCalendarEventAsync(string title, string description, DateTime startDate, int totalDays)
    {
        try
        {
            bool hasPermission = await RequestCalendarPermissionAsync();
            if (!hasPermission)
            {
                return false;
            }

            Context? context = null;
            
            var activity = Platform.CurrentActivity;
            if (activity != null)
            {
                context = activity.ApplicationContext;
            }
            
            if (context == null)
            {
                context = global::Android.App.Application.Context;
            }
            
            if (context == null)
            {
                return false;
            }
            
            var values = new ContentValues();
            values.Put(CalendarContract.Events.InterfaceConsts.CalendarId, GetDefaultCalendarId(context));
            values.Put(CalendarContract.Events.InterfaceConsts.Title, title);
            values.Put(CalendarContract.Events.InterfaceConsts.Description, description);
            values.Put(CalendarContract.Events.InterfaceConsts.EventTimezone, Java.Util.TimeZone.Default?.ID ?? "Asia/Shanghai");
            
            var startMillis = GetMilliseconds(startDate);
            var endDate = startDate.AddDays(totalDays);
            var endMillis = GetMilliseconds(endDate);
            
            values.Put(CalendarContract.Events.InterfaceConsts.Dtstart, startMillis);
            values.Put(CalendarContract.Events.InterfaceConsts.Dtend, endMillis);
            values.Put(CalendarContract.Events.InterfaceConsts.AllDay, 1);
            
            var uri = context.ContentResolver?.Insert(CalendarContract.Events.ContentUri, values);
            
            return uri != null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"添加日历事件失败: {ex.Message}");
            return false;
        }
    }

    private static long GetDefaultCalendarId(Context context)
    {
        var uri = CalendarContract.Calendars.ContentUri;
        string[] projection = { 
            CalendarContract.Calendars.InterfaceConsts.Id,
            CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName
        };
        
        using var cursor = context.ContentResolver?.Query(uri, projection, null, null, null);
        
        if (cursor != null && cursor.MoveToFirst())
        {
            int idIndex = cursor.GetColumnIndex(CalendarContract.Calendars.InterfaceConsts.Id);
            if (idIndex >= 0)
            {
                return cursor.GetLong(idIndex);
            }
        }
        
        return 1;
    }

    private static long GetMilliseconds(DateTime date)
    {
        var calendar = Calendar.GetInstance(Java.Util.TimeZone.Default);
        calendar.Set(CalendarField.Year, date.Year);
        calendar.Set(CalendarField.Month, date.Month - 1);
        calendar.Set(CalendarField.DayOfMonth, date.Day);
        calendar.Set(CalendarField.HourOfDay, 0);
        calendar.Set(CalendarField.Minute, 0);
        calendar.Set(CalendarField.Second, 0);
        calendar.Set(CalendarField.Millisecond, 0);
        return calendar.TimeInMillis;
    }
}
