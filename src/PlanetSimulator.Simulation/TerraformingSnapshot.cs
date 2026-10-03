namespace PlanetSimulator.Simulation;

/// <summary>
/// Infrastructure stores, shipments and closed engineering budgets, including all cells.
/// </summary>
public sealed record TerraformingSnapshot
{
    public required IReadOnlyList<TerraformInstallationSnapshot> Installations { get; init; }
    public required IReadOnlyList<double> RemainingEnergyJoulesPerSquareMeter { get; init; }
    public required IReadOnlyList<double> StoredCarbonDioxideKilogramsPerSquareMeter { get; init; }
    public required IReadOnlyList<double> InTransitCarbonDioxideKilogramsPerSquareMeter { get; init; }
    public required IReadOnlyList<double> RemainingMaterialKilogramsPerSquareMeter { get; init; }
    public required double TotalRemainingEnergyJoules { get; init; }
    public required double TotalUsedEnergyJoules { get; init; }
    public required double TotalRemainingMaterialKilograms { get; init; }
    public required double TotalBuiltMaterialKilograms { get; init; }
    public required double TotalStoredCarbonDioxideKilograms { get; init; }
    public required double TotalInTransitCarbonDioxideKilograms { get; init; }
    public required double CumulativeCapturedKilograms { get; init; }
    public required double CumulativeDeliveredKilograms { get; init; }
    public required double EnergyBudgetErrorJoulesPerSquareMeter { get; init; }
    public required double MaterialBudgetErrorKilograms { get; init; }
    public required double LogisticsBudgetErrorKilograms { get; init; }
}
