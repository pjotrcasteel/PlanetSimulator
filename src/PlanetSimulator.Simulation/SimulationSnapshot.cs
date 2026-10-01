namespace PlanetSimulator.Simulation;

/// <summary>
/// Contains calculated time and rotation; renderers must not recalculate these values.
/// </summary>
public sealed record SimulationSnapshot(double ElapsedSeconds, double RotationRadians);
