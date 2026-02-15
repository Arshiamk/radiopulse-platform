using Microsoft.EntityFrameworkCore;
using RadioPulse.Domain;
using RadioPulse.Worker.Ai;
using RadioPulse.Worker.Data;

namespace RadioPulse.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory,
    ITranscriptProvider transcriptProvider,
    ISummarizer summarizer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextEpisodeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Worker ingestion cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
    }

    private async Task ProcessNextEpisodeAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();

        var episode = await dbContext.Episodes
            .OrderByDescending(x => x.PublishedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (episode is null)
        {
            logger.LogInformation("No episodes available for transcript processing.");
            return;
        }

        var alreadyProcessed = await dbContext.Transcripts
            .AnyAsync(x => x.EpisodeId == episode.Id, cancellationToken);

        if (alreadyProcessed)
        {
            logger.LogInformation("Episode {EpisodeId} already has transcript.", episode.Id);
            return;
        }

        var transcriptText = await transcriptProvider.TranscribeAsync(episode.AudioUrl ?? episode.Title, cancellationToken);
        var summary = await summarizer.SummarizeAsync(transcriptText, cancellationToken);

        dbContext.Transcripts.Add(new Transcript
        {
            EpisodeId = episode.Id,
            FullText = transcriptText,
            Summary = summary,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Transcript persisted for episode {EpisodeTitle}", episode.Title);
    }
}
