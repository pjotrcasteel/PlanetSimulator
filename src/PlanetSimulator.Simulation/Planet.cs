namespace PlanetSimulator.Simulation;

/// <summary>
/// Describes radius, mass and prescribed rotation of the selected terrestrial planet in SI units.
/// </summary>
public sealed class Planet
{
    public PlanetParameters Parameters { get; }
    public double RadiusMeters => Parameters.RadiusMeters;
    public double MassKilograms => Parameters.MassKilograms;
    public double RotationPeriodSeconds => Parameters.RotationPeriodSeconds;
    public double SurfaceGravity => Parameters.SurfaceGravityMetersPerSecondSquared;

    public Planet(PlanetParameters? parameters = null)
    {
        Parameters = parameters ?? new PlanetParameters();
        Parameters.Validate();
    }

    public double GetRotationRadians(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        return elapsedSeconds % RotationPeriodSeconds / RotationPeriodSeconds * Math.Tau;
    }
}
