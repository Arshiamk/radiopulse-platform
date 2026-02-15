using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace RadioPulse.Mobile.Services;

public sealed class RadioApiService(SessionState sessionState)
{
    private readonly HttpClient httpClient = new();

    public async Task<bool> LoginDevAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<TokenResponse>($"{sessionState.ApiBaseUrl}/api/auth/dev-token/{sessionState.UserId}", cancellationToken);
        sessionState.AccessToken = response?.AccessToken ?? string.Empty;
        return !string.IsNullOrWhiteSpace(sessionState.AccessToken);
    }

    public Task<EpisodeDto?> GetNowPlayingAsync(CancellationToken cancellationToken)
    {
        return httpClient.GetFromJsonAsync<EpisodeDto>($"{sessionState.ApiBaseUrl}/api/now-playing", cancellationToken);
    }

    public Task<PollDto?> GetActivePollAsync(CancellationToken cancellationToken)
    {
        return httpClient.GetFromJsonAsync<PollDto>($"{sessionState.ApiBaseUrl}/api/polls/active", cancellationToken);
    }

    public async Task<List<RecommendationDto>> GetRecommendationsAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<List<RecommendationDto>>($"{sessionState.ApiBaseUrl}/api/recommendations/{sessionState.UserId}", cancellationToken)
            ?? new List<RecommendationDto>();
    }
}

public sealed class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
}

public sealed class EpisodeDto
{
    public string Title { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
}

public sealed class PollDto
{
    public Guid Id { get; set; }
    public string Question { get; set; } = string.Empty;
}

public sealed class RecommendationDto
{
    public string StationName { get; set; } = string.Empty;
    public float Score { get; set; }
}
