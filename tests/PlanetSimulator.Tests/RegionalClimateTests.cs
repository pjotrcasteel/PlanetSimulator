using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Tests;

[TestClass]
public sealed class RegionalClimateTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void Grid_AreasCoverTheSphereAndLongitudeWraps()
    {
        foreach (var dimensions in new[] { (12, 24), (24, 48), (6, 12) })
        {
            var grid = new SphericalGrid(6371000, dimensions.Item1, dimensions.Item2);
            var sphereArea = 4 * Math.PI * grid.RadiusMeters * grid.RadiusMeters;
            Assert.AreEqual(1d, grid.Cells.Sum(c => c.SolidAngle * grid.RadiusMeters * grid.RadiusMeters) / sphereArea, 1e-12);
            Assert.AreEqual(grid.GetCellIndex(0.3, 0), grid.GetCellIndex(0.3, Math.Tau));
            Assert.AreEqual(grid.GetCellIndex(0.3, Math.Tau - 0.1), grid.GetCellIndex(0.3, -0.1));
            Assert.AreEqual(0, grid.Cells[grid.GetCellIndex(Math.PI / 2, 0)].LatitudeIndex);
            Assert.AreEqual(grid.LatitudeBands - 1, grid.Cells[grid.GetCellIndex(-Math.PI / 2, 0)].LatitudeIndex);
        }
    }

    [TestMethod]
    public void Sunlight_InterceptedPowerIsCorrectInEverySeason()
    {
        foreach (var tilt in new[] { 0d, 23.44, 90 })
        {
            var model = new RegionalClimateModel(parameters: new RegionalParameters(tilt));
            foreach (var day in new[] { 0d, 13.125, 91.3125, 182.625, 273.9375 })
            {
                var fluxes = model.GetAbsorbedFluxes(day * 86400, TestContext.CancellationToken);
                Assert.AreEqual(238.175, fluxes.Average(), 1e-10);
                Assert.IsTrue(fluxes.Any(f => f == 0));
                Assert.IsTrue(fluxes.All(f => f >= 0));
            }
        }
    }

    [TestMethod]
    public void Equinox_IsSymmetricAndSunlightRotatesWithTheSolarDay()
    {
        var model = new RegionalClimateModel(parameters: new RegionalParameters(0));
        var morning = model.GetAbsorbedFluxes(0, TestContext.CancellationToken);
        var evening = model.GetAbsorbedFluxes(43200, TestContext.CancellationToken);
        foreach (var cell in model.Grid.Cells)
        {
            var mirror = (11 - cell.LatitudeIndex) * 24 + cell.LongitudeIndex;
            var rotated = cell.LatitudeIndex * 24 + (cell.LongitudeIndex + 12) % 24;
            Assert.AreEqual(morning[cell.Index], morning[mirror], 1e-10);
            Assert.AreEqual(morning[cell.Index], evening[rotated], 1e-10);
        }
    }

    [TestMethod]
    public void Tilt_ChangesSeasonalHemisphericPowerAndPolarNight()
    {
        var model = new RegionalClimateModel(parameters: new RegionalParameters(45));
        var summer = 365.25 * 86400 / 4;
        Assert.AreEqual(Math.PI / 4, model.SolarDeclinationRadians(summer), 1e-12);
        Assert.AreEqual(-Math.PI / 4, model.SolarDeclinationRadians(3 * summer), 1e-12);
        var fluxes = model.GetAbsorbedFluxes(summer, TestContext.CancellationToken);
        Assert.IsTrue(fluxes.Take(144).Average() > fluxes.Skip(144).Average());
        Assert.IsTrue(fluxes.Skip(264).All(f => f == 0));
    }

    [TestMethod]
    public void Transport_ConservesHeatIncludingTheLongitudeSeam()
    {
        var model = new RegionalClimateModel();
        var constant = Enumerable.Repeat(230d, 288).ToArray();
        Assert.IsTrue(model.GetTransportFluxes(constant, TestContext.CancellationToken).All(f => f == 0));
        constant[0] = 300;
        var fluxes = model.GetTransportFluxes(constant, TestContext.CancellationToken);
        Assert.AreEqual(0d, fluxes.Sum(), 1e-9);
        Assert.IsTrue(fluxes[0] < 0 && fluxes[23] > 0 && fluxes[24] > 0);
        model.SetParameters(new RegionalParameters(heatDiffusionWattsPerSquareMeterKelvin: 0), TestContext.CancellationToken);
        Assert.IsTrue(model.GetTransportFluxes(constant, TestContext.CancellationToken).All(f => f == 0));
    }

    [TestMethod]
    public void RadiationAndTransport_CloseTheEnergyBudgetAfterForcingEdits()
    {
        var model = new RegionalClimateModel();
        Integrate(model, 2 * 86400, 60);
        Assert.IsTrue(model.TemperaturesKelvin.Max() - model.TemperaturesKelvin.Min() > 1);
        var before = model.TemperaturesKelvin.ToArray();
        model.SetForcing(1.5, 0.6, TestContext.CancellationToken);
        model.SetParameters(new RegionalParameters(45, 1.2), TestContext.CancellationToken);
        CollectionAssert.AreEqual(before, model.TemperaturesKelvin.ToArray());
        Integrate(model, 86400, 60);
        Assert.AreEqual(0d, model.EnergyBalanceErrorJoulesPerSquareMeter, 0.02);
        Assert.IsTrue(model.TemperaturesKelvin.All(t => double.IsFinite(t) && t > 0));
    }

    [TestMethod]
    public void Diffusion_ReducesASharpTemperatureContrastWithoutAddingEnergy()
    {
        var initial = Enumerable.Repeat(230d, 288).ToArray();
        initial[0] = 350;
        var dark = new ClimateParameters(stellarLuminositySolarUnits: 0);
        var diffusive = new RegionalClimateModel(dark, new RegionalParameters(heatDiffusionWattsPerSquareMeterKelvin: 2), initial);
        var isolated = new RegionalClimateModel(dark, new RegionalParameters(heatDiffusionWattsPerSquareMeterKelvin: 0), initial);
        Integrate(diffusive, 86400, 60); Integrate(isolated, 86400, 60);
        Assert.IsTrue(diffusive.TemperaturesKelvin.Max() - diffusive.TemperaturesKelvin.Min()
            < isolated.TemperaturesKelvin.Max() - isolated.TemperaturesKelvin.Min());
        Assert.AreEqual(0d, diffusive.EnergyBalanceErrorJoulesPerSquareMeter, 0.02);
        Assert.IsTrue(diffusive.MeanTemperatureKelvin < initial.Average());
    }

    [TestMethod]
    public void RegionalRk4_ConvergesWhenTheTimeStepIsHalved()
    {
        var climate = new ClimateParameters(0.2, 0, 1e5, 1000, 2);
        var coarse = new RegionalClimateModel(climate, new RegionalParameters(90, 2));
        var fine = new RegionalClimateModel(climate, new RegionalParameters(90, 2));
        Integrate(coarse, 3600, 60); Integrate(fine, 3600, 30);
        for (var index = 0; index < 288; index++) Assert.AreEqual(fine.TemperaturesKelvin[index], coarse.TemperaturesKelvin[index], 0.02);
        Assert.IsTrue(coarse.TemperaturesKelvin.All(t => double.IsFinite(t) && t > 0));
    }

    [TestMethod]
    public void Session_FramePartitionPauseAndResetPreserveRegionalConsistency()
    {
        var first = new SimulationSession(regional: new RegionalParameters());
        var second = new SimulationSession(regional: new RegionalParameters());
        first.Clock.Speed = second.Clock.Speed = 86400;
        first.AdvanceClock(1, TestContext.CancellationToken);
        for (var frame = 0; frame < 60; frame++) second.AdvanceClock(1d / 60, TestContext.CancellationToken);
        CollectionAssert.AreEqual(first.Regional!.TemperaturesKelvin.ToArray(), second.Regional!.TemperaturesKelvin.ToArray());
        second.Clock.IsPaused = true;
        second.AdvanceClock(10, TestContext.CancellationToken);
        Assert.AreEqual(86400d, second.Regional.ElapsedSeconds);
        second.SetForcing(1.2, 0.4, TestContext.CancellationToken);
        second.Reset(TestContext.CancellationToken);
        Assert.AreEqual(0d, second.Regional.ElapsedSeconds);
        Assert.AreEqual(0d, second.Clock.ElapsedSeconds);
        Assert.IsTrue(second.Regional.TemperaturesKelvin.All(t => t == 230));
        Assert.AreEqual(1.2, second.Regional.Climate.DistanceAstronomicalUnits);
    }

    [TestMethod]
    public void RegionalScenario_JsonReplayReproducesEveryCellAndDailyExport()
    {
        var scenario = ExperimentScenario.CreateRegional("Seizoenen", 3, new ClimateParameters(), new RegionalParameters(45, 0.8),
            new ForcingChange { Day = 2, DistanceAstronomicalUnits = 1.2, BondAlbedo = 0.4 });
        var first = new ExperimentRunner(scenario).Finish(TestContext.CancellationToken);
        var second = new ExperimentRunner(ScenarioJson.Deserialize(ScenarioJson.Serialize(scenario))).Finish(TestContext.CancellationToken);
        Assert.AreEqual(first.ToCsv(), second.ToCsv());
        Assert.AreEqual(first.ToRegionalCsv(), second.ToRegionalCsv());
        CollectionAssert.AreEqual(first.FinalRegions.ToArray(), second.FinalRegions.ToArray());
        Assert.AreEqual(289, first.ToRegionalCsv().TrimEnd().Split('\n').Length);
        Assert.IsTrue(first.ToCsv().Contains("north_mean_K", StringComparison.Ordinal));
        Assert.AreEqual(4, first.Samples.Count);
    }

    [TestMethod]
    public void RegionalValidationAndCancellation_DoNotChangeState()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RegionalParameters(double.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RegionalParameters(91));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RegionalParameters(heatDiffusionWattsPerSquareMeterKelvin: 2.1));
        var model = new RegionalClimateModel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        cancellation.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => model.Advance(60, cancellation.Token));
        Assert.ThrowsExactly<OperationCanceledException>(() => model.SetForcing(1.5, 0.5, cancellation.Token));
        Assert.AreEqual(0d, model.ElapsedSeconds);
        Assert.IsTrue(model.TemperaturesKelvin.All(t => t == 230));
        var scenario = ExperimentScenario.CreateRegional("Test", 1, new ClimateParameters(), new RegionalParameters());
        Assert.ThrowsExactly<ArgumentException>(() => (scenario with { Regional = null }).Validate());
        Assert.ThrowsExactly<ArgumentException>(() => ScenarioJson.Deserialize(ScenarioJson.Serialize(scenario).Replace("\"yearDays\": 365.25", "\"wrongYear\": 365.25")));
    }

    private void Integrate(RegionalClimateModel model, int seconds, int step)
    {
        for (var time = 0; time < seconds; time += step) model.Advance(step, TestContext.CancellationToken);
    }
}
