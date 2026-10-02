namespace PlanetSimulator.Simulation;

/// <summary>Explicit inoculum and illustrative microbial kinetics, not calibrated ecology.</summary>
public sealed record BiologyParameters
{
    public double InitialBiomassKilogramsPerSquareMeter { get; init; } = 0.001;
    public double TotalPhosphorusKilogramsPerSquareMeter { get; init; } = 0.0001;
    public double MaximumGrowthPerDay { get; init; } = 1;
    public double RespirationPerDay { get; init; } = 0.03;
    public double PhotosyntheticEfficiency { get; init; } = 0.02;
    public double PhosphorusKilogramsPerMoleCarbon { get; init; } = 0.030974 / 106;

    internal void Validate()
    {
        Check(InitialBiomassKilogramsPerSquareMeter, 0, 1, nameof(InitialBiomassKilogramsPerSquareMeter));
        Check(TotalPhosphorusKilogramsPerSquareMeter, 0, 1, nameof(TotalPhosphorusKilogramsPerSquareMeter));
        Check(MaximumGrowthPerDay, 0, 10, nameof(MaximumGrowthPerDay));
        Check(RespirationPerDay, 0, 10, nameof(RespirationPerDay));
        Check(PhotosyntheticEfficiency, 0, 0.1, nameof(PhotosyntheticEfficiency));
        Check(PhosphorusKilogramsPerMoleCarbon, 1e-6, 0.1, nameof(PhosphorusKilogramsPerMoleCarbon));
    }

    private static void Check(double value, double minimum, double maximum, string name)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum) throw new ArgumentOutOfRangeException(name);
    }
}
