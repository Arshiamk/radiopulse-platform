using Microsoft.EntityFrameworkCore;
using RadioPulse.Api.Data;
using RadioPulse.Domain;

namespace RadioPulse.Api.Services;

public interface IRadioPulseService
{
    Task<Poll?> GetActivePollAsync(CancellationToken cancellationToken);
    Task<Poll> CreatePollAsync(Guid showId, string question, CancellationToken cancellationToken);
    Task<Vote> CreateVoteAsync(Guid pollId, Guid userId, string choice, CancellationToken cancellationToken);
    Task<Shoutout> CreateShoutoutAsync(Guid userId, string message, CancellationToken cancellationToken);
    Task<IReadOnlyList<Shoutout>> GetLatestShoutoutsAsync(int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<Show>> GetScheduleAsync(CancellationToken cancellationToken);
    Task<Episode?> GetNowPlayingAsync(CancellationToken cancellationToken);
}

public sealed class RadioPulseService(RadioPulseDbContext dbContext) : IRadioPulseService
{
    public async Task<Poll?> GetActivePollAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Polls
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => x.IsOpen, cancellationToken);
    }

    public async Task<Poll> CreatePollAsync(Guid showId, string question, CancellationToken cancellationToken)
    {
        var poll = new Poll
        {
            ShowId = showId,
            Question = question,
            IsOpen = true
        };

        dbContext.Polls.Add(poll);
        await dbContext.SaveChangesAsync(cancellationToken);
        return poll;
    }

    public async Task<Vote> CreateVoteAsync(Guid pollId, Guid userId, string choice, CancellationToken cancellationToken)
    {
        var vote = new Vote
        {
            PollId = pollId,
            UserId = userId,
            Choice = choice
        };

        dbContext.Votes.Add(vote);
        await dbContext.SaveChangesAsync(cancellationToken);
        return vote;
    }

    public async Task<Shoutout> CreateShoutoutAsync(Guid userId, string message, CancellationToken cancellationToken)
    {
        var shoutout = new Shoutout
        {
            UserId = userId,
            Message = message
        };

        dbContext.Shoutouts.Add(shoutout);
        await dbContext.SaveChangesAsync(cancellationToken);
        return shoutout;
    }

    public async Task<IReadOnlyList<Shoutout>> GetLatestShoutoutsAsync(int take, CancellationToken cancellationToken)
    {
        return await dbContext.Shoutouts
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Show>> GetScheduleAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Shows
            .Include(x => x.Station)
            .OrderBy(x => x.StartTimeUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Episode?> GetNowPlayingAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Episodes
            .OrderByDescending(x => x.PublishedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
