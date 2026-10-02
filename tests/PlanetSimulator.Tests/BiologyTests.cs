using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Tests;

[TestClass]
public sealed class BiologyTests
{
    public TestContext TestContext { get; set; } = null!;
    private RegionalClimateModel Model(BiologyParameters? life = null, double temperature = 285, double light = 1,
        double water = 3, AtmosphereParameters? gas = null, bool hydrology = false) => new(
        new ClimateParameters(initialTemperatureKelvin: temperature, stellarLuminositySolarUnits: light),
        surface: new SurfaceParameters(0, water), waterCycle: hydrology ? new WaterCycleParameters() : null,
        atmosphere: (gas ?? new AtmosphereParameters()) with { Biology = life ?? new BiologyParameters() });

    private void Run(RegionalClimateModel model, int seconds = 86400, int step = 60)
    {
        for (var elapsed = 0; elapsed < seconds; elapsed += step) model.Advance(step, TestContext.CancellationToken);
    }

    [TestMethod]
    public void LightGrowth_ConservesWaterCarbonOxygenPhosphorusAndEnergy()
    {
        foreach (var hydrology in new[] { false, true })
        {
            var model = Model(hydrology: hydrology);
            var initial = model.Snapshot();
            Run(model, 3 * 86400);
            var result = model.Snapshot(); var life = result.Biology!; var gas = result.Atmosphere!;
            Assert.IsTrue(life.CumulativeProductionKilograms > 0);
            Assert.IsTrue(life.TotalBiomassKilograms > initial.Biology!.TotalBiomassKilograms);
            Assert.IsTrue(life.BiomassKilogramsPerSquareMeter.All(n => n >= 0));
            Assert.IsTrue(life.AvailablePhosphorusKilogramsPerSquareMeter.All(n => n >= 0));
            Assert.AreEqual(0, life.PhosphorusBudgetErrorKilograms / initial.Biology.TotalAvailablePhosphorusKilograms, 1e-12);
            Assert.AreEqual(0, gas.CarbonMassErrorKilograms / gas.TotalAtmosphericMassKilograms, 1e-12);
            Assert.AreEqual(0, gas.OxygenMassErrorKilograms / gas.TotalAtmosphericMassKilograms, 1e-12);
            Assert.AreEqual(0, result.Surface!.WaterMassErrorKilograms / initial.Surface!.TotalWaterMassKilograms, 1e-12);
            Assert.AreEqual(initial.Surface.TotalWaterMassKilograms, result.Surface.TotalWaterMassKilograms, initial.Surface.TotalWaterMassKilograms * 1e-12);
            Assert.AreEqual(0, result.BudgetErrorJoulesPerSquareMeter, 0.02);
        }
    }

    [TestMethod]
    public void Darkness_RespirationReleasesCarbonWaterAndChemicalEnergy()
    {
        var model = Model(light: 0); var initial = model.Snapshot();
        Run(model, 3600); var after = model.Snapshot();
        Assert.AreEqual(0d, after.Biology!.CumulativeProductionKilograms);
        Assert.IsTrue(after.Biology.CumulativeRespirationKilograms > 0);
        Assert.IsTrue(after.Biology.TotalBiomassKilograms < initial.Biology!.TotalBiomassKilograms);
        Assert.IsTrue(after.Atmosphere!.OxygenMassPerSquareMeter.Sum() < initial.Atmosphere!.OxygenMassPerSquareMeter.Sum());
        Assert.IsTrue(after.Surface!.TotalSurfaceWaterMassKilograms > initial.Surface!.TotalSurfaceWaterMassKilograms);
        Assert.AreEqual(0, after.BudgetErrorJoulesPerSquareMeter, 0.02);
    }

    [TestMethod]
    public void NoOxygen_BlocksAerobicRespiration()
    {
        var model = Model(light: 0, gas: new AtmosphereParameters(101325, 0.9996, 0, 0.0004, 0, 0));
        var initial = model.Snapshot().Biology!;
        Run(model, 3600);
        Assert.AreEqual(initial.TotalBiomassKilograms, model.Snapshot().Biology!.TotalBiomassKilograms);
        Assert.AreEqual(0d, model.Snapshot().Biology!.CumulativeRespirationKilograms);
    }

    [TestMethod]
    public void EmptyCarbonNutrientsOrInoculum_BlockProduction()
    {
        var noCarbon = Model(new BiologyParameters { RespirationPerDay = 0 }, gas: new AtmosphereParameters(101325, 0.79, 0.21, 0, 0, 0));
        var noNutrients = Model(new BiologyParameters { TotalPhosphorusKilogramsPerSquareMeter = 0 });
        var noSeed = Model(new BiologyParameters { InitialBiomassKilogramsPerSquareMeter = 0 });
        foreach (var model in new[] { noCarbon, noNutrients, noSeed })
        {
            Run(model, 3600);
            Assert.AreEqual(0d, model.Snapshot().Biology!.CumulativeProductionKilograms);
        }
    }

