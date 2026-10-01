namespace PlanetSimulator.Simulation;

/// <summary>
/// Exposes the same simulation clock and planetary rotation to browser and other hosts.
/// </summary>
public sealed class SimulationSession
{
    public SimulationClock Clock { get; } = new();
    public Planet Planet { get; } = new();

    public SimulationSnapshot Advance(double realSeconds, CancellationToken cancellationToken)
    {
        Clock.Advance(realSeconds, cancellationToken);
        return new SimulationSnapshot(Clock.ElapsedSeconds, Planet.GetRotationRadians(Clock.ElapsedSeconds));
    }
}
