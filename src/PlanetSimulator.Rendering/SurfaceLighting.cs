using System.Numerics;

namespace PlanetSimulator.Rendering;

/// <summary>
/// Display lighting shared with the browser shader: diffuse rock and ice, Fresnel water and a solar highlight.
/// The returned colour is display RGB; this lighting does not participate in the energy balance.
/// </summary>
public static class SurfaceLighting
{
    public static Vector3 Shade(AppearanceVertex vertex, float iceFraction, Vector3 camera, Vector3 sunlight, float relief)
    {
        var radial = Vector3.Normalize(vertex.Position);
        var position = radial * (1 + (vertex.Position.Length() - 1) * relief);
        var normal = Vector3.Normalize(Vector3.Lerp(radial, vertex.Normal, relief));
        var view = Vector3.Normalize(camera - position);
        var ice = Math.Clamp(iceFraction, 0, 1) * vertex.Water;
        var liquid = vertex.Water * (1 - ice);
        var grain = 0.92f + 0.08f * MathF.Sin(radial.X * 179 + MathF.Sin(radial.Z * 113) * 3) * MathF.Sin(radial.Y * 151);
        var albedo = Vector3.Lerp(vertex.Albedo, new Vector3(0.61f, 0.75f, 0.83f) * grain, ice);
        var light = Math.Max(0, Vector3.Dot(normal, sunlight));
        var colour = albedo * (0.018f + 1.35f * light);
        var half = (sunlight + view) / Math.Max(1e-6f, (sunlight + view).Length());
        var fresnel = 0.02f + 0.98f * MathF.Pow(1 - Math.Max(0, Vector3.Dot(normal, view)), 5);
        var highlight = MathF.Pow(Math.Max(0, Vector3.Dot(normal, half)), 150) * liquid * (0.25f + fresnel) * light * 4;
        colour += new Vector3(1, 0.91f, 0.75f) * highlight;
        colour += new Vector3(0.025f, 0.065f, 0.12f) * (liquid * fresnel * Math.Max(0, Vector3.Dot(radial, sunlight)));
        return new Vector3(ToDisplay(colour.X), ToDisplay(colour.Y), ToDisplay(colour.Z));
    }

    private static float ToDisplay(float value) => MathF.Pow(Math.Clamp(value, 0, 1), 1 / 2.2f);
}
