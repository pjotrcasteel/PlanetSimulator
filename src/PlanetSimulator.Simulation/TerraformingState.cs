namespace PlanetSimulator.Simulation;

/// <summary>
/// Allocates finite construction stocks, then converts stored energy to heat and moves whole CO2 molecules.
/// Installations share source-cell resources in scenario order. Delivery never precedes departure plus travel time.
/// </summary>
internal sealed class TerraformingState
{
    private readonly SphericalGrid grid;
    private readonly TerraformingParameters parameters;
    private readonly AtmosphereState atmosphere;
    private readonly TerraformInstallation[] plans;
    private readonly double[] energy, material, stored, transit, started, used, processed, transportCost;
    private readonly string[] status;
    private readonly PriorityQueue<(int Source, int Destination, double Mass), double> shipments = new();
    private readonly CompensatedSum captured = new(), delivered = new(), spent = new(), built = new();
    private double elapsed;

    public TerraformingState(SphericalGrid grid, TerraformingParameters parameters, AtmosphereState atmosphere)
    {
        parameters.Validate(grid.Cells.Count);
        this.grid = grid; this.parameters = parameters; this.atmosphere = atmosphere;
        plans = parameters.Installations.ToArray();
        energy = Enumerable.Repeat(parameters.InitialEnergyJoulesPerSquareMeter, grid.Cells.Count).ToArray();
        material = Enumerable.Repeat(parameters.InitialMaterialKilogramsPerSquareMeter, grid.Cells.Count).ToArray();
        stored = new double[grid.Cells.Count]; transit = new double[grid.Cells.Count];
        started = Enumerable.Repeat(double.NaN, plans.Length).ToArray();
        used = new double[plans.Length]; processed = new double[plans.Length];
        status = Enumerable.Repeat("Gepland", plans.Length).ToArray();
        transportCost = plans.Select(p => p.ProcessingJoulesPerKilogram + Distance(p) * p.TransportJoulesPerKilogramMeter).ToArray();
    }

    public double StoredEnergyChange => energy.Average() - parameters.InitialEnergyJoulesPerSquareMeter;

    public void Advance(double seconds, double[] enthalpy, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (seconds == 0) return;
        var end = elapsed + seconds;
        while (shipments.TryPeek(out var shipment, out var arrival) && arrival <= end)
        {
            shipments.Dequeue();
            transit[shipment.Source] = Math.Max(0, transit[shipment.Source] - shipment.Mass);
            atmosphere.DeliverCarbonDioxide(shipment.Destination, shipment.Mass);
            delivered.Add(shipment.Mass);
        }
        for (var i = 0; i < plans.Length; i++) AdvanceInstallation(i, end, enthalpy);
        atmosphere.SetEngineeredCarbonDioxideInventory(stored.Sum() + transit.Sum());
        elapsed = end;
    }

    private void AdvanceInstallation(int i, double end, double[] enthalpy)
    {
        var plan = plans[i]; var cell = plan.CellIndex;
        if (elapsed >= plan.EndDay * 86400) { status[i] = "Gestopt"; return; }
        if (end <= plan.StartDay * 86400) return;
        if (plan.PowerWattsPerSquareMeter == 0) { status[i] = "Uitgeschakeld"; return; }
        if (double.IsNaN(started[i]))
        {
            if (material[cell] < plan.ConstructionMaterialKilogramsPerSquareMeter) { status[i] = "Geen bouwmateriaal"; return; }
            if (energy[cell] < plan.ConstructionEnergyJoulesPerSquareMeter) { status[i] = "Geen energie"; return; }
            material[cell] -= plan.ConstructionMaterialKilogramsPerSquareMeter;
            built.Add(plan.ConstructionMaterialKilogramsPerSquareMeter);
            Spend(i, plan.ConstructionEnergyJoulesPerSquareMeter, enthalpy);
            started[i] = Math.Max(elapsed, plan.StartDay * 86400);
        }
        var ready = started[i] + plan.ConstructionDays * 86400;
        var activeSeconds = Math.Max(0, Math.Min(end, plan.EndDay * 86400) - Math.Max(elapsed, ready));
        if (activeSeconds == 0) { status[i] = "In aanbouw"; return; }
        var availableEnergy = Math.Min(energy[cell], plan.PowerWattsPerSquareMeter * activeSeconds);
        if (availableEnergy <= 0) { status[i] = "Geen energie"; return; }
        status[i] = "Actief";
        if (plan.Kind == TerraformInstallation.Heater) { Spend(i, availableEnergy, enthalpy); return; }
        var cost = plan.Kind == TerraformInstallation.Transport ? transportCost[i] : plan.ProcessingJoulesPerKilogram;
        var amount = Math.Min(plan.MaximumThroughputKilogramsPerSquareMeterPerDay * activeSeconds / 86400, availableEnergy / cost);
        if (plan.Kind == TerraformInstallation.Capture)
        {
            var space = Math.Max(0, parameters.StorageCapacityKilogramsPerSquareMeter - stored[cell]);
            if (space == 0) { status[i] = "Opslag vol"; return; }
            amount = atmosphere.CaptureCarbonDioxide(cell, Math.Min(amount, space));
            stored[cell] += amount; captured.Add(amount);
        }
        else
        {
            amount = Math.Min(amount, stored[cell]);
            stored[cell] -= amount; transit[cell] += amount;
            if (amount > 0) shipments.Enqueue((cell, plan.DestinationCellIndex, amount), end + plan.TransportDays * 86400);
        }
        if (amount <= 0) { status[i] = "Geen aanvoer"; return; }
        processed[i] += amount;
        Spend(i, amount * cost, enthalpy);
    }

