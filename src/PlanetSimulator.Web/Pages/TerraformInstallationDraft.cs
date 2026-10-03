using PlanetSimulator.Simulation;

namespace PlanetSimulator.Web.Pages;

/// <summary>
/// Editable values that retain unedited technical assumptions when importing a scenario.
/// </summary>
public sealed class TerraformInstallationDraft
{
    private readonly TerraformInstallation original;
    public string Name { get; set; }
    public string Kind { get; set; }
    public int Cell { get; set; }
    public int Destination { get; set; }
    public double Start { get; set; }
    public double End { get; set; }
    public double BuildDays { get; set; }
    public double Power { get; set; }
    public double Rate { get; set; }
    public double TravelDays { get; set; }
    public TerraformInstallationDraft(TerraformInstallation installation)
    {
        original = installation;
        Name = installation.Name; Kind = installation.Kind; Cell = installation.CellIndex; Destination = installation.DestinationCellIndex;
        Start = installation.StartDay; End = installation.EndDay; BuildDays = installation.ConstructionDays;
        Power = installation.PowerWattsPerSquareMeter; Rate = installation.MaximumThroughputKilogramsPerSquareMeterPerDay;
        TravelDays = installation.TransportDays;
    }
    public TerraformInstallation Build() => original with
    {
        Name = Name, Kind = Kind, CellIndex = Cell, DestinationCellIndex = Destination, StartDay = Start, EndDay = End,
        ConstructionDays = BuildDays, PowerWattsPerSquareMeter = Power, MaximumThroughputKilogramsPerSquareMeterPerDay = Rate, TransportDays = TravelDays,
    };
}
