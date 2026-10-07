namespace PlanetSimulator.Simulation;

/// <summary>
/// Compact, non-normative diagnostics for comparing completed experiments.
/// It intentionally reports state rather than claiming habitability.
/// </summary>
public sealed record ExperimentInspection
{
    public required double FinalTemperatureKelvin { get; init; }
    public required double TemperatureChangeKelvin { get; init; }
    public required double MinimumTemperatureKelvin { get; init; }
    public required double MaximumTemperatureKelvin { get; init; }
    public double? LiquidWaterMassFraction { get; init; }
    public double? MeanSurfacePressurePascals { get; init; }
    public double? CarbonDioxidePartsPerMillion { get; init; }
    public double? TotalBiomassKilograms { get; init; }
    public double? EngineeringEnergyUsedJoules { get; init; }
}

public static class ExperimentInspector
{
    public static ExperimentInspection Inspect(ExperimentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Samples.Count == 0) throw new ArgumentException("Experiment bevat geen meetpunten.", nameof(result));

        var first = result.Samples[0];
        var final = result.Samples[^1];
        var regional = final.Regional;
        var atmosphere = regional?.Atmosphere;
        return new ExperimentInspection
        {
            FinalTemperatureKelvin = final.TemperatureKelvin,
            TemperatureChangeKelvin = final.TemperatureKelvin - first.TemperatureKelvin,
            MinimumTemperatureKelvin = regional?.MinimumTemperatureKelvin ?? final.TemperatureKelvin,
            MaximumTemperatureKelvin = regional?.MaximumTemperatureKelvin ?? final.TemperatureKelvin,
            LiquidWaterMassFraction = regional?.Surface?.LiquidMassFraction,
            MeanSurfacePressurePascals = atmosphere?.MeanSurfacePressurePascals,
            CarbonDioxidePartsPerMillion = atmosphere is null ? null : atmosphere.MeanCarbonDioxideMoleFraction * 1e6,
            TotalBiomassKilograms = regional?.Biology?.TotalBiomassKilograms,
            EngineeringEnergyUsedJoules = regional?.Terraforming?.TotalUsedEnergyJoules,
        };
    }
}
