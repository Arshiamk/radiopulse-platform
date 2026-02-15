using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RadioPulse.Worker.Ai;

public sealed class AzureAiProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ITranscriptProvider, ISummarizer
{
    private readonly string endpoint = configuration["AZURE_OPENAI_ENDPOINT"] ?? string.Empty;
    private readonly string apiKey = configuration["AZURE_OPENAI_API_KEY"] ?? string.Empty;
    private readonly string deployment = configuration["AZURE_OPENAI_DEPLOYMENT"] ?? string.Empty;

    public async Task<string> TranscribeAsync(string audioReference, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(deployment))
        {
            return $"Azure credentials not configured. Simulated transcript for {audioReference}.";
        }

        return await PromptAsync($"Create concise transcript notes for this radio source: {audioReference}", cancellationToken);
    }

    public async Task<string> SummarizeAsync(string transcript, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(deployment))
        {
            return "Azure credentials not configured. Simulated summary generated.";
        }

        return await PromptAsync($"Summarize this radio transcript in 3 bullets: {transcript}", cancellationToken);
    }

    private async Task<string> PromptAsync(string prompt, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("azure-openai");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            messages = new[]
            {
                new { role = "system", content = "You summarize commercial radio content." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version=2024-10-21", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return "Azure provider failed; fallback summary unavailable.";
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var value = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return value ?? string.Empty;
    }
}
