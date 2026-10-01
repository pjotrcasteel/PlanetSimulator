namespace PlanetSimulator.Simulation;

/// <summary>
/// Changes distance and albedo at a whole-day experiment boundary.
/// </summary>
public sealed record ForcingChange
{
    public required int Day { get; init; }
    public required double DistanceAstronomicalUnits { get; init; }
    public required double BondAlbedo { get; init; }
}
