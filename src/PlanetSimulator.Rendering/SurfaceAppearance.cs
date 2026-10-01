using System.Numerics;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Rendering;

/// <summary>
/// Reconstructs continuous coastlines from the model's height function and initial water level.
/// Small ridges and colour variation are presentation detail; they never change climate cells or water mass.
/// </summary>
public sealed class SurfaceAppearance
{
    public const int Segments = 192;
    public const int Rings = 96;
    private readonly AppearanceVertex[] vertices;
    private readonly int[] indices;
    public IReadOnlyList<AppearanceVertex> Vertices { get; }
    public IReadOnlyList<int> Indices { get; }
    private readonly SurfaceReservoirs surface;

    public SurfaceAppearance(SurfaceReservoirs surface, CancellationToken cancellationToken)
    {
        this.surface = surface;
        vertices = new AppearanceVertex[(Segments + 1) * (Rings + 1)];
        indices = new int[Segments * Rings * 6];
        for (var ring = 0; ring <= Rings; ring++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var latitude = Math.PI / 2 - Math.PI * ring / Rings;
            for (var segment = 0; segment <= Segments; segment++)
            {
                var longitude = segment == Segments ? 0 : Math.Tau * segment / Segments;
                var sample = Sample(latitude, longitude);
                vertices[ring * (Segments + 1) + segment] = sample with { Coordinates = new Vector2((float)segment / Segments, (float)ring / Rings) };
            }
        }
        var index = 0;
        for (var ring = 0; ring < Rings; ring++)
            for (var segment = 0; segment < Segments; segment++)
            {
                var a = ring * (Segments + 1) + segment;
                var b = a + Segments + 1;
                indices[index++] = a; indices[index++] = b; indices[index++] = a + 1;
                indices[index++] = a + 1; indices[index++] = b; indices[index++] = b + 1;
            }
        Vertices = Array.AsReadOnly(vertices);
        Indices = Array.AsReadOnly(indices);
    }

    public SurfaceGeometryData Export() => new(vertices.SelectMany(v => new[] { v.Position.X, v.Position.Y, v.Position.Z,
        v.Normal.X, v.Normal.Y, v.Normal.Z, v.Albedo.X, v.Albedo.Y, v.Albedo.Z, v.Coordinates.X, v.Coordinates.Y, v.Water }).ToArray(), indices.ToArray());

    private AppearanceVertex Sample(double latitude, double longitude)
    {
        var direction = Direction(latitude, longitude);
        var height = SurfaceReservoirs.Elevation(latitude, longitude, surface.Parameters.ReliefMeters) - surface.InitialWaterLevelMeters;
        var water = height < 0 && surface.TotalWaterMassKilograms > 0;
        var scale = Math.Max(1, surface.Parameters.ReliefMeters);
        var noise = Detail(direction * 13);
        var altitude = (float)Math.Max(0, height / scale);
        var position = Position(latitude, longitude);
        var normal = direction;
        if (!water && Math.Abs(latitude) < Math.PI / 2 - 0.001)
        {
            var east = Position(latitude, longitude + 0.0004) - Position(latitude, longitude - 0.0004);
            var north = Position(latitude + 0.0004, longitude) - Position(latitude - 0.0004, longitude);
            normal = Vector3.Normalize(Vector3.Cross(north, east));
        }
        var soil = Vector3.Lerp(new Vector3(0.20f, 0.16f, 0.10f), new Vector3(0.34f, 0.29f, 0.21f), noise);
        var rock = Vector3.Lerp(soil, new Vector3(0.24f, 0.25f, 0.25f), Math.Clamp(altitude * 1.5f, 0, 1));
        var depth = (float)Math.Clamp(-height / scale * 5, 0, 1);
        var ocean = Vector3.Lerp(new Vector3(0.025f, 0.15f, 0.17f), new Vector3(0.006f, 0.025f, 0.06f), depth);
        return new AppearanceVertex(position, normal, water ? ocean : rock, default, water ? 1 : 0);
    }

    private Vector3 Position(double latitude, double longitude)
    {
        var direction = Direction(latitude, longitude);
        var height = SurfaceReservoirs.Elevation(latitude, longitude, surface.Parameters.ReliefMeters) - surface.InitialWaterLevelMeters;
        var elevation = Math.Max(0, height) / Math.Max(1, surface.Parameters.ReliefMeters);
        // Ridges only modify dry-land display height. Coastlines still follow the model's continuous height function.
        var ridge = 1 - Math.Abs(Detail(direction * 18) * 2 - 1);
        var radius = 1 + Math.Min(0.04, elevation * (0.012 + 0.024 * ridge * ridge));
        return direction * (float)radius;
    }

    private static Vector3 Direction(double latitude, double longitude) => new((float)(Math.Cos(latitude) * Math.Cos(longitude)),
        (float)Math.Sin(latitude), (float)(Math.Cos(latitude) * Math.Sin(longitude)));

    public static float Detail(Vector3 position)
    {
        var value = 0f;
        var weight = 0.55f;
        for (var octave = 0; octave < 4; octave++)
        {
            value += Noise(position) * weight;
            position = position * 2.03f + new Vector3(17.2f, 9.1f, 13.7f);
            weight *= 0.5f;
        }
        return value / 1.03125f;
    }

    private static float Noise(Vector3 position)
    {
        var x = (int)MathF.Floor(position.X); var y = (int)MathF.Floor(position.Y); var z = (int)MathF.Floor(position.Z);
        var f = position - new Vector3(x, y, z);
        f = f * f * (new Vector3(3) - 2 * f);
        static float Mix(float a, float b, float t) => a + (b - a) * t;
        var low = Mix(Mix(Hash(x, y, z), Hash(x + 1, y, z), f.X), Mix(Hash(x, y + 1, z), Hash(x + 1, y + 1, z), f.X), f.Y);
        var high = Mix(Mix(Hash(x, y, z + 1), Hash(x + 1, y, z + 1), f.X), Mix(Hash(x, y + 1, z + 1), Hash(x + 1, y + 1, z + 1), f.X), f.Y);
        return Mix(low, high, f.Z);
    }

    private static float Hash(int x, int y, int z)
    {
        unchecked
        {
            var value = (uint)(x * 374761393 + y * 668265263 + z * 2147483647);
            value = (value ^ (value >> 13)) * 1274126177;
            return (value ^ (value >> 16)) / (float)uint.MaxValue;
        }
    }
}
