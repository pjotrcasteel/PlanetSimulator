namespace PlanetSimulator.Rendering;

/// <summary>
/// Packed geometry for the browser: position, normal, linear albedo, UV and water mask.
/// Sent only when the surface model changes, never on the simulation tick path.
/// </summary>
public sealed record SurfaceGeometryData(float[] Vertices, int[] Indices);
