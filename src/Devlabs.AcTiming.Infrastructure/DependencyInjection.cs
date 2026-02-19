using Devlabs.AcTiming.Application.LiveTiming;
using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Infrastructure.AcServer;
using Devlabs.AcTiming.Infrastructure.Persistence;
using Devlabs.AcTiming.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devlabs.AcTiming.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Connection string 'Default' not found.");
        services.AddDbContext<AcTimingDbContext>(options =>
            options.UseSqlite(connectionString));

        services.Configure<AcServerOptions>(configuration.GetSection(AcServerOptions.SectionName));
        services.AddSingleton<ILiveTimingService, LiveTimingService>();
        services.AddSingleton<IAcUdpClient, AcUdpClient>();
        services.AddSingleton<AcServerEventProcessor>();
        services.AddHostedService<AcServerBackgroundService>();

        return services;
    }
}
