namespace PlanetSimulator.Simulation;

/// <summary>CH2O-equivalent biomass and a separately conserved phosphorus quota.</summary>
public sealed record BiologySnapshot
{
    public required IReadOnlyList<double> BiomassKilogramsPerSquareMeter { get; init; }
    public required IReadOnlyList<double> AvailablePhosphorusKilogramsPerSquareMeter { get; init; }
    public required double TotalBiomassKilograms { get; init; }
    public required double TotalAvailablePhosphorusKilograms { get; init; }
    public required double PhosphorusBudgetErrorKilograms { get; init; }
    public required double CumulativeProductionKilograms { get; init; }
    public required double CumulativeRespirationKilograms { get; init; }
    public required double ChemicalEnergyJoulesPerSquareMeter { get; init; }
    public required double BoundWaterEquivalentKilograms { get; init; }
}
