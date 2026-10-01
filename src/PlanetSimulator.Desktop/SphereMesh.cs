using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PlanetSimulator.Desktop;

/// <summary>
/// Builds a unit sphere. Decorative bands make rotation visible; they are not terrain.
/// </summary>
internal sealed class SphereMesh
{
    public VertexPositionNormalTexture[] Vertices { get; }
    public int[] Indices { get; }

    public SphereMesh(int segments = 96, int rings = 48)
    {
        Vertices = new VertexPositionNormalTexture[(rings + 1) * (segments + 1)];
        Indices = new int[rings * segments * 6];
        for (var ring = 0; ring <= rings; ring++)
        {
            var latitude = MathF.PI * ring / rings;
            for (var segment = 0; segment <= segments; segment++)
            {
                var longitude = MathHelper.TwoPi * segment / segments;
                var position = new Vector3(MathF.Sin(latitude) * MathF.Cos(longitude), MathF.Cos(latitude), MathF.Sin(latitude) * MathF.Sin(longitude));
                Vertices[ring * (segments + 1) + segment] = new(position, position, new Vector2((float)segment / segments, (float)ring / rings));
            }
        }

        var index = 0;
        for (var ring = 0; ring < rings; ring++)
        {
            for (var segment = 0; segment < segments; segment++)
            {
                var a = ring * (segments + 1) + segment;
                var b = a + segments + 1;
                Indices[index++] = a;
                Indices[index++] = b;
                Indices[index++] = a + 1;
                Indices[index++] = a + 1;
                Indices[index++] = b;
                Indices[index++] = b + 1;
            }
        }
    }
}
