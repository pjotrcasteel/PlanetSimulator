using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Tests;

[TestClass]
public sealed class TerraformingTests
{
    public TestContext TestContext { get; set; } = null!;
    private static TerraformInstallation Plant(string kind = TerraformInstallation.Heater) => new()
    {
        Kind = kind, ConstructionDays = 0, ConstructionEnergyJoulesPerSquareMeter = 0,
        ConstructionMaterialKilogramsPerSquareMeter = 0, StartDay = 0, EndDay = 30,
    };
    private static RegionalClimateModel Model(TerraformingParameters settings, bool coupled = false) => new(
        new ClimateParameters(initialTemperatureKelvin: 285), surface: new SurfaceParameters(0, 3),
        waterCycle: coupled ? new WaterCycleParameters() : null,
        atmosphere: new AtmosphereParameters { Co2HenryMolesPerCubicMeterPascal = coupled ? 3.4e-4 : 0,
            Biology = coupled ? new BiologyParameters() : null, Terraforming = settings });
    private void Run(RegionalClimateModel model, int duration = 86400, int step = 60)
    {
        for (var elapsed = 0; elapsed < duration; elapsed += step) model.Advance(step, TestContext.CancellationToken);
    }

    [TestMethod]
    public void Heater_UsesFiniteEnergyAndWarmsOnlyThroughTheBudget()
    {
        var heater = Plant() with { PowerWattsPerSquareMeter = 100 };
        var model = Model(new TerraformingParameters { InitialEnergyJoulesPerSquareMeter = 1000, Installations = [heater] });
        var control = Model(new TerraformingParameters { InitialEnergyJoulesPerSquareMeter = 1000, Installations = [] });
        Run(model, 120); Run(control, 120);
        var snapshot = model.Snapshot(); var engineering = snapshot.Terraforming!;
        Assert.AreEqual(1000, engineering.TotalUsedEnergyJoules / model.Grid.CellAreaSquareMeters, 1e-9);
        Assert.AreEqual(0d, engineering.RemainingEnergyJoulesPerSquareMeter[144]);
        Assert.AreEqual("Geen energie", engineering.Installations[0].Status);
        Assert.IsTrue(snapshot.TemperaturesKelvin[144] > control.TemperaturesKelvin[144]);
        Assert.AreEqual(0, snapshot.BudgetErrorJoulesPerSquareMeter, 0.02);
        Assert.AreEqual(0, engineering.EnergyBudgetErrorJoulesPerSquareMeter, 0.02);
    }

    [TestMethod]
    public void ConstructionAndOperatingWindow_RespectExactBoundaries()
    {
        var plan = Plant() with { StartDay = 60d / 86400, ConstructionDays = 120d / 86400, EndDay = 300d / 86400 };
        var model = Model(new TerraformingParameters { Installations = [plan] });
        Run(model, 180);
        Assert.AreEqual(0d, model.Snapshot().Terraforming!.TotalUsedEnergyJoules);
        Run(model, 180);
        var result = model.Snapshot().Terraforming!;
        Assert.AreEqual(120 * 100, result.TotalUsedEnergyJoules / model.Grid.CellAreaSquareMeters, 1e-6);
        Assert.AreEqual("Gestopt", result.Installations[0].Status);
    }

    [TestMethod]
    public void Construction_SharedStocksAreAllocatedOnceInScenarioOrder()
    {
        var plan = Plant() with { ConstructionMaterialKilogramsPerSquareMeter = 2, ConstructionEnergyJoulesPerSquareMeter = 100 };
        var model = Model(new TerraformingParameters { InitialMaterialKilogramsPerSquareMeter = 2, Installations = [plan, plan] });
        Run(model, 120);
        var engineering = model.Snapshot().Terraforming!;
        Assert.AreEqual(2, engineering.TotalBuiltMaterialKilograms / model.Grid.CellAreaSquareMeters, 1e-12);
        Assert.AreEqual("Geen bouwmateriaal", engineering.Installations[1].Status);
        Assert.AreEqual(0d, engineering.MaterialBudgetErrorKilograms);
        Assert.AreEqual(12100, engineering.TotalUsedEnergyJoules / model.Grid.CellAreaSquareMeters, 1e-6);
    }

    [TestMethod]
    public void InsufficientConstructionEnergy_ConsumesNoMaterial()
    {
        var model = Model(new TerraformingParameters { InitialEnergyJoulesPerSquareMeter = 10,
            Installations = [Plant() with { ConstructionEnergyJoulesPerSquareMeter = 11, ConstructionMaterialKilogramsPerSquareMeter = 2 }] });
        Run(model, 120); var result = model.Snapshot().Terraforming!;
        Assert.AreEqual("Geen energie", result.Installations[0].Status);
        Assert.AreEqual(0d, result.TotalUsedEnergyJoules);
        Assert.AreEqual(0d, result.TotalBuiltMaterialKilograms);
    }

