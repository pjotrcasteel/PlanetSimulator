namespace PlanetSimulator.Simulation;

/// <summary>
/// Piecewise enthalpy of a substrate plus a well-mixed freshwater column at prescribed pressure.
/// Constants are rounded approximations near freezing, not temperature-dependent property tables.
/// </summary>
public static class WaterThermodynamics
{
    public const double MeltingKelvin = 273.15;
    public const double LatentHeatJoulesPerKilogram = 333500;
    public const double IceHeatCapacity = 2100;
    public const double LiquidHeatCapacity = 4180;
    public const double LiquidDensity = 1000;

    public static double Enthalpy(double kelvin, double massPerSquareMeter, double substrateCapacity)
    {
        var delta = kelvin - MeltingKelvin;
        return delta <= 0 ? (substrateCapacity + massPerSquareMeter * IceHeatCapacity) * delta
            : massPerSquareMeter * LatentHeatJoulesPerKilogram + (substrateCapacity + massPerSquareMeter * LiquidHeatCapacity) * delta;
    }

    public static double Temperature(double enthalpy, double massPerSquareMeter, double substrateCapacity)
    {
        if (enthalpy < 0) return MeltingKelvin + enthalpy / (substrateCapacity + massPerSquareMeter * IceHeatCapacity);
        var latent = massPerSquareMeter * LatentHeatJoulesPerKilogram;
        return enthalpy <= latent ? MeltingKelvin : MeltingKelvin + (enthalpy - latent) / (substrateCapacity + massPerSquareMeter * LiquidHeatCapacity);
    }

    public static double LiquidFraction(double enthalpy, double massPerSquareMeter) => massPerSquareMeter == 0 ? 0
        : Math.Clamp(enthalpy / (massPerSquareMeter * LatentHeatJoulesPerKilogram), 0, 1);
}
