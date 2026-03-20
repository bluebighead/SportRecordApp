using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace SportRecordApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public static bool ShowAddProjectDialog { get; set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        CheckIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        CheckIntent(intent);
    }

    private void CheckIntent(Intent? intent)
    {
        if (intent != null && intent.GetBooleanExtra("showAddProject", false))
        {
            ShowAddProjectDialog = true;
        }
    }
}
