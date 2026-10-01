using System.Text.Json.Serialization;

namespace PlanetSimulator.Simulation;

[JsonSerializable(typeof(ExperimentScenario))]
internal partial class ScenarioJsonContext : JsonSerializerContext;
