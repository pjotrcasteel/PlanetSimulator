using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlanetSimulator.Simulation;

/// <summary>
/// Strict JSON with explicit model identity; future versions require an intentional migration.
/// </summary>
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
        if (document.RootElement.TryGetProperty("planet", out var planet) && planet.ValueKind != JsonValueKind.Null)
        {
            if (planet.ValueKind != JsonValueKind.Object) throw new ArgumentException("Planeeteigenschappen zijn ongeldig.");
            foreach (var name in new[] { "radiusMeters", "massKilograms", "rotationPeriodSeconds" })
                if (!planet.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
                    throw new ArgumentException($"Planeeteigenschap {name} ontbreekt of is geen getal.");
        }
        if (document.RootElement.TryGetProperty("regional", out var regional) && regional.ValueKind != JsonValueKind.Null)
        {
            if (regional.ValueKind != JsonValueKind.Object) throw new ArgumentException("Regionale instellingen zijn ongeldig.");
            foreach (var name in new[] { "axialTiltDegrees", "heatDiffusionWattsPerSquareMeterKelvin", "yearDays" })
                if (!regional.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
                    throw new ArgumentException($"Regionale instelling {name} ontbreekt of is geen getal.");
        }
        if (document.RootElement.TryGetProperty("surface", out var surface) && surface.ValueKind != JsonValueKind.Null)
        {
            if (surface.ValueKind != JsonValueKind.Object) throw new ArgumentException("Oppervlakte-instellingen zijn ongeldig.");
            foreach (var name in new[] { "reliefMeters", "waterEquivalentDepthMeters" })
                if (!surface.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
                    throw new ArgumentException($"Oppervlakte-instelling {name} ontbreekt of is geen getal.");
        }
        if (document.RootElement.TryGetProperty("hydrology", out var hydrology) && hydrology.ValueKind != JsonValueKind.Null)
        {
            if (hydrology.ValueKind != JsonValueKind.Object) throw new ArgumentException("Hydrologische instellingen zijn ongeldig.");
            foreach (var name in new[] { "initialVaporEquivalentDepthMeters", "initialCloudEquivalentDepthMeters", "evaporationTimescaleHours", "condensationTimescaleHours", "precipitationTimescaleHours", "runoffTimescaleHours" })
                if (!hydrology.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
                    throw new ArgumentException($"Hydrologische instelling {name} ontbreekt of is geen getal.");
        }
        if (document.RootElement.TryGetProperty("atmosphere", out var atmosphere) && atmosphere.ValueKind != JsonValueKind.Null)
        {
            if (atmosphere.ValueKind != JsonValueKind.Object) throw new ArgumentException("Atmosfeerinstellingen zijn ongeldig.");
            foreach (var name in new[] { "surfacePressurePascals", "nitrogenMoleFraction", "oxygenMoleFraction", "carbonDioxideMoleFraction", "argonMoleFraction", "crustalCarbonDioxideMassPerSquareMeter" })
                if (!atmosphere.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
                    throw new ArgumentException($"Atmosfeerinstelling {name} ontbreekt of is geen getal.");
        }
        var scenario = JsonSerializer.Deserialize(json, Context.ExperimentScenario) ?? throw new ArgumentException("Scenario ontbreekt.");
        scenario.Validate();
        return scenario;
    }
}
