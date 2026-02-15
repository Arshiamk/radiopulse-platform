using Microsoft.EntityFrameworkCore;
using RadioPulse.Domain;

namespace RadioPulse.Worker.Data;

public sealed class WorkerDbContext(DbContextOptions<WorkerDbContext> options) : DbContext(options)
{
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Transcript> Transcripts => Set<Transcript>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Episode>().ToTable("Episodes");
        modelBuilder.Entity<Transcript>().ToTable("Transcripts");
    }
}
