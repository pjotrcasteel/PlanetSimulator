using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Tests;

[TestClass]
public sealed class EnergyBalanceTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void ReferenceCase_HasExpectedAbsorptionAndBlackbodyEquilibrium()
    {
        var model = new EnergyBalanceModel();
        Assert.AreEqual(238.175, model.AbsorbedWattsPerSquareMeter, 1e-9);
        Assert.AreEqual(254.578, model.EquilibriumTemperatureKelvin, 0.001);
        Assert.IsTrue(model.NetWattsPerSquareMeter > 0);
    }

    [TestMethod]
    public void DistanceAndAlbedo_ObeyInverseSquareAndReflectionLimits()
    {
        var model = new EnergyBalanceModel();
        var initial = model.AbsorbedWattsPerSquareMeter;
        model.SetForcing(2, 0.3, TestContext.CancellationToken);
        Assert.AreEqual(initial / 4, model.AbsorbedWattsPerSquareMeter, 1e-10);
        Assert.AreEqual(230d, model.TemperatureKelvin);
        model.SetForcing(1, 1, TestContext.CancellationToken);
        Assert.AreEqual(0d, model.AbsorbedWattsPerSquareMeter);
        Assert.AreEqual(0d, model.EquilibriumTemperatureKelvin);
        model.Advance(60, TestContext.CancellationToken);
        Assert.IsTrue(model.TemperatureKelvin is > 0 and < 230);
    }

    [TestMethod]
    public void InitiallyHotAndColdPlanets_ApproachTheSameEquilibrium()
    {
        foreach (var initial in new[] { 230d, 400d })
        {
            var model = new EnergyBalanceModel(new ClimateParameters(initialTemperatureKelvin: initial));
            Integrate(model, 365 * 86400, 60);
            Assert.AreEqual(model.EquilibriumTemperatureKelvin, model.TemperatureKelvin, 0.003);
            Assert.AreEqual(0d, model.EnergyBalanceErrorJoulesPerSquareMeter, 0.02);
        }
    }

    [TestMethod]
    public void EquilibriumInitialState_RemainsConstant()
    {
        var equilibrium = new EnergyBalanceModel().EquilibriumTemperatureKelvin;
        var model = new EnergyBalanceModel(new ClimateParameters(initialTemperatureKelvin: equilibrium));
        Integrate(model, 86400, 60);
        Assert.AreEqual(equilibrium, model.TemperatureKelvin, 1e-10);
    }

    [TestMethod]
    public void HalvingTheIntegrationStep_DoesNotChangeTheResolvedTemperature()
    {
        var parameters = new ClimateParameters(arealHeatCapacity: 1e5, initialTemperatureKelvin: 1000);
        var coarse = new EnergyBalanceModel(parameters);
        var fine = new EnergyBalanceModel(parameters);
        Integrate(coarse, 3600, 60);
        Integrate(fine, 3600, 30);
        Assert.AreEqual(fine.TemperatureKelvin, coarse.TemperatureKelvin, 0.001);
        Assert.IsTrue(coarse.TemperatureKelvin > 0);
    }

    [TestMethod]
    public void EnergyBudget_RemainsClosedAfterChangingForcing()
    {
        var model = new EnergyBalanceModel();
        Integrate(model, 10 * 86400, 60);
        var temperature = model.TemperatureKelvin;
        model.SetForcing(1.5, 0.6, TestContext.CancellationToken);
        Assert.AreEqual(temperature, model.TemperatureKelvin);
        Assert.IsTrue(model.NetWattsPerSquareMeter < 0);
        Integrate(model, 10 * 86400, 60);
        Assert.AreEqual(0d, model.EnergyBalanceErrorJoulesPerSquareMeter, 0.002);
    }

    [TestMethod]
    public void Session_IsIndependentOfFramePartitionAndResetKeepsForcing()
    {
        var first = new SimulationSession();
        var second = new SimulationSession();
        first.Clock.Speed = second.Clock.Speed = 604800;
        var expected = first.Advance(1, TestContext.CancellationToken);
        SimulationSnapshot? actual = null;
        for (var frame = 0; frame < 60; frame++) actual = second.Advance(1d / 60, TestContext.CancellationToken);
        Assert.AreEqual(expected, actual);
        second.Climate.SetForcing(1.2, 0.4, TestContext.CancellationToken);
        second.Reset(TestContext.CancellationToken);
        Assert.AreEqual(230d, second.Climate.TemperatureKelvin);
        Assert.AreEqual(1.2, second.Climate.Parameters.DistanceAstronomicalUnits);
        Assert.AreEqual(0d, second.Clock.ElapsedSeconds);
        Assert.AreEqual(0d, second.Climate.CumulativeAbsorbedJoulesPerSquareMeter);
    }

    [TestMethod]
    public void CancelledOrInvalidOperations_DoNotChangeClimate()
    {
        var model = new EnergyBalanceModel();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        source.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => model.Advance(60, source.Token));
        Assert.ThrowsExactly<OperationCanceledException>(() => model.SetForcing(2, 0.5, source.Token));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => model.SetForcing(double.NaN, 0.5, TestContext.CancellationToken));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => model.Advance(61, TestContext.CancellationToken));
        Assert.AreEqual(230d, model.TemperatureKelvin);
        Assert.AreEqual(1d, model.Parameters.DistanceAstronomicalUnits);
        Assert.AreEqual(0d, model.CumulativeAbsorbedJoulesPerSquareMeter);
    }

    [TestMethod]
    public void ParameterRanges_RejectNonFiniteAndPhysicallyInvalidValues()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ClimateParameters(distanceAstronomicalUnits: 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ClimateParameters(bondAlbedo: 1.01));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ClimateParameters(arealHeatCapacity: 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ClimateParameters(initialTemperatureKelvin: -1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ClimateParameters(stellarLuminositySolarUnits: double.PositiveInfinity));
    }

    [TestMethod]
    public void Clock_CancellationBetweenStepsKeepsTheClockAlignedWithCompletedSteps()
    {
        var clock = new SimulationClock { Speed = 120 };
        var completed = 0;
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        Assert.ThrowsExactly<OperationCanceledException>(() => clock.Advance(1, _ =>
        {
            completed++;
            source.Cancel();
        }, source.Token));
        Assert.AreEqual(1, completed);
        Assert.AreEqual(1L, clock.TickCount);
        clock.Advance(0, TestContext.CancellationToken);
        Assert.AreEqual(2L, clock.TickCount);
    }

    private void Integrate(EnergyBalanceModel model, int seconds, int step)
    {
        for (var elapsed = 0; elapsed < seconds; elapsed += step) model.Advance(step, TestContext.CancellationToken);
    }
}
