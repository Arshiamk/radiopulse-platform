using Microsoft.EntityFrameworkCore;
using RadioPulse.Domain;

namespace RadioPulse.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(RadioPulseDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Stations.AnyAsync(cancellationToken))
        {
            return;
        }

        var station = new Station
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Pulse One Europe",
            Region = "EU"
        };

        var morningShow = new Show
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Station = station,
            Title = "Morning Pulse",
            HostName = "Mila Novak",
            StartTimeUtc = new TimeOnly(6, 0),
            EndTimeUtc = new TimeOnly(10, 0)
        };

        var driveShow = new Show
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Station = station,
            Title = "Drive Europa",
            HostName = "Jonas Weber",
            StartTimeUtc = new TimeOnly(15, 0),
            EndTimeUtc = new TimeOnly(19, 0)
        };

        var users = new[]
        {
            new User { DisplayName = "Demo DJ", Email = "dj@radiopulse.local" },
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                DisplayName = "Lena",
                Email = "lena@radiopulse.local"
            },
            new User { DisplayName = "Marco", Email = "marco@radiopulse.local" }
        };

        var episode = new Episode
        {
            Show = morningShow,
            Title = "Morning Pulse - Episode 001",
            AudioUrl = "https://example.com/audio/morning-pulse-001.mp3",
            PublishedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var poll = new Poll
        {
            Show = morningShow,
            Question = "Which track should play next?",
            IsOpen = true
        };

        dbContext.Stations.Add(station);
        dbContext.Shows.AddRange(morningShow, driveShow);
        dbContext.Users.AddRange(users);
        dbContext.Episodes.Add(episode);
        dbContext.Polls.Add(poll);

        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.ListenEvents.AddRange(
            new ListenEvent { UserId = users[1].Id, StationId = station.Id, Liked = true },
            new ListenEvent { UserId = users[2].Id, StationId = station.Id, Liked = false });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
