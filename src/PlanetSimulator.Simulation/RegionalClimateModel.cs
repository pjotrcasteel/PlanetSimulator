namespace PlanetSimulator.Simulation;

/// <summary>
/// Integrates spatial blackbody radiation and conservative spherical diffusion with RK4.
/// Cell-centre sunlight is normalized to the exact intercepted power; it is a coarse-grid approximation.
/// </summary>
public sealed class RegionalClimateModel
{
    private readonly double[] temperatures, normalX, normalY, normalZ, firstRate, secondRate, thirdRate, fourthRate, temporary;
    private readonly double[] initialSunlight, middleSunlight, finalSunlight;
    private readonly (int First, int Second, double Conductance)[] edges;
    private readonly CompensatedSum absorbedEnergy = new();
    private readonly CompensatedSum emittedEnergy = new();
    private readonly double[] enthalpies, stageTemperatures;
    private readonly double initialEnthalpy;
    private readonly WaterCycleState? waterCycle;
    private readonly AtmosphereState? atmosphereState;
    private readonly BiologyState? biology;
    private readonly TerraformingState? terraforming;
    public SurfaceReservoirs? Surface { get; }
    public WaterCycleParameters? WaterCycle { get; }
    public AtmosphereParameters? Atmosphere { get; }
    public SphericalGrid Grid { get; } = new();
    public ClimateParameters Climate { get; private set; }
    public RegionalParameters Parameters { get; private set; }
    public IReadOnlyList<double> TemperaturesKelvin { get; }
    public double ElapsedSeconds { get; private set; }
    public double MeanTemperatureKelvin => temperatures.Average();
    public double AbsorbedWattsPerSquareMeter => EnergyBalanceModel.SolarIrradianceAtOneAu * Climate.StellarLuminositySolarUnits
        * (1 - Climate.BondAlbedo) / (4 * Climate.DistanceAstronomicalUnits * Climate.DistanceAstronomicalUnits);
    public double EmittedWattsPerSquareMeter => temperatures.Average(Emission);
    public double EquilibriumTemperatureKelvin => Math.Sqrt(Math.Sqrt(AbsorbedWattsPerSquareMeter / EnergyBalanceModel.StefanBoltzmannConstant));
    public double CumulativeAbsorbedJoulesPerSquareMeter => absorbedEnergy.Value;
    public double CumulativeEmittedJoulesPerSquareMeter => emittedEnergy.Value;
    public double EnergyBalanceErrorJoulesPerSquareMeter => StoredEnergyChangeJoulesPerSquareMeter
        - (absorbedEnergy.Value - emittedEnergy.Value);

