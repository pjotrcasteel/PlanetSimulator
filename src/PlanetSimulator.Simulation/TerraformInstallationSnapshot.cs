namespace PlanetSimulator.Simulation;

/// <summary>
/// Detached progress and resource use for one installation.
/// </summary>
public sealed record TerraformInstallationSnapshot
{
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required int CellIndex { get; init; }
    public required int DestinationCellIndex { get; init; }
    public required string Status { get; init; }
    public required double ConstructionFraction { get; init; }
    public required double ProcessedKilograms { get; init; }
    public required double EnergyUsedJoules { get; init; }
}
