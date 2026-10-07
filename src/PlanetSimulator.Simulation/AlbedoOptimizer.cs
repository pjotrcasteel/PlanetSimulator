using System.Globalization;
using System.Text;

namespace PlanetSimulator.Simulation;

/// <summary>
/// Deterministic one-dimensional parameter search. It does not imply that changing Bond albedo is technically achievable.
/// </summary>
public static class AlbedoOptimizer
{
    public static AlbedoOptimizationResult Optimize(ExperimentScenario scenario, double targetTemperatureKelvin, double minimumAlbedo,
        double maximumAlbedo, int steps, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        scenario.Validate();
        if (!double.IsFinite(targetTemperatureKelvin) || targetTemperatureKelvin is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(targetTemperatureKelvin));
        if (!double.IsFinite(minimumAlbedo) || !double.IsFinite(maximumAlbedo) || minimumAlbedo < 0 || maximumAlbedo > 1 || minimumAlbedo >= maximumAlbedo)
            throw new ArgumentOutOfRangeException(nameof(minimumAlbedo));
        if (steps is < 2 or > 101) throw new ArgumentOutOfRangeException(nameof(steps));
        if (scenario.Changes.Length != 0) throw new ArgumentException("Albedo-optimalisatie vereist een scenario zonder geplande forcingwijzigingen.", nameof(scenario));

        var candidates = new List<AlbedoOptimizationCandidate>(steps);
        ExperimentResult? bestResult = null;
        AlbedoOptimizationCandidate? best = null;
        for (var index = 0; index < steps; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var albedo = minimumAlbedo + (maximumAlbedo - minimumAlbedo) * index / (steps - 1d);
            var climate = scenario.Climate;
            var candidateScenario = scenario with
            {
                Name = $"{scenario.Name} · albedo {albedo.ToString("0.###", CultureInfo.InvariantCulture)}",
                Climate = new ClimateParameters(climate.DistanceAstronomicalUnits, albedo, climate.ArealHeatCapacity,
                    climate.InitialTemperatureKelvin, climate.StellarLuminositySolarUnits),
            };
            candidateScenario.Validate();
            var result = new ExperimentRunner(candidateScenario).Finish(cancellationToken);
            var finalTemperature = result.Samples[^1].TemperatureKelvin;
            var candidate = new AlbedoOptimizationCandidate
            {
                BondAlbedo = albedo,
                FinalTemperatureKelvin = finalTemperature,
                AbsoluteTemperatureErrorKelvin = Math.Abs(finalTemperature - targetTemperatureKelvin),
            };
            candidates.Add(candidate);
            if (best is null || candidate.AbsoluteTemperatureErrorKelvin < best.AbsoluteTemperatureErrorKelvin
                || candidate.AbsoluteTemperatureErrorKelvin == best.AbsoluteTemperatureErrorKelvin && candidate.BondAlbedo < best.BondAlbedo)
            {
                best = candidate;
                bestResult = result;
            }
        }

        return new AlbedoOptimizationResult
        {
            TargetTemperatureKelvin = targetTemperatureKelvin,
            MinimumAlbedo = minimumAlbedo,
            MaximumAlbedo = maximumAlbedo,
            Candidates = candidates.AsReadOnly(),
            BestCandidate = best!,
            BestResult = bestResult!,
        };
    }
}

public sealed record AlbedoOptimizationCandidate
{
    public required double BondAlbedo { get; init; }
    public required double FinalTemperatureKelvin { get; init; }
    public required double AbsoluteTemperatureErrorKelvin { get; init; }
}

public sealed record AlbedoOptimizationResult
{
    public required double TargetTemperatureKelvin { get; init; }
    public required double MinimumAlbedo { get; init; }
    public required double MaximumAlbedo { get; init; }
    public required IReadOnlyList<AlbedoOptimizationCandidate> Candidates { get; init; }
    public required AlbedoOptimizationCandidate BestCandidate { get; init; }
    public required ExperimentResult BestResult { get; init; }

    public string ToCsv()
    {
        var csv = new StringBuilder("bond_albedo,final_temperature_K,absolute_temperature_error_K,is_best\n");
        foreach (var candidate in Candidates)
        {
            csv.Append(candidate.BondAlbedo.ToString("R", CultureInfo.InvariantCulture)).Append(',');
            csv.Append(candidate.FinalTemperatureKelvin.ToString("R", CultureInfo.InvariantCulture)).Append(',');
            csv.Append(candidate.AbsoluteTemperatureErrorKelvin.ToString("R", CultureInfo.InvariantCulture)).Append(',');
            csv.Append(candidate == BestCandidate ? "true" : "false").Append('\n');
        }

        return csv.ToString();
    }
}