    public RegionalClimateModel(ClimateParameters? climate = null, RegionalParameters? parameters = null, IReadOnlyList<double>? initialTemperatures = null,
        SurfaceParameters? surface = null, WaterCycleParameters? waterCycle = null, AtmosphereParameters? atmosphere = null)
    {
        Climate = climate ?? new ClimateParameters();
        Parameters = parameters ?? new RegionalParameters();
        var count = Grid.Cells.Count;
        if (initialTemperatures is not null && (initialTemperatures.Count != count || initialTemperatures.Any(t => !double.IsFinite(t) || t is < 1 or > 1000)))
            throw new ArgumentException("Expected one finite initial temperature (1–1000 K) per cell.", nameof(initialTemperatures));
        temperatures = initialTemperatures?.ToArray() ?? Enumerable.Repeat(Climate.InitialTemperatureKelvin, count).ToArray();
        if (initialTemperatures is not null) initialMeanTemperature = temperatures.Average();
        else initialMeanTemperature = Climate.InitialTemperatureKelvin;
        if (waterCycle is not null && surface is null) throw new ArgumentException("The water cycle requires surface reservoirs.", nameof(waterCycle));
        Surface = surface is null ? null : new SurfaceReservoirs(Grid, surface);
        WaterCycle = waterCycle;
        if (Surface is not null && waterCycle is not null)
            this.waterCycle = new WaterCycleState(Grid, waterCycle, Surface, temperatures);
        enthalpies = temperatures.Select((t, i) => WaterThermodynamics.Enthalpy(t, Surface?.WaterMassPerSquareMeter[i] ?? 0, Climate.ArealHeatCapacity)).ToArray();
        Atmosphere = atmosphere;
        if (atmosphere is not null) atmosphereState = new AtmosphereState(Grid, atmosphere);
        if (atmosphere?.Biology is { } life)
        {
            if (Surface is null) throw new ArgumentException("Biology requires water reservoirs.", nameof(surface));
            biology = new BiologyState(Grid, life, atmosphereState!, Surface.WaterMassPerSquareMeter);
        }
        if (atmosphere?.Terraforming is { } engineering)
        {
            if (Surface is null) throw new ArgumentException("Terraforming requires surface reservoirs.", nameof(surface));
            terraforming = new TerraformingState(Grid, engineering, atmosphereState!);
        }
        initialEnthalpy = enthalpies.Average();
        stageTemperatures = new double[count];
        TemperaturesKelvin = Array.AsReadOnly(temperatures);
        normalX = new double[count]; normalY = new double[count]; normalZ = new double[count];
        firstRate = new double[count]; secondRate = new double[count]; thirdRate = new double[count]; fourthRate = new double[count]; temporary = new double[count];
        initialSunlight = new double[count]; middleSunlight = new double[count]; finalSunlight = new double[count];
        foreach (var cell in Grid.Cells)
        {
            normalX[cell.Index] = Math.Cos(cell.LatitudeRadians) * Math.Cos(cell.LongitudeRadians);
            normalY[cell.Index] = Math.Sin(cell.LatitudeRadians);
            normalZ[cell.Index] = Math.Cos(cell.LatitudeRadians) * Math.Sin(cell.LongitudeRadians);
        }
        edges = BuildEdges();
    }

    private readonly double initialMeanTemperature;
    public double StoredEnergyChangeJoulesPerSquareMeter => Surface is null
        ? Climate.ArealHeatCapacity * (MeanTemperatureKelvin - initialMeanTemperature)
        : enthalpies.Average() - initialEnthalpy + (waterCycle?.AtmosphericEnergyJoulesPerSquareMeter ?? 0)
            - (waterCycle?.InitialAtmosphericEnergyJoulesPerSquareMeter ?? 0) + (biology?.EnergyChange ?? 0) + (terraforming?.StoredEnergyChange ?? 0);

    public void SetForcing(double distanceAstronomicalUnits, double bondAlbedo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Climate = new ClimateParameters(distanceAstronomicalUnits, bondAlbedo, Climate.ArealHeatCapacity, Climate.InitialTemperatureKelvin, Climate.StellarLuminositySolarUnits);
    }

    public void SetParameters(RegionalParameters parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(parameters);
        Parameters = parameters;
    }

    public double SolarDeclinationRadians(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        var phase = Math.Tau * (elapsedSeconds % (Parameters.YearDays * 86400)) / (Parameters.YearDays * 86400);
        return Math.Asin(Math.Sin(Parameters.AxialTiltDegrees * Math.PI / 180) * Math.Sin(phase));
    }

    public double[] GetAbsorbedFluxes(double elapsedSeconds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fluxes = new double[temperatures.Length];
        FillSunlight(elapsedSeconds, fluxes);
        return fluxes;
    }

    public double[] GetTransportFluxes(IReadOnlyList<double> state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (state.Count != temperatures.Length || state.Any(t => !double.IsFinite(t))) throw new ArgumentException("Invalid temperature field.", nameof(state));
        var fluxes = new double[temperatures.Length];
        AddTransport(state.ToArray(), fluxes);
        return fluxes;
    }

