using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace SportRecordApp.Platforms.Android
{
    public class NotificationHelper
    {
        private static readonly string CHANNEL_ID = "DrinkWaterReminderChannel";
        private static readonly string CHANNEL_NAME = "定时喝水提醒";
        private static readonly string CHANNEL_DESCRIPTION = "定时喝水提醒通知";

        public static void CreateNotificationChannel(Activity activity)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.High)
                {
                    Description = CHANNEL_DESCRIPTION
                };
                channel.EnableLights(true);
                channel.EnableVibration(true);
                channel.SetVibrationPattern(new long[] { 100, 200, 300, 400, 500 });
                var notificationManager = activity.GetSystemService(Context.NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        public static void ShowNotification(string title, string subtitle, string body)
        {
            var context = global::Android.App.Application.Context;
            if (context == null) return;

            var intent = new Intent(context, typeof(SportRecordApp.MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.Immutable);

            var builder = new Notification.Builder(context)
                .SetSmallIcon(Resource.Drawable.applogo)
                .SetContentTitle(title)
                .SetContentText(body)
                .SetContentIntent(pendingIntent)
                .SetAutoCancel(true)
                .SetPriority((int)NotificationPriority.High)
                .SetDefaults(NotificationDefaults.All);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                builder.SetChannelId(CHANNEL_ID);
            }

            var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
            notificationManager?.Notify(1, builder.Build());
        }

        public static void ShowNotification(Activity activity, string title, string subtitle, string body)
        {
            ShowNotification(title, subtitle, body);
        }

        public static bool CheckNotificationPermission(Activity activity)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                return activity.CheckSelfPermission("android.permission.POST_NOTIFICATIONS") == Permission.Granted;
            }
            return true;
        }

        public static void RequestNotificationPermission(Activity activity, int requestCode)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                activity.RequestPermissions(new string[] { "android.permission.POST_NOTIFICATIONS" }, requestCode);
            }
        }
    }
}