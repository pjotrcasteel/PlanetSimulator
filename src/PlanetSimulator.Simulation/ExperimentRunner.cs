using System.Globalization;
using System.Text;

namespace PlanetSimulator.Simulation;

/// <summary>Runs fixed one-minute steps with daily samples, independent of wall time and presentation.</summary>
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
        session = new SimulationSession(Scenario.Climate);
        session.Clock.Speed = SimulationClock.StepSeconds;
        Sample();
    }

    public void AdvanceDay(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsComplete) return;
        var targetTick = (CompletedDays + 1L) * 1440;
        session.Advance(0, cancellationToken);
        while (session.Clock.TickCount < targetTick) session.Advance(1, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        CompletedDays++;
        if (nextChange < Scenario.Changes.Length && Scenario.Changes[nextChange].Day == CompletedDays)
        {
            var change = Scenario.Changes[nextChange++];
            session.Climate.SetForcing(change.DistanceAstronomicalUnits, change.BondAlbedo, CancellationToken.None);
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
        return new ExperimentResult(Scenario, samples.ToArray());
    }

    private void Sample()
    {
        var climate = session.Climate;
        samples.Add(new ExperimentSample(CompletedDays, climate.TemperatureKelvin, climate.EquilibriumTemperatureKelvin,
            climate.AbsorbedWattsPerSquareMeter, climate.EmittedWattsPerSquareMeter,
            climate.CumulativeAbsorbedJoulesPerSquareMeter, climate.CumulativeEmittedJoulesPerSquareMeter,
            climate.EnergyBalanceErrorJoulesPerSquareMeter));
    }
}

public sealed record ExperimentSample(int Day, double TemperatureKelvin, double EquilibriumTemperatureKelvin,
    double AbsorbedWattsPerSquareMeter, double EmittedWattsPerSquareMeter,
    double AbsorbedJoulesPerSquareMeter, double EmittedJoulesPerSquareMeter, double BudgetErrorJoulesPerSquareMeter);

public sealed record ExperimentResult(ExperimentScenario Scenario, IReadOnlyList<ExperimentSample> Samples)
{
    public string ToCsv()
    {
        var csv = new StringBuilder("day,temperature_K,equilibrium_K,absorbed_W_m2,emitted_W_m2,net_W_m2,absorbed_J_m2,emitted_J_m2,budget_error_J_m2\n");
        foreach (var sample in Samples)
        {
            var values = new double[] { sample.Day, sample.TemperatureKelvin, sample.EquilibriumTemperatureKelvin,
                sample.AbsorbedWattsPerSquareMeter, sample.EmittedWattsPerSquareMeter,
                sample.AbsorbedWattsPerSquareMeter - sample.EmittedWattsPerSquareMeter,
                sample.AbsorbedJoulesPerSquareMeter, sample.EmittedJoulesPerSquareMeter, sample.BudgetErrorJoulesPerSquareMeter };
            csv.AppendJoin(',', values.Select(value => value.ToString("R", CultureInfo.InvariantCulture))).Append('\n');
        }

        return csv.ToString();
    }
}