    public void Advance(double seconds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!double.IsFinite(seconds) || seconds < 0 || seconds > SimulationClock.StepSeconds) throw new ArgumentOutOfRangeException(nameof(seconds));
        FillSunlight(ElapsedSeconds, initialSunlight);
        FillSunlight(ElapsedSeconds + seconds / 2, middleSunlight);
        FillSunlight(ElapsedSeconds + seconds, finalSunlight);
        var state = Surface is null ? temperatures : enthalpies;
        var firstEmission = Rate(state, initialSunlight, firstRate);
        Prepare(state, firstRate, seconds / 2);
        var secondEmission = Rate(temporary, middleSunlight, secondRate);
        Prepare(state, secondRate, seconds / 2);
        var thirdEmission = Rate(temporary, middleSunlight, thirdRate);
        Prepare(state, thirdRate, seconds);
        var fourthEmission = Rate(temporary, finalSunlight, fourthRate);
        for (var index = 0; index < temperatures.Length; index++)
            state[index] += seconds * (firstRate[index] + 2 * secondRate[index] + 2 * thirdRate[index] + fourthRate[index]) / 6;
        if (Surface is not null)
            for (var index = 0; index < temperatures.Length; index++)
                temperatures[index] = WaterThermodynamics.Temperature(enthalpies[index], Surface.WaterMassPerSquareMeter[index], Climate.ArealHeatCapacity);
        // A minute is committed atomically; cancellation is checked before its first mutation.
        waterCycle?.Advance(seconds, Surface!.MutableWaterMassPerSquareMeter, enthalpies, Climate.ArealHeatCapacity, CancellationToken.None);
        atmosphereState?.Advance(seconds, Surface?.MutableWaterMassPerSquareMeter, enthalpies, Climate.ArealHeatCapacity, CancellationToken.None);
        biology?.Advance(seconds, Surface!.MutableWaterMassPerSquareMeter, enthalpies, middleSunlight, Climate.ArealHeatCapacity, CancellationToken.None);
        terraforming?.Advance(seconds, enthalpies, CancellationToken.None);
        if (Surface is not null)
            for (var index = 0; index < temperatures.Length; index++)
                temperatures[index] = WaterThermodynamics.Temperature(enthalpies[index], Surface.WaterMassPerSquareMeter[index], Climate.ArealHeatCapacity);
        absorbedEnergy.Add(AbsorbedWattsPerSquareMeter * seconds);
        emittedEnergy.Add((firstEmission + 2 * secondEmission + 2 * thirdEmission + fourthEmission) * seconds / 6);
        ElapsedSeconds += seconds;
    }

    public RegionalSnapshot Snapshot()
    {
        var surface = Surface;
        var surfaceSnapshot = surface is null ? null : surface.Snapshot(enthalpies, surface.MutableWaterMassPerSquareMeter,
            waterCycle?.Snapshot(surface.MutableWaterMassPerSquareMeter));
        if (surfaceSnapshot is not null && biology is not null)
        {
            var total = surfaceSnapshot.TotalWaterMassKilograms + biology.BoundWater;
            var error = waterCycle is null
                ? surfaceSnapshot.TotalWaterMassKilograms - surface!.TotalWaterMassKilograms + biology.BoundWaterChange
                : surfaceSnapshot.WaterMassErrorKilograms + biology.BoundWaterChange;
            surfaceSnapshot = surfaceSnapshot with { TotalWaterMassKilograms = total, WaterMassErrorKilograms = error,
                BiologicallyBoundWaterEquivalentKilograms = biology.BoundWater,
                WaterCycle = surfaceSnapshot.WaterCycle is { } cycle ? cycle with { WaterMassErrorKilograms = error } : null };
        }
        return new RegionalSnapshot
        {
            TemperaturesKelvin = temperatures.ToArray(),
            Surface = surfaceSnapshot,
            Atmosphere = atmosphereState?.Snapshot(),
            Biology = biology?.Snapshot(),
            Terraforming = terraforming?.Snapshot(),
            MinimumTemperatureKelvin = temperatures.Min(),
            MaximumTemperatureKelvin = temperatures.Max(),
            NorthMeanTemperatureKelvin = temperatures.Take(temperatures.Length / 2).Average(),
            SouthMeanTemperatureKelvin = temperatures.Skip(temperatures.Length / 2).Average(),
            SolarDeclinationRadians = SolarDeclinationRadians(ElapsedSeconds),
            BudgetErrorJoulesPerSquareMeter = EnergyBalanceErrorJoulesPerSquareMeter,
        };
    }

    private void FillSunlight(double elapsedSeconds, double[] destination)
    {
        var declination = SolarDeclinationRadians(elapsedSeconds);
        // A prescribed 24-hour solar day. No orbital eccentricity or Keplerian period coupling.
        var longitude = Math.Tau * (elapsedSeconds % 86400) / 86400;
        var x = Math.Cos(declination) * Math.Cos(longitude);
        var y = Math.Sin(declination);
        var z = Math.Cos(declination) * Math.Sin(longitude);
        var sum = 0d;
        for (var index = 0; index < destination.Length; index++)
        {
            destination[index] = Math.Max(0, normalX[index] * x + normalY[index] * y + normalZ[index] * z);
            sum += destination[index];
        }
        var scale = AbsorbedWattsPerSquareMeter * destination.Length / sum;
        for (var index = 0; index < destination.Length; index++) destination[index] *= scale;
    }

    private double Rate(double[] state, double[] sunlight, double[] rate)
    {
        var thermalState = state;
        if (Surface is not null)
        {
            for (var index = 0; index < state.Length; index++)
                stageTemperatures[index] = WaterThermodynamics.Temperature(state[index], Surface.WaterMassPerSquareMeter[index], Climate.ArealHeatCapacity);
            thermalState = stageTemperatures;
        }
        var totalEmission = 0d;
        for (var index = 0; index < state.Length; index++)
        {
            var emission = Emission(thermalState[index]);
            totalEmission += emission;
            rate[index] = sunlight[index] - emission;
        }
        AddTransport(thermalState, rate);
        if (Surface is null)
            for (var index = 0; index < rate.Length; index++) rate[index] /= Climate.ArealHeatCapacity;
        return totalEmission / state.Length;
    }

    private void Prepare(double[] state, double[] rate, double seconds)
    {
        for (var index = 0; index < state.Length; index++) temporary[index] = state[index] + seconds * rate[index];
    }

    private void AddTransport(double[] state, double[] fluxes)
    {
        foreach (var edge in edges)
        {
            var transfer = Parameters.HeatDiffusionWattsPerSquareMeterKelvin * edge.Conductance * (state[edge.Second] - state[edge.First]);
            fluxes[edge.First] += transfer;
            fluxes[edge.Second] -= transfer;
        }
    }

    private (int First, int Second, double Conductance)[] BuildEdges()
    {
        var result = new List<(int, int, double)>();
        var deltaMu = 2d / Grid.LatitudeBands;
        var deltaLongitude = Math.Tau / Grid.LongitudeBands;
        var solidAngle = deltaMu * deltaLongitude;
        foreach (var cell in Grid.Cells)
        {
            var mu = Math.Sin(cell.LatitudeRadians);
            var east = cell.LatitudeIndex * Grid.LongitudeBands + (cell.LongitudeIndex + 1) % Grid.LongitudeBands;
            result.Add((cell.Index, east, deltaMu / ((1 - mu * mu) * deltaLongitude * solidAngle)));
            if (cell.LatitudeIndex + 1 < Grid.LatitudeBands)
            {
                var boundaryMu = 1 - (cell.LatitudeIndex + 1) * deltaMu;
                result.Add((cell.Index, cell.Index + Grid.LongitudeBands, (1 - boundaryMu * boundaryMu) * deltaLongitude / (deltaMu * solidAngle)));
            }
        }
        return result.ToArray();
    }

    private static double Emission(double kelvin)
    {
        var squared = kelvin * kelvin;
        return EnergyBalanceModel.StefanBoltzmannConstant * squared * squared;
    }
}
