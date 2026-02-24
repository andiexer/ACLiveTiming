using Devlabs.AcTiming.Application;
using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Infrastructure;
using Devlabs.AcTiming.Infrastructure.Persistence;
using Devlabs.AcTiming.Web.Components;
using Devlabs.AcTiming.Web.LiveTiming;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddSignalR();

builder.Services.AddApplicationLayer();
builder.Services.AddInfrastructureLayer(builder.Configuration);

builder.Services.AddHostedService<LiveTimingSnapshotBroadcaster>();
builder.Services.AddSingleton<TrackMapService>();
builder.Services.AddSingleton<IPitLaneProvider, PitLaneSplineLoader>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapHub<LiveTimingHub>(LiveTimingHub.HubUrl);

// Ensure DB exists on every startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AcTimingDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
