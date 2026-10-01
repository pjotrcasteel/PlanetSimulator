namespace PlanetSimulator.Simulation;

/// <summary>
/// Limits floating-point accumulation error in long-running energy budgets.
/// </summary>
internal sealed class CompensatedSum
{
    private double correction;
    public double Value { get; private set; }

    public void Add(double value)
    {
        var corrected = value - correction;
        var next = Value + corrected;
        correction = next - Value - corrected;
        Value = next;
    }
}