    [TestMethod]
    public void FrozenHotAndDryHabitats_AreInactive()
    {
        foreach (var model in new[] { Model(temperature: 230), Model(temperature: 330), Model(water: 0) })
        {
            var initial = model.Snapshot().Biology!;
            Run(model, 3600); var after = model.Snapshot().Biology!;
            Assert.AreEqual(initial.TotalBiomassKilograms, after.TotalBiomassKilograms);
            Assert.AreEqual(0d, after.CumulativeProductionKilograms);
            Assert.AreEqual(0d, after.CumulativeRespirationKilograms);
        }
    }

    [TestMethod]
    public void Production_IsBoundedByPhotonEnergyAndFiniteNutrients()
    {
        var settings = new BiologyParameters { PhotosyntheticEfficiency = 1e-8, RespirationPerDay = 0 };
        var model = Model(settings); Run(model, 3600); var life = model.Snapshot().Biology!;
        var area = model.Grid.CellAreaSquareMeters * model.Grid.Cells.Count;
        var chemicalIncrease = (life.TotalBiomassKilograms - Model(settings).Snapshot().Biology!.TotalBiomassKilograms) / area / 0.03002678 * 467000;
        Assert.IsTrue(chemicalIncrease <= model.CumulativeAbsorbedJoulesPerSquareMeter * settings.PhotosyntheticEfficiency * (1 + 1e-7));
        var limited = Model(new BiologyParameters { InitialBiomassKilogramsPerSquareMeter = 1, RespirationPerDay = 0 });
        var before = limited.Snapshot().Biology!; Run(limited, 3600);
        Assert.AreEqual(before.TotalBiomassKilograms, limited.Snapshot().Biology!.TotalBiomassKilograms, before.TotalBiomassKilograms * 1e-12);
    }

    [TestMethod]
    public void VersionedScenario_ReplaysDailyAndRegionalBiology()
    {
        var scenario = ExperimentScenario.CreateAtmosphereWithSurface("Microben", 2, new ClimateParameters(initialTemperatureKelvin: 285),
            new RegionalParameters(), new SurfaceParameters(), new AtmosphereParameters { Biology = new BiologyParameters() });
        Assert.AreEqual(ExperimentScenario.BiologyModelVersion, scenario.ModelVersion);
        var first = new ExperimentRunner(scenario).Finish(TestContext.CancellationToken);
        var second = new ExperimentRunner(ScenarioJson.Deserialize(ScenarioJson.Serialize(scenario))).Finish(TestContext.CancellationToken);
        Assert.AreEqual(first.ToCsv(), second.ToCsv()); Assert.AreEqual(first.ToRegionalCsv(), second.ToRegionalCsv());
        Assert.IsTrue(first.ToCsv().Contains("biomass_kg"));
        Assert.ThrowsExactly<ArgumentException>(() => (scenario with { ModelVersion = ExperimentScenario.AtmosphereModelVersion }).Validate());
    }

    [TestMethod]
    public void SmallerSteps_ConvergeForBiomass()
    {
        var coarse = Model(); var medium = Model(); var fine = Model();
        Run(coarse, 86400, 60); Run(medium, 86400, 30); Run(fine, 86400, 15);
        var a = coarse.Snapshot().Biology!.TotalBiomassKilograms;
        var b = medium.Snapshot().Biology!.TotalBiomassKilograms;
        var c = fine.Snapshot().Biology!.TotalBiomassKilograms;
        Assert.IsTrue(Math.Abs(b - c) < Math.Abs(a - c));
        Assert.AreEqual(c, a, c * 0.002);
    }

    [TestMethod]
    public void ValidationCancellationAndReset_PreserveState()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Model(new BiologyParameters { PhotosyntheticEfficiency = double.NaN }));
        Assert.ThrowsExactly<ArgumentException>(() => new RegionalClimateModel(atmosphere: new AtmosphereParameters { Biology = new BiologyParameters() }));
        var session = new SimulationSession(new ClimateParameters(initialTemperatureKelvin: 285), new RegionalParameters(), new SurfaceParameters(),
            atmosphere: new AtmosphereParameters { Biology = new BiologyParameters() });
        var initial = session.Snapshot().Regional!.Biology!; session.Clock.Speed = 86400;
        session.Advance(1, TestContext.CancellationToken); session.Reset(TestContext.CancellationToken);
        Assert.AreEqual(initial.TotalBiomassKilograms, session.Snapshot().Regional!.Biology!.TotalBiomassKilograms);
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken); source.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => session.Regional!.Advance(60, source.Token));
        Assert.AreEqual(initial.TotalBiomassKilograms, session.Snapshot().Regional!.Biology!.TotalBiomassKilograms);
    }
}
