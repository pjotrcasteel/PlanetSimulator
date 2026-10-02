namespace PlanetSimulator.Simulation;

/// <summary>
/// Cell-wise gas inventory with conservative CO2 dissolution, release and finite outgassing.
/// The model intentionally does not claim a complete reaction network; every included reaction
/// moves a whole CO2 molecule between named reservoirs, making C and O balances auditable.
/// </summary>
internal sealed class AtmosphereState
{
    private readonly SphericalGrid grid;
    private readonly AtmosphereParameters parameters;
    private readonly double[] nitrogen, oxygen, carbonDioxide, argon, dissolvedCarbonDioxide, crustalCarbonDioxide;
    private readonly double initialCarbonMassKilograms, initialOxygenMassKilograms, initialNitrogenMassKilograms;
    private double cumulativeUptake, cumulativeRelease, cumulativeOutgassing;

    public AtmosphereState(SphericalGrid grid, AtmosphereParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.ValidateRuntimeValues();
        if (parameters.SurfacePressurePascals > 0 && parameters.TotalMoleFraction <= 0) throw new ArgumentException("At least one gas fraction is required.", nameof(parameters));
        this.grid = grid;
        this.parameters = parameters;
        var meanMolarMass = parameters.NitrogenMoleFraction * AtmosphereParameters.NitrogenMolarMassKilogramsPerMole
            + parameters.OxygenMoleFraction * AtmosphereParameters.OxygenMolarMassKilogramsPerMole
            + parameters.CarbonDioxideMoleFraction * AtmosphereParameters.CarbonDioxideMolarMassKilogramsPerMole
            + parameters.ArgonMoleFraction * AtmosphereParameters.ArgonMolarMassKilogramsPerMole;
        if (meanMolarMass <= 0) meanMolarMass = 1;
        var totalColumnMass = parameters.SurfacePressurePascals / parameters.SurfaceGravityMetersPerSecondSquared;
        var count = grid.Cells.Count;
        nitrogen = Enumerable.Repeat(totalColumnMass * parameters.NitrogenMoleFraction * AtmosphereParameters.NitrogenMolarMassKilogramsPerMole / meanMolarMass, count).ToArray();
        oxygen = Enumerable.Repeat(totalColumnMass * parameters.OxygenMoleFraction * AtmosphereParameters.OxygenMolarMassKilogramsPerMole / meanMolarMass, count).ToArray();
        carbonDioxide = Enumerable.Repeat(totalColumnMass * parameters.CarbonDioxideMoleFraction
            * AtmosphereParameters.CarbonDioxideMolarMassKilogramsPerMole / meanMolarMass, count).ToArray();
        argon = Enumerable.Repeat(totalColumnMass * parameters.ArgonMoleFraction * AtmosphereParameters.ArgonMolarMassKilogramsPerMole / meanMolarMass, count).ToArray();
        dissolvedCarbonDioxide = new double[count];
        crustalCarbonDioxide = Enumerable.Repeat(parameters.CrustalCarbonDioxideMassPerSquareMeter, count).ToArray();
        initialCarbonMassKilograms = CarbonMassKilograms();
        initialOxygenMassKilograms = OxygenMassKilograms();
        initialNitrogenMassKilograms = NitrogenMassKilograms();
    }

