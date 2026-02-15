namespace RadioPulse.Web.Services;

public sealed class DemoAuthSession
{
    public const string StorageKey = "radiopulse-demo-auth";

    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = "Anonymous";
    public string AccessToken { get; private set; } = string.Empty;
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    public void SignIn(Guid userId, string displayName, string accessToken)
    {
        UserId = userId;
        DisplayName = displayName;
        AccessToken = accessToken;
    }

    public void SignOut()
    {
        UserId = Guid.Empty;
        DisplayName = "Anonymous";
        AccessToken = string.Empty;
    }
}
