using RadioPulse.Mobile.Services;

namespace RadioPulse.Mobile.Pages;

public partial class NowPlayingPage : ContentPage
{
    private readonly RadioApiService apiService;
    private readonly SessionState sessionState;

    public NowPlayingPage()
    {
        InitializeComponent();
        apiService = ServiceHelper.GetService<RadioApiService>();
        sessionState = ServiceHelper.GetService<SessionState>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var nowPlaying = await apiService.GetNowPlayingAsync(CancellationToken.None);
            TitleLabel.Text = nowPlaying?.Title ?? $"Unavailable ({sessionState.ApiBaseUrl})";
        }
        catch (Exception ex)
        {
            TitleLabel.Text = $"Error: {ex.Message}";
        }
    }
}
