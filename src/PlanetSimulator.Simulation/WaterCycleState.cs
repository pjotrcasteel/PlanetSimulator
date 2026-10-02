namespace PlanetSimulator.Simulation;

/// <summary>
/// Bounded surface, vapor and cloud transfers carrying explicit enthalpy.
/// Prescribed winds and relaxation times are parameterizations, not atmospheric dynamics.
/// </summary>
internal sealed class WaterCycleState
{
    private const double VaporizationHeat = 2500300;
    private readonly SphericalGrid grid;
    private readonly SurfaceReservoirs surface;
    private readonly WaterCycleParameters parameters;
    private readonly double[] vapor, cloud, vaporEnergy, cloudEnergy, runoff;
    private readonly double initialMass;
    private double evaporationTotal, condensationTotal, precipitationTotal, runoffTotal;

    public WaterCycleState(SphericalGrid grid, WaterCycleParameters parameters, SurfaceReservoirs surface, IReadOnlyList<double> temperatures)
    {
        this.grid = grid;
        this.parameters = parameters;
        this.surface = surface;
        var count = grid.Cells.Count;
        vapor = new double[count]; cloud = new double[count]; vaporEnergy = new double[count]; cloudEnergy = new double[count]; runoff = new double[count];
        initialMass = surface.WaterMassPerSquareMeter.Sum();
        var requestedVapor = parameters.InitialVaporEquivalentDepthMeters * 1000;
        var requestedCloud = parameters.InitialCloudEquivalentDepthMeters * 1000;
        var requested = requestedVapor + requestedCloud;
        for (var i = 0; i < count; i++)
        {
            var taken = Math.Min(surface.MutableWaterMassPerSquareMeter[i], requested);
            vapor[i] = requested == 0 ? 0 : taken * requestedVapor / requested;
            cloud[i] = taken - vapor[i];
            surface.MutableWaterMassPerSquareMeter[i] -= taken;
            vaporEnergy[i] = vapor[i] * VaporEnthalpy(temperatures[i]);
            cloudEnergy[i] = cloud[i] * LiquidEnthalpy(temperatures[i]);
        }
        InitialAtmosphericEnergyJoulesPerSquareMeter = AtmosphericEnergyJoulesPerSquareMeter;
    }

    public double AtmosphericEnergyJoulesPerSquareMeter => (vaporEnergy.Sum() + cloudEnergy.Sum()) / grid.Cells.Count;
    public double InitialAtmosphericEnergyJoulesPerSquareMeter { get; }

    public WaterCycleSnapshot Snapshot(double[] surfaceMass) => new()
    {
        VaporMassPerSquareMeter = Array.AsReadOnly(vapor.ToArray()),
        CloudMassPerSquareMeter = Array.AsReadOnly(cloud.ToArray()),
        RunoffMassPerSquareMeter = Array.AsReadOnly(runoff.ToArray()),
        TotalVaporMassKilograms = vapor.Sum() * grid.CellAreaSquareMeters,
        TotalCloudMassKilograms = cloud.Sum() * grid.CellAreaSquareMeters,
        TotalRunoffMassKilograms = runoff.Sum() * grid.CellAreaSquareMeters,
        TotalAtmosphericWaterMassKilograms = (vapor.Sum() + cloud.Sum()) * grid.CellAreaSquareMeters,
        CumulativeEvaporationKilograms = evaporationTotal * grid.CellAreaSquareMeters,
        CumulativeCondensationKilograms = condensationTotal * grid.CellAreaSquareMeters,
        CumulativePrecipitationKilograms = precipitationTotal * grid.CellAreaSquareMeters,
        CumulativeRunoffKilograms = runoffTotal * grid.CellAreaSquareMeters,
        WaterMassErrorKilograms = (surfaceMass.Sum() + vapor.Sum() + cloud.Sum() - initialMass) * grid.CellAreaSquareMeters,
        AtmosphericEnergyJoulesPerSquareMeter = AtmosphericEnergyJoulesPerSquareMeter,
    };

