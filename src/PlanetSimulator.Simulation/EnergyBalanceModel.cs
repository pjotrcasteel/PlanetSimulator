namespace PlanetSimulator.Simulation;

/// <summary>
/// Integrates C dT/dt = S(1-a)/4 - sigma T^4 using fourth-order Runge-Kutta in SI units.
/// The model has no atmosphere, regional climate, latent heat or geological heat sources.
/// </summary>
public sealed class EnergyBalanceModel
{
    private readonly CompensatedSum absorbedEnergy = new();
    private readonly CompensatedSum emittedEnergy = new();
    public const double StefanBoltzmannConstant = 5.670374419e-8;
    public const double SolarIrradianceAtOneAu = 1361;
    public ClimateParameters Parameters { get; private set; }
    public double TemperatureKelvin { get; private set; }
    public double CumulativeAbsorbedJoulesPerSquareMeter => absorbedEnergy.Value;
    public double CumulativeEmittedJoulesPerSquareMeter => emittedEnergy.Value;
    public double AbsorbedWattsPerSquareMeter => SolarIrradianceAtOneAu * Parameters.StellarLuminositySolarUnits
        / (Parameters.DistanceAstronomicalUnits * Parameters.DistanceAstronomicalUnits) * (1 - Parameters.BondAlbedo) / 4;
    public double EmittedWattsPerSquareMeter => Emission(TemperatureKelvin);
    public double NetWattsPerSquareMeter => AbsorbedWattsPerSquareMeter - EmittedWattsPerSquareMeter;
    public double EquilibriumTemperatureKelvin => Math.Sqrt(Math.Sqrt(AbsorbedWattsPerSquareMeter / StefanBoltzmannConstant));
    public double StoredEnergyChangeJoulesPerSquareMeter => Parameters.ArealHeatCapacity * (TemperatureKelvin - Parameters.InitialTemperatureKelvin);
    public double EnergyBalanceErrorJoulesPerSquareMeter => StoredEnergyChangeJoulesPerSquareMeter
        - (CumulativeAbsorbedJoulesPerSquareMeter - CumulativeEmittedJoulesPerSquareMeter);

    public EnergyBalanceModel(ClimateParameters? parameters = null)
    {
        Parameters = parameters ?? new ClimateParameters();
        TemperatureKelvin = Parameters.InitialTemperatureKelvin;
    }

    public void SetForcing(double distanceAstronomicalUnits, double bondAlbedo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Parameters = new ClimateParameters(
            distanceAstronomicalUnits, bondAlbedo, Parameters.ArealHeatCapacity, Parameters.InitialTemperatureKelvin, Parameters.StellarLuminositySolarUnits);
    }

    public void Advance(double seconds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!double.IsFinite(seconds) || seconds < 0 || seconds > SimulationClock.StepSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        var absorbed = AbsorbedWattsPerSquareMeter;
        var capacity = Parameters.ArealHeatCapacity;
        var firstEmission = Emission(TemperatureKelvin);
        var firstRate = (absorbed - firstEmission) / capacity;
        var secondEmission = Emission(TemperatureKelvin + seconds * firstRate / 2);
        var secondRate = (absorbed - secondEmission) / capacity;
        var thirdEmission = Emission(TemperatureKelvin + seconds * secondRate / 2);
        var thirdRate = (absorbed - thirdEmission) / capacity;
        var fourthEmission = Emission(TemperatureKelvin + seconds * thirdRate);
        var averageEmission = (firstEmission + 2 * secondEmission + 2 * thirdEmission + fourthEmission) / 6;
        TemperatureKelvin += (absorbed - averageEmission) * seconds / capacity;
        absorbedEnergy.Add(absorbed * seconds);
        emittedEnergy.Add(averageEmission * seconds);
    }

    private static double Emission(double temperatureKelvin)
    {
        var squared = temperatureKelvin * temperatureKelvin;
        return StefanBoltzmannConstant * squared * squared;
    }
}
