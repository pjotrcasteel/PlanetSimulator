namespace PlanetSimulator.Simulation;

/// <summary>
/// Detached terrain and conserved freshwater reservoir diagnostics.
/// Depths use reference liquid density; ice expansion is not represented.
/// </summary>
public sealed record SurfaceSnapshot
{
    public required IReadOnlyList<double> ElevationsMeters { get; init; }
    public required IReadOnlyList<double> WaterMassPerSquareMeter { get; init; }
    public required IReadOnlyList<double> LiquidMassPerSquareMeter { get; init; }
    public required IReadOnlyList<double> IceMassPerSquareMeter { get; init; }
    public required double WaterLevelMeters { get; init; }
    public required double WaterCoveredFraction { get; init; }
    public required double LiquidMassFraction { get; init; }
    public required double TotalWaterMassKilograms { get; init; }
    public required double WaterMassErrorKilograms { get; init; }
    public double TotalSurfaceWaterMassKilograms { get; init; }
    public double AtmosphericWaterMassKilograms { get; init; }
    public double BiologicallyBoundWaterEquivalentKilograms { get; init; }
    public WaterCycleSnapshot? WaterCycle { get; init; }
}
