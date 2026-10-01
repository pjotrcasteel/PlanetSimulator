namespace PlanetSimulator.Simulation;

/// <summary>
/// Defines synthetic relief and a global inventory expressed as liquid-equivalent metres.
/// Shallow defaults demonstrate phase changes; they do not represent Earth's oceans.
/// </summary>
public sealed record SurfaceParameters
{
    public double ReliefMeters { get; }
    public double WaterEquivalentDepthMeters { get; }

    public SurfaceParameters(double reliefMeters = 10, double waterEquivalentDepthMeters = 3)
    {
        if (!double.IsFinite(reliefMeters) || reliefMeters is < 0 or > 5000) throw new ArgumentOutOfRangeException(nameof(reliefMeters));
        if (!double.IsFinite(waterEquivalentDepthMeters) || waterEquivalentDepthMeters is < 0 or > 5000)
            throw new ArgumentOutOfRangeException(nameof(waterEquivalentDepthMeters));
        ReliefMeters = reliefMeters;
        WaterEquivalentDepthMeters = waterEquivalentDepthMeters;
    }
}
