namespace PlanetSimulator.Simulation;

/// <summary>
/// Defines a uniform blackbody planet. Heat capacity is an explicit scenario assumption, not an Earth calibration.
/// </summary>
public sealed class ClimateParameters
{
    public double DistanceAstronomicalUnits { get; }
    public double BondAlbedo { get; }
    public double ArealHeatCapacity { get; }
    public double InitialTemperatureKelvin { get; }
    public double StellarLuminositySolarUnits { get; }

    public ClimateParameters(
        double distanceAstronomicalUnits = 1,
        double bondAlbedo = 0.3,
        double arealHeatCapacity = 1e7,
        double initialTemperatureKelvin = 230,
        double stellarLuminositySolarUnits = 1)
    {
        Validate(distanceAstronomicalUnits, 0.2, 5, nameof(distanceAstronomicalUnits));
        Validate(bondAlbedo, 0, 1, nameof(bondAlbedo));
        Validate(arealHeatCapacity, 1e5, 1e10, nameof(arealHeatCapacity));
        Validate(initialTemperatureKelvin, 1, 1000, nameof(initialTemperatureKelvin));
        Validate(stellarLuminositySolarUnits, 0, 2, nameof(stellarLuminositySolarUnits));
        DistanceAstronomicalUnits = distanceAstronomicalUnits;
        BondAlbedo = bondAlbedo;
        ArealHeatCapacity = arealHeatCapacity;
        InitialTemperatureKelvin = initialTemperatureKelvin;
        StellarLuminositySolarUnits = stellarLuminositySolarUnits;
    }

    private static void Validate(double value, double minimum, double maximum, string name)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(name, value, $"Expected a finite value between {minimum} and {maximum}.");
        }
    }
}
