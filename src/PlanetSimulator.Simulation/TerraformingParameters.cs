namespace PlanetSimulator.Simulation;

/// <summary>
/// Finite, externally supplied initial infrastructure stocks. No free refuelling or mining is assumed.
/// </summary>
public sealed record TerraformingParameters
{
    public double InitialEnergyJoulesPerSquareMeter { get; init; } = 1e8;
    public double InitialMaterialKilogramsPerSquareMeter { get; init; } = 10;
    public double StorageCapacityKilogramsPerSquareMeter { get; init; } = 1;
    public IReadOnlyList<TerraformInstallation> Installations { get; init; } = Array.AsReadOnly(new[]
    {
        new TerraformInstallation(),
        new TerraformInstallation { Name = "CO₂-afvang", Kind = TerraformInstallation.Capture },
        new TerraformInstallation { Name = "CO₂-transport", Kind = TerraformInstallation.Transport, StartDay = 0.5 },
    });

    internal void Validate(int cells = SphericalGrid.DefaultLatitudeBands * SphericalGrid.DefaultLongitudeBands)
    {
        Check(InitialEnergyJoulesPerSquareMeter, 0, 1e10, nameof(InitialEnergyJoulesPerSquareMeter));
        Check(InitialMaterialKilogramsPerSquareMeter, 0, 1000, nameof(InitialMaterialKilogramsPerSquareMeter));
        Check(StorageCapacityKilogramsPerSquareMeter, 0, 1000, nameof(StorageCapacityKilogramsPerSquareMeter));
        if (Installations is null || Installations.Count > 16) throw new ArgumentException("Maximaal zestien installaties toegestaan.");
        foreach (var installation in Installations)
        {
            if (installation is null) throw new ArgumentException("Installatie ontbreekt.");
            installation.Validate(cells);
        }
    }

    internal static void Check(double value, double minimum, double maximum, string name)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum) throw new ArgumentOutOfRangeException(name);
    }
}
