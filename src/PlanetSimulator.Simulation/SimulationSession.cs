namespace PlanetSimulator.Simulation;

/// <summary>
/// Advances one selected climate model and planetary rotation with a shared fixed-step clock.
/// Switching model resets time and temperatures; forcing and regional parameter edits preserve stored energy.
/// </summary>
public sealed class SimulationSession
{
    public SimulationClock Clock { get; } = new();
    public Planet Planet { get; }
    public EnergyBalanceModel Climate { get; private set; }
    public RegionalClimateModel? Regional { get; private set; }
    public AtmosphereParameters? Atmosphere => Regional?.Atmosphere;
    public double CumulativeAbsorbedJoulesPerSquareMeter => Regional?.CumulativeAbsorbedJoulesPerSquareMeter ?? Climate.CumulativeAbsorbedJoulesPerSquareMeter;
    public double CumulativeEmittedJoulesPerSquareMeter => Regional?.CumulativeEmittedJoulesPerSquareMeter ?? Climate.CumulativeEmittedJoulesPerSquareMeter;
    public double EnergyBalanceErrorJoulesPerSquareMeter => Regional?.EnergyBalanceErrorJoulesPerSquareMeter ?? Climate.EnergyBalanceErrorJoulesPerSquareMeter;

    public SimulationSession(ClimateParameters? parameters = null, RegionalParameters? regional = null, SurfaceParameters? surface = null,
        WaterCycleParameters? waterCycle = null, AtmosphereParameters? atmosphere = null, PlanetParameters? planet = null)
    {
        Planet = new Planet(planet);
        if (surface is not null && regional is null) throw new ArgumentException("Surface reservoirs require a regional model.", nameof(surface));
        if (waterCycle is not null && surface is null) throw new ArgumentException("The water cycle requires surface reservoirs.", nameof(waterCycle));
        if (atmosphere is not null && regional is null) throw new ArgumentException("The atmosphere requires a regional model.", nameof(atmosphere));
        Climate = new EnergyBalanceModel(parameters);
        if (regional is not null) Regional = new RegionalClimateModel(Climate.Parameters, regional, surface: surface, waterCycle: waterCycle, atmosphere: atmosphere,
            planet: Planet.Parameters);
    }

    public SimulationSnapshot Advance(double realSeconds, CancellationToken cancellationToken)
    {
        AdvanceClock(realSeconds, cancellationToken);
        return Snapshot();
    }

    public void AdvanceClock(double realSeconds, CancellationToken cancellationToken)
    {
        Clock.Advance(realSeconds, seconds =>
        {
            if (Regional is not null) Regional.Advance(seconds, cancellationToken);
            else Climate.Advance(seconds, cancellationToken);
        }, cancellationToken);
    }

    public SimulationSnapshot Snapshot() => new(Clock.ElapsedSeconds, Planet.GetRotationRadians(Clock.ElapsedSeconds),
        Regional?.MeanTemperatureKelvin ?? Climate.TemperatureKelvin,
        Regional?.EquilibriumTemperatureKelvin ?? Climate.EquilibriumTemperatureKelvin,
        Regional?.AbsorbedWattsPerSquareMeter ?? Climate.AbsorbedWattsPerSquareMeter,
        Regional?.EmittedWattsPerSquareMeter ?? Climate.EmittedWattsPerSquareMeter)
    { Regional = Regional?.Snapshot() };

    public void SetForcing(double distanceAstronomicalUnits, double bondAlbedo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Validation happens before either model is changed.
        _ = new ClimateParameters(distanceAstronomicalUnits, bondAlbedo);
        Climate.SetForcing(distanceAstronomicalUnits, bondAlbedo, CancellationToken.None);
        Regional?.SetForcing(distanceAstronomicalUnits, bondAlbedo, CancellationToken.None);
    }

    public void SelectModel(RegionalParameters? regional, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var nextRegional = regional is null ? null : new RegionalClimateModel(Climate.Parameters, regional, planet: Planet.Parameters);
        var nextGlobal = new EnergyBalanceModel(Climate.Parameters);
        Regional = nextRegional;
        Climate = nextGlobal;
        Clock.Reset();
    }

    public void SelectSurface(SurfaceParameters? surface, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Regional is null) throw new InvalidOperationException("Surface reservoirs require a regional model.");
        Regional = new RegionalClimateModel(Climate.Parameters, Regional.Parameters, surface: surface,
            waterCycle: surface is null ? null : Regional.WaterCycle, atmosphere: surface is null ? null : Regional.Atmosphere, planet: Planet.Parameters);
        Clock.Reset();
    }

    public void SelectWaterCycle(WaterCycleParameters? waterCycle, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Regional is null) throw new InvalidOperationException("The water cycle requires a regional model.");
        if (waterCycle is not null && Regional.Surface is null) throw new InvalidOperationException("The water cycle requires surface reservoirs.");
        Regional = new RegionalClimateModel(Climate.Parameters, Regional.Parameters, surface: Regional.Surface?.Parameters, waterCycle: waterCycle,
            atmosphere: Regional.Atmosphere, planet: Planet.Parameters);
        Clock.Reset();
    }

    public void SelectAtmosphere(AtmosphereParameters? atmosphere, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Regional is null) throw new InvalidOperationException("The atmosphere requires a regional model.");
        Regional = new RegionalClimateModel(Climate.Parameters, Regional.Parameters, surface: Regional.Surface?.Parameters,
            waterCycle: Regional.WaterCycle, atmosphere: atmosphere, planet: Planet.Parameters);
        Clock.Reset();
    }

    public void Reset(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Clock.Reset();
        Climate = new EnergyBalanceModel(Climate.Parameters);
        if (Regional is not null)
            Regional = new RegionalClimateModel(Climate.Parameters, Regional.Parameters, surface: Regional.Surface?.Parameters,
                waterCycle: Regional.WaterCycle, atmosphere: Regional.Atmosphere, planet: Planet.Parameters);
    }
}
