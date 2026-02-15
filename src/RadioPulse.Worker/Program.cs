using Microsoft.EntityFrameworkCore;
using RadioPulse.Worker;
using RadioPulse.Worker.Ai;
using RadioPulse.Worker.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var postgresConnection = builder.Configuration.GetConnectionString("radiopulsedb")
    ?? builder.Configuration.GetConnectionString("postgres")
    ?? "Host=localhost;Port=5432;Database=radiopulsedb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<WorkerDbContext>(options => options.UseNpgsql(postgresConnection));
builder.Services.AddHttpClient("azure-openai");

var useAzure = builder.Configuration.GetValue<bool>("UseAzureAi");
var hasAzureConfig =
    !string.IsNullOrWhiteSpace(builder.Configuration["AZURE_OPENAI_ENDPOINT"]) &&
    !string.IsNullOrWhiteSpace(builder.Configuration["AZURE_OPENAI_API_KEY"]) &&
    !string.IsNullOrWhiteSpace(builder.Configuration["AZURE_OPENAI_DEPLOYMENT"]);

if (useAzure || hasAzureConfig)
{
    builder.Services.AddSingleton<ITranscriptProvider, AzureAiProvider>();
    builder.Services.AddSingleton<ISummarizer, AzureAiProvider>();
}
else
{
    builder.Services.AddSingleton<ITranscriptProvider, FakeAzureAiProvider>();
    builder.Services.AddSingleton<ISummarizer, FakeAzureAiProvider>();
}

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("RadioPulse.Worker.Startup");
if (useAzure || hasAzureConfig)
{
    logger.LogInformation("Worker AI provider: AzureAiProvider (UseAzureAi={UseAzureAi}, HasAzureConfig={HasAzureConfig})", useAzure, hasAzureConfig);
}
else
{
    logger.LogInformation("Worker AI provider: FakeAzureAiProvider (local deterministic mode).");
}

host.Run();
