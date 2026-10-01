using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlanetSimulator.Rendering;
using PlanetSimulator.Simulation;
using NumericVector = System.Numerics.Vector3;

namespace PlanetSimulator.Desktop;

/// <summary>
/// Draws shared terrain geometry with phase-dependent materials and an optical atmosphere.
/// CPU vertex lighting keeps the MonoGame host portable without a platform-specific shader compiler.
/// </summary>
internal sealed class RealisticPlanetRenderer : IDisposable
{
    private readonly GraphicsDevice graphics;
    private readonly BasicEffect effect;
    private readonly SurfaceAppearance appearance;
    private readonly VertexPositionColor[] vertices, airVertices;
    private readonly ReservoirInterpolation[] interpolators;
    private readonly int[] indices, airIndices;
    private readonly SphereMesh atmosphere = new(128, 64);
    private readonly RasterizerState solid = new() { CullMode = CullMode.None };
    private readonly RasterizerState wire = new() { CullMode = CullMode.None, FillMode = FillMode.WireFrame };

    public RealisticPlanetRenderer(GraphicsDevice graphics, SurfaceReservoirs surface, CancellationToken cancellationToken)
    {
        this.graphics = graphics;
        appearance = new SurfaceAppearance(surface, cancellationToken);
        effect = new BasicEffect(graphics) { VertexColorEnabled = true, LightingEnabled = false };
        vertices = new VertexPositionColor[appearance.Vertices.Count];
        indices = appearance.Indices.ToArray();
        interpolators = appearance.Vertices.Select(v => new ReservoirInterpolation(v.Coordinates)).ToArray();
        airVertices = new VertexPositionColor[atmosphere.Vertices.Length];
        airIndices = new int[atmosphere.Indices.Length];
    }

    public void Draw(SurfaceSnapshot surface, Matrix world, Matrix view, Matrix projection, AppearanceOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var inverse = Matrix.Invert(world);
        var camera = Numeric(Vector3.Transform(options.Camera, inverse));
        var sunlight = Numeric(Vector3.TransformNormal(options.Sunlight, inverse));
        for (var index = 0; index < vertices.Length; index++)
        {
            var sample = appearance.Vertices[index];
            var radial = NumericVector.Normalize(sample.Position);
            var position = radial * (1 + (sample.Position.Length() - 1) * options.Relief);
            var colour = SurfaceLighting.Shade(sample, interpolators[index].IceFraction(surface), camera, sunlight, options.Relief);
            vertices[index] = new VertexPositionColor(GameVector(position), new Color(GameVector(colour)));
        }
        effect.World = world; effect.View = view; effect.Projection = projection;
        graphics.BlendState = BlendState.Opaque;
        graphics.DepthStencilState = DepthStencilState.Default;
        graphics.RasterizerState = options.Wireframe ? wire : solid;
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphics.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3);
        }
        if (options.Atmosphere && !options.Wireframe) DrawAtmosphere(options);
    }

    private void DrawAtmosphere(AppearanceOptions options)
    {
        var camera = Numeric(options.Camera);
        var sunlight = Numeric(options.Sunlight);
        for (var index = 0; index < airVertices.Length; index++)
        {
            var position = atmosphere.Vertices[index].Position * AtmosphereOptics.OuterRadius;
            var colour = AtmosphereOptics.Scatter(camera, Numeric(position) - camera, sunlight);
            airVertices[index] = new VertexPositionColor(position, new Color(GameVector(colour)));
        }
        var count = 0;
        for (var index = 0; index < atmosphere.Indices.Length; index += 3)
        {
            var a = atmosphere.Indices[index]; var b = atmosphere.Indices[index + 1]; var c = atmosphere.Indices[index + 2];
            var centre = (airVertices[a].Position + airVertices[b].Position + airVertices[c].Position) / 3;
            if (Vector3.Dot(centre, options.Camera - centre) <= 0) continue;
            airIndices[count++] = a; airIndices[count++] = b; airIndices[count++] = c;
        }
        effect.World = Matrix.Identity;
        graphics.BlendState = BlendState.Additive;
        graphics.DepthStencilState = DepthStencilState.DepthRead;
        graphics.RasterizerState = solid;
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphics.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, airVertices, 0, airVertices.Length, airIndices, 0, count / 3);
        }
        graphics.BlendState = BlendState.Opaque;
        graphics.DepthStencilState = DepthStencilState.Default;
    }

    private static NumericVector Numeric(Vector3 value) => new(value.X, value.Y, value.Z);
    private static Vector3 GameVector(NumericVector value) => new(value.X, value.Y, value.Z);

    public void Dispose()
    {
        effect.Dispose(); solid.Dispose(); wire.Dispose();
    }
}
