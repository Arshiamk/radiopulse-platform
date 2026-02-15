using Microsoft.EntityFrameworkCore;
using RadioPulse.Api.Data;
using RadioPulse.Api.Services;
using RadioPulse.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();

var postgresConnection = builder.Configuration.GetConnectionString("radiopulsedb")
    ?? builder.Configuration.GetConnectionString("postgres")
    ?? "Host=localhost;Port=5432;Database=radiopulsedb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<RadioPulseDbContext>(options => options.UseNpgsql(postgresConnection));
builder.Services.AddScoped<IRadioPulseService, RadioPulseService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RadioPulseDbContext>();
    dbContext.Database.Migrate();
    await SeedData.EnsureSeededAsync(dbContext, CancellationToken.None);
}

app.MapGet("/api/status", () => Results.Ok(new
{
    Service = "RadioPulse.Api",
    Version = "phase-2",
    UtcNow = DateTimeOffset.UtcNow
}));

app.MapGet("/api/stations", async (RadioPulseDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Stations.OrderBy(x => x.Name).ToListAsync(cancellationToken));

app.MapGet("/api/shows", async (IRadioPulseService service, CancellationToken cancellationToken) =>
    await service.GetScheduleAsync(cancellationToken));

app.MapGet("/api/episodes", async (RadioPulseDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Episodes.OrderByDescending(x => x.PublishedAtUtc).ToListAsync(cancellationToken));

app.MapGet("/api/polls/active", async (IRadioPulseService service, CancellationToken cancellationToken) =>
{
    var poll = await service.GetActivePollAsync(cancellationToken);
    return poll is null ? Results.NotFound() : Results.Ok(poll);
});

app.Run();