    [TestMethod]
    public void Capture_StopsAtTankCapacityAndRetainsCarbonAndOxygen()
    {
        var model = Model(new TerraformingParameters { StorageCapacityKilogramsPerSquareMeter = 0.0001,
            Installations = [Plant(TerraformInstallation.Capture)] });
        var initial = model.Snapshot().Atmosphere!;
        Run(model, 180); var result = model.Snapshot(); var engineering = result.Terraforming!;
        Assert.AreEqual(0.0001, engineering.StoredCarbonDioxideKilogramsPerSquareMeter[144], 1e-12);
        Assert.AreEqual("Opslag vol", engineering.Installations[0].Status);
        Assert.IsTrue(result.Atmosphere!.CarbonDioxidePartialPressurePascals[144] < initial.CarbonDioxidePartialPressurePascals[144]);
        Assert.AreEqual(0, result.Atmosphere.CarbonMassErrorKilograms / initial.TotalAtmosphericMassKilograms, 1e-12);
        Assert.AreEqual(0, result.Atmosphere.OxygenMassErrorKilograms / initial.TotalAtmosphericMassKilograms, 1e-12);
        Assert.AreEqual(0, result.BudgetErrorJoulesPerSquareMeter, 0.02);
    }

    [TestMethod]
    public void Transport_CannotDeliverEarlyAndDrainsShipmentsAfterShutdown()
    {
        var capture = Plant(TerraformInstallation.Capture) with { EndDay = 120d / 86400 };
        var transport = Plant(TerraformInstallation.Transport) with { EndDay = 120d / 86400, TransportDays = 120d / 86400 };
        var model = Model(new TerraformingParameters { Installations = [capture, transport] });
        var initial = model.Snapshot().Atmosphere!;
        Run(model, 120);
        Assert.AreEqual(0d, model.Snapshot().Terraforming!.CumulativeDeliveredKilograms);
        Assert.IsTrue(model.Snapshot().Terraforming!.TotalInTransitCarbonDioxideKilograms > 0);
        Run(model, 120); var snapshot = model.Snapshot(); var result = snapshot.Terraforming!;
        Assert.AreEqual(result.CumulativeCapturedKilograms, result.CumulativeDeliveredKilograms, result.CumulativeCapturedKilograms * 1e-12);
        Assert.AreEqual(0d, result.TotalInTransitCarbonDioxideKilograms, 1);
        Assert.IsTrue(snapshot.Atmosphere!.CarbonDioxidePartialPressurePascals[145] > initial.CarbonDioxidePartialPressurePascals[145]);
        Assert.AreEqual(0, result.LogisticsBudgetErrorKilograms, 1);
        Assert.AreEqual(0, snapshot.Atmosphere.CarbonMassErrorKilograms / initial.TotalAtmosphericMassKilograms, 1e-12);
    }

    [TestMethod]
    public void DistantTransport_CostsMoreForTheSamePayload()
    {
        var capture = Plant(TerraformInstallation.Capture);
        var transport = Plant(TerraformInstallation.Transport);
        var near = Model(new TerraformingParameters { Installations = [capture, transport] });
        var far = Model(new TerraformingParameters { Installations = [capture, transport with { DestinationCellIndex = 156 }] });
        Run(near, 60); Run(far, 60);
        var first = near.Snapshot().Terraforming!.Installations[1]; var second = far.Snapshot().Terraforming!.Installations[1];
        Assert.AreEqual(first.ProcessedKilograms, second.ProcessedKilograms);
        Assert.IsTrue(second.EnergyUsedJoules > first.EnergyUsedJoules);
    }

    [TestMethod]
    public void NoFeedstockAndZeroPower_DoNotCreateOutput()
    {
        var settings = new TerraformingParameters { Installations = [Plant(TerraformInstallation.Transport), Plant() with { PowerWattsPerSquareMeter = 0 }] };
        var model = Model(settings); Run(model, 120); var result = model.Snapshot().Terraforming!;
        Assert.AreEqual(0d, result.TotalUsedEnergyJoules);
        Assert.AreEqual(0d, result.CumulativeDeliveredKilograms);
        Assert.AreEqual("Geen aanvoer", result.Installations[0].Status);
        Assert.AreEqual("Uitgeschakeld", result.Installations[1].Status);
        var vacuum = new RegionalClimateModel(surface: new SurfaceParameters(), atmosphere: new AtmosphereParameters(surfacePressurePascals: 0)
            { Terraforming = settings with { Installations = [Plant(TerraformInstallation.Capture)] } });
        Run(vacuum, 120);
        Assert.AreEqual(0d, vacuum.Snapshot().Terraforming!.CumulativeCapturedKilograms);
    }

