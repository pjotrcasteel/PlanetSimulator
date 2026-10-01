using System.Numerics;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Rendering;

/// <summary>
/// Reconstructs phase fraction by interpolating both conserved masses on the equal-area grid.
/// Interpolating ratios directly would create artificial ice near dry cells.
/// </summary>
public readonly struct ReservoirInterpolation
{
    private readonly int first, second, third, fourth;
    private readonly float horizontal, vertical;

    public ReservoirInterpolation(Vector2 coordinates)
    {
        var x = coordinates.X * 24 - 0.5f;
        var y = Math.Clamp((1 - MathF.Cos(coordinates.Y * MathF.PI)) * 6 - 0.5f, 0, 11);
        var left = (int)MathF.Floor(x);
        var top = (int)MathF.Floor(y);
        horizontal = x - MathF.Floor(x); vertical = y - top;
        var next = Math.Min(11, top + 1);
        first = top * 24 + (left + 24) % 24; second = top * 24 + (left + 25) % 24;
        third = next * 24 + (left + 24) % 24; fourth = next * 24 + (left + 25) % 24;
    }

    public float IceFraction(SurfaceSnapshot surface)
    {
        var total = Sample(surface.WaterMassPerSquareMeter);
        return total <= 0 ? 0 : (float)Math.Clamp(Sample(surface.IceMassPerSquareMeter) / total, 0, 1);
    }

    private double Sample(IReadOnlyList<double> field)
    {
        var top = field[first] + horizontal * (field[second] - field[first]);
        var bottom = field[third] + horizontal * (field[fourth] - field[third]);
        return top + vertical * (bottom - top);
    }
}
