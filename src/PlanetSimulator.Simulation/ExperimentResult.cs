using System.Globalization;
using System.Text;

namespace PlanetSimulator.Simulation;

/// <summary>
/// Exports a completed run and its final spatial field in explicit SI units.
/// </summary>
public sealed record ExperimentResult(ExperimentScenario Scenario, IReadOnlyList<ExperimentSample> Samples)
{
    public SurfaceSnapshot? FinalSurface { get; init; }
    public TerraformingSnapshot? FinalTerraforming { get; init; }
    public BiologySnapshot? FinalBiology { get; init; }
    public AtmosphereSnapshot? FinalAtmosphere { get; init; }
    public IReadOnlyList<double> FinalRegions { get; init; } = [];
    public string ToRegionalCsv()
    {
        var grid = new SphericalGrid(Scenario.Planet.RadiusMeters);
        var csv = new StringBuilder("cell,latitude_deg,longitude_deg,area_m2,temperature_K");
        if (FinalSurface is not null) csv.Append(",elevation_m,water_kg_m2,liquid_kg_m2,ice_kg_m2");
        if (FinalSurface?.WaterCycle is not null) csv.Append(",vapor_kg_m2,cloud_kg_m2,runoff_kg_m2");
        if (FinalAtmosphere is not null) csv.Append(",n2_kg_m2,o2_kg_m2,co2_kg_m2,argon_kg_m2,co2_partial_pressure_pa");
        if (FinalBiology is not null) csv.Append(",biomass_kg_m2,available_phosphorus_kg_m2");
        if (FinalTerraforming is not null) csv.Append(",engineering_energy_J_m2,engineering_material_kg_m2,stored_co2_kg_m2,transit_co2_kg_m2");
        csv.Append('\n');
        foreach (var cell in grid.Cells.Take(FinalRegions.Count))
        {
            var values = new[] { (double)cell.Index, cell.LatitudeRadians * 180 / Math.PI, cell.LongitudeRadians * 180 / Math.PI,
                grid.CellAreaSquareMeters, FinalRegions[cell.Index] };
            csv.AppendJoin(',', values.Select(value => value.ToString("R", CultureInfo.InvariantCulture)));
            if (FinalSurface is { } surface)
            {
                var fields = new[] { surface.ElevationsMeters[cell.Index], surface.WaterMassPerSquareMeter[cell.Index],
                    surface.LiquidMassPerSquareMeter[cell.Index], surface.IceMassPerSquareMeter[cell.Index] };
                csv.Append(',').AppendJoin(',', fields.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));
                if (surface.WaterCycle is { } cycle)
                {
                    var cycleFields = new[] { cycle.VaporMassPerSquareMeter[cell.Index], cycle.CloudMassPerSquareMeter[cell.Index], cycle.RunoffMassPerSquareMeter[cell.Index] };
                    csv.Append(',').AppendJoin(',', cycleFields.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));
                }
            }
            if (FinalAtmosphere is { } atmosphere)
            {
                var fields = new[] { atmosphere.NitrogenMassPerSquareMeter[cell.Index], atmosphere.OxygenMassPerSquareMeter[cell.Index],
                    atmosphere.CarbonDioxideMassPerSquareMeter[cell.Index], atmosphere.ArgonMassPerSquareMeter[cell.Index],
                    atmosphere.CarbonDioxidePartialPressurePascals[cell.Index] };
                csv.Append(',').AppendJoin(',', fields.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));
            }
            if (FinalBiology is { } life)
                csv.Append(',').AppendJoin(',', new[] { life.BiomassKilogramsPerSquareMeter[cell.Index],
                    life.AvailablePhosphorusKilogramsPerSquareMeter[cell.Index] }.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));
            if (FinalTerraforming is { } engineering)
                csv.Append(',').AppendJoin(',', new[] { engineering.RemainingEnergyJoulesPerSquareMeter[cell.Index],
                    engineering.RemainingMaterialKilogramsPerSquareMeter[cell.Index], engineering.StoredCarbonDioxideKilogramsPerSquareMeter[cell.Index],
                    engineering.InTransitCarbonDioxideKilogramsPerSquareMeter[cell.Index] }.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));
            csv.Append('\n');
        }
        return csv.ToString();
    }

    public string ToCsv()
    {
        var csv = new StringBuilder("day,temperature_K,equilibrium_K,absorbed_W_m2,emitted_W_m2,net_W_m2,absorbed_J_m2,emitted_J_m2,budget_error_J_m2");
        if (Scenario.Regional is not null) csv.Append(",minimum_K,maximum_K,north_mean_K,south_mean_K");
        if (Scenario.Surface is not null) csv.Append(",total_water_kg,liquid_mass_fraction,water_mass_error_kg");
        if (Scenario.Hydrology is not null) csv.Append(",total_vapor_kg,total_cloud_kg,evaporation_kg,condensation_kg,precipitation_kg,runoff_kg,water_budget_error_kg");
        if (Scenario.Atmosphere is not null) csv.Append(",total_dry_gas_kg,surface_pressure_pa,co2_partial_pressure_pa,co2_mole_fraction,dissolved_co2_kg,co2_uptake_kg,co2_release_kg,carbon_budget_error_kg,oxygen_budget_error_kg,nitrogen_budget_error_kg");
        if (Scenario.Atmosphere?.Biology is not null)
            csv.Append(",biomass_kg,available_phosphorus_kg,phosphorus_budget_error_kg,biomass_production_kg,biomass_respiration_kg,chemical_energy_J_m2,bound_water_kg");
        if (Scenario.Atmosphere?.Terraforming is not null)
            csv.Append(",engineering_energy_J,engineering_used_J,engineering_material_kg,engineering_built_kg,stored_co2_kg,transit_co2_kg,captured_co2_kg,delivered_co2_kg")
                .Append(",engineering_energy_error_J_m2,material_budget_error_kg,logistics_budget_error_kg");
        csv.Append('\n');
        foreach (var sample in Samples)
        {
            var values = new double[] { sample.Day, sample.TemperatureKelvin, sample.EquilibriumTemperatureKelvin,
                sample.AbsorbedWattsPerSquareMeter, sample.EmittedWattsPerSquareMeter,
                sample.AbsorbedWattsPerSquareMeter - sample.EmittedWattsPerSquareMeter,
                sample.AbsorbedJoulesPerSquareMeter, sample.EmittedJoulesPerSquareMeter, sample.BudgetErrorJoulesPerSquareMeter };
            csv.AppendJoin(',', values.Select(value => value.ToString("R", CultureInfo.InvariantCulture)));
            if (sample.Regional is { } region)
            {
                var regionalValues = new[] { region.MinimumTemperatureKelvin, region.MaximumTemperatureKelvin,
                    region.NorthMeanTemperatureKelvin, region.SouthMeanTemperatureKelvin };
                csv.Append(',').AppendJoin(',', regionalValues.Select(value => value.ToString("R", CultureInfo.InvariantCulture)));
            }
            if (sample.Regional?.Surface is { } surface)
            {
                var fields = new[] { surface.TotalWaterMassKilograms, surface.LiquidMassFraction, surface.WaterMassErrorKilograms };
                csv.Append(',').AppendJoin(',', fields.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));
                if (surface.WaterCycle is { } cycle)
                {
                    var cycleFields = new[] { cycle.TotalVaporMassKilograms, cycle.TotalCloudMassKilograms, cycle.CumulativeEvaporationKilograms,
                        cycle.CumulativeCondensationKilograms, cycle.CumulativePrecipitationKilograms, cycle.CumulativeRunoffKilograms,
                        cycle.WaterMassErrorKilograms };
                    csv.Append(',').AppendJoin(',', cycleFields.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));
                }
            }
            if (sample.Regional?.Atmosphere is { } atmosphere)
            {
                var fields = new[] { atmosphere.TotalAtmosphericMassKilograms, atmosphere.MeanSurfacePressurePascals, atmosphere.MeanCarbonDioxidePartialPressurePascals,
                    atmosphere.MeanCarbonDioxideMoleFraction, atmosphere.TotalDissolvedCarbonDioxideMassKilograms,
                    atmosphere.CumulativeCarbonDioxideUptakeKilograms, atmosphere.CumulativeCarbonDioxideReleaseKilograms,
                    atmosphere.CarbonMassErrorKilograms, atmosphere.OxygenMassErrorKilograms, atmosphere.NitrogenMassErrorKilograms };
                csv.Append(',').AppendJoin(',', fields.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));
            }
            if (sample.Regional?.Biology is { } life)
                csv.Append(',').AppendJoin(',', new[] { life.TotalBiomassKilograms, life.TotalAvailablePhosphorusKilograms,
                    life.PhosphorusBudgetErrorKilograms, life.CumulativeProductionKilograms, life.CumulativeRespirationKilograms,
                    life.ChemicalEnergyJoulesPerSquareMeter, life.BoundWaterEquivalentKilograms }.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));
            if (sample.Regional?.Terraforming is { } engineering)
                csv.Append(',').AppendJoin(',', new[] { engineering.TotalRemainingEnergyJoules, engineering.TotalUsedEnergyJoules,
                    engineering.TotalRemainingMaterialKilograms, engineering.TotalBuiltMaterialKilograms, engineering.TotalStoredCarbonDioxideKilograms,
                    engineering.TotalInTransitCarbonDioxideKilograms, engineering.CumulativeCapturedKilograms, engineering.CumulativeDeliveredKilograms,
                    engineering.EnergyBudgetErrorJoulesPerSquareMeter, engineering.MaterialBudgetErrorKilograms,
                    engineering.LogisticsBudgetErrorKilograms }.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));
            csv.Append('\n');
        }

        return csv.ToString();
    }
}