    public AtmosphereSnapshot Snapshot()
    {
        var partial = Enumerable.Range(0, grid.Cells.Count).Select(PartialPressure).ToArray();
        var totalMass = (nitrogen.Sum() + oxygen.Sum() + carbonDioxide.Sum() + argon.Sum()) * grid.CellAreaSquareMeters;
        var totalPressure = totalMass / (grid.CellAreaSquareMeters * grid.Cells.Count) * parameters.SurfaceGravityMetersPerSecondSquared;
        var co2Pressure = partial.Average();
        var co2MoleFraction = Enumerable.Range(0, grid.Cells.Count).Average(MoleFraction);
        return new AtmosphereSnapshot
        {
            NitrogenMassPerSquareMeter = Array.AsReadOnly(nitrogen.ToArray()),
            OxygenMassPerSquareMeter = Array.AsReadOnly(oxygen.ToArray()),
            CarbonDioxideMassPerSquareMeter = Array.AsReadOnly(carbonDioxide.ToArray()),
            ArgonMassPerSquareMeter = Array.AsReadOnly(argon.ToArray()),
            DissolvedCarbonDioxideMassPerSquareMeter = Array.AsReadOnly(dissolvedCarbonDioxide.ToArray()),
            CarbonDioxidePartialPressurePascals = Array.AsReadOnly(partial),
            NitrogenPartialPressurePascals = Pressures(nitrogen, AtmosphereParameters.NitrogenMolarMassKilogramsPerMole),
            OxygenPartialPressurePascals = Pressures(oxygen, AtmosphereParameters.OxygenMolarMassKilogramsPerMole),
            ArgonPartialPressurePascals = Pressures(argon, AtmosphereParameters.ArgonMolarMassKilogramsPerMole),
            TotalAtmosphericMassKilograms = totalMass,
            MeanSurfacePressurePascals = totalPressure,
            MeanCarbonDioxidePartialPressurePascals = co2Pressure,
            MeanCarbonDioxideMoleFraction = co2MoleFraction,
            TotalDissolvedCarbonDioxideMassKilograms = dissolvedCarbonDioxide.Sum() * grid.CellAreaSquareMeters,
            RemainingCrustalCarbonDioxideMassKilograms = crustalCarbonDioxide.Sum() * grid.CellAreaSquareMeters,
            CumulativeCarbonDioxideUptakeKilograms = cumulativeUptake,
            CumulativeCarbonDioxideReleaseKilograms = cumulativeRelease,
            CumulativeOutgassingKilograms = cumulativeOutgassing,
            CarbonMassErrorKilograms = CarbonMassKilograms() - initialCarbonMassKilograms,
            OxygenMassErrorKilograms = OxygenMassKilograms() - initialOxygenMassKilograms,
            NitrogenMassErrorKilograms = NitrogenMassKilograms() - initialNitrogenMassKilograms,
            MinimumGasMassPerSquareMeter = nitrogen.Concat(oxygen).Concat(carbonDioxide).Concat(argon).Min(),
        };
    }

