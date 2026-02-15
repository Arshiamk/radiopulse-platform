namespace RadioPulse.Worker;

public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    private static readonly string[] Stations =
    {
        "Pulse One", "Europa Hits", "Nordic Drive", "Sunset FM"
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var station = Stations[Random.Shared.Next(Stations.Length)];
            logger.LogInformation("Simulated ingestion heartbeat from station {Station} at {TimestampUtc}", station, DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
