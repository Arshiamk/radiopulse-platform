using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using RadioPulse.Api.Services;

namespace RadioPulse.Api.Hubs;

public sealed class EngagementHub(IRadioPulseService service) : Hub
{
    public async Task CreatePoll(Guid showId, string question)
    {
        if (showId == Guid.Empty || string.IsNullOrWhiteSpace(question) || question.Length > 160)
        {
            throw new HubException("Invalid poll payload.");
        }

        var poll = await service.CreatePollAsync(showId, question, Context.ConnectionAborted);
        await Clients.All.SendAsync("PollCreated", poll, Context.ConnectionAborted);
    }

    public async Task Vote(Guid pollId, Guid userId, string choice)
    {
        if (pollId == Guid.Empty || string.IsNullOrWhiteSpace(choice))
        {
            throw new HubException("Invalid vote payload.");
        }

        var authenticatedUserId = GetAuthenticatedUserId();
        if (authenticatedUserId != userId)
        {
            throw new HubException("User mismatch.");
        }

        var vote = await service.CreateVoteAsync(pollId, authenticatedUserId, choice, Context.ConnectionAborted);
        await Clients.All.SendAsync("VoteReceived", vote, Context.ConnectionAborted);
    }

    public async Task SendShoutout(Guid userId, string message)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Length > 280)
        {
            throw new HubException("Invalid shoutout payload.");
        }

        var authenticatedUserId = GetAuthenticatedUserId();
        if (authenticatedUserId != userId)
        {
            throw new HubException("User mismatch.");
        }

        var shoutout = await service.CreateShoutoutAsync(authenticatedUserId, message, Context.ConnectionAborted);
        await Clients.All.SendAsync("ShoutoutReceived", shoutout, Context.ConnectionAborted);
    }

    private Guid GetAuthenticatedUserId()
    {
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(raw, out var userId))
        {
            throw new HubException("Missing or invalid user identity.");
        }

        return userId;
    }
}