    [TestMethod]
    public void CoupledHydrologyBiologyAndEngineering_CloseAllBudgets()
    {
        var model = Model(new TerraformingParameters(), true); Run(model, 3 * 86400);
        var snapshot = model.Snapshot(); var result = snapshot.Terraforming!; var gas = snapshot.Atmosphere!;
        Assert.IsTrue(result.CumulativeCapturedKilograms > 0 && result.CumulativeDeliveredKilograms > 0);
        Assert.AreEqual(0, snapshot.BudgetErrorJoulesPerSquareMeter, 0.02);
        Assert.AreEqual(0, result.EnergyBudgetErrorJoulesPerSquareMeter, 0.02);
        Assert.AreEqual(0, result.MaterialBudgetErrorKilograms / result.TotalRemainingMaterialKilograms, 1e-12);
        Assert.AreEqual(0, result.LogisticsBudgetErrorKilograms / result.CumulativeCapturedKilograms, 1e-12);
        Assert.AreEqual(0, gas.CarbonMassErrorKilograms / gas.TotalAtmosphericMassKilograms, 1e-12);
        Assert.AreEqual(0, gas.OxygenMassErrorKilograms / gas.TotalAtmosphericMassKilograms, 1e-12);
        Assert.AreEqual(0, snapshot.Surface!.WaterMassErrorKilograms / snapshot.Surface.TotalWaterMassKilograms, 1e-12);
        Assert.IsTrue(result.RemainingEnergyJoulesPerSquareMeter.All(v => v >= 0));
        Assert.IsTrue(result.RemainingMaterialKilogramsPerSquareMeter.All(v => v >= 0));
    }

    [TestMethod]
    public void ScenarioReplayAndReset_ReproduceAllEngineeringInventories()
    {
        var gas = new AtmosphereParameters { Terraforming = new TerraformingParameters(), Biology = new BiologyParameters() };
        var scenario = ExperimentScenario.CreateAtmosphereWithSurface("Terraforming", 2, new ClimateParameters(initialTemperatureKelvin: 285),
            new RegionalParameters(), new SurfaceParameters(), gas);
        var first = new ExperimentRunner(scenario).Finish(TestContext.CancellationToken);
        var second = new ExperimentRunner(ScenarioJson.Deserialize(ScenarioJson.Serialize(scenario))).Finish(TestContext.CancellationToken);
        Assert.AreEqual(ExperimentScenario.TerraformingModelVersion, scenario.ModelVersion);
        Assert.AreEqual(first.ToCsv(), second.ToCsv()); Assert.AreEqual(first.ToRegionalCsv(), second.ToRegionalCsv());
        var session = new SimulationSession(scenario.Climate, scenario.Regional, scenario.Surface, atmosphere: gas);
        var initial = session.Snapshot().Regional!.Terraforming!;
        session.Clock.Speed = 86400; session.Advance(1, TestContext.CancellationToken); session.Reset(TestContext.CancellationToken);
        Assert.AreEqual(initial.TotalRemainingEnergyJoules, session.Snapshot().Regional!.Terraforming!.TotalRemainingEnergyJoules);
        Assert.AreEqual(0d, session.Snapshot().Regional!.Terraforming!.TotalUsedEnergyJoules);
    }

    [TestMethod]
    public void ValidationAndCancellation_PreventPartialInstallation()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Model(new TerraformingParameters { Installations = [Plant("unknown")] }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Model(new TerraformingParameters { InitialEnergyJoulesPerSquareMeter = double.NaN }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Model(new TerraformingParameters { Installations = [Plant() with { CellIndex = 288 }] }));
        Assert.ThrowsExactly<ArgumentException>(() => Model(new TerraformingParameters { Installations = [Plant() with { StartDay = 2, EndDay = 1 }] }));
        Assert.ThrowsExactly<ArgumentException>(() => new RegionalClimateModel(atmosphere: new AtmosphereParameters { Terraforming = new TerraformingParameters() }));
        var model = Model(new TerraformingParameters()); var initial = model.Snapshot().Terraforming!;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken); cancellation.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => model.Advance(60, cancellation.Token));
        Assert.AreEqual(initial.TotalRemainingEnergyJoules, model.Snapshot().Terraforming!.TotalRemainingEnergyJoules);
        Assert.AreEqual(0d, model.ElapsedSeconds);
    }

    [TestMethod]
    public void HeaterTimeSteps_ConvergeWithoutChangingTotalCosts()
    {
        var parameters = new TerraformingParameters { Installations = [Plant()] };
        var coarse = Model(parameters); var medium = Model(parameters); var fine = Model(parameters);
        Run(coarse, 86400, 60); Run(medium, 86400, 30); Run(fine, 86400, 15);
        var a = coarse.TemperaturesKelvin[144]; var b = medium.TemperaturesKelvin[144]; var c = fine.TemperaturesKelvin[144];
        Assert.IsTrue(Math.Abs(b - c) < Math.Abs(a - c));
        Assert.AreEqual(c, a, 0.01);
        Assert.AreEqual(coarse.Snapshot().Terraforming!.TotalUsedEnergyJoules, fine.Snapshot().Terraforming!.TotalUsedEnergyJoules);
    }
}
