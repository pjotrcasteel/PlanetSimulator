using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Tests;

[TestClass]
public sealed class Milestone12Tests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void PlanetScenarioCatalog_AllTemplatesAreUniqueValidAndReplayable()
    {
        Assert.AreEqual(PlanetScenarioCatalog.Templates.Count, PlanetScenarioCatalog.Templates.Select(item => item.Id).Distinct().Count());
        foreach (var template in PlanetScenarioCatalog.Templates)
        {
            var scenario = PlanetScenarioCatalog.Create(template.Id);
            scenario.Validate();
            Assert.AreEqual(ScenarioJson.Serialize(scenario), ScenarioJson.Serialize(ScenarioJson.Deserialize(ScenarioJson.Serialize(scenario))));
        }
    }

    [TestMethod]
    public void Scenario_LongHorizonAndSparseSampling_AreValidated()
    {
        var scenario = ExperimentScenario.Create("Lang", ExperimentScenario.MaximumDurationDays, new ClimateParameters()) with { SampleEveryDays = 30 };
        scenario.Validate();
        Assert.ThrowsExactly<ArgumentException>(() => (scenario with { DurationDays = ExperimentScenario.MaximumDurationDays + 1 }).Validate());
        Assert.ThrowsExactly<ArgumentException>(() => (scenario with { SampleEveryDays = scenario.DurationDays + 1 }).Validate());
    }

    [TestMethod]
    public void SparseSampling_PreservesChangeBoundariesAndFinalDay()
    {
        var scenario = ExperimentScenario.Create("Sparse", 120, new ClimateParameters(),
            new ForcingChange { Day = 45, DistanceAstronomicalUnits = 1.2, BondAlbedo = 0.4 }) with { SampleEveryDays = 30 };
        var result = new ExperimentRunner(scenario).Finish(TestContext.CancellationToken);
        CollectionAssert.AreEqual(new[] { 0, 30, 45, 60, 90, 120 }, result.Samples.Select(sample => sample.Day).ToArray());
    }

    [TestMethod]
    public void Inspector_ReportsFinalStateWithoutHabitabilityClassification()
    {
        var result = new ExperimentRunner(ExperimentScenario.Create("Inspectie", 2, new ClimateParameters())).Finish(TestContext.CancellationToken);
        var inspection = ExperimentInspector.Inspect(result);
        Assert.AreEqual(result.Samples[^1].TemperatureKelvin, inspection.FinalTemperatureKelvin);
        Assert.AreEqual(result.Samples[^1].TemperatureKelvin - result.Samples[0].TemperatureKelvin, inspection.TemperatureChangeKelvin);
        Assert.IsNull(inspection.LiquidWaterMassFraction);
        Assert.IsNull(inspection.TotalBiomassKilograms);
    }

    [TestMethod]
    public void AlbedoOptimizer_FindsKnownGridCandidateAndExportsInvariantCsv()
    {
        var baseline = ExperimentScenario.Create("Doel", 2, new ClimateParameters(bondAlbedo: 0.3));
        var target = new ExperimentRunner(baseline).Finish(TestContext.CancellationToken).Samples[^1].TemperatureKelvin;
        var result = AlbedoOptimizer.Optimize(baseline, target, 0.2, 0.4, 3, TestContext.CancellationToken);
        Assert.AreEqual(0.3, result.BestCandidate.BondAlbedo, 1e-12);

        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
            var csv = result.ToCsv();
            Assert.IsTrue(csv.Contains("0.3,", StringComparison.Ordinal));
            Assert.IsFalse(csv.Contains("0,3", StringComparison.Ordinal));
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
    }

    [TestMethod]
    public void AlbedoOptimizer_RejectsScheduledForcing()
    {
        var scenario = ExperimentScenario.Create("Wijziging", 2, new ClimateParameters(),
            new ForcingChange { Day = 1, DistanceAstronomicalUnits = 1, BondAlbedo = 0.4 });
        Assert.ThrowsExactly<ArgumentException>(() => AlbedoOptimizer.Optimize(scenario, 260, 0.1, 0.8, 5, TestContext.CancellationToken));
    }
}
