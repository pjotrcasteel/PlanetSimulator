using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Tests;

[TestClass]
public sealed class PlanetProfileTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void Profiles_HaveDistinctPhysicalPropertiesAndValidCoupledInputs()
    {
        Assert.AreEqual(3, PlanetProfiles.All.Count);
        foreach (var profile in PlanetProfiles.All)
        {
            profile.Planet.Validate();
            Assert.IsTrue(profile.Planet.SurfaceGravityMetersPerSecondSquared > 0);
            Assert.IsTrue(profile.Surface.WaterEquivalentDepthMeters >= 0);
        }

        var reference = PlanetProfiles.Get(PlanetProfiles.ReferenceTerrestrialId);
        var small = PlanetProfiles.Get(PlanetProfiles.SmallDryId);
        var ocean = PlanetProfiles.Get(PlanetProfiles.OceanSuperEarthId);
        Assert.IsTrue(small.Planet.RadiusMeters < reference.Planet.RadiusMeters);
        Assert.IsTrue(ocean.Planet.SurfaceGravityMetersPerSecondSquared > reference.Planet.SurfaceGravityMetersPerSecondSquared);
        Assert.IsTrue(ocean.Surface.WaterEquivalentDepthMeters > reference.Surface.WaterEquivalentDepthMeters);
        foreach (var profile in PlanetProfiles.All)
        {
            var scenario = ExperimentScenario.CreateAtmosphereWithSurface(profile.Name, 1, profile.Climate, profile.Regional, profile.Surface, profile.Atmosphere)
                with { Planet = profile.Planet };
            scenario.Validate();
        }
    }

    [TestMethod]
    public void CustomPlanetScenario_ReplaysAndExportsItsPhysicalCellArea()
    {
        var profile = PlanetProfiles.Get(PlanetProfiles.SmallDryId);
        var scenario = ExperimentScenario.CreateSurface("Small world", 1, profile.Climate, profile.Regional, profile.Surface)
            with { Planet = profile.Planet };
        scenario.Validate();
        var serialized = ScenarioJson.Serialize(scenario);
        var reloaded = ScenarioJson.Deserialize(serialized);
        var first = new ExperimentRunner(scenario).Finish(TestContext.CancellationToken);
        var second = new ExperimentRunner(reloaded).Finish(TestContext.CancellationToken);
        Assert.AreEqual(first.ToCsv(), second.ToCsv());
        Assert.AreEqual(first.ToRegionalCsv(), second.ToRegionalCsv());
        Assert.AreEqual(profile.Planet.RadiusMeters, reloaded.Planet.RadiusMeters);
        var firstArea = double.Parse(first.ToRegionalCsv().Split('\n')[1].Split(',')[3], System.Globalization.CultureInfo.InvariantCulture);
        var expectedArea = 4 * Math.PI * profile.Planet.RadiusMeters * profile.Planet.RadiusMeters / 288;
        Assert.AreEqual(expectedArea, firstArea, expectedArea * 1e-12);
    }

    [TestMethod]
    public void PlanetParameters_ChangeRotationGridAreaAndGravity()
    {
        var parameters = new PlanetParameters(3_000_000, 8e23, 100_000);
        var planet = new Planet(parameters);
        var model = new RegionalClimateModel(planet: parameters);
        Assert.AreEqual(Math.PI, planet.GetRotationRadians(50_000), 1e-12);
        Assert.AreEqual(parameters.SurfaceGravityMetersPerSecondSquared, planet.SurfaceGravity, 1e-12);
        Assert.AreEqual(4 * Math.PI * parameters.RadiusMeters * parameters.RadiusMeters,
            model.Grid.Cells.Count * model.Grid.CellAreaSquareMeters, 1);
    }

    [TestMethod]
    public void PlanetRotationPeriod_ChangesRegionalDaylightTiming()
    {
        var fast = new PlanetParameters(rotationPeriodSeconds: 43_200);
        var normal = new PlanetParameters(rotationPeriodSeconds: 86_400);
        var fastModel = new RegionalClimateModel(planet: fast);
        var normalModel = new RegionalClimateModel(planet: normal);
        var start = fastModel.GetAbsorbedFluxes(0, TestContext.CancellationToken);
        var fastHalfDay = fastModel.GetAbsorbedFluxes(21_600, TestContext.CancellationToken);
        var normalQuarterDay = normalModel.GetAbsorbedFluxes(21_600, TestContext.CancellationToken);
        CollectionAssert.AreNotEqual(start, fastHalfDay);
        CollectionAssert.AreNotEqual(fastHalfDay, normalQuarterDay);
    }
}
