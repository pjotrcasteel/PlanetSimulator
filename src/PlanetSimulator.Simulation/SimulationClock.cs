namespace PlanetSimulator.Simulation;

/// <summary>
/// Advances simulation time in fixed one-minute steps, independently of rendering.
/// </summary>
public sealed class SimulationClock
{
    public const double StepSeconds = 60;
    private double remainderSeconds;
    private double speed = 3600;

    public bool IsPaused { get; set; }
    public long TickCount { get; private set; }
    public double ElapsedSeconds => TickCount * StepSeconds;
    public double Speed
    {
        get => speed;
        set
        {
            if (!double.IsFinite(value) || value < 0 || value > 86400)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            speed = value;
        }
    }

    public void Advance(double realSeconds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!double.IsFinite(realSeconds) || realSeconds < 0 || realSeconds > 60)
        {
            throw new ArgumentOutOfRangeException(nameof(realSeconds));
        }

        if (IsPaused)
        {
            return;
        }

        remainderSeconds += realSeconds * Speed;
        var steps = (long)Math.Floor((remainderSeconds + 1e-8) / StepSeconds);
        TickCount += steps;
        remainderSeconds = Math.Max(0, remainderSeconds - steps * StepSeconds);
    }

    public void Reset()
    {
        TickCount = 0;
        remainderSeconds = 0;
    }
}
