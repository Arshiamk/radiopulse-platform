using RadioPulse.Mobile.Services;

namespace RadioPulse.Mobile.Pages;

public partial class RecommendationsPage : ContentPage
{
    private readonly RadioApiService apiService;

    public RecommendationsPage()
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
        RecommendationsList.ItemsSource = await apiService.GetRecommendationsAsync(CancellationToken.None);
    }
}
