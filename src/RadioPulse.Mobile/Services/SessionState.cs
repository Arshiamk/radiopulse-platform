namespace RadioPulse.Mobile.Services;

public sealed class SessionState
{
    public Guid UserId { get; set; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public string AccessToken { get; set; } = string.Empty;
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    public string ApiBaseUrl { get; private set; } =
        NormalizeApiBaseUrl(Environment.GetEnvironmentVariable("RADIOPULSE_API_URL") ?? "http://localhost:5003");

    public void SetApiBaseUrl(string value)
    {
        ApiBaseUrl = NormalizeApiBaseUrl(value);
    }

    public void SignOut()
    {
        AccessToken = string.Empty;
    }

    private static string NormalizeApiBaseUrl(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "http://localhost:5003";
        }

        return trimmed.TrimEnd('/');
    }
}
