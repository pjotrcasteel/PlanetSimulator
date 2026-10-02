namespace PlanetSimulator.Simulation;

/// <summary>
/// Detached gas columns, partial pressures and atom-inventory diagnostics.
/// </summary>
public sealed record AtmosphereSnapshot
{
    public required IReadOnlyList<double> NitrogenMassPerSquareMeter { get; init; }
    public required IReadOnlyList<double> OxygenMassPerSquareMeter { get; init; }
    public required IReadOnlyList<double> CarbonDioxideMassPerSquareMeter { get; init; }
    public required IReadOnlyList<double> ArgonMassPerSquareMeter { get; init; }
    public required IReadOnlyList<double> DissolvedCarbonDioxideMassPerSquareMeter { get; init; }
    public required IReadOnlyList<double> CarbonDioxidePartialPressurePascals { get; init; }
    public required IReadOnlyList<double> NitrogenPartialPressurePascals { get; init; }
    public required IReadOnlyList<double> OxygenPartialPressurePascals { get; init; }
    public required IReadOnlyList<double> ArgonPartialPressurePascals { get; init; }
    public required double TotalAtmosphericMassKilograms { get; init; }
    public required double MeanSurfacePressurePascals { get; init; }
    public required double MeanCarbonDioxidePartialPressurePascals { get; init; }
    public required double MeanCarbonDioxideMoleFraction { get; init; }
    public required double TotalDissolvedCarbonDioxideMassKilograms { get; init; }
    public required double RemainingCrustalCarbonDioxideMassKilograms { get; init; }
    public required double CumulativeCarbonDioxideUptakeKilograms { get; init; }
    public required double CumulativeCarbonDioxideReleaseKilograms { get; init; }
    public required double CumulativeOutgassingKilograms { get; init; }
    public required double CarbonMassErrorKilograms { get; init; }
    public required double OxygenMassErrorKilograms { get; init; }
    public required double NitrogenMassErrorKilograms { get; init; }
    public required double MinimumGasMassPerSquareMeter { get; init; }
    public double TotalPressurePascals => MeanSurfacePressurePascals;
    public double MeanCarbonDioxidePressurePascals => MeanCarbonDioxidePartialPressurePascals;
    public double CarbonBudgetErrorKilograms => CarbonMassErrorKilograms;
    public double OxygenBudgetErrorKilograms => OxygenMassErrorKilograms;
    public double NitrogenBudgetErrorKilograms => NitrogenMassErrorKilograms;
}
