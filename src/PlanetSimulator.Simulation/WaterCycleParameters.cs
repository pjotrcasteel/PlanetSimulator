namespace PlanetSimulator.Simulation;

/// <summary>
/// Controls the deliberately coarse atmospheric water cycle. Values are SI quantities;
/// the defaults are a synthetic, shallow reference ocean rather than an Earth calibration.
/// </summary>
public sealed record WaterCycleParameters
{
    public double InitialVaporEquivalentDepthMeters { get; }
    public double InitialCloudEquivalentDepthMeters { get; }
    public double EvaporationTimescaleHours { get; }
    public double CondensationTimescaleHours { get; }
    public double PrecipitationTimescaleHours { get; }
    public double RunoffTimescaleHours { get; }
    public double SaturationVaporEquivalentDepthMeters { get; } = 0.05;
    public double ZonalWindMetersPerSecond { get; } = 12;
    public double MeridionalWindMetersPerSecond { get; } = 2;

    public WaterCycleParameters(double initialVaporEquivalentDepthMeters = 0.02, double initialCloudEquivalentDepthMeters = 0.001,
        double evaporationTimescaleHours = 120, double condensationTimescaleHours = 18, double precipitationTimescaleHours = 30,
        double runoffTimescaleHours = 72)
    {
        ValidateDepth(initialVaporEquivalentDepthMeters, 1, nameof(initialVaporEquivalentDepthMeters));
        ValidateDepth(initialCloudEquivalentDepthMeters, 0.5, nameof(initialCloudEquivalentDepthMeters));
        ValidateTimescale(evaporationTimescaleHours, nameof(evaporationTimescaleHours));
        ValidateTimescale(condensationTimescaleHours, nameof(condensationTimescaleHours));
        ValidateTimescale(precipitationTimescaleHours, nameof(precipitationTimescaleHours));
        ValidateTimescale(runoffTimescaleHours, nameof(runoffTimescaleHours));
        InitialVaporEquivalentDepthMeters = initialVaporEquivalentDepthMeters;
        InitialCloudEquivalentDepthMeters = initialCloudEquivalentDepthMeters;
        EvaporationTimescaleHours = evaporationTimescaleHours;
        CondensationTimescaleHours = condensationTimescaleHours;
        PrecipitationTimescaleHours = precipitationTimescaleHours;
        RunoffTimescaleHours = runoffTimescaleHours;
    }

    private static void ValidateDepth(double value, double maximum, string name)
    {
        if (!double.IsFinite(value) || value < 0 || value > maximum) throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateTimescale(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0 || value > 100000) throw new ArgumentOutOfRangeException(name);
    }
}
