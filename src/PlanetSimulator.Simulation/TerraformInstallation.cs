namespace PlanetSimulator.Simulation;

/// <summary>
/// One region-wide installation. Intensities are per square metre of its source cell, not one small machine.
/// </summary>
public sealed record TerraformInstallation
{
    public const string Heater = "heater";
    public const string Capture = "capture";
    public const string Transport = "transport";
    public string Name { get; init; } = "Oppervlakteverwarmer";
    public string Kind { get; init; } = Heater;
    public int CellIndex { get; init; } = 144;
    public int DestinationCellIndex { get; init; } = 145;
    public double StartDay { get; init; }
    public double EndDay { get; init; } = 30;
    public double ConstructionDays { get; init; } = 0.25;
    public double ConstructionMaterialKilogramsPerSquareMeter { get; init; } = 2;
    public double ConstructionEnergyJoulesPerSquareMeter { get; init; } = 1e6;
    public double PowerWattsPerSquareMeter { get; init; } = 100;
    public double MaximumThroughputKilogramsPerSquareMeterPerDay { get; init; } = 0.1;
    public double ProcessingJoulesPerKilogram { get; init; } = 2e6;
    public double TransportDays { get; init; } = 0.25;
    public double TransportJoulesPerKilogramMeter { get; init; } = 0.1;

    internal void Validate(int cells)
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 60) throw new ArgumentException("Installatienaam moet 1–60 tekens bevatten.");
        if (Kind is not (Heater or Capture or Transport)) throw new ArgumentException("Onbekend installatietype.");
        if (CellIndex < 0 || CellIndex >= cells || DestinationCellIndex < 0 || DestinationCellIndex >= cells)
            throw new ArgumentOutOfRangeException(nameof(CellIndex));
        if (Kind == Transport && CellIndex == DestinationCellIndex) throw new ArgumentException("Transport vraagt twee verschillende cellen.");
        TerraformingParameters.Check(StartDay, 0, ExperimentScenario.MaximumDurationDays, nameof(StartDay));
        TerraformingParameters.Check(EndDay, 0, ExperimentScenario.MaximumDurationDays, nameof(EndDay));
        if (EndDay <= StartDay) throw new ArgumentException("Einddag moet na startdag liggen.");
        TerraformingParameters.Check(ConstructionDays, 0, ExperimentScenario.MaximumDurationDays, nameof(ConstructionDays));
        TerraformingParameters.Check(ConstructionMaterialKilogramsPerSquareMeter, 0, 1000, nameof(ConstructionMaterialKilogramsPerSquareMeter));
        TerraformingParameters.Check(ConstructionEnergyJoulesPerSquareMeter, 0, 1e10, nameof(ConstructionEnergyJoulesPerSquareMeter));
        TerraformingParameters.Check(PowerWattsPerSquareMeter, 0, 1000, nameof(PowerWattsPerSquareMeter));
        TerraformingParameters.Check(MaximumThroughputKilogramsPerSquareMeterPerDay, 0, 100, nameof(MaximumThroughputKilogramsPerSquareMeterPerDay));
        TerraformingParameters.Check(ProcessingJoulesPerKilogram, 1, 1e9, nameof(ProcessingJoulesPerKilogram));
        TerraformingParameters.Check(TransportDays, 1d / 1440, 30, nameof(TransportDays));
        TerraformingParameters.Check(TransportJoulesPerKilogramMeter, 0, 1000, nameof(TransportJoulesPerKilogramMeter));
    }
}
