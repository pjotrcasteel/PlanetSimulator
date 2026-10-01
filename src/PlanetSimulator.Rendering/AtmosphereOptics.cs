using System.Numerics;

namespace PlanetSimulator.Rendering;

/// <summary>
/// A thin exponential atmosphere with single-scattering colour and a spherical planetary shadow.
/// Optical constants are illustrative; pressure, composition and climate coupling are not inferred.
/// </summary>
public static class AtmosphereOptics
{
    public const float OuterRadius = 1.065f;
    public const float ScaleHeight = 0.012f;

    public static Vector3 Scatter(Vector3 camera, Vector3 direction, Vector3 sunlight)
    {
        direction = Vector3.Normalize(direction);
        var b = Vector3.Dot(camera, direction);
        var discriminant = b * b - camera.LengthSquared() + OuterRadius * OuterRadius;
        if (discriminant <= 0) return Vector3.Zero;
        var root = MathF.Sqrt(discriminant);
        var start = Math.Max(0, -b - root);
        var end = -b + root;
        var planet = b * b - camera.LengthSquared() + 1;
        if (planet > 0) end = Math.Min(end, -b - MathF.Sqrt(planet));
        if (end <= start) return Vector3.Zero;
        var step = (end - start) / 12;
        var depth = 0f;
        for (var sample = 0; sample < 12; sample++)
        {
            var point = camera + direction * (start + (sample + 0.5f) * step);
            var sunDot = Vector3.Dot(point, sunlight);
            if (sunDot < 0 && sunDot * sunDot > point.LengthSquared() - 1) continue;
            depth += MathF.Exp(-(point.Length() - 1) / ScaleHeight) * step;
        }
        var cosine = Vector3.Dot(direction, sunlight);
        var phase = 0.75f * (1 + cosine * cosine);
        return new Vector3(1 - MathF.Exp(-depth * 3.8f), 1 - MathF.Exp(-depth * 8.5f), 1 - MathF.Exp(-depth * 18)) * (0.38f * phase);
    }
}
