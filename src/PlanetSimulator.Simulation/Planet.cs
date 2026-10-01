namespace PlanetSimulator.Simulation;

/// <summary>
/// Describes a fictional terrestrial planet using SI units. No climate is modeled yet.
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
