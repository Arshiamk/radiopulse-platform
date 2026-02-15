namespace RadioPulse.Domain;

public sealed class Station
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Region { get; set; }
    public ICollection<Show> Shows { get; set; } = new List<Show>();
}

public sealed class Show
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StationId { get; set; }
    public required string Title { get; set; }
    public required string HostName { get; set; }
    public TimeOnly StartTimeUtc { get; set; }
    public TimeOnly EndTimeUtc { get; set; }
    public Station? Station { get; set; }
    public ICollection<Episode> Episodes { get; set; } = new List<Episode>();
    public ICollection<Poll> Polls { get; set; } = new List<Poll>();
}

public sealed class Episode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShowId { get; set; }
    public required string Title { get; set; }
    public string? AudioUrl { get; set; }
    public DateTimeOffset PublishedAtUtc { get; set; }
    public Show? Show { get; set; }
}

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string DisplayName { get; set; }
    public required string Email { get; set; }
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public ICollection<Shoutout> Shoutouts { get; set; } = new List<Shoutout>();
    public ICollection<ListenEvent> ListenEvents { get; set; } = new List<ListenEvent>();
}

public sealed class Poll
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShowId { get; set; }
    public required string Question { get; set; }
    public bool IsOpen { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Show? Show { get; set; }
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}

public sealed class Vote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PollId { get; set; }
    public Guid UserId { get; set; }
    public required string Choice { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Poll? Poll { get; set; }
    public User? User { get; set; }
}

public sealed class Shoutout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Message { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public User? User { get; set; }
}

public sealed class ListenEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid StationId { get; set; }
    public bool Liked { get; set; }
    public DateTimeOffset ListenedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public User? User { get; set; }
    public Station? Station { get; set; }
}

public sealed class Transcript
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EpisodeId { get; set; }
    public string? FullText { get; set; }
    public string? Summary { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Episode? Episode { get; set; }
}
