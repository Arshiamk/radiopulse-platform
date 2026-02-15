var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var database = postgres.AddDatabase("radiopulsedb");

// Use a stable dev password to avoid random special-character edge cases
// that can surface as transient local health-check failures on some setups.
var redisPassword = builder.AddParameter(
    "redis-password",
    "radiopulse-dev-redis-password",
    publishValueAsDefault: true);

var redis = builder.AddRedis("redis", port: null, password: redisPassword)
    .WithDataVolume();

var api = builder.AddProject<Projects.RadioPulse_Api>("api")
    .WithExternalHttpEndpoints()
    .WithReference(database)
    .WithReference(redis)
    .WaitFor(database)
    .WaitFor(redis);

builder.AddProject<Projects.RadioPulse_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.RadioPulse_Worker>("worker")
    .WithReference(api)
    .WithReference(database)
    .WithReference(redis)
    .WaitFor(api)
    .WaitFor(database)
    .WaitFor(redis);

builder.Build().Run();
