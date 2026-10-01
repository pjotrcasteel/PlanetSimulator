namespace PlanetSimulator.Simulation;

/// <summary>
/// Advances simulation time in fixed one-minute steps, independently of rendering.
/// Each completed model step advances the clock once; cancellation preserves their alignment.
/// </summary>
public sealed class SimulationClock
{
    public const double StepSeconds = 60;
    public const double MaximumSpeed = 604800;
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
            if (!double.IsFinite(value) || value < 0 || value > MaximumSpeed)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            speed = value;
        }
    }

    public void Advance(double realSeconds, CancellationToken cancellationToken) => Advance(realSeconds, null, cancellationToken);

    public void Advance(double realSeconds, Action<double>? advanceStep, CancellationToken cancellationToken)
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
        while (remainderSeconds + 1e-8 >= StepSeconds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            advanceStep?.Invoke(StepSeconds);
            TickCount++;
            remainderSeconds = Math.Max(0, remainderSeconds - StepSeconds);
        }
    }

    public void Reset()
    {
        TickCount = 0;
        remainderSeconds = 0;
    }
}
