using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Desktop;

internal sealed class ExperimentGraph : IDisposable
{
    private readonly Texture2D pixel;
    public ExperimentGraph(GraphicsDevice device)
    {
        pixel = new Texture2D(device, 1, 1);
        pixel.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch batch, PixelTextRenderer text, Rectangle area, ExperimentResult current, ExperimentResult? previous)
    {
        batch.Draw(pixel, area, new Color(15, 25, 36, 245));
        var samples = current.Samples.Concat(previous?.Samples ?? []).ToArray();
        var minimum = samples.Min(s => Math.Min(s.TemperatureKelvin, s.EquilibriumTemperatureKelvin)) - 3;
        var maximum = samples.Max(s => Math.Max(s.TemperatureKelvin, s.EquilibriumTemperatureKelvin)) + 3;
        var days = Math.Max(current.Scenario.DurationDays, previous?.Scenario.DurationDays ?? 0);
        var plot = new Rectangle(area.X + 44, area.Y + 49, area.Width - 66, area.Height - 93);
        var mint = new Color(139, 216, 191);
        var gold = new Color(217, 179, 119);
        var muted = new Color(146, 165, 183);
        var legend = previous is null ? "TEMPERATURE C / EQUILIBRIUM" : "TEMPERATURE C / EQUILIBRIUM / PREVIOUS";
        text.Draw(batch, legend, new Vector2(area.X + 14, area.Y + 14), mint, 1);
        for (var tick = 0; tick < 5; tick++)
        {
            var y = plot.Y + plot.Height * tick / 4;
            Line(batch, new Vector2(plot.X, y), new Vector2(plot.Right, y), new Color(41, 57, 72), 1);
            var celsius = maximum - (maximum - minimum) * tick / 4 - 273.15;
            text.Draw(batch, celsius.ToString("F0", System.Globalization.CultureInfo.InvariantCulture), new Vector2(area.X + 7, y - 3), muted, 1);
        }

        Vector2 Point(int day, double kelvin) => new(plot.X + plot.Width * (float)day / days,
            plot.Bottom - plot.Height * (float)((kelvin - minimum) / (maximum - minimum)));
        void Curve(ExperimentResult result, Color color, bool equilibrium)
        {
            for (var index = 1; index < result.Samples.Count; index++)
            {
                var first = result.Samples[index - 1];
                var second = result.Samples[index];
                if (equilibrium)
                {
                    Line(batch, Point(first.Day, first.EquilibriumTemperatureKelvin), Point(second.Day, first.EquilibriumTemperatureKelvin), color, 1);
                    Line(batch, Point(second.Day, first.EquilibriumTemperatureKelvin), Point(second.Day, second.EquilibriumTemperatureKelvin), color, 1);
                }
                else Line(batch, Point(first.Day, first.TemperatureKelvin), Point(second.Day, second.TemperatureKelvin), color, 2);
            }
        }

        if (previous is not null) Curve(previous, new Color(158, 145, 212), false);
        Curve(current, gold, true);
        Curve(current, mint, false);
        text.Draw(batch, "0", new Vector2(plot.X, plot.Bottom + 12), muted, 1);
        text.Draw(batch, "DAYS", new Vector2(plot.Center.X - 12, plot.Bottom + 12), muted, 1);
        var endDay = days.ToString(System.Globalization.CultureInfo.InvariantCulture);
        text.Draw(batch, endDay, new Vector2(plot.Right - endDay.Length * 6, plot.Bottom + 12), muted, 1);
        var last = current.Samples[^1];
        text.Draw(batch, $"FINAL: {last.TemperatureKelvin:F2} K / {current.Samples.Count} SAMPLES", new Vector2(area.X + 14, area.Bottom - 18), mint, 1);
    }

    private void Line(SpriteBatch batch, Vector2 start, Vector2 end, Color color, int width)
    {
        var difference = end - start;
        if (difference.LengthSquared() == 0) return;
        batch.Draw(pixel, start, null, color, MathF.Atan2(difference.Y, difference.X), Vector2.Zero,
            new Vector2(difference.Length(), width), SpriteEffects.None, 0);
    }

    public void Dispose() => pixel.Dispose();
}
