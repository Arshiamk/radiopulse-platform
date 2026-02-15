using RadioPulse.Web.Components;
using RadioPulse.Web.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var apiClientBuilder = builder.Services.AddHttpClient("RadioPulse.Api", client =>
{
    client.BaseAddress = new Uri("https://api");
})
.AddServiceDiscovery();

if (builder.Environment.IsDevelopment())
{
    apiClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<DemoAuthSession>();

var app = builder.Build();
var enableHttpsRedirection = builder.Configuration.GetValue("EnableHttpsRedirection", app.Environment.IsDevelopment());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
