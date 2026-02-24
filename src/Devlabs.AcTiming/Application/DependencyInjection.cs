using System.Threading.Channels;
using Devlabs.AcTiming.Application.EventRouting;
using Devlabs.AcTiming.Application.LiveTiming;
using Devlabs.AcTiming.Application.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Devlabs.AcTiming.Application;

public static class DependencyInjection
{
    public static void AddApplicationLayer(this IServiceCollection services)
    {
        services.AddSingleton(
            new RealtimeBus(
                Channel.CreateBounded<SimEvent>(
                    new BoundedChannelOptions(50_000)
                    {
                        SingleWriter = true,
                        SingleReader = false,
                        FullMode = BoundedChannelFullMode.DropOldest,
                    }
                )
            )
        );

        services.AddSingleton(
            new PersistenceBus(
                Channel.CreateBounded<SimEvent>(
                    new BoundedChannelOptions(50_000)
                    {
                        SingleWriter = true,
                        SingleReader = false,
                        FullMode = BoundedChannelFullMode.Wait,
                    }
                )
            )
        );

        services.AddSingleton<ILiveTimingService, LiveTimingService>();
        services.AddHostedService<SimEventRouter>();
        services.AddHostedService<RealTimeProcessor>();
    }
}
