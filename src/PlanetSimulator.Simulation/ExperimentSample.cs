namespace PlanetSimulator.Simulation;

/// <summary>
/// Stores daily global diagnostics and a detached field for spatial experiments.
/// </summary>
public sealed record ExperimentSample
{
    public required int Day { get; init; }
    public required double TemperatureKelvin { get; init; }
    public required double EquilibriumTemperatureKelvin { get; init; }
    public required double AbsorbedWattsPerSquareMeter { get; init; }
    public required double EmittedWattsPerSquareMeter { get; init; }
    public required double AbsorbedJoulesPerSquareMeter { get; init; }
    public required double EmittedJoulesPerSquareMeter { get; init; }
    public required double BudgetErrorJoulesPerSquareMeter { get; init; }
    public RegionalSnapshot? Regional { get; init; }
}
