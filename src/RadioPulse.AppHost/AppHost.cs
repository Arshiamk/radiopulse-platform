var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.RadioPulse_Api>("api")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.RadioPulse_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.RadioPulse_Worker>("worker")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
