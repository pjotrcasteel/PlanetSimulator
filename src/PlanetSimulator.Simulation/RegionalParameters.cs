namespace PlanetSimulator.Simulation;

/// <summary>
/// Sets an idealized circular seasonal cycle and an effective angular heat diffusion coefficient.
/// Diffusion is a parameterization, not simulated atmospheric or oceanic flow.
/// </summary>
public sealed class RegionalParameters
{
    public double AxialTiltDegrees { get; }
    public double HeatDiffusionWattsPerSquareMeterKelvin { get; }
    public double YearDays { get; }

    public RegionalParameters(double axialTiltDegrees = 23.44, double heatDiffusionWattsPerSquareMeterKelvin = 0.6, double yearDays = 365.25)
    {
        if (!double.IsFinite(axialTiltDegrees) || axialTiltDegrees is < 0 or > 90) throw new ArgumentOutOfRangeException(nameof(axialTiltDegrees));
        if (!double.IsFinite(heatDiffusionWattsPerSquareMeterKelvin) || heatDiffusionWattsPerSquareMeterKelvin is < 0 or > 2)
            throw new ArgumentOutOfRangeException(nameof(heatDiffusionWattsPerSquareMeterKelvin));
        if (!double.IsFinite(yearDays) || yearDays is < 30 or > 1000) throw new ArgumentOutOfRangeException(nameof(yearDays));
        AxialTiltDegrees = axialTiltDegrees;
        HeatDiffusionWattsPerSquareMeterKelvin = heatDiffusionWattsPerSquareMeterKelvin;
        YearDays = yearDays;
    }
}