    private void Spend(int installation, double joules, double[] enthalpy)
    {
        var cell = plans[installation].CellIndex;
        energy[cell] = Math.Max(0, energy[cell] - joules);
        enthalpy[cell] += joules; used[installation] += joules; spent.Add(joules);
    }

    private double Distance(TerraformInstallation plan)
    {
        var first = grid.Cells[plan.CellIndex]; var second = grid.Cells[plan.DestinationCellIndex];
        var cosine = Math.Sin(first.LatitudeRadians) * Math.Sin(second.LatitudeRadians)
            + Math.Cos(first.LatitudeRadians) * Math.Cos(second.LatitudeRadians) * Math.Cos(first.LongitudeRadians - second.LongitudeRadians);
        return grid.RadiusMeters * Math.Acos(Math.Clamp(cosine, -1, 1));
    }

    public TerraformingSnapshot Snapshot() => new()
    {
        Installations = Array.AsReadOnly(plans.Select((p, i) => new TerraformInstallationSnapshot
        {
            Name = p.Name, Kind = p.Kind, CellIndex = p.CellIndex, DestinationCellIndex = p.DestinationCellIndex, Status = status[i],
            ConstructionFraction = double.IsNaN(started[i]) ? 0 : p.ConstructionDays == 0 ? 1
                : Math.Clamp((Math.Min(elapsed, p.EndDay * 86400) - started[i]) / (p.ConstructionDays * 86400), 0, 1),
            ProcessedKilograms = processed[i] * grid.CellAreaSquareMeters, EnergyUsedJoules = used[i] * grid.CellAreaSquareMeters,
        }).ToArray()),
        RemainingEnergyJoulesPerSquareMeter = Array.AsReadOnly(energy.ToArray()),
        StoredCarbonDioxideKilogramsPerSquareMeter = Array.AsReadOnly(stored.ToArray()),
        InTransitCarbonDioxideKilogramsPerSquareMeter = Array.AsReadOnly(transit.ToArray()),
        RemainingMaterialKilogramsPerSquareMeter = Array.AsReadOnly(material.ToArray()),
        TotalRemainingEnergyJoules = energy.Sum() * grid.CellAreaSquareMeters,
        TotalUsedEnergyJoules = spent.Value * grid.CellAreaSquareMeters,
        TotalRemainingMaterialKilograms = material.Sum() * grid.CellAreaSquareMeters,
        TotalBuiltMaterialKilograms = built.Value * grid.CellAreaSquareMeters,
        TotalStoredCarbonDioxideKilograms = stored.Sum() * grid.CellAreaSquareMeters,
        TotalInTransitCarbonDioxideKilograms = transit.Sum() * grid.CellAreaSquareMeters,
        CumulativeCapturedKilograms = captured.Value * grid.CellAreaSquareMeters,
        CumulativeDeliveredKilograms = delivered.Value * grid.CellAreaSquareMeters,
        EnergyBudgetErrorJoulesPerSquareMeter = (energy.Sum() + spent.Value) / energy.Length - parameters.InitialEnergyJoulesPerSquareMeter,
        MaterialBudgetErrorKilograms = (material.Sum() + built.Value - parameters.InitialMaterialKilogramsPerSquareMeter * material.Length) * grid.CellAreaSquareMeters,
        LogisticsBudgetErrorKilograms = (stored.Sum() + transit.Sum() + delivered.Value - captured.Value) * grid.CellAreaSquareMeters,
    };
}
