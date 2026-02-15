using Microsoft.AspNetCore.Mvc.Testing;

namespace RadioPulse.Web.Tests;

public sealed class WebPageSmokeTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient client;

    public WebPageSmokeTests(WebAppFactory factory)
    {
        client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/", "RadioPulse Control Room")]
    [InlineData("/auth", "Continue as Lena")]
    [InlineData("/engagement", "Live Poll and Shoutouts")]
    [InlineData("/media", "Now Playing and Schedule")]
    [InlineData("/recommendations", "Recommended For You")]
    [InlineData("/diagnostics", "System Diagnostics")]
    public async Task Route_RendersExpectedHeading(string route, string expectedText)
    {
        var response = await client.GetAsync(route);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedText, html);

        if (route == "/auth")
        {
            Assert.Contains("Continue as Luna", html);
        }
    }
}

public sealed class WebAppFactory : WebApplicationFactory<Program>
{
}
