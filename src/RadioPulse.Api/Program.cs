using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RadioPulse.Api.Data;
using RadioPulse.Api.Hubs;
using RadioPulse.Api.Services;
using RadioPulse.Ml.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

var jwtIssuer = builder.Configuration["Auth:Issuer"] ?? "RadioPulse.LocalIssuer";
var jwtAudience = builder.Configuration["Auth:Audience"] ?? "RadioPulse.Clients";
var jwtKey = builder.Configuration["Auth:Key"] ?? "radiopulse-dev-signing-key-please-change";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/engagement"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("public", policy =>
    {
        policy.PermitLimit = 120;
        policy.Window = TimeSpan.FromMinutes(1);
        policy.QueueLimit = 0;
    });
});

var postgresConnection = builder.Configuration.GetConnectionString("radiopulsedb")
    ?? builder.Configuration.GetConnectionString("postgres")
    ?? "Host=localhost;Port=5432;Database=radiopulsedb;Username=postgres;Password=postgres";
var useInMemoryDb = builder.Configuration.GetValue<bool>("UseInMemoryDb");

builder.Services.AddDbContext<RadioPulseDbContext>(options =>
{
    if (useInMemoryDb)
    {
        options.UseInMemoryDatabase("radiopulse-test");
        return;
    }

    options.UseNpgsql(postgresConnection);
});
builder.Services.AddScoped<IRadioPulseService, RadioPulseService>();
builder.Services.AddSingleton<RecommendationEngine>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var statusCode = exception switch
        {
            BadHttpRequestException => StatusCodes.Status400BadRequest,
            JsonException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        var title = statusCode == StatusCodes.Status400BadRequest
            ? "Invalid request payload"
            : "Unhandled error";

        return Results.Problem(
            title: title,
            detail: exception?.Message,
            statusCode: statusCode).ExecuteAsync(context);
    });
});

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RadioPulseDbContext>();
    var recommendationEngine = scope.ServiceProvider.GetRequiredService<RecommendationEngine>();
    if (!useInMemoryDb)
    {
        dbContext.Database.Migrate();
    }

    await SeedData.EnsureSeededAsync(dbContext, CancellationToken.None);
    var modelPath = Path.Combine(AppContext.BaseDirectory, "models", "recommendations.zip");
    recommendationEngine.EnsureModel(modelPath, await dbContext.ListenEvents.ToListAsync());
}

app.MapGet("/api/status", () => Results.Ok(new
{
    Service = "RadioPulse.Api",
    Version = "phase-5",
    UtcNow = DateTimeOffset.UtcNow
}))
.RequireRateLimiting("public");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

WeatherForecast[] BuildWeatherForecast()
{
    return Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]))
        .ToArray();
}

app.MapGet("/weatherforecast", BuildWeatherForecast)
    .RequireRateLimiting("public");

app.MapGet("/api/weatherforecast", BuildWeatherForecast)
    .RequireRateLimiting("public");

app.MapGet("/api/auth/dev-token/{userId:guid}", (Guid userId) =>
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var descriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "DemoUser")
        }),
        Expires = DateTime.UtcNow.AddHours(12),
        Issuer = jwtIssuer,
        Audience = jwtAudience,
        SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
    };

    var token = tokenHandler.CreateToken(descriptor);
    return Results.Ok(new { access_token = tokenHandler.WriteToken(token) });
});

