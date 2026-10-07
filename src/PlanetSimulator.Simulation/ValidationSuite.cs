using System.Globalization;
using System.Text;

namespace PlanetSimulator.Simulation;

/// <summary>
/// Runs a small, reproducible scientific benchmark set and deterministic one-at-a-time sensitivity sweeps.
/// Reference tolerances describe these benchmark comparisons, not general model accuracy.
/// </summary>
public static class ValidationSuite
{
    public const int SensitivityHorizonDays = 30;
    public const string PassStatus = "pass";
    public const string ExpectedGapStatus = "expected-gap";
    public const string FailStatus = "fail";

    public static ValidationReport Run(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var earth = new EnergyBalanceModel();
        var surface = new EnergyBalanceModel(new ClimateParameters(initialTemperatureKelvin: 288));

        var cases = new[]
        {
            Compare(
                "solar-irradiance-1au",
                "Solar irradiance at the mean Sun-Earth distance",
                EnergyBalanceModel.SolarIrradianceAtOneAu,
                1361,
                "W/m2",
                1,
                "https://earth.gsfc.nasa.gov/climate/projects/solar-irradiance/science",
                "Reference constant used by the climate core."),
            Compare(
                "earth-absorbed-solar-flux",
                "Global annual-mean absorbed solar flux for an Earth-like albedo",
                earth.AbsorbedWattsPerSquareMeter,
                240,
                "W/m2",
                3,
                "https://science.nasa.gov/earth/earth-observatory/climate-and-earths-energy-budget/",
                "NASA reports about 240 W/m2; the model uses Bond albedo 0.30 and 1361 W/m2."),
            Compare(
                "earth-effective-temperature",
                "Earth-like blackbody effective temperature",
                earth.EquilibriumTemperatureKelvin,
                255,
                "K",
                1,
                "https://sunclimate.gsfc.nasa.gov/science",
                "This is an effective radiating temperature, not a surface-temperature prediction."),
            Compare(
                "earth-mean-surface-temperature",
                "Earth mean surface temperature without greenhouse physics",
                earth.EquilibriumTemperatureKelvin,
                288,
                "K",
                5,
                "https://sunclimate.gsfc.nasa.gov/science",
                "The missing roughly 33 K is an expected gap until longwave atmospheric greenhouse physics is implemented.",
                knownModelLimitation: true),
            Compare(
                "earth-surface-emission-288k",
                "Blackbody thermal emission at an Earth-like mean surface temperature",
                surface.EmittedWattsPerSquareMeter,
                390,
                "W/m2",
                2,
                "https://www.giss.nasa.gov/pubs/abs/la00500y.html",
                "Checks the Stefan-Boltzmann implementation at 288 K.")
        };

        cancellationToken.ThrowIfCancellationRequested();
        var sensitivities = new[]
        {
            Sweep("distance", "AU", 0.95, 1.0, 1.05, value => new ClimateParameters(distanceAstronomicalUnits: value), cancellationToken),
            Sweep("bond-albedo", "fraction", 0.25, 0.30, 0.35, value => new ClimateParameters(bondAlbedo: value), cancellationToken),
            Sweep("stellar-luminosity", "solar", 0.95, 1.0, 1.05, value => new ClimateParameters(stellarLuminositySolarUnits: value), cancellationToken),
            Sweep("areal-heat-capacity", "J/m2/K", 5e6, 1e7, 1.5e7, value => new ClimateParameters(arealHeatCapacity: value), cancellationToken)
        };

        return new ValidationReport
        {
            SchemaVersion = ValidationReport.CurrentSchemaVersion,
            ModelVersion = ExperimentScenario.CurrentModelVersion,
            Cases = cases,
            Sensitivities = sensitivities,
        };
    }

    private static ValidationCaseResult Compare(string id, string description, double modelValue, double referenceValue, string unit,
        double allowedAbsoluteError, string source, string notes, bool knownModelLimitation = false)
    {
        var absoluteError = Math.Abs(modelValue - referenceValue);
        var status = absoluteError <= allowedAbsoluteError ? PassStatus : knownModelLimitation ? ExpectedGapStatus : FailStatus;
        return new ValidationCaseResult
        {
            Id = id,
            Description = description,
            ModelValue = modelValue,
            ReferenceValue = referenceValue,
            Unit = unit,
            AllowedAbsoluteError = allowedAbsoluteError,
            AbsoluteError = absoluteError,
            RelativeErrorPercent = referenceValue == 0 ? 0 : absoluteError / Math.Abs(referenceValue) * 100,
            KnownModelLimitation = knownModelLimitation,
            Status = status,
            Source = source,
            Notes = notes,
        };
    }

