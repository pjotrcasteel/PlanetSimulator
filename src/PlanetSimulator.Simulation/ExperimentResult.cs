using System.Globalization;
using System.Text;

namespace PlanetSimulator.Simulation;

/// <summary>
/// Exports a completed run and its final spatial field in explicit SI units.
/// </summary>
public sealed record ExperimentResult(ExperimentScenario Scenario, IReadOnlyList<ExperimentSample> Samples)
{
    public SurfaceSnapshot? FinalSurface { get; init; }
    public IReadOnlyList<double> FinalRegions { get; init; } = [];
    public string ToRegionalCsv()
    {
        var grid = new SphericalGrid();
        var csv = new StringBuilder("cell,latitude_deg,longitude_deg,area_m2,temperature_K");
        if (FinalSurface is not null) csv.Append(",elevation_m,water_kg_m2,liquid_kg_m2,ice_kg_m2");
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
            }
            csv.Append('\n');
        }
        return csv.ToString();
    }

    public string ToCsv()
    {
        var csv = new StringBuilder("day,temperature_K,equilibrium_K,absorbed_W_m2,emitted_W_m2,net_W_m2,absorbed_J_m2,emitted_J_m2,budget_error_J_m2");
        if (Scenario.Regional is not null) csv.Append(",minimum_K,maximum_K,north_mean_K,south_mean_K");
        if (Scenario.Surface is not null) csv.Append(",total_water_kg,liquid_mass_fraction,water_mass_error_kg");
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
            }
            csv.Append('\n');
        }

        return csv.ToString();
    }
}
