using System.Threading.Channels;
using Devlabs.AcTiming.Application.EventRouting.Pipeline;
using Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.Pit;
using Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.SectorTiming;
using Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.SpeedTrap;
using Devlabs.AcTiming.Application.EventRouting.Pipeline.Sink;
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
        services.AddSingleton<SectorTimingTracker>();
        services.AddSingleton<PitStatusTracker>();
        services.AddSingleton<SpeedTrapTracker>();

        // routing
        services.AddSimEventPipeline(config =>
        {
            config.AddEnricher<SectorTimingEnricher>();
            config.AddEnricher<PitStatusEnricher>();
            config.AddEnricher<SpeedTrapEnricher>();

            config.AddSink<ChannelSink>();
        });

        services.AddHostedService<RealTimeProcessor>();
    }
}
