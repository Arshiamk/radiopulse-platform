using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RadioPulse.Api.Tests;

public sealed class ApiSmokeTests : IClassFixture<RadioPulseApiFactory>
{
    private readonly HttpClient client;

    public ApiSmokeTests(RadioPulseApiFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task StatusEndpoint_ReturnsOk()
    {
        var response = await client.GetAsync("/api/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StationsEndpoint_ReturnsSeededStation()
    {
        var response = await client.GetAsync("/api/stations");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Pulse One Europe", body);
    }

    [Theory]
    [InlineData("/weatherforecast")]
    [InlineData("/api/weatherforecast")]
    public async Task WeatherProbeEndpoints_ReturnForecastPayload(string route)
    {
        var response = await client.GetAsync(route);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.True(document.RootElement.ValueKind == JsonValueKind.Array);
        Assert.True(document.RootElement.GetArrayLength() > 0);
    }

    [Fact]
    public async Task ShowsEndpoint_ReturnsDtoShapeWithoutStationGraph()
    {
        var response = await client.GetAsync("/api/shows");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var first = document.RootElement[0];

        Assert.True(first.TryGetProperty("stationId", out _));
        Assert.False(first.TryGetProperty("station", out _));
    }

    [Fact]
    public async Task PollsEndpoint_InvalidJson_ReturnsBadRequest()
    {
        var token = await GetDevTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/polls")
        {
            Content = new StringContent("{bad json", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedWriteFlow_CreatesPollVoteAndShoutout()
    {
        var token = await GetDevTokenAsync();
        var authHeader = new AuthenticationHeaderValue("Bearer", token);

        using var createPollRequest = new HttpRequestMessage(HttpMethod.Post, "/api/polls")
        {
            Content = JsonContent.Create(new
            {
                ShowId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Question = "Test poll from integration test"
            })
        };
        createPollRequest.Headers.Authorization = authHeader;

        var createPollResponse = await client.SendAsync(createPollRequest);
        createPollResponse.EnsureSuccessStatusCode();

        var activePoll = await client.GetFromJsonAsync<PollDto>("/api/polls/active");
        Assert.NotNull(activePoll);

        using var voteRequest = new HttpRequestMessage(HttpMethod.Post, "/api/polls/votes")
        {
            Content = JsonContent.Create(new
            {
                PollId = activePoll!.Id,
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Choice = "Track A"
            })
        };
        voteRequest.Headers.Authorization = authHeader;

        var voteResponse = await client.SendAsync(voteRequest);
        voteResponse.EnsureSuccessStatusCode();

        using var shoutoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/shoutouts")
        {
            Content = JsonContent.Create(new
            {
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Message = "Test shoutout from integration test"
            })
        };
        shoutoutRequest.Headers.Authorization = authHeader;

        var shoutoutResponse = await client.SendAsync(shoutoutRequest);
        shoutoutResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task DevTokenEndpoint_SupportsMultipleDemoProfiles()
    {
        var lena = await client.GetFromJsonAsync<TokenResponse>("/api/auth/dev-token/22222222-2222-2222-2222-222222222222");
        var luna = await client.GetFromJsonAsync<TokenResponse>("/api/auth/dev-token/33333333-3333-3333-3333-333333333333");

        Assert.False(string.IsNullOrWhiteSpace(lena?.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(luna?.AccessToken));
        Assert.NotEqual(lena!.AccessToken, luna!.AccessToken);
    }

    private async Task<string> GetDevTokenAsync()
    {
        var tokenResponse = await client.GetFromJsonAsync<TokenResponse>("/api/auth/dev-token/22222222-2222-2222-2222-222222222222");
        Assert.NotNull(tokenResponse);
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse!.AccessToken));
        return tokenResponse.AccessToken;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class PollDto
    {
        public Guid Id { get; set; }
    }
}

public sealed class RadioPulseApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("UseInMemoryDb", "true");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDb"] = "true",
                ["Auth:Key"] = "radiopulse-dev-signing-key-please-change"
            });
        });
    }
}