    public void Advance(double seconds, double[] masses, double[] enthalpies, double capacity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        for (var i = 0; i < masses.Length; i++)
        {
            var temperature = WaterThermodynamics.Temperature(enthalpies[i], masses[i], capacity);
            var liquid = masses[i] * WaterThermodynamics.LiquidFraction(enthalpies[i], masses[i]);
            // Effective column capacity: no vertical relative-humidity profile is resolved.
            var saturation = parameters.SaturationVaporEquivalentDepthMeters * 1000 * Math.Exp(Math.Clamp((temperature - 288.15) / 17, -20, 5));
            var specificEnergy = VaporEnthalpy(temperature);
            var evaporation = Math.Min(liquid, Math.Max(0, saturation - vapor[i]) * Fraction(seconds, parameters.EvaporationTimescaleHours));
            evaporation = Math.Min(evaporation, Math.Max(0, enthalpies[i]) / Math.Max(1, specificEnergy));
            masses[i] -= evaporation; vapor[i] += evaporation;
            enthalpies[i] -= evaporation * specificEnergy;
            vaporEnergy[i] += evaporation * specificEnergy;
            evaporationTotal += evaporation;
            var condensation = Math.Max(0, vapor[i] - saturation) * Fraction(seconds, parameters.CondensationTimescaleHours);
            var removedEnergy = vapor[i] == 0 ? 0 : vaporEnergy[i] * (condensation / vapor[i]);
            var cloudHeat = condensation * LiquidEnthalpy(temperature);
            vapor[i] -= condensation; vaporEnergy[i] -= removedEnergy;
            cloud[i] += condensation; cloudEnergy[i] += cloudHeat;
            enthalpies[i] += removedEnergy - cloudHeat;
            condensationTotal += condensation;
            var rainFraction = Fraction(seconds, parameters.PrecipitationTimescaleHours);
            var rain = cloud[i] * rainFraction;
            var rainEnergy = cloudEnergy[i] * rainFraction;
            cloud[i] -= rain; cloudEnergy[i] -= rainEnergy;
            masses[i] += rain; enthalpies[i] += rainEnergy;
            precipitationTotal += rain;
        }
        Advect(seconds);
        Runoff(seconds, masses, enthalpies, capacity);
    }

    private void Advect(double seconds)
    {
        // Sequential first-order sweeps move both mass and its stored specific enthalpy.
        foreach (var cell in grid.Cells)
        {
            var east = cell.LatitudeIndex * grid.LongitudeBands + (cell.LongitudeIndex + 1) % grid.LongitudeBands;
            var distance = grid.RadiusMeters * Math.Cos(cell.LatitudeRadians) * Math.Tau / grid.LongitudeBands;
            var fraction = Math.Min(0.45, parameters.ZonalWindMetersPerSecond * seconds / distance);
            Move(vapor, vaporEnergy, cell.Index, east, fraction);
            Move(cloud, cloudEnergy, cell.Index, east, fraction);
            if (cell.LatitudeIndex + 1 == grid.LatitudeBands) continue;
            var south = cell.Index + grid.LongitudeBands;
            distance = grid.RadiusMeters * (cell.LatitudeRadians - grid.Cells[south].LatitudeRadians);
            fraction = Math.Min(0.45, parameters.MeridionalWindMetersPerSecond * seconds / distance);
            Move(vapor, vaporEnergy, cell.Index, south, fraction);
            Move(cloud, cloudEnergy, cell.Index, south, fraction);
        }
    }

    private void Runoff(double seconds, double[] masses, double[] enthalpies, double capacity)
    {
        foreach (var cell in grid.Cells)
        {
            var source = cell.Index;
            var destination = source;
            var sourceLevel = surface.ElevationsMeters[source] + masses[source] / 1000;
            var low = sourceLevel;
            foreach (var neighbour in Neighbours(cell))
            {
                var level = surface.ElevationsMeters[neighbour] + masses[neighbour] / 1000;
                if (level >= low) continue;
                low = level; destination = neighbour;
            }
            if (source == destination) continue;
            var liquid = masses[source] * WaterThermodynamics.LiquidFraction(enthalpies[source], masses[source]);
            var amount = Math.Min(liquid, (sourceLevel - low) * 500) * Fraction(seconds, parameters.RunoffTimescaleHours);
            var temperature = WaterThermodynamics.Temperature(enthalpies[source], masses[source], capacity);
            var energy = amount * LiquidEnthalpy(temperature);
            masses[source] -= amount; masses[destination] += amount;
            enthalpies[source] -= energy; enthalpies[destination] += energy;
            runoff[source] += amount; runoffTotal += amount;
        }
    }

    private IEnumerable<int> Neighbours(SurfaceCell cell)
    {
        yield return cell.LatitudeIndex * grid.LongitudeBands + (cell.LongitudeIndex + 1) % grid.LongitudeBands;
        yield return cell.LatitudeIndex * grid.LongitudeBands + (cell.LongitudeIndex + grid.LongitudeBands - 1) % grid.LongitudeBands;
        if (cell.LatitudeIndex > 0) yield return cell.Index - grid.LongitudeBands;
        if (cell.LatitudeIndex + 1 < grid.LatitudeBands) yield return cell.Index + grid.LongitudeBands;
    }

    private static void Move(double[] masses, double[] energies, int source, int destination, double fraction)
    {
        var mass = masses[source] * fraction;
        var energy = energies[source] * fraction;
        masses[source] -= mass; masses[destination] += mass;
        energies[source] -= energy; energies[destination] += energy;
    }

    private static double Fraction(double seconds, double hours) => 1 - Math.Exp(-seconds / (hours * 3600));
    private static double LiquidEnthalpy(double kelvin) => WaterThermodynamics.LatentHeatJoulesPerKilogram
        + WaterThermodynamics.LiquidHeatCapacity * (kelvin - WaterThermodynamics.MeltingKelvin);
    private static double VaporEnthalpy(double kelvin) => WaterThermodynamics.LatentHeatJoulesPerKilogram + VaporizationHeat
        + 1850 * (kelvin - WaterThermodynamics.MeltingKelvin);
}
