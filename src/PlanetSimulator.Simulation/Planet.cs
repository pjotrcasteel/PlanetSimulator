namespace PlanetSimulator.Simulation;

/// <summary>
/// Describes the radius, mass and prescribed rotation of a fictional terrestrial planet in SI units.
/// </summary>
public sealed class Planet
{
    public double RadiusMeters { get; } = 6_371_000;
    public double MassKilograms { get; } = 5.972e24;
    public double RotationPeriodSeconds { get; } = 86_400;
    public double SurfaceGravity => 6.67430e-11 * MassKilograms / (RadiusMeters * RadiusMeters);

    public double GetRotationRadians(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        }

        return elapsedSeconds % RotationPeriodSeconds / RotationPeriodSeconds * Math.Tau;
    }
}
