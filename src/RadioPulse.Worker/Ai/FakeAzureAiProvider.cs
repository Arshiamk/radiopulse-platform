namespace RadioPulse.Worker.Ai;

public sealed class FakeAzureAiProvider : ITranscriptProvider, ISummarizer
{
    public Task<string> TranscribeAsync(string audioReference, CancellationToken cancellationToken)
    {
        return Task.FromResult($"[FAKE-TRANSCRIPT] Source={audioReference}. Host intro, caller reaction, and ad break markers generated for local demo.");
    }

    public Task<string> SummarizeAsync(string transcript, CancellationToken cancellationToken)
    {
        return Task.FromResult("Top moments: host greeting, listener shoutout, and song reveal segment.");
    }
}
