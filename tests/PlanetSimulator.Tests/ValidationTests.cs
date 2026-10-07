using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Tests;

[TestClass]
public sealed class ValidationTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void ReferenceBenchmarks_ExposeAgreementAndKnownGreenhouseGap()
    {
        var report = ValidationSuite.Run(TestContext.CancellationToken);
        Assert.IsTrue(report.IsSuccessful);
        Assert.AreEqual(4, report.PassedCases);
        Assert.AreEqual(1, report.ExpectedGapCases);
        Assert.AreEqual(0, report.FailedCases);
        Assert.AreEqual(ValidationSuite.PassStatus, report.Cases.Single(item => item.Id == "earth-effective-temperature").Status);
        var surface = report.Cases.Single(item => item.Id == "earth-mean-surface-temperature");
        Assert.AreEqual(ValidationSuite.ExpectedGapStatus, surface.Status);
        Assert.IsTrue(surface.AbsoluteError > 30);
    }

    [TestMethod]
    public void SensitivitySweep_HasExpectedPhysicalDirections()
    {
        var report = ValidationSuite.Run(TestContext.CancellationToken);
        Assert.IsTrue(report.Sensitivities.Single(item => item.Parameter == "distance").NormalizedSensitivity < 0);
        Assert.IsTrue(report.Sensitivities.Single(item => item.Parameter == "bond-albedo").NormalizedSensitivity < 0);
        Assert.IsTrue(report.Sensitivities.Single(item => item.Parameter == "stellar-luminosity").NormalizedSensitivity > 0);
        Assert.IsTrue(report.Sensitivities.Single(item => item.Parameter == "areal-heat-capacity").NormalizedSensitivity < 0);
        Assert.IsTrue(report.Sensitivities.All(item => double.IsFinite(item.NormalizedSensitivity)));
    }

    [TestMethod]
    public void ValidationArtifacts_AreDeterministicAndCultureInvariant()
    {
        var first = ValidationSuite.Run(TestContext.CancellationToken);
        var second = ValidationSuite.Run(TestContext.CancellationToken);
        Assert.AreEqual(ValidationJson.Serialize(first), ValidationJson.Serialize(second));

        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
            Assert.AreEqual(first.ToCasesCsv(), second.ToCasesCsv());
            Assert.IsTrue(first.ToCasesCsv().Contains("254.", StringComparison.Ordinal));
            Assert.IsTrue(first.ToSensitivityCsv().Contains("normalized_sensitivity", StringComparison.Ordinal));
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
    }

    [TestMethod]
    public void CancelledValidation_StopsBeforeProducingAReport()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        cancellation.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => ValidationSuite.Run(cancellation.Token));
    }
}
