using System.Text.Json.Serialization;

namespace PlanetSimulator.Simulation;

/// <summary>
/// Physical planet properties used by geometry, rotation and diagnostics. Climate forcing remains explicit in <see cref="ClimateParameters"/>.
/// </summary>
public sealed record PlanetParameters
{
    public const double GravitationalConstant = 6.67430e-11;
    public double RadiusMeters { get; init; }
    public double MassKilograms { get; init; }
    public double RotationPeriodSeconds { get; init; }

    [JsonIgnore]
    public double SurfaceGravityMetersPerSecondSquared => GravitationalConstant * MassKilograms / (RadiusMeters * RadiusMeters);

    public PlanetParameters(double radiusMeters = 6_371_000, double massKilograms = 5.972e24, double rotationPeriodSeconds = 86_400)
    {
        Check(radiusMeters, 100_000, 100_000_000, nameof(radiusMeters));
        Check(massKilograms, 1e20, 1e28, nameof(massKilograms));
        Check(rotationPeriodSeconds, 3_600, 365d * 86_400, nameof(rotationPeriodSeconds));
        RadiusMeters = radiusMeters;
        MassKilograms = massKilograms;
        RotationPeriodSeconds = rotationPeriodSeconds;
    }

    public void Validate()
    {
        Check(RadiusMeters, 100_000, 100_000_000, nameof(RadiusMeters));
        Check(MassKilograms, 1e20, 1e28, nameof(MassKilograms));
        Check(RotationPeriodSeconds, 3_600, 365d * 86_400, nameof(RotationPeriodSeconds));
    }

    private static void Check(double value, double minimum, double maximum, string name)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum) throw new ArgumentOutOfRangeException(name);
    }
}
