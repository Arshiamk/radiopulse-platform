using RadioPulse.Mobile.Services;

namespace RadioPulse.Mobile.Pages;

public partial class EngagementPage : ContentPage
{
    private readonly RadioApiService apiService;
    private readonly EngagementSignalRService signalRService;
    private PollDto? activePoll;

    public EngagementPage()
    {
        InitializeComponent();
        apiService = ServiceHelper.GetService<RadioApiService>();
        signalRService = ServiceHelper.GetService<EngagementSignalRService>();
        signalRService.OnShoutout += message => MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = message);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await signalRService.EnsureConnectedAsync(CancellationToken.None);
        activePoll = await apiService.GetActivePollAsync(CancellationToken.None);
        PollLabel.Text = activePoll?.Question ?? "No active poll";
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

        await signalRService.VoteAsync(activePoll.Id, choice, CancellationToken.None);
        StatusLabel.Text = $"Vote sent: {choice}";
    }

    private async void OnSendShoutoutClicked(object? sender, EventArgs e)
    {
        await signalRService.SendShoutoutAsync(ShoutoutEntry.Text ?? string.Empty, CancellationToken.None);
        StatusLabel.Text = "Shoutout sent";
        ShoutoutEntry.Text = string.Empty;
    }
}
