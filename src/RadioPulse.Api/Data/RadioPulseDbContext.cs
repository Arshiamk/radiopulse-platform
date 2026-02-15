using Microsoft.EntityFrameworkCore;
using RadioPulse.Domain;

namespace RadioPulse.Api.Data;

public sealed class RadioPulseDbContext(DbContextOptions<RadioPulseDbContext> options) : DbContext(options)
{
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Show> Shows => Set<Show>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<Shoutout> Shoutouts => Set<Shoutout>();
    public DbSet<ListenEvent> ListenEvents => Set<ListenEvent>();
    public DbSet<Transcript> Transcripts => Set<Transcript>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();

        modelBuilder.Entity<Show>()
            .HasOne(x => x.Station)
            .WithMany(x => x.Shows)
            .HasForeignKey(x => x.StationId);

        modelBuilder.Entity<Episode>()
            .HasOne(x => x.Show)
            .WithMany(x => x.Episodes)
            .HasForeignKey(x => x.ShowId);

        modelBuilder.Entity<Poll>()
            .HasOne(x => x.Show)
            .WithMany(x => x.Polls)
            .HasForeignKey(x => x.ShowId);

        modelBuilder.Entity<Vote>()
            .HasOne(x => x.Poll)
            .WithMany(x => x.Votes)
            .HasForeignKey(x => x.PollId);

        modelBuilder.Entity<Vote>()
            .HasOne(x => x.User)
            .WithMany(x => x.Votes)
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<Shoutout>()
            .HasOne(x => x.User)
            .WithMany(x => x.Shoutouts)
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<ListenEvent>()
            .HasOne(x => x.User)
            .WithMany(x => x.ListenEvents)
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<ListenEvent>()
            .HasOne(x => x.Station)
            .WithMany()
            .HasForeignKey(x => x.StationId);

        modelBuilder.Entity<Transcript>()
            .HasOne(x => x.Episode)
            .WithMany()
            .HasForeignKey(x => x.EpisodeId);
    }
}
