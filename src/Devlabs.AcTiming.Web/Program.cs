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

// Generate pit_lane.ai for the simulator track if missing
EnsureSimulatorPitLane(Path.Combine(app.Environment.WebRootPath, "maps", "simulator"));

app.Run();

// Generates a synthetic pit_lane.ai for the simulator track.
// Binary format matches real AC pit_lane.ai (version 7):
//   24-byte header: int32 version=7, int32 count, 16 bytes padding
//   N × 20-byte records: float worldZ, float cumDist, float 0, float worldX, float elevY
// Simulator pit lane: vertical line at X=500, Z from -140 to +140.
// Z range is intentionally narrower than the physical pit lane (-200..+200) so the
// detection zone does not overlap the main track near entry/exit. Derived from map.ini:
//   worldZ = py × SCALE_FACTOR − Z_OFFSET  →  py=180 → Z=−140,  py=320 → Z=+140
static void EnsureSimulatorPitLane(string dir)
{
    var path = Path.Combine(dir, "pit_lane.ai");
    // Always regenerate so parameter changes take effect without manual file deletion.
    File.Delete(path);

    const float pitX = 500f;
    const float startZ = -140f;
    const int pointCount = 281; // -140 to +140 inclusive (1m steps)

    using var fs = File.Create(path);
    using var w = new BinaryWriter(fs);

    // 24-byte header
    w.Write(7); // version
    w.Write(pointCount); // count
    w.Write(new byte[16]); // padding

    for (var i = 0; i < pointCount; i++)
    {
        var worldZ = startZ + i;
        w.Write(worldZ); // f[0] worldZ
        w.Write((float)i); // f[1] cumulative distance (metres)
        w.Write(0f); // f[2] unused
        w.Write(pitX); // f[3] worldX
        w.Write(0f); // f[4] elevation
    }
}
