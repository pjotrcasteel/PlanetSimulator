namespace PlanetSimulator.Simulation;

/// <summary>
/// Contains calculated time, rotation and uniform climate diagnostics in SI units.
/// </summary>
public sealed record SimulationSnapshot(
    double ElapsedSeconds,
    double RotationRadians,
    double TemperatureKelvin,
    double EquilibriumTemperatureKelvin,
    double AbsorbedWattsPerSquareMeter,
    double EmittedWattsPerSquareMeter)
{
    public double NetWattsPerSquareMeter => AbsorbedWattsPerSquareMeter - EmittedWattsPerSquareMeter;
}
