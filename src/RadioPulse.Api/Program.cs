using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RadioPulse.Api.Data;
using RadioPulse.Api.Hubs;
using RadioPulse.Api.Services;

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

builder.Services.AddDbContext<RadioPulseDbContext>(options => options.UseNpgsql(postgresConnection));
builder.Services.AddScoped<IRadioPulseService, RadioPulseService>();

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
        return Results.Problem(
            title: "Unhandled error",
            detail: exception?.Message,
            statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(context);
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
    dbContext.Database.Migrate();
    await SeedData.EnsureSeededAsync(dbContext, CancellationToken.None);
}

app.MapGet("/api/status", () => Results.Ok(new
{
    Service = "RadioPulse.Api",
    Version = "phase-5",
    UtcNow = DateTimeOffset.UtcNow
}))
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
    await dbContext.Stations.OrderBy(x => x.Name).ToListAsync(cancellationToken))
.RequireRateLimiting("public");

app.MapGet("/api/shows", async (IRadioPulseService service, CancellationToken cancellationToken) =>
    await service.GetScheduleAsync(cancellationToken))
.RequireRateLimiting("public");

app.MapGet("/api/episodes", async (RadioPulseDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Episodes.OrderByDescending(x => x.PublishedAtUtc).ToListAsync(cancellationToken))
.RequireRateLimiting("public");

app.MapGet("/api/now-playing", async (IRadioPulseService service, CancellationToken cancellationToken) =>
{
    var episode = await service.GetNowPlayingAsync(cancellationToken);
    return episode is null ? Results.NotFound() : Results.Ok(episode);
})
.RequireRateLimiting("public");

app.MapGet("/api/polls/active", async (IRadioPulseService service, CancellationToken cancellationToken) =>
{
    var poll = await service.GetActivePollAsync(cancellationToken);
    return poll is null ? Results.NotFound() : Results.Ok(poll);
})
.RequireRateLimiting("public");

app.MapPost("/api/polls", async ([FromBody] CreatePollRequest request, IRadioPulseService service, CancellationToken cancellationToken) =>
{
    if (request.ShowId == Guid.Empty || string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 160)
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

app.MapPost("/api/polls/votes", async ([FromBody] CreateVoteRequest request, IRadioPulseService service, CancellationToken cancellationToken) =>
{
    if (request.PollId == Guid.Empty || request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Choice))
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
    await service.GetLatestShoutoutsAsync(50, cancellationToken))
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

app.MapPost("/api/shoutouts", async ([FromBody] CreateShoutoutRequest request, IRadioPulseService service, CancellationToken cancellationToken) =>
{
    if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 280)
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
