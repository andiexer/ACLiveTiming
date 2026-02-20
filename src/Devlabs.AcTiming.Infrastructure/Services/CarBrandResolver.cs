using Devlabs.AcTiming.Application.Cars;

namespace Devlabs.AcTiming.Infrastructure.Services;

// TODO: proof of concept implementation, needs to go to the database in the future
public sealed class CarBrandResolver : ICarBrandResolver
{
    // Slug values must match the filippofilip95/car-logos-dataset naming (lowercase, hyphenated).
    // Dataset browser: https://filippofilip95.github.io/car-logos-dataset-web/
    private static readonly (string Keyword, string Brand, string Model)[] Mappings =
    [
        ("ks_ferrari_f2004", "Ferrari", "F2004"),
        ("ks_ferrari_sf70h", "Ferrari", "SF70H"),
        ("ks_ferrari_488_gt3", "Ferrari", "488 GT3"),
        ("ks_porsche_911_gt3_cup_2017", "Porsche", "911 GT3 Cup 2017"),
        ("ks_porsche_911_gt3_r", "Porsche", "911 GT3"),
        ("ks_lamborghini_huracan_gt3", "Lamborghini", "Huracan GT3"),
        ("ks_mclaren_720s_gt3", "McLaren", "720S GT3"),
        ("ks_mercedes_amg_gt3", "Mercedes-AMG", "GT3"),
        ("ks_audi_r8_lms_2016", "Audi", "R8 LMS 2016"),
        ("ks_mclaren_650s_gt3", "McLaren", "650S GT3"),
        ("ks_bmw_m6_gt3", "BMW", "M6 GT3"),
        ("ks_nissan_gtr_gt3", "Nissan", "GT-R GT3"),
        ("ks_corvette_c7r", "Chevrolet", "Corvette C7.R"),
        ("ks_ford_gt40", "Ford", "GT40"),
        ("ks_audi_r18_etron_quattro", "Audi","R18 e-tron quattro"),
        ("bmw_m3_gt2", "BMW", "M3 GT2")
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