    private static SensitivityResult Sweep(string parameter, string inputUnit, double low, double baseline, double high,
        Func<double, ClimateParameters> createParameters, CancellationToken cancellationToken)
    {
        var lowTemperature = TemperatureAfterDays(createParameters(low), SensitivityHorizonDays, cancellationToken);
        var baselineTemperature = TemperatureAfterDays(createParameters(baseline), SensitivityHorizonDays, cancellationToken);
        var highTemperature = TemperatureAfterDays(createParameters(high), SensitivityHorizonDays, cancellationToken);
        var normalizedInputSpan = (high - low) / baseline;
        var normalizedOutputSpan = (highTemperature - lowTemperature) / baselineTemperature;

        return new SensitivityResult
        {
            Parameter = parameter,
            InputUnit = inputUnit,
            HorizonDays = SensitivityHorizonDays,
            LowValue = low,
            BaselineValue = baseline,
            HighValue = high,
            LowTemperatureKelvin = lowTemperature,
            BaselineTemperatureKelvin = baselineTemperature,
            HighTemperatureKelvin = highTemperature,
            NormalizedSensitivity = normalizedOutputSpan / normalizedInputSpan,
        };
    }

    private static double TemperatureAfterDays(ClimateParameters parameters, int days, CancellationToken cancellationToken)
    {
        var model = new EnergyBalanceModel(parameters);
        var steps = checked(days * 24 * 60);
        for (var step = 0; step < steps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            model.Advance(SimulationClock.StepSeconds, cancellationToken);
        }

        return model.TemperatureKelvin;
    }
}

public sealed record ValidationCaseResult
{
    public required string Id { get; init; }
    public required string Description { get; init; }
    public required double ModelValue { get; init; }
    public required double ReferenceValue { get; init; }
    public required string Unit { get; init; }
    public required double AllowedAbsoluteError { get; init; }
    public required double AbsoluteError { get; init; }
    public required double RelativeErrorPercent { get; init; }
    public required bool KnownModelLimitation { get; init; }
    public required string Status { get; init; }
    public required string Source { get; init; }
    public required string Notes { get; init; }
}

public sealed record SensitivityResult
{
    public required string Parameter { get; init; }
    public required string InputUnit { get; init; }
    public required int HorizonDays { get; init; }
    public required double LowValue { get; init; }
    public required double BaselineValue { get; init; }
    public required double HighValue { get; init; }
    public required double LowTemperatureKelvin { get; init; }
    public required double BaselineTemperatureKelvin { get; init; }
    public required double HighTemperatureKelvin { get; init; }
    public required double NormalizedSensitivity { get; init; }
}

public sealed record ValidationReport
{
    public const int CurrentSchemaVersion = 1;
    public required int SchemaVersion { get; init; }
    public required string ModelVersion { get; init; }
    public required ValidationCaseResult[] Cases { get; init; }
    public required SensitivityResult[] Sensitivities { get; init; }
    public int PassedCases => Cases.Count(item => item.Status == ValidationSuite.PassStatus);
    public int ExpectedGapCases => Cases.Count(item => item.Status == ValidationSuite.ExpectedGapStatus);
    public int FailedCases => Cases.Count(item => item.Status == ValidationSuite.FailStatus);
    public bool IsSuccessful => FailedCases == 0;

    public string ToCasesCsv()
    {
        var csv = new StringBuilder("id,status,model_value,reference_value,unit,allowed_absolute_error,absolute_error,relative_error_percent,source,notes\n");
        foreach (var item in Cases)
        {
            csv.Append(Csv(item.Id)).Append(',').Append(Csv(item.Status)).Append(',');
            csv.Append(Number(item.ModelValue)).Append(',').Append(Number(item.ReferenceValue)).Append(',').Append(Csv(item.Unit)).Append(',');
            csv.Append(Number(item.AllowedAbsoluteError)).Append(',').Append(Number(item.AbsoluteError)).Append(',');
            csv.Append(Number(item.RelativeErrorPercent)).Append(',').Append(Csv(item.Source)).Append(',').Append(Csv(item.Notes)).Append('\n');
        }

        return csv.ToString();
    }

    public string ToSensitivityCsv()
    {
        var csv = new StringBuilder(
            "parameter,input_unit,horizon_days,low_value,baseline_value,high_value,low_temperature_K,baseline_temperature_K,high_temperature_K,normalized_sensitivity\n");
        foreach (var item in Sensitivities)
        {
            csv.Append(Csv(item.Parameter)).Append(',').Append(Csv(item.InputUnit)).Append(',').Append(item.HorizonDays).Append(',');
            csv.Append(Number(item.LowValue)).Append(',').Append(Number(item.BaselineValue)).Append(',').Append(Number(item.HighValue)).Append(',');
            csv.Append(Number(item.LowTemperatureKelvin)).Append(',').Append(Number(item.BaselineTemperatureKelvin)).Append(',');
            csv.Append(Number(item.HighTemperatureKelvin)).Append(',').Append(Number(item.NormalizedSensitivity)).Append('\n');
        }

        return csv.ToString();
    }

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string Csv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n')) return value;
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
