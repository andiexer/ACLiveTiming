using Devlabs.AcTiming.Application.EventRouting.Enricher;
using Microsoft.Extensions.DependencyInjection;

namespace Devlabs.AcTiming.Application.EventRouting;

public static class SimEventRouterServiceCollectionExtensions
{
    public static void AddSimEventRouter(
        this IServiceCollection services,
        Action<SimEventRouterBuilder> configure
    )
    {
        var builder = new SimEventRouterBuilder(services);
        configure(builder);

        services.AddHostedService<SimEventRouter>();
    }
}

public sealed class SimEventRouterBuilder(IServiceCollection services)
{
    public SimEventRouterBuilder AddEnricher<T>()
        where T : class, ISimEventEnricher
    {
        services.AddSingleton<ISimEventEnricher, T>();
        return this;
    }
}
