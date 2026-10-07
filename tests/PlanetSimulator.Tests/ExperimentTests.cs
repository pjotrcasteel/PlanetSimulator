using System.Globalization;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Tests;

[TestClass]
public sealed class ExperimentTests
{
    public TestContext TestContext { get; set; } = null!;
    private static ExperimentScenario Example() => ExperimentScenario.Create("Afkoeling", 60, new ClimateParameters(),
        new ForcingChange { Day = 30, DistanceAstronomicalUnits = 1.5, BondAlbedo = 0.6 });

    [TestMethod]
    public void SerializedScenario_ReproducesEverySampleAndCsvExactly()
    {
        var scenario = Example();
        var first = new ExperimentRunner(scenario).Finish(TestContext.CancellationToken);
        var reloaded = ScenarioJson.Deserialize(ScenarioJson.Serialize(scenario));
        var second = new ExperimentRunner(reloaded).Finish(TestContext.CancellationToken);
        CollectionAssert.AreEqual(first.Samples.ToArray(), second.Samples.ToArray());
        Assert.AreEqual(first.ToCsv(), second.ToCsv());
        Assert.AreEqual(61, first.Samples.Count);
        Assert.AreEqual(230d, first.Samples[0].TemperatureKelvin);
    }

    [TestMethod]
    public void ScheduledChange_AppliesAtTheBoundaryWithoutATemperatureJump()
    {
        var changed = new ExperimentRunner(Example()).Finish(TestContext.CancellationToken);
        var baseline = new ExperimentRunner(ExperimentScenario.Create("Controle", 60, new ClimateParameters())).Finish(TestContext.CancellationToken);
        Assert.AreEqual(baseline.Samples[30].TemperatureKelvin, changed.Samples[30].TemperatureKelvin);
        Assert.IsTrue(changed.Samples[30].EquilibriumTemperatureKelvin < 200);
        Assert.IsTrue(changed.Samples[31].TemperatureKelvin < changed.Samples[30].TemperatureKelvin);
        Assert.IsTrue(baseline.Samples[31].TemperatureKelvin > baseline.Samples[30].TemperatureKelvin);
        Assert.IsTrue(changed.Samples.All(s => Math.Abs(s.BudgetErrorJoulesPerSquareMeter) < 0.01));
    }

    [TestMethod]
    public void DailyBatchesAndAnInteractiveSession_ProduceTheSameTemperature()
    {
        var scenario = ExperimentScenario.Create("Warm", 10, new ClimateParameters(1.2, 0.1, 2e7, 350, 0.8));
        var runner = new ExperimentRunner(scenario);
        var session = new SimulationSession(scenario.Climate);
        session.Clock.Speed = 86400;
        for (var day = 0; day < 10; day++)
        {
            runner.AdvanceDay(TestContext.CancellationToken);
            for (var frame = 0; frame < 60; frame++) session.Advance(1d / 60, TestContext.CancellationToken);
            Assert.AreEqual(session.Climate.TemperatureKelvin, runner.Samples[day + 1].TemperatureKelvin);
        }
        Assert.AreEqual(runner.Finish(TestContext.CancellationToken).ToCsv(),
            new ExperimentRunner(scenario).Finish(TestContext.CancellationToken).ToCsv());
    }

    [TestMethod]
    public void Runner_OwnsItsScenarioAndDoesNotAdvanceAfterCompletion()
    {
        var scenario = Example();
        var runner = new ExperimentRunner(scenario);
        scenario.Changes[0] = new ForcingChange { Day = 5, DistanceAstronomicalUnits = 2, BondAlbedo = 1 };
        Assert.AreEqual(30, runner.Scenario.Changes[0].Day);
        var result = runner.Finish(TestContext.CancellationToken);
        runner.AdvanceDay(TestContext.CancellationToken);
        Assert.AreEqual(result.ToCsv(), runner.GetResult().ToCsv());
    }

    [TestMethod]
    public void InvalidOrIncompatibleScenarios_AreRejected()
    {
        var valid = Example();
        Assert.ThrowsExactly<ArgumentException>(() => (valid with { FormatVersion = 2 }).Validate());
        Assert.ThrowsExactly<ArgumentException>(() => (valid with { ModelVersion = "future-model" }).Validate());
        Assert.ThrowsExactly<ArgumentException>(() => (valid with { DurationDays = ExperimentScenario.MaximumDurationDays + 1 }).Validate());
        Assert.ThrowsExactly<ArgumentException>(() => (valid with { Changes = [valid.Changes[0], valid.Changes[0]] }).Validate());
        Assert.ThrowsExactly<ArgumentException>(() => (valid with { Changes = [valid.Changes[0] with { Day = 60 }] }).Validate());
        var json = ScenarioJson.Serialize(valid);
        Assert.ThrowsExactly<JsonException>(() => ScenarioJson.Deserialize(json.Replace("\"formatVersion\": 1,", "\"formatVersion\": 1, \"unknown\": 1,")));
        Assert.ThrowsExactly<ArgumentException>(() => ScenarioJson.Deserialize(json.Replace("\"initialTemperatureKelvin\": 230,", "")));
        Assert.ThrowsExactly<JsonException>(() => ScenarioJson.Deserialize(json.Replace("\"durationDays\": 60,", "")));
        Assert.ThrowsExactly<ArgumentException>(() => ScenarioJson.Deserialize(new string(' ', ScenarioJson.MaximumBytes + 1)));
    }

    [TestMethod]
    public void Cancellation_CanResumeWithoutLosingOrRepeatingSamples()
    {
        var scenario = Example();
        var runner = new ExperimentRunner(scenario);
        runner.AdvanceDay(TestContext.CancellationToken);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => runner.AdvanceDay(cancelled.Token));
        Assert.AreEqual(1, runner.CompletedDays);
        Assert.ThrowsExactly<InvalidOperationException>(() => runner.GetResult());
        Assert.AreEqual(new ExperimentRunner(scenario).Finish(TestContext.CancellationToken).ToCsv(), runner.Finish(TestContext.CancellationToken).ToCsv());
    }

    [TestMethod]
    public void Csv_UsesExplicitUnitsAndInvariantRoundTripNumbers()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
            var result = new ExperimentRunner(ExperimentScenario.Create("Test", 1, new ClimateParameters())).Finish(TestContext.CancellationToken);
            var rows = result.ToCsv().TrimEnd().Split('\n');
            Assert.AreEqual(3, rows.Length);
            Assert.IsTrue(rows[0].Contains("budget_error_J_m2", StringComparison.Ordinal));
            Assert.AreEqual(9, rows[1].Split(',').Length);
            Assert.AreEqual(result.Samples[1].TemperatureKelvin, double.Parse(rows[2].Split(',')[1], CultureInfo.InvariantCulture));
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
    }
}
