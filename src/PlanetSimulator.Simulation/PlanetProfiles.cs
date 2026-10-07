namespace PlanetSimulator.Simulation;

/// <summary>
/// Reproducible starting points for experiments. Profiles are illustrative worlds, not calibrated predictions of real planets.
/// </summary>
public static class PlanetProfiles
{
    public const string ReferenceTerrestrialId = "reference-terrestrial";
    public const string SmallDryId = "small-dry";
    public const string OceanSuperEarthId = "ocean-super-earth";

    public static IReadOnlyList<PlanetProfile> All { get; } = Array.AsReadOnly(new[]
    {
        new PlanetProfile
        {
            Id = ReferenceTerrestrialId,
            Name = "Referentie-aarde",
            Description = "De bestaande aardgrote referentiewereld; behoudt de eerdere simulatorstandaard.",
            Planet = new PlanetParameters(),
            Climate = new ClimateParameters(),
            Regional = new RegionalParameters(),
            Surface = new SurfaceParameters(),
            Hydrology = new WaterCycleParameters(),
            Atmosphere = new AtmosphereParameters(),
        },
        CreateSmallDry(),
        CreateOceanSuperEarth(),
    });

    public static PlanetProfile Get(string id) => All.SingleOrDefault(profile => profile.Id == id)
        ?? throw new ArgumentException("Onbekend planeetprofiel.", nameof(id));

    private static PlanetProfile CreateSmallDry()
    {
        var planet = new PlanetParameters(3_390_000, 6.42e23, 88_775);
        return new PlanetProfile
        {
            Id = SmallDryId,
            Name = "Kleine droge wereld",
            Description = "Een koude Mars-schaalwereld met weinig water en een dunne CO₂-rijke atmosfeer; geen Mars-kalibratie.",
            Planet = planet,
            Climate = new ClimateParameters(1.52, 0.25, 2e6, 210),
            Regional = new RegionalParameters(25, 0.15, 687),
            Surface = new SurfaceParameters(3000, 0.01),
            Hydrology = new WaterCycleParameters(initialVaporEquivalentDepthMeters: 0.000001, initialCloudEquivalentDepthMeters: 0),
            Atmosphere = new AtmosphereParameters(700, 0.027, 0.001, 0.953, 0.019, 10)
            {
                SurfaceGravityMetersPerSecondSquared = planet.SurfaceGravityMetersPerSecondSquared,
            },
        };
    }

    private static PlanetProfile CreateOceanSuperEarth()
    {
        var planet = new PlanetParameters(7_645_200, 1.1944e25, 108_000);
        return new PlanetProfile
        {
            Id = OceanSuperEarthId,
            Name = "Oceaan-superaarde",
            Description = "Een fictieve grotere waterwereld met sterkere zwaartekracht, diep water en een dichtere atmosfeer.",
            Planet = planet,
            Climate = new ClimateParameters(0.9, 0.35, 2e7, 285),
            Regional = new RegionalParameters(15, 0.9, 300),
            Surface = new SurfaceParameters(1500, 500),
            Hydrology = new WaterCycleParameters(),
            Atmosphere = new AtmosphereParameters(200_000, 0.85, 0.14, 0.001, 0.009, 100)
            {
                SurfaceGravityMetersPerSecondSquared = planet.SurfaceGravityMetersPerSecondSquared,
            },
        };
    }
}

public sealed record PlanetProfile
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required PlanetParameters Planet { get; init; }
    public required ClimateParameters Climate { get; init; }
    public required RegionalParameters Regional { get; init; }
    public required SurfaceParameters Surface { get; init; }
    public required WaterCycleParameters Hydrology { get; init; }
    public required AtmosphereParameters Atmosphere { get; init; }
}
