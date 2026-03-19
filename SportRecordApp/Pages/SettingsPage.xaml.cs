using SportRecordApp.Services;

namespace SportRecordApp.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        DailyCheckInSwitch.IsToggled = SettingsService.GetDailyCheckInLimit();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateSwitchColor();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnDailyCheckInToggled(object? sender, ToggledEventArgs e)
    {
        SettingsService.SetDailyCheckInLimit(e.Value);
        UpdateSwitchColor();
    }

    private void UpdateSwitchColor()
    {
#if ANDROID
        var handler = DailyCheckInSwitch.Handler;
        if (handler != null && handler.PlatformView is AndroidX.AppCompat.Widget.SwitchCompat switchCompat)
        {
            if (DailyCheckInSwitch.IsToggled)
            {
                switchCompat.TrackTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.ParseColor("#81C784"));
            }
            else
            {
                switchCompat.TrackTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.ParseColor("#9E9E9E"));
            }
        }
#endif
    }
}