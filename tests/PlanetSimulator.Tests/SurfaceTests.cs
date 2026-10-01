using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Tests;

/// <summary>
/// Checks phase energetics, inventory allocation and deterministic coupled integration.
/// </summary>
[TestClass]
public sealed class SurfaceTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void Enthalpy_PaysLatentHeatBeforeTemperatureRises()
    {
        const double mass = 20, capacity = 1e7;
        var latent = mass * WaterThermodynamics.LatentHeatJoulesPerKilogram;
        foreach (var fraction in new[] { 0d, 0.25, 0.5, 1 })
        {
            Assert.AreEqual(273.15, WaterThermodynamics.Temperature(latent * fraction, mass, capacity));
            Assert.AreEqual(fraction, WaterThermodynamics.LiquidFraction(latent * fraction, mass));
        }
        foreach (var kelvin in new[] { 230d, 273.15, 280 })
            Assert.AreEqual(kelvin, WaterThermodynamics.Temperature(WaterThermodynamics.Enthalpy(kelvin, mass, capacity), mass, capacity), 1e-12);
        Assert.AreEqual(274.15, WaterThermodynamics.Temperature(latent + capacity + mass * 4180, mass, capacity), 1e-12);
    }

    [TestMethod]
    public void Reservoirs_FillBasinsToCommonLevelAndPreserveInventory()
    {
        var grid = new SphericalGrid();
        var surface = new SurfaceReservoirs(grid, new SurfaceParameters());
        var expected = grid.CellAreaSquareMeters * grid.Cells.Count * 3000;
        Assert.AreEqual(1d, surface.TotalWaterMassKilograms / expected, 1e-12);
        Assert.IsTrue(surface.WaterMassPerSquareMeter.Any(m => m == 0));
        Assert.IsTrue(surface.WaterMassPerSquareMeter.Any(m => m > 0));
        foreach (var cell in grid.Cells.Where(c => surface.WaterMassPerSquareMeter[c.Index] > 0))
            Assert.AreEqual(surface.InitialWaterLevelMeters,
                surface.ElevationsMeters[cell.Index] + surface.WaterMassPerSquareMeter[cell.Index] / 1000, 1e-12);
        CollectionAssert.AreEqual(surface.ElevationsMeters.ToArray(), new SurfaceReservoirs(grid, new SurfaceParameters()).ElevationsMeters.ToArray());
    }

    [TestMethod]
    public void CoupledModel_MeltsAndFreezesWithClosedBudgets()
    {
        foreach (var warming in new[] { true, false })
        {
            var model = new RegionalClimateModel(new ClimateParameters(initialTemperatureKelvin: warming ? 273.15 : 273.16,
                stellarLuminositySolarUnits: warming ? 2 : 0), new RegionalParameters(), surface: new SurfaceParameters(0, 0.01));
            var before = model.Snapshot().Surface!;
            for (var step = 0; step < 1440; step++) model.Advance(60, TestContext.CancellationToken);
            var after = model.Snapshot().Surface!;
            Assert.AreEqual(before.TotalWaterMassKilograms, after.TotalWaterMassKilograms);
            Assert.IsTrue(warming ? after.LiquidMassFraction > 0 : after.LiquidMassFraction < 1);
            for (var i = 0; i < 288; i++)
            {
                Assert.IsTrue(after.LiquidMassPerSquareMeter[i] >= 0 && after.IceMassPerSquareMeter[i] >= 0);
                Assert.AreEqual(after.WaterMassPerSquareMeter[i], after.LiquidMassPerSquareMeter[i] + after.IceMassPerSquareMeter[i], 1e-10);
            }
            Assert.IsTrue(Math.Abs(model.EnergyBalanceErrorJoulesPerSquareMeter) < 0.02);
            Assert.IsTrue(Math.Abs(after.WaterMassErrorKilograms) / after.TotalWaterMassKilograms < 1e-12);
        }
    }

    [TestMethod]
    public void ZeroWater_RecoversRegionalTemperatures()
    {
        var dry = new RegionalClimateModel();
        var surface = new RegionalClimateModel(surface: new SurfaceParameters(waterEquivalentDepthMeters: 0));
        for (var step = 0; step < 1440; step++)
        {
            dry.Advance(60, TestContext.CancellationToken);
            surface.Advance(60, TestContext.CancellationToken);
        }
        for (var i = 0; i < 288; i++) Assert.AreEqual(dry.TemperaturesKelvin[i], surface.TemperaturesKelvin[i], 1e-9);
        Assert.AreEqual(0d, surface.Snapshot().Surface!.TotalWaterMassKilograms);
    }

    [TestMethod]
    public void CoupledSteps_ConvergeAcrossPhaseBoundary()
    {
        var climate = new ClimateParameters(initialTemperatureKelvin: 273.14, stellarLuminositySolarUnits: 2);
        var full = new RegionalClimateModel(climate, surface: new SurfaceParameters(0, 0.01));
        var half = new RegionalClimateModel(climate, surface: new SurfaceParameters(0, 0.01));
        for (var step = 0; step < 1440; step++)
        {
            full.Advance(60, TestContext.CancellationToken);
            half.Advance(30, TestContext.CancellationToken); half.Advance(30, TestContext.CancellationToken);
        }
        for (var i = 0; i < 288; i++) Assert.AreEqual(full.TemperaturesKelvin[i], half.TemperaturesKelvin[i], 0.001);
        Assert.AreEqual(full.Snapshot().Surface!.LiquidMassFraction, half.Snapshot().Surface!.LiquidMassFraction, 0.0001);
    }

    [TestMethod]
    public void Scenario_RoundTripReplaysWaterAndEveryCell()
    {
        var scenario = ExperimentScenario.CreateSurface("Smelten", 2, new ClimateParameters(initialTemperatureKelvin: 273.15),
            new RegionalParameters(), new SurfaceParameters(0, 0.01));
        var first = new ExperimentRunner(scenario).Finish(TestContext.CancellationToken);
        var second = new ExperimentRunner(ScenarioJson.Deserialize(ScenarioJson.Serialize(scenario))).Finish(TestContext.CancellationToken);
        Assert.AreEqual(first.ToCsv(), second.ToCsv());
        Assert.AreEqual(first.ToRegionalCsv(), second.ToRegionalCsv());
        Assert.IsTrue(first.ToCsv().Contains("liquid_mass_fraction"));
        Assert.IsTrue(first.ToRegionalCsv().Contains("ice_kg_m2"));
        var session = new SimulationSession(scenario.Climate, scenario.Regional, scenario.Surface);
        session.Advance(1, TestContext.CancellationToken);
        session.Reset(TestContext.CancellationToken);
        Assert.AreEqual(0d, session.Clock.ElapsedSeconds);
        Assert.AreEqual(scenario.Surface, session.Regional!.Surface!.Parameters);
    }

    [TestMethod]
    public void InvalidSettingsAndCancellation_LeaveStateIntact()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SurfaceParameters(double.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SurfaceParameters(waterEquivalentDepthMeters: -1));
        var model = new RegionalClimateModel(surface: new SurfaceParameters());
        using var source = new CancellationTokenSource(); source.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => model.Advance(60, source.Token));
        Assert.AreEqual(0d, model.ElapsedSeconds);
        var scenario = ExperimentScenario.CreateSurface("Test", 1, new ClimateParameters(), new RegionalParameters(), new SurfaceParameters());
        Assert.ThrowsExactly<ArgumentException>(() => ScenarioJson.Deserialize(ScenarioJson.Serialize(scenario).Replace("\"reliefMeters\"", "\"unknown\"")));
    }
}
