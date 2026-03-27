namespace Devlabs.AcTiming.Domain.Shared;

public class CarDefinition
{
    public int Id { get; set; }

    /// <summary>AC car model string, e.g. "ks_corvette_c7r". Unique.</summary>
    public required string Model { get; set; }

    /// <summary>Friendly brand name, e.g. "Chevrolet". Null until configured.</summary>
    public string? Brand { get; set; }

    /// <summary>Friendly model name, e.g. "Corvette C7.R". Null until configured.</summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// CDN logo slug (filippofilip95/car-logos-dataset).
    /// When null, derived automatically as Brand.ToLower().
    /// </summary>
    public string? LogoSlug { get; set; }

    /// <summary>False = auto-discovered but not yet named by admin.</summary>
    public bool IsConfigured { get; set; }

    /// <summary>Effective slug used for CDN logo URL resolution.</summary>
    public string? EffectiveSlug => LogoSlug ?? Brand?.ToLower();
}
