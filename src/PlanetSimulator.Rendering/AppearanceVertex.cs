using System.Numerics;

namespace PlanetSimulator.Rendering;

/// <summary>
/// A visual surface sample; positions and decorative relief use unit-planet coordinates.
/// </summary>
public readonly record struct AppearanceVertex(Vector3 Position, Vector3 Normal, Vector3 Albedo, Vector2 Coordinates, float Water);
