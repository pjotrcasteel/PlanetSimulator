namespace PlanetSimulator.Simulation;

/// <summary>
/// Describes an equal-area cell using planet-fixed latitude and longitude, in radians.
/// </summary>
public sealed record SurfaceCell(int Index, int LatitudeIndex, int LongitudeIndex, double LatitudeRadians, double LongitudeRadians, double SolidAngle);
