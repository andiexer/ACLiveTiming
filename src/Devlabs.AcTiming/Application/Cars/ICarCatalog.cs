namespace Devlabs.AcTiming.Application.Cars;

public interface ICarCatalog
{
    CarBrandModel? Resolve(string carModel);

    Task EnsureRegisteredAsync(string carModel);

    void Refresh();
}

public record CarBrandModel(string Brand, string DisplayName, string? Slug)
{
    public override string ToString() => $"{Brand} {DisplayName}";
}
