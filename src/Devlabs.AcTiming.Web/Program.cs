using Devlabs.AcTiming.Infrastructure;
using Devlabs.AcTiming.Infrastructure.Persistence;
using Devlabs.AcTiming.Web.Components;
using Devlabs.AcTiming.Web.Hubs;
using Devlabs.AcTiming.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();


builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<TimingHubNotifier>();
builder.Services.AddSingleton<TrackMapService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<TimingHub>(TimingHub.HubUrl);

// Ensure DB exists on every startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AcTimingDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
