using Microsoft.AspNetCore.SignalR.Client;

namespace RadioPulse.Mobile.Services;

public sealed class EngagementSignalRService(SessionState sessionState)
{
    private HubConnection? connection;

    public event Action<string>? OnShoutout;

    public async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (connection is { State: HubConnectionState.Connected })
        {
            return;
        }

        connection = new HubConnectionBuilder()
            .WithUrl($"{sessionState.ApiBaseUrl}/hubs/engagement", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(sessionState.AccessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<object>("ShoutoutReceived", _ =>
        {
            OnShoutout?.Invoke("New shoutout arrived");
        });

        await connection.StartAsync(cancellationToken);
    }

    public Task VoteAsync(Guid pollId, string choice, CancellationToken cancellationToken)
    {
        return connection?.SendAsync("Vote", pollId, sessionState.UserId, choice, cancellationToken) ?? Task.CompletedTask;
    }

    public Task SendShoutoutAsync(string message, CancellationToken cancellationToken)
    {
        return connection?.SendAsync("SendShoutout", sessionState.UserId, message, cancellationToken) ?? Task.CompletedTask;
    }
}
