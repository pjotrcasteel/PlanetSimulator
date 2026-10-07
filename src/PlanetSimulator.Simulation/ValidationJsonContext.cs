using System.Text.Json.Serialization;

namespace PlanetSimulator.Simulation;

[JsonSerializable(typeof(ValidationReport))]
internal partial class ValidationJsonContext : JsonSerializerContext;
