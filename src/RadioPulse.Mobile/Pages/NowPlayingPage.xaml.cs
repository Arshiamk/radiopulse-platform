using RadioPulse.Mobile.Services;

namespace RadioPulse.Mobile.Pages;

public partial class NowPlayingPage : ContentPage
{
    private readonly RadioApiService apiService;

    public NowPlayingPage()
    {
        InitializeComponent();
        apiService = ServiceHelper.GetService<RadioApiService>();
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
        var nowPlaying = await apiService.GetNowPlayingAsync(CancellationToken.None);
        TitleLabel.Text = nowPlaying?.Title ?? "Unavailable";
    }
}
