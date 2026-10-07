namespace PlanetSimulator.Simulation;

/// <summary>Source-generated JSON for deterministic validation artifacts and trim-safe browser builds.</summary>
public static class ValidationJson
{
    private static readonly ValidationJsonContext Context = new(new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    });

    public static string Serialize(ValidationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return System.Text.Json.JsonSerializer.Serialize(report, Context.ValidationReport);
    }
}
