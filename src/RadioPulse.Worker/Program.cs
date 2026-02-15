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
if (useAzure)
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
host.Run();
