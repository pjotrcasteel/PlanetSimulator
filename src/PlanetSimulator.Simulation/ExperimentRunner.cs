namespace PlanetSimulator.Simulation;

/// <summary>
/// Runs fixed one-minute steps with daily samples, independent of wall time and presentation.
/// </summary>
public sealed class ExperimentRunner
{
    private readonly SimulationSession session;
    private readonly List<ExperimentSample> samples = [];
    private int nextChange;
    public ExperimentScenario Scenario { get; }
    public int CompletedDays { get; private set; }
    public bool IsComplete => CompletedDays == Scenario.DurationDays;
    public IReadOnlyList<ExperimentSample> Samples => samples.AsReadOnly();

    public ExperimentRunner(ExperimentScenario scenario)
    {
        // Own a validated copy: edits in a host cannot alter an experiment that is already running.
        Scenario = ScenarioJson.Deserialize(ScenarioJson.Serialize(scenario));
        session = new SimulationSession(Scenario.Climate, Scenario.Regional, Scenario.Surface, Scenario.Hydrology, Scenario.Atmosphere, Scenario.Planet);
        session.Clock.Speed = SimulationClock.StepSeconds;
        Sample();
    }

    public void AdvanceDay(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsComplete) return;
        var targetTick = (CompletedDays + 1L) * 1440;
        session.AdvanceClock(0, cancellationToken);
        while (session.Clock.TickCount < targetTick) session.AdvanceClock(1, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        CompletedDays++;
        if (nextChange < Scenario.Changes.Length && Scenario.Changes[nextChange].Day == CompletedDays)
        {
            var change = Scenario.Changes[nextChange++];
            session.SetForcing(change.DistanceAstronomicalUnits, change.BondAlbedo, CancellationToken.None);
        }

        Sample();
    }

    public ExperimentResult Finish(CancellationToken cancellationToken)
    {
        while (!IsComplete) AdvanceDay(cancellationToken);
        return GetResult();
    }

    public ExperimentResult GetResult()
    {
        if (!IsComplete) throw new InvalidOperationException("Het experiment is nog niet voltooid.");
        return new ExperimentResult(Scenario, samples.ToArray())
        {
            FinalRegions = session.Regional?.TemperaturesKelvin.ToArray() ?? [],
            FinalSurface = session.Snapshot().Regional?.Surface,
            FinalAtmosphere = session.Snapshot().Regional?.Atmosphere,
            FinalBiology = session.Snapshot().Regional?.Biology,
            FinalTerraforming = session.Snapshot().Regional?.Terraforming,
        };
    }

    private void Sample()
    {
        var climate = session.Snapshot();
        samples.Add(new ExperimentSample
        {
            Day = CompletedDays,
            TemperatureKelvin = climate.TemperatureKelvin,
            EquilibriumTemperatureKelvin = climate.EquilibriumTemperatureKelvin,
            AbsorbedWattsPerSquareMeter = climate.AbsorbedWattsPerSquareMeter,
            EmittedWattsPerSquareMeter = climate.EmittedWattsPerSquareMeter,
            AbsorbedJoulesPerSquareMeter = session.CumulativeAbsorbedJoulesPerSquareMeter,
            EmittedJoulesPerSquareMeter = session.CumulativeEmittedJoulesPerSquareMeter,
            BudgetErrorJoulesPerSquareMeter = session.EnergyBalanceErrorJoulesPerSquareMeter,
            Regional = climate.Regional,
        });
    }
}
