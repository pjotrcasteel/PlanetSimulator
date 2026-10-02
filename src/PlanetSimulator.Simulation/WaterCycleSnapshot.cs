namespace PlanetSimulator.Simulation;

/// <summary>
/// Detached water-cycle state and global conservation diagnostics.
/// </summary>
public sealed record WaterCycleSnapshot
{
    public required IReadOnlyList<double> VaporMassPerSquareMeter { get; init; }
    public required IReadOnlyList<double> CloudMassPerSquareMeter { get; init; }
    public required IReadOnlyList<double> RunoffMassPerSquareMeter { get; init; }
    public required double TotalVaporMassKilograms { get; init; }
    public required double TotalCloudMassKilograms { get; init; }
    public required double TotalRunoffMassKilograms { get; init; }
    public required double TotalAtmosphericWaterMassKilograms { get; init; }
    public required double CumulativeEvaporationKilograms { get; init; }
    public required double CumulativeCondensationKilograms { get; init; }
    public required double CumulativePrecipitationKilograms { get; init; }
    public required double CumulativeRunoffKilograms { get; init; }
    public required double WaterMassErrorKilograms { get; init; }
    public required double AtmosphericEnergyJoulesPerSquareMeter { get; init; }
    public double TotalWaterMassKilograms => TotalVaporMassKilograms + TotalCloudMassKilograms;
    public double WaterBudgetErrorKilograms => WaterMassErrorKilograms;
}
