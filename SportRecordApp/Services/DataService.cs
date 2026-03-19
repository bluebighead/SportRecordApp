using System.Text.Json;
using SportRecordApp.Models;

namespace SportRecordApp.Services;

public static class DataService
{
    private const string ProjectsKey = "sport_projects";

    public static void SaveProjects(List<SportProject> projects)
    {
        try
        {
            string json = JsonSerializer.Serialize(projects);
            Preferences.Set(ProjectsKey, json);
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
            string json = Preferences.Get(ProjectsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<List<SportProject>>(json) ?? new List<SportProject>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载数据失败: {ex.Message}");
        }
        return new List<SportProject>();
    }
}
