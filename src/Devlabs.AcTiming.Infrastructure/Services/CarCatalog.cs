using Devlabs.AcTiming.Application.Cars;
using Devlabs.AcTiming.Domain.Shared;
using Devlabs.AcTiming.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Devlabs.AcTiming.Infrastructure.Services;

public sealed class CarCatalog : ICarCatalog
{
    private readonly IServiceScopeFactory _scopeFactory;

    // Immutable snapshot replaced atomically on refresh — lock-free reads.
    private volatile CatalogState _state;

    public CarCatalog(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _state = Load();
    }

    public CarBrandModel? Resolve(string carModel)
    {
        if (string.IsNullOrWhiteSpace(carModel))
            return null;
        _state.Resolved.TryGetValue(carModel, out var result);
        return result;
    }

    public async Task EnsureRegisteredAsync(string carModel)
    {
        if (string.IsNullOrWhiteSpace(carModel))
            return;

        // Fast path — already known (configured or discovered before).
        if (_state.AllKnown.Contains(carModel))
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcTimingDbContext>();

        var exists = await db.CarDefinitions.AnyAsync(c => c.Model == carModel);
        if (!exists)
        {
            db.CarDefinitions.Add(new CarDefinition { Model = carModel, IsConfigured = false });
            await db.SaveChangesAsync();
        }

        // Reload so AllKnown is updated and further calls hit the fast path.
        _state = Load();
    }

    public void Refresh() => _state = Load();

    private CatalogState Load()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcTimingDbContext>();

        var all = db.CarDefinitions.AsNoTracking().ToList();

        var resolved = all.Where(c => c.IsConfigured && c.Brand != null && c.DisplayName != null)
            .ToDictionary(
                c => c.Model,
                c => new CarBrandModel(c.Brand!, c.DisplayName!, c.EffectiveSlug)
            );

        var allKnown = all.Select(c => c.Model).ToHashSet();

        return new CatalogState(resolved, allKnown);
    }

    private record CatalogState(
        IReadOnlyDictionary<string, CarBrandModel> Resolved,
        IReadOnlySet<string> AllKnown
    );
}
