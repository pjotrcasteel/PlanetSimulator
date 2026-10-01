using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Tests;

[TestClass]
public sealed class SimulationTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void Clock_IsIndependentOfFramePartition()
    {
        var first = new SimulationClock();
        var second = new SimulationClock();
        first.Advance(1, TestContext.CancellationToken);
        for (var frame = 0; frame < 60; frame++) second.Advance(1d / 60, TestContext.CancellationToken);
        Assert.AreEqual(first.TickCount, second.TickCount);
        Assert.AreEqual(3600d, second.ElapsedSeconds);
    }

    [TestMethod]
    public void Clock_PauseDoesNotAccumulateTime()
    {
        var clock = new SimulationClock { IsPaused = true };
        clock.Advance(10, TestContext.CancellationToken);
        clock.IsPaused = false;
        clock.Advance(1, TestContext.CancellationToken);
        Assert.AreEqual(3600d, clock.ElapsedSeconds);
    }

    [TestMethod]
    public void Clock_PreservesFractionalStepsAndResetClearsThem()
    {
        var clock = new SimulationClock { Speed = 1 };
        clock.Advance(30, TestContext.CancellationToken);
        Assert.AreEqual(0L, clock.TickCount);
        clock.Advance(30, TestContext.CancellationToken);
        Assert.AreEqual(1L, clock.TickCount);
        clock.Advance(30, TestContext.CancellationToken);
        clock.Reset();
        clock.Advance(30, TestContext.CancellationToken);
        Assert.AreEqual(0L, clock.TickCount);
    }

    [TestMethod]
    public void Clock_CancellationDoesNotAdvanceTime()
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        source.Cancel();
        var clock = new SimulationClock();
        Assert.ThrowsExactly<OperationCanceledException>(() => clock.Advance(1, source.Token));
        Assert.AreEqual(0L, clock.TickCount);
    }

    [TestMethod]
    public void Clock_RejectsInvalidValues()
    {
        var clock = new SimulationClock();
        foreach (var invalid in new[] { -1d, double.NaN, double.PositiveInfinity })
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => clock.Speed = invalid);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => clock.Advance(invalid, TestContext.CancellationToken));
        }
    }

    [TestMethod]
    public void Planet_RotationRepeatsAndGravityIsEarthLike()
    {
        var planet = new Planet();
        Assert.AreEqual(0d, planet.GetRotationRadians(planet.RotationPeriodSeconds), 1e-12);
        Assert.AreEqual(Math.PI, planet.GetRotationRadians(planet.RotationPeriodSeconds / 2), 1e-12);
        Assert.AreEqual(9.82, planet.SurfaceGravity, 0.02);
    }
    [TestMethod]
    public void Session_SnapshotMatchesDesktopClockAndRotation()
    {
        var session = new SimulationSession();
        var desktopClock = new SimulationClock();
        var desktopPlanet = new Planet();
        foreach (var elapsed in new[] { 0.02, 0.7, 3.0, 0.01 })
        {
            var snapshot = session.Advance(elapsed, TestContext.CancellationToken);
            desktopClock.Advance(elapsed, TestContext.CancellationToken);
            Assert.AreEqual(desktopClock.ElapsedSeconds, snapshot.ElapsedSeconds);
            Assert.AreEqual(desktopPlanet.GetRotationRadians(desktopClock.ElapsedSeconds), snapshot.RotationRadians);
        }

        session.Clock.IsPaused = true;
        var before = session.Advance(0, TestContext.CancellationToken);
        Assert.AreEqual(before, session.Advance(10, TestContext.CancellationToken));
        session.Clock.Reset();
        Assert.AreEqual(new SimulationSnapshot(0, 0), session.Advance(0, TestContext.CancellationToken));
    }
}
