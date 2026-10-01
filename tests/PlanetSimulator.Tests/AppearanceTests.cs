using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanetSimulator.Rendering;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Tests;

/// <summary>
/// Verifies topology, outward normals, model-derived coastlines and presentation isolation.
/// </summary>
[TestClass]
public sealed class AppearanceTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void Geometry_IsClosedFiniteAndNormalsPointOutward()
    {
        var model = new RegionalClimateModel(surface: new SurfaceParameters());
        var mesh = new SurfaceAppearance(model.Surface!, TestContext.CancellationToken);
        Assert.AreEqual((SurfaceAppearance.Segments + 1) * (SurfaceAppearance.Rings + 1), mesh.Vertices.Count);
        Assert.IsTrue(mesh.Vertices.Count < ushort.MaxValue);
        Assert.IsTrue(mesh.Indices.All(i => i >= 0 && i < mesh.Vertices.Count));
        foreach (var vertex in mesh.Vertices)
        {
            Assert.IsTrue(float.IsFinite(vertex.Normal.Length()));
            Assert.AreEqual(1f, vertex.Normal.Length(), 1e-5);
            Assert.IsTrue(Vector3.Dot(vertex.Position, vertex.Normal) > 0);
            Assert.IsTrue(vertex.Position.Length() is >= 0.99999f and <= 1.04001f);
            if (vertex.Water == 1) Assert.AreEqual(1f, vertex.Position.Length(), 1e-6);
        }
        for (var ring = 0; ring <= SurfaceAppearance.Rings; ring++)
        {
            var first = mesh.Vertices[ring * (SurfaceAppearance.Segments + 1)];
            var last = mesh.Vertices[ring * (SurfaceAppearance.Segments + 1) + SurfaceAppearance.Segments];
            Assert.AreEqual(first.Position, last.Position);
            Assert.AreEqual(first.Normal, last.Normal);
        }
    }

    [TestMethod]
    public void ContinuousHeight_MatchesAllPhysicalCellSamples()
    {
        var grid = new SphericalGrid();
        var surface = new SurfaceReservoirs(grid, new SurfaceParameters());
        foreach (var cell in grid.Cells)
            Assert.AreEqual(surface.ElevationsMeters[cell.Index], SurfaceReservoirs.Elevation(cell.LatitudeRadians, cell.LongitudeRadians, 10));
        var mesh = new SurfaceAppearance(surface, TestContext.CancellationToken);
        foreach (var vertex in mesh.Vertices.Where(v => v.Water > 0))
        {
            var latitude = Math.Asin(Math.Clamp(Vector3.Normalize(vertex.Position).Y, -1, 1));
            var longitude = Math.Atan2(vertex.Position.Z, vertex.Position.X);
            Assert.IsTrue(SurfaceReservoirs.Elevation(latitude, longitude, 10) < surface.InitialWaterLevelMeters + 1e-5);
        }
        var dry = new SurfaceAppearance(new SurfaceReservoirs(grid, new SurfaceParameters(waterEquivalentDepthMeters: 0)), TestContext.CancellationToken);
        Assert.IsTrue(dry.Vertices.All(v => v.Water == 0));
    }

    [TestMethod]
    public void IceReconstruction_IsBoundedAndPeriodicAcrossLongitude()
    {
        var model = new RegionalClimateModel(surface: new SurfaceParameters());
        var surface = model.Snapshot().Surface!;
        for (var row = 0; row <= 12; row++)
        {
            var left = new ReservoirInterpolation(new Vector2(0, row / 12f)).IceFraction(surface);
            var right = new ReservoirInterpolation(new Vector2(1, row / 12f)).IceFraction(surface);
            Assert.AreEqual(left, right, 1e-6);
            Assert.IsTrue(left is >= 0 and <= 1);
        }
        var warm = new RegionalClimateModel(new ClimateParameters(initialTemperatureKelvin: 285), surface: new SurfaceParameters());
        Assert.AreEqual(0f, new ReservoirInterpolation(new Vector2(0.4f, 0.6f)).IceFraction(warm.Snapshot().Surface!));
    }

    [TestMethod]
    public void OpticalControls_DoNotModifySimulationOrInventSurfacePhases()
    {
        var model = new RegionalClimateModel(surface: new SurfaceParameters());
        var before = model.Snapshot();
        var appearance = new SurfaceAppearance(model.Surface!, TestContext.CancellationToken);
        var vertex = appearance.Vertices.First(v => v.Water == 1);
        var camera = new Vector3(2, 0, 2);
        foreach (var relief in new[] { 0f, 1f })
        {
            var ice = SurfaceLighting.Shade(vertex, 1, camera, Vector3.UnitX, relief);
            var liquid = SurfaceLighting.Shade(vertex, 0, camera, Vector3.UnitX, relief);
            Assert.IsTrue(float.IsFinite(ice.Length()) && float.IsFinite(liquid.Length()));
            Assert.AreNotEqual(ice, liquid);
        }
        CollectionAssert.AreEqual(before.TemperaturesKelvin.ToArray(), model.Snapshot().TemperaturesKelvin.ToArray());
        Assert.AreEqual(before.Surface!.TotalWaterMassKilograms, model.Snapshot().Surface!.TotalWaterMassKilograms);
        Assert.AreEqual(0d, model.ElapsedSeconds);
    }

    [TestMethod]
    public void Atmosphere_IsFiniteAndObeysThePlanetaryShadow()
    {
        var camera = new Vector3(0, 0, 3);
        var day = AtmosphereOptics.Scatter(camera, -Vector3.UnitZ, Vector3.UnitZ);
        var night = AtmosphereOptics.Scatter(camera, -Vector3.UnitZ, -Vector3.UnitZ);
        Assert.IsTrue(day.Z > day.X && day.X > 0);
        Assert.AreEqual(Vector3.Zero, night);
        Assert.AreEqual(Vector3.Zero, AtmosphereOptics.Scatter(camera, Vector3.UnitX, Vector3.UnitZ));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var surface = new SurfaceReservoirs(new SphericalGrid(), new SurfaceParameters());
        Assert.ThrowsExactly<OperationCanceledException>(() => new SurfaceAppearance(surface, cancelled.Token));
    }
}
