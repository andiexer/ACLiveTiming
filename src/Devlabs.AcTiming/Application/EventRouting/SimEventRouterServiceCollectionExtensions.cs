using Devlabs.AcTiming.Application.EventRouting.Enricher;
using Microsoft.Extensions.DependencyInjection;

namespace Devlabs.AcTiming.Application.EventRouting;

public static class SimEventRouterServiceCollectionExtensions
{
    public static IServiceCollection AddSimEventRouter(
        this IServiceCollection services,
        Action<SimEventRouterBuilder> configure
    )
    {
        services.AddOptions<SimEventRouterOptions>();

        var builder = new SimEventRouterBuilder(services);
        configure(builder);

        services.AddHostedService<SimEventRouter>();
        return services;
    }
}

public sealed class SimEventRouterBuilder(IServiceCollection services)
{
    public SimEventRouterBuilder AddPreEventEnricher<T>()
        where T : class, ISimEventEnricher => AddEnricher(EnricherPhase.Pre, typeof(T));

    public SimEventRouterBuilder AddPostEventEnricher<T>()
        where T : class, ISimEventEnricher => AddEnricher(EnricherPhase.Post, typeof(T));

    private SimEventRouterBuilder AddEnricher(EnricherPhase phase, Type type)
    {
        services.AddSingleton(typeof(ISimEventEnricher), type);
        services.Configure<SimEventRouterOptions>(options =>
        {
            options.Enrichers.Add(new EnricherRegistration(type, phase));
        });
        return this;
    }
}

public sealed class SimEventRouterOptions
{
    public List<EnricherRegistration> Enrichers { get; } = new();
}

public sealed record EnricherRegistration(Type EnricherType, EnricherPhase Phase);
