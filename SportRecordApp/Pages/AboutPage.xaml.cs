namespace SportRecordApp.Pages;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnInstructionsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new InstructionsPage());
    }
}