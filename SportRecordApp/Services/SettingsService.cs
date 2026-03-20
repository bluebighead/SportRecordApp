using System.IO;
using System.Text.Json;

namespace SportRecordApp.Services;

public static class SettingsService
{
    private static readonly string _settingsFile = Path.Combine(FileSystem.AppDataDirectory, "settings.json");
    private static Settings _settings = new Settings();

    static SettingsService()
    {
        // 清除旧的设置文件，强制使用默认值
        try
        {
            if (File.Exists(_settingsFile))
            {
                File.Delete(_settingsFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清除旧设置失败: {ex.Message}");
        }
        
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
                // 默认设置：每天只可打卡一次默认为开启
                _settings = new Settings
                {
                    DailyCheckInLimit = true,
                    HasSeenInstructions = false
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
}

public class Settings
{
    public bool DailyCheckInLimit { get; set; }
    public bool HasSeenInstructions { get; set; }
}