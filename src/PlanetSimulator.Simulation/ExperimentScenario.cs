using System.Text.Json.Serialization;

namespace PlanetSimulator.Simulation;

/// <summary>
/// Versioned initial conditions and forcing changes; elapsed time always starts at zero.
/// </summary>
public sealed record ExperimentScenario
{
    public const int CurrentFormatVersion = 1;
    public const string CurrentModelVersion = "global-blackbody-rk4-60s-v1";
    public const string RegionalModelVersion = "regional-blackbody-rk4-60s-12x24-v1";
    public const string SurfaceModelVersion = "surface-enthalpy-rk4-60s-12x24-v1";
    public const string HydrologyModelVersion = "hydrology-rk4-60s-12x24-v1";
    public const string TerraformingModelVersion = "terraforming-inventory-rk4-60s-12x24-v1";
    public const string BiologyModelVersion = "microbial-ch2o-rk4-60s-12x24-v1";
    public const string AtmosphereModelVersion = "atmosphere-chemistry-rk4-60s-12x24-v1";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SurfaceParameters? Surface { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RegionalParameters? Regional { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WaterCycleParameters? Hydrology { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AtmosphereParameters? Atmosphere { get; init; }
    public required int FormatVersion { get; init; }
    public required string ModelVersion { get; init; }
    public required string Name { get; init; }
    public required int DurationDays { get; init; }
    public required ClimateParameters Climate { get; init; }
    public ForcingChange[] Changes { get; init; } = [];

    public static ExperimentScenario Create(string name, int durationDays, ClimateParameters climate, params ForcingChange[] changes)
    {
        var scenario = new ExperimentScenario
        {
            FormatVersion = CurrentFormatVersion,
            ModelVersion = CurrentModelVersion,
            Name = name,
            DurationDays = durationDays,
            Climate = climate,
            Changes = changes,
        };
        scenario.Validate();
        return scenario;
    }

    public static ExperimentScenario CreateRegional(string name, int durationDays, ClimateParameters climate, RegionalParameters regional, params ForcingChange[] changes)
    {
        ArgumentNullException.ThrowIfNull(regional);
        var scenario = Create(name, durationDays, climate, changes) with { Regional = regional, ModelVersion = RegionalModelVersion };
        scenario.Validate();
        return scenario;
    }

    public static ExperimentScenario CreateSurface(string name, int durationDays, ClimateParameters climate,
        RegionalParameters regional, SurfaceParameters surface, params ForcingChange[] changes)
    {
        ArgumentNullException.ThrowIfNull(surface);
        var scenario = CreateRegional(name, durationDays, climate, regional, changes) with { Surface = surface, ModelVersion = SurfaceModelVersion };
        scenario.Validate();
        return scenario;
    }

    public static ExperimentScenario CreateHydrology(string name, int durationDays, ClimateParameters climate, RegionalParameters regional,
        SurfaceParameters surface, WaterCycleParameters hydrology)
    {
        ArgumentNullException.ThrowIfNull(hydrology);
        var scenario = CreateSurface(name, durationDays, climate, regional, surface) with
        {
            Hydrology = hydrology,
            ModelVersion = HydrologyModelVersion,
        };
        scenario.Validate();
        return scenario;
    }

    public static ExperimentScenario CreateAtmosphere(string name, int durationDays, ClimateParameters climate, RegionalParameters regional,
        AtmosphereParameters atmosphere, params ForcingChange[] changes)
    {
        ArgumentNullException.ThrowIfNull(atmosphere);
        var scenario = CreateRegional(name, durationDays, climate, regional, changes) with
        {
            Atmosphere = atmosphere,
            ModelVersion = GetAtmosphereModelVersion(atmosphere),
        };
        scenario.Validate();
        return scenario;
    }

    public static ExperimentScenario CreateAtmosphereWithSurface(string name, int durationDays, ClimateParameters climate,
        RegionalParameters regional, SurfaceParameters surface, AtmosphereParameters atmosphere)
    {
        ArgumentNullException.ThrowIfNull(atmosphere);
        var scenario = CreateSurface(name, durationDays, climate, regional, surface) with
        {
            Atmosphere = atmosphere,
            ModelVersion = GetAtmosphereModelVersion(atmosphere),
        };
        scenario.Validate();
        return scenario;
    }

    public static string GetAtmosphereModelVersion(AtmosphereParameters atmosphere) => atmosphere.Terraforming is not null ? TerraformingModelVersion
        : atmosphere.Biology is not null ? BiologyModelVersion : AtmosphereModelVersion;

    public void Validate()
    {
        var expectedVersion = Atmosphere is not null ? GetAtmosphereModelVersion(Atmosphere) :
            Hydrology is not null ? HydrologyModelVersion :
            Surface is not null ? SurfaceModelVersion : Regional is null ? CurrentModelVersion : RegionalModelVersion;
        if (FormatVersion != CurrentFormatVersion || ModelVersion != expectedVersion)
            throw new ArgumentException("Onbekende scenario- of modelversie.");
        if (Surface is not null && Regional is null) throw new ArgumentException("Waterreservoirs vereisen een regionaal model.");
        if (Hydrology is not null && (Surface is null || Regional is null)) throw new ArgumentException("Hydrologie vereist oppervlak en regionaal model.");
        if (Atmosphere is not null && Regional is null) throw new ArgumentException("De atmosfeer vereist een regionaal model.");
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 80) throw new ArgumentException("Geef een naam van 1–80 tekens.");
        if (DurationDays is < 1 or > 730) throw new ArgumentException("De duur moet 1–730 dagen zijn.");
        if (Climate is null) throw new ArgumentException("Beginwaarden ontbreken.");
        if (Changes is null || Changes.Length > 32) throw new ArgumentException("Maximaal 32 wijzigingen toegestaan.");
        if (Atmosphere?.Biology is not null && Surface is null) throw new ArgumentException("Micro-organismen vereisen waterreservoirs.");
        if (Atmosphere?.Terraforming is not null && Surface is null) throw new ArgumentException("Terraforming vereist oppervlakte-reservoirs.");
        Atmosphere?.ValidateRuntimeValues();
        var previousDay = 0;
        foreach (var change in Changes)
        {
            if (change is null || change.Day <= previousDay || change.Day >= DurationDays)
                throw new ArgumentException("Wijzigingsdagen moeten oplopen en tussen start en einde liggen.");
            _ = new ClimateParameters(change.DistanceAstronomicalUnits, change.BondAlbedo);
            previousDay = change.Day;
        }
    }
}
