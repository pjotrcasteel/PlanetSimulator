namespace PlanetSimulator.Simulation;

/// <summary>
/// Fills a deterministic synthetic landscape to one common initial water level.
/// The optional water cycle owns subsequent mass transfers; ice displacement remains deferred.
/// </summary>
public sealed class SurfaceReservoirs
{
    private readonly double[] heights, masses;
    private readonly double cellArea;
    public SurfaceParameters Parameters { get; }
    public IReadOnlyList<double> ElevationsMeters { get; }
    public IReadOnlyList<double> WaterMassPerSquareMeter { get; }
    internal double[] MutableWaterMassPerSquareMeter => masses;
    public double InitialWaterLevelMeters { get; }
    public double TotalWaterMassKilograms { get; }

    public SurfaceReservoirs(SphericalGrid grid, SurfaceParameters parameters)
    {
        Parameters = parameters;
        cellArea = grid.CellAreaSquareMeters;
        heights = grid.Cells.Select(c => Elevation(c.LatitudeRadians, c.LongitudeRadians, parameters.ReliefMeters)).ToArray();
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

    /// <summary>
    /// Evaluates the continuous height field sampled by the finite-volume climate grid.
    /// </summary>
    public static double Elevation(double latitude, double longitude, double reliefMeters) => reliefMeters
        * (0.55 * Math.Cos(latitude) * Math.Sin(2 * longitude + 0.4) + 0.30 * Math.Sin(3 * latitude + 0.2)
            + 0.15 * Math.Cos(latitude) * Math.Cos(5 * longitude));

    public SurfaceSnapshot Snapshot(IReadOnlyList<double> enthalpies) => Snapshot(enthalpies, masses, null);

    public SurfaceSnapshot Snapshot(IReadOnlyList<double> enthalpies, IReadOnlyList<double> waterMassPerSquareMeter, WaterCycleSnapshot? waterCycle)
    {
        ArgumentNullException.ThrowIfNull(waterMassPerSquareMeter);
        if (waterMassPerSquareMeter.Count != masses.Length || enthalpies.Count != masses.Length)
            throw new ArgumentException("Surface snapshot arrays must match the grid.");
        var massesSnapshot = waterMassPerSquareMeter.ToArray();
        var liquids = massesSnapshot.Select((m, i) => m * WaterThermodynamics.LiquidFraction(enthalpies[i], m)).ToArray();
        var ice = massesSnapshot.Select((m, i) => m - liquids[i]).ToArray();
        var totalSurface = massesSnapshot.Sum() * cellArea;
        var atmospheric = waterCycle?.TotalAtmosphericWaterMassKilograms ?? 0;
        var waterError = waterCycle?.WaterMassErrorKilograms ?? massesSnapshot.Select((m, i) => liquids[i] + ice[i] - m).Sum()
            * cellArea;
        return new SurfaceSnapshot
        {
            ElevationsMeters = heights.ToArray(), WaterMassPerSquareMeter = massesSnapshot, LiquidMassPerSquareMeter = liquids,
            IceMassPerSquareMeter = ice, WaterLevelMeters = InitialWaterLevelMeters,
            WaterCoveredFraction = massesSnapshot.Count(m => m > 0) / (double)massesSnapshot.Length,
            LiquidMassFraction = massesSnapshot.Sum() == 0 ? 0 : liquids.Sum() / massesSnapshot.Sum(),
            TotalWaterMassKilograms = totalSurface + atmospheric,
            TotalSurfaceWaterMassKilograms = totalSurface,
            AtmosphericWaterMassKilograms = atmospheric,
            WaterMassErrorKilograms = waterError,
            WaterCycle = waterCycle,
        };
    }
}
