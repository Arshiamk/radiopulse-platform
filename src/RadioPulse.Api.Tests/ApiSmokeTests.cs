using System.Net;
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
