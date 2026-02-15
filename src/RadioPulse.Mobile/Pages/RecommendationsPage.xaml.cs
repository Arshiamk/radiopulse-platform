using RadioPulse.Mobile.Services;

namespace RadioPulse.Mobile.Pages;

public partial class RecommendationsPage : ContentPage
{
    private readonly RadioApiService apiService;
    private readonly SessionState sessionState;

    public RecommendationsPage()
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
        if (!sessionState.IsAuthenticated)
        {
            RecommendationsList.ItemsSource = new[] { new RecommendationDto { StationName = "Login required", Score = 0f } };
            return;
        }

        try
        {
            RecommendationsList.ItemsSource = await apiService.GetRecommendationsAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            RecommendationsList.ItemsSource = new[] { new RecommendationDto { StationName = $"Error: {ex.Message}", Score = 0f } };
        }
    }
}
