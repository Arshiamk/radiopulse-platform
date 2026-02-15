using Microsoft.AspNetCore.SignalR;
using RadioPulse.Api.Services;

namespace RadioPulse.Api.Hubs;

public sealed class EngagementHub(IRadioPulseService service) : Hub
{
    public async Task CreatePoll(Guid showId, string question)
    {
        var poll = await service.CreatePollAsync(showId, question, Context.ConnectionAborted);
        await Clients.All.SendAsync("PollCreated", poll, Context.ConnectionAborted);
    }

    public async Task Vote(Guid pollId, Guid userId, string choice)
    {
        var vote = await service.CreateVoteAsync(pollId, userId, choice, Context.ConnectionAborted);
        await Clients.All.SendAsync("VoteReceived", vote, Context.ConnectionAborted);
    }

    public async Task SendShoutout(Guid userId, string message)
    {
        var shoutout = await service.CreateShoutoutAsync(userId, message, Context.ConnectionAborted);
        await Clients.All.SendAsync("ShoutoutReceived", shoutout, Context.ConnectionAborted);
    }
}
