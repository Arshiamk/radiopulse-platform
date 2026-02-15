using Microsoft.EntityFrameworkCore;
using RadioPulse.Api.Data;
using RadioPulse.Api.Hubs;
using RadioPulse.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

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
    Version = "phase-3",
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

app.MapPost("/api/polls", async (CreatePollRequest request, IRadioPulseService service, CancellationToken cancellationToken) =>
{
    var poll = await service.CreatePollAsync(request.ShowId, request.Question, cancellationToken);
    return Results.Ok(poll);
});

app.MapPost("/api/polls/votes", async (CreateVoteRequest request, IRadioPulseService service, CancellationToken cancellationToken) =>
{
    var vote = await service.CreateVoteAsync(request.PollId, request.UserId, request.Choice, cancellationToken);
    return Results.Ok(vote);
});

app.MapGet("/api/shoutouts", async (IRadioPulseService service, CancellationToken cancellationToken) =>
    await service.GetLatestShoutoutsAsync(50, cancellationToken));

app.MapPost("/api/shoutouts", async (CreateShoutoutRequest request, IRadioPulseService service, CancellationToken cancellationToken) =>
{
    var shoutout = await service.CreateShoutoutAsync(request.UserId, request.Message, cancellationToken);
    return Results.Ok(shoutout);
});

app.MapHub<EngagementHub>("/hubs/engagement");

app.Run();

public sealed record CreatePollRequest(Guid ShowId, string Question);
public sealed record CreateVoteRequest(Guid PollId, Guid UserId, string Choice);
public sealed record CreateShoutoutRequest(Guid UserId, string Message);
