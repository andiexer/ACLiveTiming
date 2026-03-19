using ApexCharts;
using Devlabs.AcTiming.Application;
using Devlabs.AcTiming.Infrastructure;
using Devlabs.AcTiming.Infrastructure.Persistence;
using Devlabs.AcTiming.Web.Auth;
using Devlabs.AcTiming.Web.Components;
using Devlabs.AcTiming.Web.LiveTiming;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRazorPages();
builder.Services.AddApexCharts();

builder.Services.AddSingleton<PasswordService>();
builder
    .Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.Cookie.Name = "actiming.auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.SlidingExpiration = true;
        o.ExpireTimeSpan = TimeSpan.FromDays(1);
    });
builder.Services.AddAuthorization();

builder.Services.AddApplicationLayer();
builder.Services.AddInfrastructureLayer(builder.Configuration);

builder.Services.AddSingleton<LiveTimingBroadcaster>();
builder.Services.AddHostedService<LiveTimingSnapshotBroadcaster>();
builder.Services.AddSingleton<TrackMapService>();

var app = builder.Build();

app.UseForwardedHeaders(
    new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    }
);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Apply any pending migrations on startup (creates DB on first run)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AcTimingDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
