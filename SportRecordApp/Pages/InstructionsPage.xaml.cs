using Microsoft.Maui.Controls;

namespace SportRecordApp.Pages
{
    public partial class InstructionsPage : ContentPage
    {
        public InstructionsPage()
        {
            InitializeComponent();
        }

        private void OnGotItClicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }
    }
}