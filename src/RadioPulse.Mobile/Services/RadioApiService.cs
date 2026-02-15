using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace RadioPulse.Mobile.Services;

public sealed class RadioApiService(SessionState sessionState)
{
    private readonly HttpClient httpClient = CreateHttpClient();

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

    public async Task<bool> SendShoutoutAsync(string message, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{sessionState.ApiBaseUrl}/api/shoutouts")
        {
            Content = JsonContent.Create(new { UserId = sessionState.UserId, Message = message })
        };
        ApplyAuthHeader(request);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> VoteAsync(Guid pollId, string choice, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{sessionState.ApiBaseUrl}/api/polls/votes")
        {
            Content = JsonContent.Create(new { PollId = pollId, UserId = sessionState.UserId, Choice = choice })
        };
        ApplyAuthHeader(request);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private void ApplyAuthHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(sessionState.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionState.AccessToken);
        }
    }

    [SuppressMessage("Reliability", "CA2000", Justification = "HttpClient disposes the owned handler instance.")]
    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler();
#if DEBUG
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            message?.RequestUri?.Host is "localhost" or "127.0.0.1";
#endif
        return new HttpClient(handler, disposeHandler: true);
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
