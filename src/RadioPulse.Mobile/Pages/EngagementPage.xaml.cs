using RadioPulse.Mobile.Services;

namespace RadioPulse.Mobile.Pages;

public partial class EngagementPage : ContentPage
{
    private readonly RadioApiService apiService;
    private readonly EngagementSignalRService signalRService;
    private readonly SessionState sessionState;
    private PollDto? activePoll;

    public EngagementPage()
    {
        InitializeComponent();
        apiService = ServiceHelper.GetService<RadioApiService>();
        signalRService = ServiceHelper.GetService<EngagementSignalRService>();
        sessionState = ServiceHelper.GetService<SessionState>();
        signalRService.OnShoutout += message => MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = message);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!sessionState.IsAuthenticated)
        {
            PollLabel.Text = "Login required";
            StatusLabel.Text = "Open Login tab first.";
            return;
        }

        try
        {
            await signalRService.EnsureConnectedAsync(CancellationToken.None);
            activePoll = await apiService.GetActivePollAsync(CancellationToken.None);
            PollLabel.Text = activePoll?.Question ?? "No active poll";
            StatusLabel.Text = $"Connected to {sessionState.ApiBaseUrl}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Connection error: {ex.Message}";
        }
    }

    private async void OnVoteAClicked(object? sender, EventArgs e)
    {
        await VoteAsync("Track A");
    }

    private async void OnVoteBClicked(object? sender, EventArgs e)
    {
        await VoteAsync("Track B");
    }

    private async Task VoteAsync(string choice)
    {
        if (activePoll is null)
        {
            StatusLabel.Text = "No active poll";
            return;
        }

        try
        {
            await signalRService.VoteAsync(activePoll.Id, choice, CancellationToken.None);
            StatusLabel.Text = $"Vote sent: {choice}";
        }
        catch
        {
            var ok = await apiService.VoteAsync(activePoll.Id, choice, CancellationToken.None);
            StatusLabel.Text = ok ? $"Vote sent: {choice}" : "Vote failed";
        }
    }

    private async void OnSendShoutoutClicked(object? sender, EventArgs e)
    {
        var message = (ShoutoutEntry.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            StatusLabel.Text = "Enter a shoutout first.";
            return;
        }

        try
        {
            await signalRService.SendShoutoutAsync(message, CancellationToken.None);
            StatusLabel.Text = "Shoutout sent";
        }
        catch
        {
            var ok = await apiService.SendShoutoutAsync(message, CancellationToken.None);
            StatusLabel.Text = ok ? "Shoutout sent" : "Shoutout failed";
        }

        ShoutoutEntry.Text = string.Empty;
    }
}
