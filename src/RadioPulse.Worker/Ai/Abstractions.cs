namespace RadioPulse.Worker.Ai;

public interface ITranscriptProvider
{
    Task<string> TranscribeAsync(string audioReference, CancellationToken cancellationToken);
}

public interface ISummarizer
{
    Task<string> SummarizeAsync(string transcript, CancellationToken cancellationToken);
}
