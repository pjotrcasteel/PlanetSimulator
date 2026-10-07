namespace PlanetSimulator.Simulation;

/// <summary>
/// Reproducible synthetic planet archetypes. These are simulator starting points, not calibrated Solar System reconstructions.
/// </summary>
public static class PlanetScenarioCatalog
{
    public static IReadOnlyList<PlanetScenarioTemplate> Templates { get; } = Array.AsReadOnly(new[]
    {
        new PlanetScenarioTemplate("bare-rock", "Kale rotswereld", "Eenvoudige globale energiebalans zonder water of atmosfeer."),
        new PlanetScenarioTemplate("ice-world", "IJswereld", "Regionale koude wereld met reliëf en een bevroren waterinventaris."),
        new PlanetScenarioTemplate("ocean-world", "Oceaanwereld", "Warme waterwereld met hydrologische cyclus en regionale seizoenen."),
        new PlanetScenarioTemplate("thin-atmosphere", "Dunne atmosfeer", "Regionale rotswereld met een dunne synthetische gaslaag en CO₂."),
        new PlanetScenarioTemplate("living-world", "Microbiële wereld", "Water, atmosfeer en een expliciet ingebracht microbieel inoculum."),
        new PlanetScenarioTemplate("terraforming-candidate", "Terraforming-kandidaat", "Volledig gekoppeld demonstratiescenario met installaties en eindige voorraden."),
        new PlanetScenarioTemplate("long-climate", "Langjarige klimaatreferentie", "Tien jaar globale klimaatdynamiek met maandelijkse uitvoer.")
    });

    public static ExperimentScenario Create(string id) => id switch
    {
        "bare-rock" => ExperimentScenario.Create("Kale rotswereld", 365, new ClimateParameters(bondAlbedo: 0.22, arealHeatCapacity: 5e6)),
        "ice-world" => ExperimentScenario.CreateSurface("IJswereld", 365,
            new ClimateParameters(bondAlbedo: 0.55, initialTemperatureKelvin: 225), new RegionalParameters(), new SurfaceParameters(1200, 2)),
        "ocean-world" => ExperimentScenario.CreateHydrology("Oceaanwereld", 180,
            new ClimateParameters(bondAlbedo: 0.3, initialTemperatureKelvin: 285), new RegionalParameters(), new SurfaceParameters(800, 3),
            new WaterCycleParameters()),
        "thin-atmosphere" => ExperimentScenario.CreateAtmosphere("Dunne atmosfeer", 180,
            new ClimateParameters(bondAlbedo: 0.28, initialTemperatureKelvin: 245), new RegionalParameters(),
            new AtmosphereParameters(12000, 0.949, 0.02, 0.03, 0.001, 25)),
        "living-world" => CreateCoupled("Microbiële wereld", 60, new AtmosphereParameters { Biology = new BiologyParameters() }),
        "terraforming-candidate" => CreateCoupled("Terraforming-kandidaat", 30,
            new AtmosphereParameters { Biology = new BiologyParameters(), Terraforming = new TerraformingParameters() }),
        "long-climate" => ExperimentScenario.Create("Langjarige klimaatreferentie", 3650,
            new ClimateParameters(initialTemperatureKelvin: 260)) with { SampleEveryDays = 30 },
        _ => throw new ArgumentException($"Onbekend planeettype: {id}.", nameof(id)),
    };

    private static ExperimentScenario CreateCoupled(string name, int durationDays, AtmosphereParameters atmosphere)
    {
        var scenario = ExperimentScenario.CreateSurface(name, durationDays, new ClimateParameters(initialTemperatureKelvin: 285),
            new RegionalParameters(), new SurfaceParameters(500, 3)) with
        {
            Hydrology = new WaterCycleParameters(),
            Atmosphere = atmosphere,
            ModelVersion = ExperimentScenario.GetAtmosphereModelVersion(atmosphere),
        };
        scenario.Validate();
        return scenario;
    }
}

public sealed record PlanetScenarioTemplate(string Id, string Name, string Description);
