namespace PlanetSimulator.Simulation;

/// <summary>
/// Supplies a detached temperature map and regional diagnostics for rendering and inspection.
/// </summary>
public sealed record RegionalSnapshot
{
    public SurfaceSnapshot? Surface { get; init; }
    public required IReadOnlyList<double> TemperaturesKelvin { get; init; }
    public required double MinimumTemperatureKelvin { get; init; }
    public required double MaximumTemperatureKelvin { get; init; }
    public required double NorthMeanTemperatureKelvin { get; init; }
    public required double SouthMeanTemperatureKelvin { get; init; }
    public required double SolarDeclinationRadians { get; init; }
    public required double BudgetErrorJoulesPerSquareMeter { get; init; }
}
