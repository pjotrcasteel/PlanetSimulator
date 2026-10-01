using Microsoft.Xna.Framework;

namespace PlanetSimulator.Desktop;

/// <summary>
/// Camera, light and display-only settings for one visual frame.
/// </summary>
internal sealed record AppearanceOptions(Vector3 Camera, Vector3 Sunlight, float Relief, bool Atmosphere, bool Wireframe);
