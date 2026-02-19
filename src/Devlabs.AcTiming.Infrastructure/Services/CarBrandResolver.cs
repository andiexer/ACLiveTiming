using Devlabs.AcTiming.Application.Cars;

namespace Devlabs.AcTiming.Infrastructure.Services;


public sealed class CarBrandResolver : ICarBrandResolver
{
    // Ordered: first match wins. More specific keywords should come before generic ones.
    // Slug values must match the filippofilip95/car-logos-dataset naming (lowercase, hyphenated).
    // Dataset browser: https://filippofilip95.github.io/car-logos-dataset-web/
    private static readonly (string Keyword, string Brand, string Model)[] Mappings =
    [
        ("ks_ferrari_f2004",        "Ferrari", "F2004"),
        ("ks_ferrari_sf70h",        "Ferrari", "SF70H"),

        ("ks_porsche_911_gt3_cup_2017",        "Porsche", "911 GT3 Cup 2017")
    ];

    public ICarBrandResolver.CarBrandModel? Resolve(string carModel)
    {
        if (string.IsNullOrWhiteSpace(carModel))
            return null;

        foreach (var (keyword, brand, model) in Mappings)
        {
            if (carModel.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return new ICarBrandResolver.CarBrandModel(brand, model);
        }

        return null;
    }
}
