using System.Globalization;
using PlanetSimulator.Desktop;
using PlanetSimulator.Simulation;

if (args.Length == 4 && args[0] == "--experiment" && args[2] == "--output")
{
    try
    {
        if (new FileInfo(args[1]).Length > ScenarioJson.MaximumBytes) throw new ArgumentException("Scenario exceeds 64 KiB.");
        var scenario = ScenarioJson.Deserialize(File.ReadAllText(args[1]));
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
        var result = new ExperimentRunner(scenario).Finish(cancellation.Token);
        Directory.CreateDirectory(args[3]);
        File.WriteAllText(Path.Combine(args[3], "scenario.json"), ScenarioJson.Serialize(result.Scenario));
        File.WriteAllText(Path.Combine(args[3], "results.csv"), result.ToCsv());
        if (result.FinalRegions.Count > 0) File.WriteAllText(Path.Combine(args[3], "regions.csv"), result.ToRegionalCsv());
        Console.WriteLine($"Completed {scenario.DurationDays} days; {result.Samples.Count} samples. Model: {scenario.ModelVersion}");
    }
    catch (Exception exception) when (exception is ArgumentException or System.Text.Json.JsonException or IOException
        or UnauthorizedAccessException or OperationCanceledException)
    {
        Console.Error.WriteLine(exception.Message);
        Environment.ExitCode = 1;
    }
}
else if (args.Length == 8 && args[0] == "--optimize-albedo" && args[6] == "--output")
{
    try
    {
        if (new FileInfo(args[1]).Length > ScenarioJson.MaximumBytes) throw new ArgumentException("Scenario exceeds 64 KiB.");
        var scenario = ScenarioJson.Deserialize(File.ReadAllText(args[1]));
        var target = double.Parse(args[2], CultureInfo.InvariantCulture);
        var minimum = double.Parse(args[3], CultureInfo.InvariantCulture);
        var maximum = double.Parse(args[4], CultureInfo.InvariantCulture);
        var steps = int.Parse(args[5], CultureInfo.InvariantCulture);
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
        var optimization = AlbedoOptimizer.Optimize(scenario, target, minimum, maximum, steps, cancellation.Token);
        Directory.CreateDirectory(args[7]);
        File.WriteAllText(Path.Combine(args[7], "optimization.csv"), optimization.ToCsv());
        File.WriteAllText(Path.Combine(args[7], "best-scenario.json"), ScenarioJson.Serialize(optimization.BestResult.Scenario));
        File.WriteAllText(Path.Combine(args[7], "best-results.csv"), optimization.BestResult.ToCsv());
        Console.WriteLine($"Best albedo {optimization.BestCandidate.BondAlbedo:R}; final {optimization.BestCandidate.FinalTemperatureKelvin:R} K; error {optimization.BestCandidate.AbsoluteTemperatureErrorKelvin:R} K.");
    }
    catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException or System.Text.Json.JsonException
        or IOException or UnauthorizedAccessException or OperationCanceledException)
    {
        Console.Error.WriteLine(exception.Message);
        Environment.ExitCode = 1;
    }
}
else if (args.Length == 3 && args[0] == "--validate" && args[1] == "--output")
{
    try
    {
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
        var report = ValidationSuite.Run(cancellation.Token);
        Directory.CreateDirectory(args[2]);
        File.WriteAllText(Path.Combine(args[2], "validation.json"), ValidationJson.Serialize(report));
        File.WriteAllText(Path.Combine(args[2], "validation.csv"), report.ToCasesCsv());
        File.WriteAllText(Path.Combine(args[2], "sensitivity.csv"), report.ToSensitivityCsv());
        Console.WriteLine($"Validation: {report.PassedCases} passed, {report.ExpectedGapCases} expected gaps, {report.FailedCases} failed.");
        if (!report.IsSuccessful) Environment.ExitCode = 2;
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OperationCanceledException)
    {
        Console.Error.WriteLine(exception.Message);
        Environment.ExitCode = 1;
    }
}
else if (args.Length != 0)
{
    Console.Error.WriteLine("Usage: PlanetSimulator [--experiment scenario.json --output directory] [--optimize-albedo scenario.json targetK minAlbedo maxAlbedo steps --output directory] [--validate --output directory]");
    Environment.ExitCode = 1;
}
else
{
    using var game = new PlanetGame();
    game.Run();
}
