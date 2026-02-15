namespace RadioPulse.Mobile.Services;

public sealed class SessionState
{
    public Guid UserId { get; set; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public string AccessToken { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = Environment.GetEnvironmentVariable("RADIOPULSE_API_URL") ?? "http://localhost:8080";
}
