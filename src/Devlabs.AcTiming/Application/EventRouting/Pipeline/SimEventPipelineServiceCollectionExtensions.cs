using Devlabs.AcTiming.Application.EventRouting.Pipeline.Abstractions;
using Devlabs.AcTiming.Application.EventRouting.Pipeline.Sink;
using Microsoft.Extensions.DependencyInjection;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline;

public static class SimEventPipelineServiceCollectionExtensions
{
    public static void AddSimEventPipeline(
        this IServiceCollection services,
        Action<SimEventPipelineBuilder> configure
    )
    {
        services.AddHostedService<SimEventRouterHostedService>();
        services.AddSingleton<SimEventPipeline>();
        var builder = new SimEventPipelineBuilder(services);
        configure(builder);
    }
}

public sealed class SimEventPipelineBuilder(IServiceCollection services)
{
    public SimEventPipelineBuilder AddEnricher<T>()
        where T : class, ISimEventEnricher
    {
        services.AddSingleton<ISimEventEnricher, T>();
        return this;
    }

    public SimEventPipelineBuilder AddSink<T>()
        where T : class, ISimEventSink
    {
        services.AddSingleton<ISimEventSink, T>();
        return this;
    }
}
