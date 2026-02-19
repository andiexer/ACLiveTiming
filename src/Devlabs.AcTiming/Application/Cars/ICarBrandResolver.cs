namespace Devlabs.AcTiming.Application.Cars;

public interface ICarBrandResolver
{
    /// <summary>
    /// Returns a normalised brand identifier (e.g. "ferrari") for the given AC car model string,
    /// or <c>null</c> if the brand is not recognised.
    /// </summary>
    CarBrandModel? Resolve(string carModel);

    public record CarBrandModel(string Brand, string Model)
    {
        public override string ToString() => $"{Brand} {Model}";
        public string Slug => Brand.ToLower();
    }
}
