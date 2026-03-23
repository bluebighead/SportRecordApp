using System.IO;
using System.Text.Json;

namespace SportRecordApp.Services;

public static class SettingsService
{
    private static readonly string _settingsFile = Path.Combine(FileSystem.AppDataDirectory, "settings.json");
    private static Settings _settings = new Settings();

    static SettingsService()
    {
        LoadSettings();
    }

    private static void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFile))
            {
                string json = File.ReadAllText(_settingsFile);
                var settings = JsonSerializer.Deserialize<Settings>(json);
                if (settings != null)
                {
                    _settings = settings;
                }
            }
            else
            {
                // 默认设置：每天只可打卡一次默认为开启，回退打卡记录功能默认为关闭
                _settings = new Settings
                {
                    DailyCheckInLimit = true,
                    HasSeenInstructions = false,
                    AllowUndoCheckIn = false
                };
                SaveSettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载设置失败: {ex.Message}");
            // 使用默认设置
            _settings = new Settings
            {
                DailyCheckInLimit = true
            };
        }
    }

    private static void SaveSettings()
    {
        try
        {
            string json = JsonSerializer.Serialize(_settings);
            File.WriteAllText(_settingsFile, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存设置失败: {ex.Message}");
        }
    }

    public static bool GetDailyCheckInLimit()
    {
        return _settings.DailyCheckInLimit;
    }

    public static void SetDailyCheckInLimit(bool value)
    {
        _settings.DailyCheckInLimit = value;
        SaveSettings();
    }

    public static bool GetHasSeenInstructions()
    {
        return _settings.HasSeenInstructions;
    }

    public static void SetHasSeenInstructions(bool value)
    {
        _settings.HasSeenInstructions = value;
        SaveSettings();
    }

    public static bool GetAllowUndoCheckIn()
    {
        return _settings.AllowUndoCheckIn;
    }

    public static void SetAllowUndoCheckIn(bool value)
    {
        _settings.AllowUndoCheckIn = value;
        SaveSettings();
    }

    public static bool GetHeartRateBroadcastEnabled()
    {
        return _settings.HeartRateBroadcastEnabled;
    }

    public static void SetHeartRateBroadcastEnabled(bool value)
    {
        _settings.HeartRateBroadcastEnabled = value;
        SaveSettings();
    }

    public static string GetLastConnectedDeviceAddress()
    {
        return _settings.LastConnectedDeviceAddress;
    }

    public static void SetLastConnectedDeviceAddress(string value)
    {
        _settings.LastConnectedDeviceAddress = value;
        SaveSettings();
    }

    public static string GetLastConnectedDeviceName()
    {
        return _settings.LastConnectedDeviceName;
    }

    public static void SetLastConnectedDeviceName(string value)
    {
        _settings.LastConnectedDeviceName = value;
        SaveSettings();
    }

    public static void ClearLastConnectedDevice()
    {
        _settings.LastConnectedDeviceAddress = string.Empty;
        _settings.LastConnectedDeviceName = string.Empty;
        SaveSettings();
    }

    public static List<DeviceHistoryItem> GetDeviceHistory()
    {
        return _settings.DeviceHistory.OrderByDescending(d => d.LastConnectedTime).ToList();
    }

    public static void AddDeviceToHistory(string deviceName, string deviceAddress)
    {
        var existingDevice = _settings.DeviceHistory.FirstOrDefault(d => d.DeviceAddress == deviceAddress);
        if (existingDevice != null)
        {
            existingDevice.ConnectCount++;
            existingDevice.LastConnectedTime = DateTime.Now;
            existingDevice.DeviceName = deviceName;
        }
        else
        {
            _settings.DeviceHistory.Add(new DeviceHistoryItem
            {
                DeviceName = deviceName,
                DeviceAddress = deviceAddress,
                ConnectCount = 1,
                LastConnectedTime = DateTime.Now
            });
        }
        SaveSettings();
    }

    public static void RemoveDeviceFromHistory(string deviceAddress)
    {
        var device = _settings.DeviceHistory.FirstOrDefault(d => d.DeviceAddress == deviceAddress);
        if (device != null)
        {
            _settings.DeviceHistory.Remove(device);
            SaveSettings();
        }
    }

    public static void ClearDeviceHistory()
    {
        _settings.DeviceHistory.Clear();
        SaveSettings();
    }
}

public class Settings
{
    public bool DailyCheckInLimit { get; set; }
    public bool HasSeenInstructions { get; set; }
    public bool AllowUndoCheckIn { get; set; }
    public bool HeartRateBroadcastEnabled { get; set; }
    public string LastConnectedDeviceAddress { get; set; } = string.Empty;
    public string LastConnectedDeviceName { get; set; } = string.Empty;
    public List<DeviceHistoryItem> DeviceHistory { get; set; } = new();
}

public class DeviceHistoryItem
{
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceAddress { get; set; } = string.Empty;
    public int ConnectCount { get; set; } = 0;
    public DateTime LastConnectedTime { get; set; } = DateTime.Now;
}