using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Tests;

[TestClass]
public sealed class WaterCycleTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void InitialReservoirs_WithdrawAtmosphericWaterFromTheSurface()
    {
        foreach (var depth in new[] { 0d, 0.001, 3 })
        {
            var model = Create(depth);
            var surface = model.Snapshot().Surface!;
            var expected = depth * 1000 * 4 * Math.PI * model.Grid.RadiusMeters * model.Grid.RadiusMeters;
            Assert.AreEqual(expected, surface.TotalWaterMassKilograms, Math.Max(1, expected) * 1e-12);
            Assert.AreEqual(285d, model.MeanTemperatureKelvin, 1e-10);
            Assert.AreEqual(0d, model.EnergyBalanceErrorJoulesPerSquareMeter, 1e-6);
            Integrate(model, 86400, 60);
            surface = model.Snapshot().Surface!;
            Assert.AreEqual(expected, surface.TotalWaterMassKilograms, Math.Max(1, expected) * 1e-12);
            Assert.IsTrue(surface.WaterCycle!.VaporMassPerSquareMeter.All(m => double.IsFinite(m) && m >= 0));
        }
    }

    [TestMethod]
    public void CoupledWaterAndRadiation_CloseEnergyAndMassBudgets()
    {
        foreach (var temperature in new[] { 230d, 273.15, 285 })
        {
            var model = new RegionalClimateModel(new ClimateParameters(initialTemperatureKelvin: temperature),
                surface: new SurfaceParameters(), waterCycle: new WaterCycleParameters());
            Integrate(model, 3 * 86400, 60);
            var surface = model.Snapshot().Surface!;
            Assert.AreEqual(0d, surface.WaterMassErrorKilograms / surface.TotalWaterMassKilograms, 1e-12);
            Assert.AreEqual(0d, model.EnergyBalanceErrorJoulesPerSquareMeter, 0.02);
            Assert.IsTrue(model.TemperaturesKelvin.All(t => double.IsFinite(t) && t > 0));
            Assert.IsTrue(surface.WaterMassPerSquareMeter.All(m => m >= 0));
            Assert.IsTrue(surface.WaterCycle!.CloudMassPerSquareMeter.All(m => m >= 0));
            Assert.IsTrue(surface.WaterCycle.CumulativePrecipitationKilograms > 0);
        }
    }

    [TestMethod]
    public void Hydrology_ConvergesWithHalvedTimeSteps()
    {
        var coarse = Create(3); var fine = Create(3); var reference = Create(3);
        Integrate(coarse, 86400, 60); Integrate(fine, 86400, 30); Integrate(reference, 86400, 15);
        var coarseError = coarse.TemperaturesKelvin.Zip(reference.TemperaturesKelvin).Max(p => Math.Abs(p.First - p.Second));
        var fineError = fine.TemperaturesKelvin.Zip(reference.TemperaturesKelvin).Max(p => Math.Abs(p.First - p.Second));
        Assert.IsTrue(coarseError < 0.02, $"Coarse temperature error: {coarseError}");
        Assert.IsTrue(fineError < coarseError * 0.75);
        var a = coarse.Snapshot().Surface!.WaterCycle!.VaporMassPerSquareMeter;
        var b = fine.Snapshot().Surface!.WaterCycle!.VaporMassPerSquareMeter;
        Assert.IsTrue(a.Zip(b).Max(p => Math.Abs(p.First - p.Second)) < 0.02);
    }

    [TestMethod]
    public void HydrologyScenario_ReplaysAndResetRestoresReservoirs()
    {
        var scenario = ExperimentScenario.CreateHydrology("Hydrology", 2, new ClimateParameters(initialTemperatureKelvin: 285),
            new RegionalParameters(), new SurfaceParameters(), new WaterCycleParameters());
        var first = new ExperimentRunner(scenario).Finish(TestContext.CancellationToken);
        var second = new ExperimentRunner(ScenarioJson.Deserialize(ScenarioJson.Serialize(scenario))).Finish(TestContext.CancellationToken);
        Assert.AreEqual(first.ToCsv(), second.ToCsv());
        Assert.AreEqual(first.ToRegionalCsv(), second.ToRegionalCsv());
        var session = new SimulationSession(scenario.Climate, scenario.Regional, scenario.Surface, scenario.Hydrology);
        var initial = session.Snapshot().Regional!.Surface!;
        session.Clock.Speed = 86400;
        session.AdvanceClock(1, TestContext.CancellationToken);
        session.Reset(TestContext.CancellationToken);
        CollectionAssert.AreEqual(initial.WaterMassPerSquareMeter.ToArray(), session.Snapshot().Regional!.Surface!.WaterMassPerSquareMeter.ToArray());
    }

    private static RegionalClimateModel Create(double depth) => new(new ClimateParameters(initialTemperatureKelvin: 285),
        surface: new SurfaceParameters(waterEquivalentDepthMeters: depth), waterCycle: new WaterCycleParameters());

    private void Integrate(RegionalClimateModel model, int duration, int step)
    {
        for (var elapsed = 0; elapsed < duration; elapsed += step) model.Advance(step, TestContext.CancellationToken);
    }
}
