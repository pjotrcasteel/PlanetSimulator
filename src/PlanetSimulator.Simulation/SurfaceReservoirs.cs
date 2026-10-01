namespace PlanetSimulator.Simulation;

/// <summary>
/// Fills a deterministic synthetic landscape to one common initial water level.
/// Each column retains its inventory; runoff, evaporation and ice displacement are deferred.
/// </summary>
public sealed class SurfaceReservoirs
{
    private readonly double[] heights, masses;
    public SurfaceParameters Parameters { get; }
    public IReadOnlyList<double> ElevationsMeters { get; }
    public IReadOnlyList<double> WaterMassPerSquareMeter { get; }
    public double InitialWaterLevelMeters { get; }
    public double TotalWaterMassKilograms { get; }

    public SurfaceReservoirs(SphericalGrid grid, SurfaceParameters parameters)
    {
        Parameters = parameters;
        heights = grid.Cells.Select(c => parameters.ReliefMeters * (0.55 * Math.Cos(c.LatitudeRadians) * Math.Sin(2 * c.LongitudeRadians + 0.4)
            + 0.30 * Math.Sin(3 * c.LatitudeRadians + 0.2) + 0.15 * Math.Cos(c.LatitudeRadians) * Math.Cos(5 * c.LongitudeRadians))).ToArray();
        var lower = heights.Min();
        var upper = heights.Max() + parameters.WaterEquivalentDepthMeters;
        for (var iteration = 0; iteration < 80; iteration++)
        {
            var level = (lower + upper) / 2;
            if (heights.Average(h => Math.Max(0, level - h)) < parameters.WaterEquivalentDepthMeters) lower = level;
            else upper = level;
        }
        InitialWaterLevelMeters = (lower + upper) / 2;
        masses = heights.Select(h => parameters.WaterEquivalentDepthMeters == 0 ? 0
            : Math.Max(0, InitialWaterLevelMeters - h) * WaterThermodynamics.LiquidDensity).ToArray();
        ElevationsMeters = Array.AsReadOnly(heights);
        WaterMassPerSquareMeter = Array.AsReadOnly(masses);
        TotalWaterMassKilograms = masses.Sum() * grid.CellAreaSquareMeters;
    }

    public SurfaceSnapshot Snapshot(IReadOnlyList<double> enthalpies)
    {
        var liquids = masses.Select((m, i) => m * WaterThermodynamics.LiquidFraction(enthalpies[i], m)).ToArray();
        var ice = masses.Select((m, i) => m - liquids[i]).ToArray();
        return new SurfaceSnapshot
        {
            ElevationsMeters = heights.ToArray(), WaterMassPerSquareMeter = masses.ToArray(), LiquidMassPerSquareMeter = liquids,
            IceMassPerSquareMeter = ice, WaterLevelMeters = InitialWaterLevelMeters,
            WaterCoveredFraction = masses.Count(m => m > 0) / (double)masses.Length,
            LiquidMassFraction = masses.Sum() == 0 ? 0 : liquids.Sum() / masses.Sum(),
            TotalWaterMassKilograms = TotalWaterMassKilograms,
            WaterMassErrorKilograms = masses.Select((m, i) => liquids[i] + ice[i] - m).Sum() * TotalWaterMassKilograms / Math.Max(1, masses.Sum()),
        };
    }
}