app.MapGet("/api/stations", async (RadioPulseDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Stations
        .AsNoTracking()
        .OrderBy(x => x.Name)
        .Select(x => new StationDto(x.Id, x.Name, x.Region))
        .ToListAsync(cancellationToken))
.RequireRateLimiting("public");

app.MapGet("/api/shows", async (RadioPulseDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Shows
        .AsNoTracking()
        .OrderBy(x => x.StartTimeUtc)
        .Select(x => new ShowDto(x.Id, x.StationId, x.Title, x.HostName, x.StartTimeUtc, x.EndTimeUtc))
        .ToListAsync(cancellationToken))
.RequireRateLimiting("public");

app.MapGet("/api/episodes", async (RadioPulseDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Episodes
        .AsNoTracking()
        .OrderByDescending(x => x.PublishedAtUtc)
        .Select(x => new EpisodeDto(x.Id, x.ShowId, x.Title, x.AudioUrl, x.PublishedAtUtc))
        .ToListAsync(cancellationToken))
.RequireRateLimiting("public");

app.MapGet("/api/now-playing", async (RadioPulseDbContext dbContext, CancellationToken cancellationToken) =>
{
    var episode = await dbContext.Episodes
        .AsNoTracking()
        .OrderByDescending(x => x.PublishedAtUtc)
        .Select(x => new EpisodeDto(x.Id, x.ShowId, x.Title, x.AudioUrl, x.PublishedAtUtc))
        .FirstOrDefaultAsync(cancellationToken);

    return episode is null ? Results.NotFound() : Results.Ok(episode);
})
.RequireRateLimiting("public");

app.MapGet("/api/polls/active", async (IRadioPulseService service, CancellationToken cancellationToken) =>
{
    var poll = await service.GetActivePollAsync(cancellationToken);
    return poll is null ? Results.NotFound() : Results.Ok(poll);
})
.RequireRateLimiting("public");

app.MapPost("/api/polls", async ([FromBody] CreatePollRequest? request, IRadioPulseService service, CancellationToken cancellationToken) =>
{
    if (request is null || request.ShowId == Guid.Empty || string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 160)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["question"] = ["Question is required and must be <= 160 chars."]
        });
    }

    var poll = await service.CreatePollAsync(request.ShowId, request.Question, cancellationToken);
    return Results.Ok(poll);
})
.RequireAuthorization();

app.MapPost("/api/polls/votes", async ([FromBody] CreateVoteRequest? request, IRadioPulseService service, CancellationToken cancellationToken) =>
{
    if (request is null || request.PollId == Guid.Empty || request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Choice))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["vote"] = ["PollId, UserId and Choice are required."]
        });
    }

    var vote = await service.CreateVoteAsync(request.PollId, request.UserId, request.Choice, cancellationToken);
    return Results.Ok(vote);
})
.RequireAuthorization();

app.MapGet("/api/shoutouts", async (IRadioPulseService service, CancellationToken cancellationToken) =>
{
    var shoutouts = await service.GetLatestShoutoutsAsync(50, cancellationToken);
    return shoutouts.Select(x => new ShoutoutDto(x.Id, x.UserId, x.Message, x.CreatedAtUtc));
})
.RequireRateLimiting("public");

app.MapGet("/api/transcripts/top-moments", async (RadioPulseDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Transcripts
        .OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new { x.EpisodeId, x.Summary, x.CreatedAtUtc })
        .Take(10)
        .ToListAsync(cancellationToken))
.RequireRateLimiting("public");

app.MapGet("/api/transcripts/search", async (string term, RadioPulseDbContext dbContext, CancellationToken cancellationToken) =>
{
    var normalized = term.Trim();
    return await dbContext.Transcripts
        .Where(x => x.FullText != null && x.FullText.Contains(normalized))
        .Select(x => new { x.EpisodeId, x.FullText, x.Summary })
        .Take(20)
        .ToListAsync(cancellationToken);
})
.RequireRateLimiting("public");

app.MapGet("/api/recommendations/{userId:guid}", async (Guid userId, RadioPulseDbContext dbContext, RecommendationEngine recommendationEngine, CancellationToken cancellationToken) =>
{
    var stations = await dbContext.Stations.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    var recommendations = recommendationEngine.Recommend(userId, stations);
    return Results.Ok(recommendations);
})
.RequireRateLimiting("public");

app.MapPost("/api/shoutouts", async ([FromBody] CreateShoutoutRequest? request, IRadioPulseService service, CancellationToken cancellationToken) =>
{
    if (request is null || request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 280)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["message"] = ["Message is required and must be <= 280 chars."]
        });
    }

    var shoutout = await service.CreateShoutoutAsync(request.UserId, request.Message, cancellationToken);
    return Results.Ok(shoutout);
})
.RequireAuthorization();

app.MapHub<EngagementHub>("/hubs/engagement").RequireAuthorization();

app.Run();

public sealed record CreatePollRequest(Guid ShowId, string Question);
public sealed record CreateVoteRequest(Guid PollId, Guid UserId, string Choice);
public sealed record CreateShoutoutRequest(Guid UserId, string Message);
public sealed record StationDto(Guid Id, string Name, string Region);
public sealed record ShowDto(Guid Id, Guid StationId, string Title, string HostName, TimeOnly StartTimeUtc, TimeOnly EndTimeUtc);
public sealed record EpisodeDto(Guid Id, Guid ShowId, string Title, string? AudioUrl, DateTimeOffset PublishedAtUtc);
public sealed record ShoutoutDto(Guid Id, Guid UserId, string Message, DateTimeOffset CreatedAtUtc);
public sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public partial class Program;
