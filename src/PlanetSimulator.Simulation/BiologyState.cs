namespace PlanetSimulator.Simulation;

/// <summary>
/// CO2 + H2O + light -> CH2O + O2; aerobic respiration reverses this net reaction.
/// One immobile microbial pool; phosphorus is a separate, recycled quota, not phosphate chemistry.
/// Cold/hot/dry organisms are dormant. No spontaneous generation or ecosystem succession.
/// </summary>
internal sealed class BiologyState
{
    internal const double WaterMolarMass = 0.01801528;
    internal const double BiomassMolarMass = AtmosphereParameters.CarbonDioxideMolarMassKilogramsPerMole
        + WaterMolarMass - AtmosphereParameters.OxygenMolarMassKilogramsPerMole;
    internal const double ChemicalJoulesPerMole = 467000;
    private readonly SphericalGrid grid;
    private readonly BiologyParameters parameters;
    private readonly AtmosphereState atmosphere;
    private readonly double[] biomass, availablePhosphorus;
    private readonly double initialMoles, initialPhosphorus;
    private double production, respiration;

    public BiologyState(SphericalGrid grid, BiologyParameters parameters, AtmosphereState atmosphere, IReadOnlyList<double> water)
    {
        parameters.Validate();
        this.grid = grid; this.parameters = parameters; this.atmosphere = atmosphere;
        // Inoculum is an explicit initial external reservoir, including its chemical energy and bound water.
        biomass = water.Select(m => m <= 0 ? 0 : Math.Min(parameters.InitialBiomassKilogramsPerSquareMeter / BiomassMolarMass,
            parameters.TotalPhosphorusKilogramsPerSquareMeter / parameters.PhosphorusKilogramsPerMoleCarbon)).ToArray();
        availablePhosphorus = biomass.Select(n => Math.Max(0, parameters.TotalPhosphorusKilogramsPerSquareMeter
            - n * parameters.PhosphorusKilogramsPerMoleCarbon)).ToArray();
        initialMoles = biomass.Sum();
        initialPhosphorus = availablePhosphorus.Sum() + initialMoles * parameters.PhosphorusKilogramsPerMoleCarbon;
    }

    public double EnergyChange => (biomass.Sum() - initialMoles) * ChemicalJoulesPerMole / biomass.Length;
    public double BoundWater => biomass.Sum() * WaterMolarMass * grid.CellAreaSquareMeters;
    public double BoundWaterChange => (biomass.Sum() - initialMoles) * WaterMolarMass * grid.CellAreaSquareMeters;

    public void Advance(double seconds, double[] water, double[] enthalpy, IReadOnlyList<double> light, double heatCapacity,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        for (var i = 0; i < biomass.Length; i++)
        {
            var temperature = WaterThermodynamics.Temperature(enthalpy[i], water[i], heatCapacity);
            var liquid = water[i] * WaterThermodynamics.LiquidFraction(enthalpy[i], water[i]);
            var activity = Math.Max(0, 1 - Math.Abs(temperature - 293.15) / 20);
            if (liquid <= 0 || activity <= 0 || biomass[i] <= 0) continue;
            var growth = biomass[i] * (Math.Exp(parameters.MaximumGrowthPerDay * activity * seconds / 86400) - 1);
            growth = Math.Min(growth, light[i] * seconds * parameters.PhotosyntheticEfficiency / ChemicalJoulesPerMole);
            growth = Math.Min(growth, atmosphere.AvailableDissolvedCarbonMoles(i));
            growth = Math.Min(growth, liquid / WaterMolarMass);
            growth = Math.Min(growth, availablePhosphorus[i] / parameters.PhosphorusKilogramsPerMoleCarbon);
            // Do not spend the energy required to keep reactant water liquid.
            growth = Math.Min(growth, Math.Max(0, enthalpy[i]) / ChemicalJoulesPerMole);
            biomass[i] += growth;
            water[i] -= growth * WaterMolarMass;
            enthalpy[i] -= growth * ChemicalJoulesPerMole;
            availablePhosphorus[i] = Math.Max(0, availablePhosphorus[i] - growth * parameters.PhosphorusKilogramsPerMoleCarbon);
            atmosphere.FixCarbon(i, growth);
            production += growth;

            var consumed = Math.Min(biomass[i] * (1 - Math.Exp(-parameters.RespirationPerDay * activity * seconds / 86400)),
                atmosphere.AvailableOxygenMoles(i));
            biomass[i] -= consumed;
            water[i] += consumed * WaterMolarMass;
            enthalpy[i] += consumed * ChemicalJoulesPerMole;
            availablePhosphorus[i] += consumed * parameters.PhosphorusKilogramsPerMoleCarbon;
            atmosphere.RespireCarbon(i, consumed);
            respiration += consumed;
        }
        atmosphere.SetBiologicalCarbonChange(biomass.Sum() - initialMoles);
    }

    public BiologySnapshot Snapshot() => new()
    {
        BiomassKilogramsPerSquareMeter = Array.AsReadOnly(biomass.Select(n => n * BiomassMolarMass).ToArray()),
        AvailablePhosphorusKilogramsPerSquareMeter = Array.AsReadOnly(availablePhosphorus.ToArray()),
        TotalBiomassKilograms = biomass.Sum() * BiomassMolarMass * grid.CellAreaSquareMeters,
        TotalAvailablePhosphorusKilograms = availablePhosphorus.Sum() * grid.CellAreaSquareMeters,
        PhosphorusBudgetErrorKilograms = (availablePhosphorus.Sum() + biomass.Sum() * parameters.PhosphorusKilogramsPerMoleCarbon
            - initialPhosphorus) * grid.CellAreaSquareMeters,
        CumulativeProductionKilograms = production * BiomassMolarMass * grid.CellAreaSquareMeters,
        CumulativeRespirationKilograms = respiration * BiomassMolarMass * grid.CellAreaSquareMeters,
        ChemicalEnergyJoulesPerSquareMeter = biomass.Average() * ChemicalJoulesPerMole,
        BoundWaterEquivalentKilograms = BoundWater,
    };
}
