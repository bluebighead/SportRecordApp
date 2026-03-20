using System.Text.Json;
using SportRecordApp.Models;
using System.IO;
using System.Collections.Generic;
using Microsoft.Maui.Devices;
#if ANDROID
using Android.OS;
#endif

namespace SportRecordApp.Services;

public static class DataService
{
    private static readonly string _dataFile = Path.Combine(FileSystem.AppDataDirectory, "sport_projects.json");
    private static readonly string _backupDir = Path.Combine(FileSystem.AppDataDirectory, "backups");
    private const string DataVersion = "1.0";

    // 数据容器类，包含版本信息
    private class DataContainer
    {
        public string Version { get; set; } = DataVersion;
        public List<SportProject> Projects { get; set; } = new List<SportProject>();
    }

    static DataService()
    {
        // 确保备份目录存在
        if (!Directory.Exists(_backupDir))
        {
            Directory.CreateDirectory(_backupDir);
        }
    }

    public static void SaveProjects(List<SportProject> projects)
    {
        try
        {
            // 创建备份
            CreateBackup();
            
            // 包装数据
            var container = new DataContainer { Projects = projects };
            string json = JsonSerializer.Serialize(container);
            
            // 写入文件
            File.WriteAllText(_dataFile, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存数据失败: {ex.Message}");
        }
    }

    public static List<SportProject> LoadProjects()
    {
        try
        {
            if (File.Exists(_dataFile))
            {
                string json = File.ReadAllText(_dataFile);
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        // 尝试以容器格式读取
                        var container = JsonSerializer.Deserialize<DataContainer>(json);
                        if (container != null)
                        {
                            // 处理版本迁移
                            return ProcessDataMigration(container);
                        }
                    }
                    catch
                    {
                        // 兼容旧格式
                        try
                        {
                            var projects = JsonSerializer.Deserialize<List<SportProject>>(json);
                            if (projects != null)
                            {
                                // 转换为新格式并保存
                                SaveProjects(projects);
                                return projects;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"读取旧格式数据失败: {ex.Message}");
                            // 尝试从备份恢复
                            return RestoreFromBackup();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载数据失败: {ex.Message}");
            // 尝试从备份恢复
            return RestoreFromBackup();
        }
        return new List<SportProject>();
    }

    private static List<SportProject> ProcessDataMigration(DataContainer container)
    {
        // 这里可以添加版本迁移逻辑
        // 例如：if (container.Version == "0.1") { /* 迁移逻辑 */ }
        return container.Projects;
    }

    private static void CreateBackup()
    {
        try
        {
            if (File.Exists(_dataFile))
            {
                // 生成备份文件名
                string backupFileName = $"sport_projects_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string backupPath = Path.Combine(_backupDir, backupFileName);
                
                // 复制文件
                File.Copy(_dataFile, backupPath, true);
                
                // 清理旧备份（保留最近10个）
                CleanupOldBackups();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创建备份失败: {ex.Message}");
        }
    }

    private static void CleanupOldBackups()
    {
        try
        {
            var backupFiles = Directory.GetFiles(_backupDir, "sport_projects_*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();
            
            // 删除超过10个的旧备份
            for (int i = 10; i < backupFiles.Count; i++)
            {
                File.Delete(backupFiles[i]);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清理备份失败: {ex.Message}");
        }
    }

    private static List<SportProject> RestoreFromBackup()
    {
        try
        {
            // 获取最新的备份文件
            var backupFiles = Directory.GetFiles(_backupDir, "sport_projects_*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();
            
            if (backupFiles.Count > 0)
            {
                string latestBackup = backupFiles[0];
                string json = File.ReadAllText(latestBackup);
                
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
                    // 尝试以旧格式读取
                    var projects = JsonSerializer.Deserialize<List<SportProject>>(json);
                    if (projects != null)
                    {
                        return projects;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"从备份恢复失败: {ex.Message}");
        }
        return new List<SportProject>();
    }

    // 手动导出数据
    public static string ExportData()
    {
        try
        {
            string exportFileName = $"sport_data_export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string exportPath = Path.Combine(FileSystem.AppDataDirectory, exportFileName);
            
            if (File.Exists(_dataFile))
            {
                File.Copy(_dataFile, exportPath, true);
                
                // 对于 Android，尝试复制到公共文档目录
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    try
                    {
#if ANDROID
                        string publicDocsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(
                            Android.OS.Environment.DirectoryDocuments).AbsolutePath;
                        string publicExportPath = Path.Combine(publicDocsPath, exportFileName);
                        File.Copy(_dataFile, publicExportPath, true);
                        return publicExportPath;
#endif
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"复制到公共目录失败: {ex.Message}");
                        // 回退到应用内部存储
                    }
                }
                
                return exportPath;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"导出数据失败: {ex.Message}");
        }
        return string.Empty;
    }

    // 手动导入数据
    public static bool ImportData(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                
                try
                {
                    var container = JsonSerializer.Deserialize<DataContainer>(json);
                    if (container != null)
                    {
                        SaveProjects(container.Projects);
                        return true;
                    }
                }
                catch
                {
                    // 尝试以旧格式读取
                    var projects = JsonSerializer.Deserialize<List<SportProject>>(json);
                    if (projects != null)
                    {
                        SaveProjects(projects);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"导入数据失败: {ex.Message}");
        }
        return false;
    }
}
