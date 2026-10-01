namespace PlanetSimulator.Simulation;

/// <summary>
/// Advances a shared clock, uniform energy balance and planetary rotation for every host.
/// </summary>
public sealed class SimulationSession
{
    public SimulationClock Clock { get; } = new();
    public Planet Planet { get; } = new();
    public EnergyBalanceModel Climate { get; private set; }

    public SimulationSession(ClimateParameters? parameters = null)
    {
        Climate = new EnergyBalanceModel(parameters);
    }

    public SimulationSnapshot Advance(double realSeconds, CancellationToken cancellationToken)
    {
        Clock.Advance(realSeconds, seconds => Climate.Advance(seconds, cancellationToken), cancellationToken);
        return new SimulationSnapshot(
            Clock.ElapsedSeconds,
            Planet.GetRotationRadians(Clock.ElapsedSeconds),
            Climate.TemperatureKelvin,
            Climate.EquilibriumTemperatureKelvin,
            Climate.AbsorbedWattsPerSquareMeter,
            Climate.EmittedWattsPerSquareMeter);
    }

    public void Reset(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Clock.Reset();
        Climate = new EnergyBalanceModel(Climate.Parameters);
    }
}
