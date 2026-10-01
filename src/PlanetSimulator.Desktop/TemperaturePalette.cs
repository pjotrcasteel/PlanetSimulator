using Microsoft.Xna.Framework;

namespace PlanetSimulator.Desktop;

/// <summary>
/// Matches the browser's fixed 170–330 K temperature scale; endpoint colours indicate values outside the range.
/// </summary>
internal static class TemperaturePalette
{
    private static readonly Color[] Colours = [new(39, 74, 142), new(59, 171, 193), new(122, 203, 164), new(239, 196, 101), new(217, 88, 73)];
    public static Color ForKelvin(double kelvin)
    {
        var value = (float)Math.Clamp((kelvin - 170) / 160 * 4, 0, 4);
        var index = Math.Min((int)value, 3);
        return Color.Lerp(Colours[index], Colours[index + 1], value - index);
    }
}