    public void Advance(double seconds, IReadOnlyList<double>? surfaceMass, IReadOnlyList<double>? enthalpies, double substrateCapacity,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!double.IsFinite(seconds) || seconds < 0 || seconds > SimulationClock.StepSeconds) throw new ArgumentOutOfRangeException(nameof(seconds));
        var hasSurface = surfaceMass is not null && enthalpies is not null;
        if (hasSurface && (surfaceMass!.Count != grid.Cells.Count || enthalpies!.Count != grid.Cells.Count)) throw new ArgumentException("Atmosphere arrays must match the grid.");
        var exchangeFraction = 1 - Math.Exp(-seconds / (parameters.Co2ExchangeTimescaleHours * 3600));
        var outgassing = parameters.Co2OutgassingKilogramsPerSquareMeterPerYear * seconds / (365.25 * 86400);
        for (var index = 0; index < grid.Cells.Count; index++)
        {
            var liquid = hasSurface ? surfaceMass![index] * WaterThermodynamics.LiquidFraction(enthalpies![index], surfaceMass[index]) : 0;
            var temperature = hasSurface ? WaterThermodynamics.Temperature(enthalpies![index], surfaceMass![index], substrateCapacity) : 298.15;
            var henry = parameters.Co2HenryMolesPerCubicMeterPascal
                * Math.Exp(parameters.Co2SolubilityTemperatureKelvin * (1 / Math.Clamp(temperature, 273.15, 323.15) - 1 / 298.15));
            var capacity = henry * liquid / 1000 * AtmosphereParameters.CarbonDioxideMolarMassKilogramsPerMole;
            var total = carbonDioxide[index] + dissolvedCarbonDioxide[index];
            // Solve d = K pCO2(g) with d + g = total before relaxation; no overshoot on fast exchange.
            var k = capacity * parameters.SurfaceGravityMetersPerSecondSquared;
            var inertMass = nitrogen[index] + oxygen[index] + argon[index];
            var b = InertMoles(index) * AtmosphereParameters.CarbonDioxideMolarMassKilogramsPerMole;
            var linear = b + k * inertMass - total;
            var discriminant = Math.Sqrt(linear * linear + 4 * (1 + k) * total * b);
            var gasAtEquilibrium = total == 0 ? 0 : linear >= 0
                ? 2 * total * b / (linear + discriminant)
                : (discriminant - linear) / (2 * (1 + k));
            var equilibrium = Math.Clamp(total - gasAtEquilibrium, 0, total);
            var transfer = (equilibrium - dissolvedCarbonDioxide[index]) * exchangeFraction;
            if (transfer > 0)
            {
                transfer = Math.Min(transfer, carbonDioxide[index]);
                carbonDioxide[index] -= transfer;
                dissolvedCarbonDioxide[index] += transfer;
                cumulativeUptake += transfer * grid.CellAreaSquareMeters;
            }
            else if (transfer < 0)
            {
                transfer = Math.Min(-transfer, dissolvedCarbonDioxide[index]);
                dissolvedCarbonDioxide[index] -= transfer;
                carbonDioxide[index] += transfer;
                cumulativeRelease += transfer * grid.CellAreaSquareMeters;
            }
            var release = Math.Min(crustalCarbonDioxide[index], outgassing);
            crustalCarbonDioxide[index] -= release;
            carbonDioxide[index] += release;
            cumulativeOutgassing += release * grid.CellAreaSquareMeters;
        }
    }

    private IReadOnlyList<double> Pressures(double[] masses, double molarMass) => Array.AsReadOnly(Enumerable.Range(0, grid.Cells.Count).Select(i =>
    {
        var moles = InertMoles(i) + carbonDioxide[i] / AtmosphereParameters.CarbonDioxideMolarMassKilogramsPerMole;
        var pressure = parameters.SurfaceGravityMetersPerSecondSquared * (nitrogen[i] + oxygen[i] + carbonDioxide[i] + argon[i]);
        return moles == 0 ? 0 : masses[i] / molarMass / moles * pressure;
    }).ToArray());

    private double InertMoles(int i) => nitrogen[i] / AtmosphereParameters.NitrogenMolarMassKilogramsPerMole
        + oxygen[i] / AtmosphereParameters.OxygenMolarMassKilogramsPerMole + argon[i] / AtmosphereParameters.ArgonMolarMassKilogramsPerMole;

    private double MoleFraction(int i)
    {
        var co2 = carbonDioxide[i] / AtmosphereParameters.CarbonDioxideMolarMassKilogramsPerMole;
        return co2 + InertMoles(i) == 0 ? 0 : co2 / (co2 + InertMoles(i));
    }

    private double PartialPressure(int i) => MoleFraction(i) * parameters.SurfaceGravityMetersPerSecondSquared
        * (nitrogen[i] + oxygen[i] + carbonDioxide[i] + argon[i]);

    private double CarbonMassKilograms() => (carbonDioxide.Sum() + dissolvedCarbonDioxide.Sum() + crustalCarbonDioxide.Sum())
        * AtmosphereParameters.CarbonMassFractionInCarbonDioxide * grid.CellAreaSquareMeters;

    private double OxygenMassKilograms() => (oxygen.Sum() + (carbonDioxide.Sum() + dissolvedCarbonDioxide.Sum() + crustalCarbonDioxide.Sum())
        * AtmosphereParameters.OxygenMassFractionInCarbonDioxide) * grid.CellAreaSquareMeters;

    private double NitrogenMassKilograms() => nitrogen.Sum() * grid.CellAreaSquareMeters;
}
