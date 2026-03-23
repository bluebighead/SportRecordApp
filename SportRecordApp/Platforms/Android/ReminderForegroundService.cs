using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;

namespace SportRecordApp.Platforms.Android
{
    [Service(Exported = false)]
    public class ReminderForegroundService : Service
    {
        private const int NOTIFICATION_ID = 1001;
        private const string CHANNEL_ID = "DrinkWaterReminderChannel";

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            try
            {
                CreateNotificationChannel();
                var notification = CreateNotification();
                StartForeground(NOTIFICATION_ID, notification);
                return StartCommandResult.Sticky;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"前台服务启动失败: {ex.Message}");
                return StartCommandResult.NotSticky;
            }
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(CHANNEL_ID, "定时喝水提醒", NotificationImportance.High)
                {
                    Description = "定时喝水提醒服务"
                };
                channel.EnableLights(true);
                channel.EnableVibration(true);
                var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        private Notification CreateNotification()
        {
            var intent = new Intent(this, typeof(SportRecordApp.MainActivity));
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable);

            var builder = new Notification.Builder(this, CHANNEL_ID)
                .SetSmallIcon(Resource.Drawable.applogo)
                .SetContentTitle("定时喝水提醒")
                .SetContentText("正在运行中...")
                .SetContentIntent(pendingIntent)
                .SetOngoing(true);

            return builder.Build();
        }

        public static void StartService(Context context)
        {
            var intent = new Intent(context, typeof(ReminderForegroundService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }

        public static void StopService(Context context)
        {
            var intent = new Intent(context, typeof(ReminderForegroundService));
            context.StopService(intent);
        }
    }
}