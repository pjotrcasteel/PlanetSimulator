namespace PlanetSimulator.Simulation;

/// <summary>
/// Uses uniform sin(latitude) and longitude intervals; each cell has the same exact spherical area.
/// Latitude rows run north to south; longitude wraps periodically at zero and two pi.
/// </summary>
public sealed class SphericalGrid
{
    public const int DefaultLatitudeBands = 12;
    public const int DefaultLongitudeBands = 24;
    public double RadiusMeters { get; }
    public int LatitudeBands { get; }
    public int LongitudeBands { get; }
    public IReadOnlyList<SurfaceCell> Cells { get; }
    public double CellAreaSquareMeters => RadiusMeters * RadiusMeters * 4 * Math.PI / Cells.Count;

    public SphericalGrid(double radiusMeters = 6371000, int latitudeBands = DefaultLatitudeBands, int longitudeBands = DefaultLongitudeBands)
    {
        if (!double.IsFinite(radiusMeters) || radiusMeters <= 0) throw new ArgumentOutOfRangeException(nameof(radiusMeters));
        if (latitudeBands is < 2 or > 96) throw new ArgumentOutOfRangeException(nameof(latitudeBands));
        if (longitudeBands is < 4 or > 192) throw new ArgumentOutOfRangeException(nameof(longitudeBands));
        RadiusMeters = radiusMeters;
        LatitudeBands = latitudeBands;
        LongitudeBands = longitudeBands;
        var cells = new SurfaceCell[latitudeBands * longitudeBands];
        for (var row = 0; row < latitudeBands; row++)
        {
            var latitude = Math.Asin(1 - (row + 0.5) * 2 / latitudeBands);
            for (var column = 0; column < longitudeBands; column++)
            {
                var index = row * longitudeBands + column;
                cells[index] = new SurfaceCell(index, row, column, latitude, (column + 0.5) * Math.Tau / longitudeBands, 4 * Math.PI / cells.Length);
            }
        }
        Cells = Array.AsReadOnly(cells);
    }

    public int GetCellIndex(double latitudeRadians, double longitudeRadians)
    {
        if (!double.IsFinite(latitudeRadians) || Math.Abs(latitudeRadians) > Math.PI / 2) throw new ArgumentOutOfRangeException(nameof(latitudeRadians));
        if (!double.IsFinite(longitudeRadians)) throw new ArgumentOutOfRangeException(nameof(longitudeRadians));
        var row = Math.Clamp((int)((1 - Math.Sin(latitudeRadians)) * LatitudeBands / 2), 0, LatitudeBands - 1);
        var longitude = (longitudeRadians % Math.Tau + Math.Tau) % Math.Tau;
        var column = Math.Min((int)(longitude / Math.Tau * LongitudeBands), LongitudeBands - 1);
        return row * LongitudeBands + column;
    }
}
