using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Tests;

[TestClass]
public sealed class AtmosphereTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void ColumnMasses_ReconstructPressureAndPartialPressures()
    {
        var model = new RegionalClimateModel(atmosphere: new AtmosphereParameters());
        var atmosphere = model.Snapshot().Atmosphere!;
        Assert.AreEqual(101325, atmosphere.MeanSurfacePressurePascals, 0.01);
        Assert.AreEqual(0.0004, atmosphere.MeanCarbonDioxideMoleFraction, 1e-6);
        Assert.AreEqual(40.53, atmosphere.MeanCarbonDioxidePartialPressurePascals, 1e-9);
        Assert.IsTrue(atmosphere.MinimumGasMassPerSquareMeter >= 0);
        Assert.AreEqual(101325d, atmosphere.NitrogenPartialPressurePascals[0] + atmosphere.OxygenPartialPressurePascals[0]
            + atmosphere.ArgonPartialPressurePascals[0] + atmosphere.CarbonDioxidePartialPressurePascals[0], 1e-8);
    }

    [TestMethod]
    public void CarbonExchange_ClosesElementBalancesAndBoundsInventories()
    {
        var atmosphere = new AtmosphereParameters(crustalCarbonDioxideMassPerSquareMeter: 20) with
        {
            Co2HenryMolesPerCubicMeterPascal = 3.4e-4,
            Co2OutgassingKilogramsPerSquareMeterPerYear = 10,
        };
        var model = new RegionalClimateModel(climate: new ClimateParameters(initialTemperatureKelvin: 285), surface: new SurfaceParameters(waterEquivalentDepthMeters: 3),
            waterCycle: new WaterCycleParameters(), atmosphere: atmosphere);
        for (var step = 0; step < 120; step++) model.Advance(60, TestContext.CancellationToken);
        var snapshot = model.Snapshot();
        var gas = snapshot.Atmosphere!;
        Assert.AreEqual(0, gas.CarbonMassErrorKilograms / gas.TotalAtmosphericMassKilograms, 1e-12);
        Assert.AreEqual(0, gas.OxygenMassErrorKilograms / gas.TotalAtmosphericMassKilograms, 1e-12);
        Assert.AreEqual(0, gas.NitrogenMassErrorKilograms / gas.TotalAtmosphericMassKilograms, 1e-12);
        Assert.IsTrue(gas.MinimumGasMassPerSquareMeter >= 0);
        Assert.IsTrue(gas.TotalDissolvedCarbonDioxideMassKilograms >= 0);
        Assert.IsTrue(Math.Abs(snapshot.Surface!.WaterMassErrorKilograms) / snapshot.Surface.TotalWaterMassKilograms < 1e-12);
    }

    [TestMethod]
    public void AtmosphereScenario_JsonReplayIsDeterministic()
    {
        var scenario = ExperimentScenario.CreateAtmosphereWithSurface("Chemie", 2, new ClimateParameters(initialTemperatureKelvin: 285),
            new RegionalParameters(), new SurfaceParameters(), new AtmosphereParameters());
        var first = new ExperimentRunner(scenario).Finish(TestContext.CancellationToken);
        var second = new ExperimentRunner(ScenarioJson.Deserialize(ScenarioJson.Serialize(scenario))).Finish(TestContext.CancellationToken);
        Assert.AreEqual(first.ToCsv(), second.ToCsv());
        Assert.AreEqual(first.ToRegionalCsv(), second.ToRegionalCsv());
    }

    [TestMethod]
    public void VacuumAndPureCarbonDioxide_HaveFinitePressure()
    {
        foreach (var pressure in new[] { 0d, 101325 })
        {
            var model = new RegionalClimateModel(atmosphere: new AtmosphereParameters(pressure, 0, 0, 1, 0, 0));
            model.Advance(60, TestContext.CancellationToken);
            var gas = model.Snapshot().Atmosphere!;
            Assert.AreEqual(pressure, gas.MeanCarbonDioxidePartialPressurePascals, 1e-8);
            Assert.AreEqual(pressure == 0 ? 0d : 1d, gas.MeanCarbonDioxideMoleFraction);
        }
    }

    [TestMethod]
    public void Outgassing_StopsWhenTheFiniteCrustIsEmpty()
    {
        var parameters = new AtmosphereParameters(crustalCarbonDioxideMassPerSquareMeter: 0.001)
        { Co2OutgassingKilogramsPerSquareMeterPerYear = 100000 };
        var model = new RegionalClimateModel(atmosphere: parameters);
        model.Advance(60, TestContext.CancellationToken);
        var first = model.Snapshot().Atmosphere!;
        model.Advance(60, TestContext.CancellationToken);
        var second = model.Snapshot().Atmosphere!;
        Assert.AreEqual(0d, second.RemainingCrustalCarbonDioxideMassKilograms);
        Assert.AreEqual(first.CumulativeOutgassingKilograms, second.CumulativeOutgassingKilograms);
        Assert.AreEqual(0d, second.CarbonMassErrorKilograms / second.TotalAtmosphericMassKilograms, 1e-12);
    }

    [TestMethod]
    public void ColderWater_DissolvesMoreCarbonDioxide()
    {
        var gas = new AtmosphereParameters() { Co2ExchangeTimescaleHours = 0.00001 };
        var cold = new RegionalClimateModel(new ClimateParameters(initialTemperatureKelvin: 278), surface: new SurfaceParameters(0, 100), atmosphere: gas);
        var warm = new RegionalClimateModel(new ClimateParameters(initialTemperatureKelvin: 298), surface: new SurfaceParameters(0, 100), atmosphere: gas);
        cold.Advance(60, TestContext.CancellationToken); warm.Advance(60, TestContext.CancellationToken);
        Assert.IsTrue(cold.Snapshot().Atmosphere!.TotalDissolvedCarbonDioxideMassKilograms > warm.Snapshot().Atmosphere!.TotalDissolvedCarbonDioxideMassKilograms);
        var pressure = warm.Snapshot().Atmosphere!.MeanCarbonDioxidePartialPressurePascals;
        var dissolved = warm.Snapshot().Atmosphere!.DissolvedCarbonDioxideMassPerSquareMeter[0];
        // Independent dilute-water reference at approximately 298 K, 100 m of liquid.
        Assert.AreEqual(3.4e-4 * 100 * pressure * AtmosphereParameters.CarbonDioxideMolarMassKilogramsPerMole, dissolved, dissolved * 0.01);
    }

    [TestMethod]
    public void InvalidParametersAndCancellation_DoNotMutateTheModel()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new AtmosphereParameters(nitrogenMoleFraction: 0.5));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RegionalClimateModel(atmosphere: new AtmosphereParameters() { Co2ExchangeTimescaleHours = double.NaN }));
        Assert.ThrowsExactly<ArgumentException>(() => new SimulationSession(atmosphere: new AtmosphereParameters()));
        var model = new RegionalClimateModel(atmosphere: new AtmosphereParameters());
        var before = model.Snapshot().Atmosphere!;
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        source.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => model.Advance(60, source.Token));
        Assert.AreEqual(0d, model.ElapsedSeconds);
        CollectionAssert.AreEqual(before.CarbonDioxideMassPerSquareMeter.ToArray(), model.Snapshot().Atmosphere!.CarbonDioxideMassPerSquareMeter.ToArray());
    }

    [TestMethod]
    public void FreezingWater_ReleasesPreviouslyDissolvedCarbonDioxide()
    {
        var model = new RegionalClimateModel(new ClimateParameters(arealHeatCapacity: 1e5, initialTemperatureKelvin: 285, stellarLuminositySolarUnits: 0),
            surface: new SurfaceParameters(0, 0.01), atmosphere: new AtmosphereParameters() { Co2ExchangeTimescaleHours = 0.001 });
        model.Advance(60, TestContext.CancellationToken);
        Assert.IsTrue(model.Snapshot().Atmosphere!.TotalDissolvedCarbonDioxideMassKilograms > 0);
        for (var step = 0; step < 1440; step++) model.Advance(60, TestContext.CancellationToken);
        var gas = model.Snapshot().Atmosphere!;
        Assert.IsTrue(gas.CumulativeCarbonDioxideReleaseKilograms > 0);
        Assert.AreEqual(0d, gas.CarbonMassErrorKilograms / gas.TotalAtmosphericMassKilograms, 1e-12);
    }
}
