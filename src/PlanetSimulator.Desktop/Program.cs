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
    catch (Exception exception) when (exception is ArgumentException or System.Text.Json.JsonException or IOException or UnauthorizedAccessException or OperationCanceledException)
    {
        Console.Error.WriteLine(exception.Message);
        Environment.ExitCode = 1;
    }
}
else if (args.Length != 0)
{
    Console.Error.WriteLine("Usage: PlanetSimulator [--experiment scenario.json --output output-directory]");
    Environment.ExitCode = 1;
}
else
{
    using var game = new PlanetGame();
    game.Run();
}
