namespace PlanetSimulator.Simulation;

/// <summary>
/// Initial dry-gas inventory and the small chemistry subset used by milestone 8.
/// Gas columns are derived from surface pressure and gravity; CO2 exchanges with liquid water
/// through a bounded Henry-style equilibrium and may be supplied by a finite crustal reservoir.
/// </summary>
public sealed record AtmosphereParameters
{
    public const double NitrogenMolarMassKilogramsPerMole = 0.0280134;
    public const double OxygenMolarMassKilogramsPerMole = 0.031998;
    public const double CarbonDioxideMolarMassKilogramsPerMole = 0.0440095;
    public const double ArgonMolarMassKilogramsPerMole = 0.039948;
    public const double CarbonMassFractionInCarbonDioxide = 12.011 / 44.0095;
    public const double OxygenMassFractionInCarbonDioxide = 31.998 / 44.0095;

    public double SurfacePressurePascals { get; init; }
    public double NitrogenMoleFraction { get; init; }
    public double OxygenMoleFraction { get; init; }
    public double CarbonDioxideMoleFraction { get; init; }
    public double ArgonMoleFraction { get; init; }
    public double CrustalCarbonDioxideMassPerSquareMeter { get; init; }
    public double SurfaceGravityMetersPerSecondSquared { get; init; } = 9.80665;
    public double Co2HenryMolesPerCubicMeterPascal { get; init; } = 3.4e-4;
    public double Co2SolubilityTemperatureKelvin { get; init; } = 2400;
    public double Co2ExchangeTimescaleHours { get; init; } = 24;
    public double Co2OutgassingKilogramsPerSquareMeterPerYear { get; init; }

    [System.Text.Json.Serialization.JsonIgnore]
    public double TotalMoleFraction => NitrogenMoleFraction + OxygenMoleFraction + CarbonDioxideMoleFraction + ArgonMoleFraction;

    public AtmosphereParameters(double surfacePressurePascals = 101325, double nitrogenMoleFraction = 0.78084,
        double oxygenMoleFraction = 0.20946, double carbonDioxideMoleFraction = 0.0004, double argonMoleFraction = 0.0093,
        double crustalCarbonDioxideMassPerSquareMeter = 50)
    {
        if (!double.IsFinite(surfacePressurePascals) || surfacePressurePascals < 0 || surfacePressurePascals > 2e7)
            throw new ArgumentOutOfRangeException(nameof(surfacePressurePascals));
        if (!double.IsFinite(nitrogenMoleFraction) || nitrogenMoleFraction < 0 || nitrogenMoleFraction > 1)
            throw new ArgumentOutOfRangeException(nameof(nitrogenMoleFraction));
        if (!double.IsFinite(oxygenMoleFraction) || oxygenMoleFraction < 0 || oxygenMoleFraction > 1)
            throw new ArgumentOutOfRangeException(nameof(oxygenMoleFraction));
        if (!double.IsFinite(carbonDioxideMoleFraction) || carbonDioxideMoleFraction < 0 || carbonDioxideMoleFraction > 1)
            throw new ArgumentOutOfRangeException(nameof(carbonDioxideMoleFraction));
        if (!double.IsFinite(argonMoleFraction) || argonMoleFraction < 0 || argonMoleFraction > 1)
            throw new ArgumentOutOfRangeException(nameof(argonMoleFraction));
        if (!double.IsFinite(crustalCarbonDioxideMassPerSquareMeter) || crustalCarbonDioxideMassPerSquareMeter < 0 || crustalCarbonDioxideMassPerSquareMeter > 1e8)
            throw new ArgumentOutOfRangeException(nameof(crustalCarbonDioxideMassPerSquareMeter));
        if (Math.Abs(nitrogenMoleFraction + oxygenMoleFraction + carbonDioxideMoleFraction + argonMoleFraction - 1) > 1e-9)
            throw new ArgumentException("Gas mole fractions must add up to one.", nameof(nitrogenMoleFraction));
        SurfacePressurePascals = surfacePressurePascals;
        NitrogenMoleFraction = nitrogenMoleFraction;
        OxygenMoleFraction = oxygenMoleFraction;
        CarbonDioxideMoleFraction = carbonDioxideMoleFraction;
        ArgonMoleFraction = argonMoleFraction;
        CrustalCarbonDioxideMassPerSquareMeter = crustalCarbonDioxideMassPerSquareMeter;
    }

    internal void ValidateRuntimeValues()
    {
        if (!double.IsFinite(SurfacePressurePascals) || SurfacePressurePascals < 0 || SurfacePressurePascals > 2e7)
            throw new ArgumentOutOfRangeException(nameof(SurfacePressurePascals));
        if (new[] { NitrogenMoleFraction, OxygenMoleFraction, CarbonDioxideMoleFraction, ArgonMoleFraction }.Any(value => !double.IsFinite(value) || value < 0 || value > 1))
            throw new ArgumentOutOfRangeException(nameof(NitrogenMoleFraction));
        if (Math.Abs(TotalMoleFraction - 1) > 1e-9) throw new ArgumentException("Gas mole fractions must add up to one.");
        if (!double.IsFinite(CrustalCarbonDioxideMassPerSquareMeter) || CrustalCarbonDioxideMassPerSquareMeter < 0)
            throw new ArgumentOutOfRangeException(nameof(CrustalCarbonDioxideMassPerSquareMeter));
        if (!double.IsFinite(SurfaceGravityMetersPerSecondSquared) || SurfaceGravityMetersPerSecondSquared <= 0 || SurfaceGravityMetersPerSecondSquared > 100)
            throw new ArgumentOutOfRangeException(nameof(SurfaceGravityMetersPerSecondSquared));
        if (!double.IsFinite(Co2HenryMolesPerCubicMeterPascal) || Co2HenryMolesPerCubicMeterPascal < 0 || Co2HenryMolesPerCubicMeterPascal > 1)
            throw new ArgumentOutOfRangeException(nameof(Co2HenryMolesPerCubicMeterPascal));
        if (!double.IsFinite(Co2SolubilityTemperatureKelvin) || Co2SolubilityTemperatureKelvin < 0 || Co2SolubilityTemperatureKelvin > 10000)
            throw new ArgumentOutOfRangeException(nameof(Co2SolubilityTemperatureKelvin));
        if (!double.IsFinite(Co2ExchangeTimescaleHours) || Co2ExchangeTimescaleHours < 1e-6 || Co2ExchangeTimescaleHours > 1e6)
            throw new ArgumentOutOfRangeException(nameof(Co2ExchangeTimescaleHours));
        if (!double.IsFinite(Co2OutgassingKilogramsPerSquareMeterPerYear) || Co2OutgassingKilogramsPerSquareMeterPerYear < 0 || Co2OutgassingKilogramsPerSquareMeterPerYear > 1e6)
            throw new ArgumentOutOfRangeException(nameof(Co2OutgassingKilogramsPerSquareMeterPerYear));
    }
}
