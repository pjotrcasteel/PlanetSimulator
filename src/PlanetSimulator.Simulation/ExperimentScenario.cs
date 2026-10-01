using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlanetSimulator.Simulation;

/// <summary>Versioned initial conditions and forcing changes; elapsed time always starts at zero.</summary>
public sealed record ExperimentScenario
{
    public const int CurrentFormatVersion = 1;
    public const string CurrentModelVersion = "global-blackbody-rk4-60s-v1";
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

    public void Validate()
    {
        if (FormatVersion != CurrentFormatVersion || ModelVersion != CurrentModelVersion)
            throw new ArgumentException("Onbekende scenario- of modelversie.");
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

public sealed record ForcingChange
{
    public required int Day { get; init; }
    public required double DistanceAstronomicalUnits { get; init; }
    public required double BondAlbedo { get; init; }
}

/// <summary>Strict JSON with explicit model identity. Future versions require an intentional migration.</summary>
public static class ScenarioJson
{
    public const int MaximumBytes = 65536;
    private static readonly ScenarioJsonContext Context = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    });

    public static string Serialize(ExperimentScenario scenario)
    {
        scenario.Validate();
        return JsonSerializer.Serialize(scenario, Context.ExperimentScenario);
    }

    public static ExperimentScenario Deserialize(string json)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaximumBytes) throw new ArgumentException("Scenariobestand is te groot (maximaal 64 KiB).");
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("climate", out var climate) || climate.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Beginwaarden ontbreken.");
        foreach (var name in new[] { "distanceAstronomicalUnits", "bondAlbedo", "arealHeatCapacity", "initialTemperatureKelvin", "stellarLuminositySolarUnits" })
            if (!climate.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
                throw new ArgumentException($"Beginwaarde {name} ontbreekt of is geen getal.");
        var scenario = JsonSerializer.Deserialize(json, Context.ExperimentScenario) ?? throw new ArgumentException("Scenario ontbreekt.");
        scenario.Validate();
        return scenario;
    }
}

[JsonSerializable(typeof(ExperimentScenario))]
internal partial class ScenarioJsonContext : JsonSerializerContext;
