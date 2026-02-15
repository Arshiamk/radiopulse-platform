using RadioPulse.Mobile.Services;

namespace RadioPulse.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly RadioApiService apiService;

    public LoginPage()
    {
        InitializeComponent();
        apiService = ServiceHelper.GetService<RadioApiService>();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var ok = await apiService.LoginDevAsync(CancellationToken.None);
        StatusLabel.Text = ok ? "Logged in" : "Login failed";
    }
}
