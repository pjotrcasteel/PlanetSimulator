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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SurfaceParameters? Surface { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RegionalParameters? Regional { get; init; }
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

    public void Validate()
    {
        if (FormatVersion != CurrentFormatVersion || ModelVersion != (Surface is not null ? SurfaceModelVersion : Regional is null ? CurrentModelVersion : RegionalModelVersion))
            throw new ArgumentException("Onbekende scenario- of modelversie.");
        if (Surface is not null && Regional is null) throw new ArgumentException("Waterreservoirs vereisen een regionaal model.");
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 80) throw new ArgumentException("Geef een naam van 1–80 tekens.");
        if (DurationDays is < 1 or > 730) throw new ArgumentException("De duur moet 1–730 dagen zijn.");
        if (Climate is null) throw new ArgumentException("Beginwaarden ontbreken.");
        if (Changes is null || Changes.Length > 32) throw new ArgumentException("Maximaal 32 wijzigingen toegestaan.");
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
