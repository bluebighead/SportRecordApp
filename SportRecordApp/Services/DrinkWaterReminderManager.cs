using SportRecordApp.Models;

namespace SportRecordApp.Services
{
    public static class DrinkWaterReminderManager
    {
        private static System.Timers.Timer? _reminderTimer;
        private static bool _isReminderActive = false;
        private static bool _isProcessing = false;
        private static SportProject? _currentProject;
        private static string _projectName = string.Empty;
        private static int _reminderInterval = 7200000; // 默认2小时
        private static readonly object _lockObject = new object();

        public static bool IsReminderActive => _isReminderActive;
        public static string ProjectName => _projectName;
        public static int ReminderInterval
        {
            get => _reminderInterval;
            set
            {
                lock (_lockObject)
                {
                    _reminderInterval = value;
                    if (_isReminderActive && _reminderTimer != null)
                    {
                        _reminderTimer.Interval = _reminderInterval;
                    }
                }
            }
        }

        public static event EventHandler? ReminderStatusChanged;

        public static async Task StartReminder(SportProject project)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_isReminderActive)
                    {
                        return;
                    }

                    _currentProject = project;
                    _projectName = project.Name;
                    _isReminderActive = true;
                    _isProcessing = false;
                }

#if ANDROID
                try
                {
                    var context = Android.App.Application.Context;
                    if (context != null)
                    {
                        Platforms.Android.ReminderForegroundService.StartService(context);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"启动前台服务失败: {ex.Message}");
                }
#endif

                lock (_lockObject)
                {
                    _reminderTimer = new System.Timers.Timer(_reminderInterval);
                    _reminderTimer.AutoReset = true;
                    _reminderTimer.Elapsed += OnTimerElapsed;
                    _reminderTimer.Start();
                }

                ReminderStatusChanged?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动提醒失败: {ex.Message}");
                _isReminderActive = false;
            }
        }

        private static async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_lockObject)
            {
                if (_isProcessing || !_isReminderActive)
                {
                    return;
                }
                _isProcessing = true;
            }

            try
            {
                await VibrateAsync();
                SendNotification();
            }
            finally
            {
                lock (_lockObject)
                {
                    _isProcessing = false;
                }
            }
        }

        public static void StopReminder()
        {
            lock (_lockObject)
            {
                if (!_isReminderActive)
                {
                    return;
                }

                _isReminderActive = false;
                _isProcessing = false;

#if ANDROID
                try
                {
                    var context = Android.App.Application.Context;
                    if (context != null)
                    {
                        Platforms.Android.ReminderForegroundService.StopService(context);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"停止前台服务失败: {ex.Message}");
                }
#endif

                if (_reminderTimer != null)
                {
                    _reminderTimer.Stop();
                    _reminderTimer.Elapsed -= OnTimerElapsed;
                    _reminderTimer.Dispose();
                    _reminderTimer = null;
                }
            }

            ReminderStatusChanged?.Invoke(null, EventArgs.Empty);
        }

        private static async Task VibrateAsync()
        {
            try
            {
                if (Microsoft.Maui.Devices.Vibration.Default.IsSupported)
                {
                    await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Microsoft.Maui.Devices.Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"震动失败: {ex.Message}");
            }
        }

        private static void SendNotification()
        {
            try
            {
#if ANDROID
                Platforms.Android.NotificationHelper.ShowNotification(
                    "定时喝水提醒",
                    _projectName,
                    "该喝水了！保持水分摄入对健康很重要。"
                );
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送通知失败: {ex.Message}");
            }
        }
    }
}