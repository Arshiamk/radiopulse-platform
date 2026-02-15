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
            .Where(x => !dbContext.Transcripts.Any(t => t.EpisodeId == x.Id))
            .OrderByDescending(x => x.PublishedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (episode is null)
        {
            logger.LogInformation("No unprocessed episodes available for transcript processing.");
            return;
        }

        var source = string.IsNullOrWhiteSpace(episode.AudioUrl) ? episode.Title : episode.AudioUrl;
        string transcriptText;
        try
        {
            transcriptText = await transcriptProvider.TranscribeAsync(source, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Transcript provider failed for episode {EpisodeId}; using local fallback transcript.", episode.Id);
            transcriptText = $"[LOCAL-FALLBACK-TRANSCRIPT] Source={source}";
        }

        string summary;
        try
        {
            summary = await summarizer.SummarizeAsync(transcriptText, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Summarizer failed for episode {EpisodeId}; using local fallback summary.", episode.Id);
            summary = "Top moments unavailable due to provider issue; generated local fallback.";
        }

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
