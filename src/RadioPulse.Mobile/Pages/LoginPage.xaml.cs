using RadioPulse.Mobile.Services;

namespace RadioPulse.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly RadioApiService apiService;
    private readonly SessionState sessionState;

    public LoginPage()
    {
        InitializeComponent();
        apiService = ServiceHelper.GetService<RadioApiService>();
        sessionState = ServiceHelper.GetService<SessionState>();

        ApiBaseUrlEntry.Text = sessionState.ApiBaseUrl;
        UserIdEntry.Text = sessionState.UserId.ToString();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StatusLabel.Text = sessionState.IsAuthenticated
            ? $"Signed in as {sessionState.UserId}"
            : "Not signed in";
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        if (!Guid.TryParse(UserIdEntry.Text, out var userId))
        {
            StatusLabel.Text = "Invalid user GUID";
            return;
        }

        sessionState.UserId = userId;
        sessionState.SetApiBaseUrl(ApiBaseUrlEntry.Text ?? string.Empty);

        try
        {
            var ok = await apiService.LoginDevAsync(CancellationToken.None);
            StatusLabel.Text = ok
                ? $"Logged in to {sessionState.ApiBaseUrl}"
                : "Login failed";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Login error: {ex.Message}";
        }
    }

    private void OnSignOutClicked(object? sender, EventArgs e)
    {
        sessionState.SignOut();
        StatusLabel.Text = "Signed out";
    }
}
