using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Desktop;

/// <summary>
/// Presents a lit sphere and shared uniform climate model with interactive forcing controls.
/// </summary>
public sealed class PlanetGame : Game
{
    private readonly GraphicsDeviceManager graphics;
    private readonly SimulationSession session = new();
    private readonly CancellationTokenSource lifetime = new();
    private SimulationClock Clock => session.Clock;
    private SpriteBatch spriteBatch = null!;
    private PixelTextRenderer text = null!;
    private ExperimentGraph graph = null!;
    private ExperimentRunner? experiment;
    private ExperimentResult? result, previousResult;
    private ExperimentScenario? loadedScenario;
    private string experimentStatus = "G: RUN 365 DAYS / S: SAVE / L: LOAD";
    private readonly SphereMesh sphere = new();
    private BasicEffect effect = null!;
    private Texture2D surface = null!;
    private KeyboardState previousKeyboard;
    private MouseState previousMouse;
    private float yaw = 0.5f;
    private float pitch = 0.25f;
    private float distance = 3.5f;
    private bool wireframe;
    private readonly RasterizerState solid = new() { CullMode = CullMode.None };
    private readonly RasterizerState wire = new() { CullMode = CullMode.None, FillMode = FillMode.WireFrame };

    public PlanetGame()
    {
        graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 800,
            SynchronizeWithVerticalRetrace = true,
        };
        Clock.Speed = 86400;
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        text = new PixelTextRenderer(GraphicsDevice);
        graph = new ExperimentGraph(GraphicsDevice);
        if (Environment.GetEnvironmentVariable("PLANET_SIMULATOR_EXPERIMENT_CAPTURE") == "1")
            result = new ExperimentRunner(CurrentScenario()).Finish(lifetime.Token);
        effect = new BasicEffect(GraphicsDevice) { TextureEnabled = true, LightingEnabled = true, AmbientLightColor = new Vector3(0.06f) };
        effect.DirectionalLight0.Enabled = true;
        effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, -0.4f, -0.5f));
        effect.DirectionalLight0.DiffuseColor = new Vector3(1, 0.96f, 0.88f);
        effect.SpecularColor = Vector3.Zero;
        surface = new Texture2D(GraphicsDevice, 256, 128);
        var pixels = new Color[256 * 128];
        for (var y = 0; y < 128; y++)
        {
            for (var x = 0; x < 256; x++)
            {
                var grid = x % 16 == 0 || y % 16 == 0;
                var band = (float)(0.5 + 0.5 * Math.Sin(x * Math.Tau / 256 * 3 + y * 0.04));
                pixels[y * 256 + x] = grid ? new Color(50, 76, 92) : Color.Lerp(new Color(95, 133, 153), new Color(166, 133, 96), band);
            }
        }

        surface.SetData(pixels);
        effect.Texture = surface;
        previousMouse = Mouse.GetState();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (Pressed(keyboard, Keys.Space)) Clock.IsPaused = !Clock.IsPaused;
        if (Pressed(keyboard, Keys.D1)) Clock.Speed = 1;
        if (Pressed(keyboard, Keys.D2)) Clock.Speed = 3600;
        if (Pressed(keyboard, Keys.D3)) Clock.Speed = 86400;
        if (Pressed(keyboard, Keys.D4)) Clock.Speed = 604800;
        if (Pressed(keyboard, Keys.A)) ChangeForcing(0, 0.05);
        if (Pressed(keyboard, Keys.Z)) ChangeForcing(0, -0.05);
        if (Pressed(keyboard, Keys.PageUp)) ChangeForcing(0.1, 0);
        if (Pressed(keyboard, Keys.PageDown)) ChangeForcing(-0.1, 0);
        if (Pressed(keyboard, Keys.W)) wireframe = !wireframe;
        if (Pressed(keyboard, Keys.G) && experiment is null) experiment = new ExperimentRunner(loadedScenario ?? CurrentScenario());
        if (Pressed(keyboard, Keys.S)) SaveExperiment();
        if (Pressed(keyboard, Keys.L) && experiment is null) LoadExperiment();
        if (Pressed(keyboard, Keys.R))
        {
            session.Reset(lifetime.Token);
            yaw = 0.5f;
            pitch = 0.25f;
            distance = 3.5f;
        }

        if (IsActive)
        {
            if (mouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Pressed)
            {
                yaw -= (mouse.X - previousMouse.X) * 0.008f;
                pitch = MathHelper.Clamp(pitch + (mouse.Y - previousMouse.Y) * 0.008f, -1.45f, 1.45f);
            }

            distance = MathHelper.Clamp(distance - (mouse.ScrollWheelValue - previousMouse.ScrollWheelValue) * 0.002f, 1.4f, 12);
        }

        if (experiment is not null)
        {
            for (var day = 0; day < 7 && !experiment.IsComplete; day++) experiment.AdvanceDay(lifetime.Token);
            experimentStatus = $"EXPERIMENT: {experiment.CompletedDays}/{experiment.Scenario.DurationDays} DAYS";
            if (experiment.IsComplete)
            {
                previousResult = result;
                result = experiment.GetResult();
                experiment = null;
                experimentStatus = "COMPLETE / G: RERUN / S: SAVE / L: LOAD";
            }
        }

        session.Advance(Math.Min(gameTime.ElapsedGameTime.TotalSeconds, 60), lifetime.Token);
        Window.Title = $"PlanetSimulator | {session.Climate.TemperatureKelvin:F2} K | day {Clock.ElapsedSeconds / 86400:F2} | {Clock.Speed:G}x";
        previousKeyboard = keyboard;
        previousMouse = mouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (GraphicsDevice.Viewport.Width == 0 || GraphicsDevice.Viewport.Height == 0) return;
        var capturePath = Environment.GetEnvironmentVariable("PLANET_SIMULATOR_CAPTURE");
        using var capture = string.IsNullOrWhiteSpace(capturePath) ? null : new RenderTarget2D(
            GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.Depth24);
        if (capture is not null) GraphicsDevice.SetRenderTarget(capture);
        GraphicsDevice.Clear(new Color(7, 10, 18));
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.RasterizerState = wireframe ? wire : solid;
        GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
        var camera = new Vector3(MathF.Cos(pitch) * MathF.Sin(yaw), MathF.Sin(pitch), MathF.Cos(pitch) * MathF.Cos(yaw)) * distance;
        effect.World = Matrix.CreateRotationY((float)session.Planet.GetRotationRadians(Clock.ElapsedSeconds));
        effect.View = Matrix.CreateLookAt(camera, Vector3.Zero, Vector3.Up);
        effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.01f, 100);
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, sphere.Vertices, 0, sphere.Vertices.Length, sphere.Indices, 0, sphere.Indices.Length / 3);
        }

        DrawReadouts();
        if (capture is not null)
        {
            GraphicsDevice.SetRenderTarget(null);
            using var stream = File.Create(capturePath!);
            capture.SaveAsPng(stream, capture.Width, capture.Height);
            Exit();
        }

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        lifetime.Cancel();
        lifetime.Dispose();
        spriteBatch.Dispose();
        text.Dispose();
        graph.Dispose();
        effect.Dispose();
        surface.Dispose();
        solid.Dispose();
        wire.Dispose();
        base.UnloadContent();
    }

    private void ChangeForcing(double distanceChange, double albedoChange)
    {
        var parameters = session.Climate.Parameters;
        var starDistance = Math.Clamp(parameters.DistanceAstronomicalUnits + distanceChange, 0.5, 2);
        var albedo = Math.Clamp(parameters.BondAlbedo + albedoChange, 0, 0.8);
        session.Climate.SetForcing(starDistance, albedo, lifetime.Token);
        loadedScenario = null;
    }

    private ExperimentScenario CurrentScenario() => ExperimentScenario.Create("Desktop experiment", 365, session.Climate.Parameters);

    private void SaveExperiment()
    {
        try
        {
            Directory.CreateDirectory("experiments");
            File.WriteAllText("experiments/scenario.json", ScenarioJson.Serialize(result?.Scenario ?? loadedScenario ?? CurrentScenario()));
            if (result is not null) File.WriteAllText("experiments/results.csv", result.ToCsv());
            experimentStatus = "SAVED: EXPERIMENTS/SCENARIO.JSON";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            experimentStatus = "SAVE FAILED / CHECK DIRECTORY PERMISSIONS";
        }
    }

    private void LoadExperiment()
    {
        try
        {
            if (new FileInfo("experiments/scenario.json").Length > ScenarioJson.MaximumBytes) throw new ArgumentException("Scenario too large.");
            loadedScenario = ScenarioJson.Deserialize(File.ReadAllText("experiments/scenario.json"));
            experimentStatus = "SCENARIO LOADED / G: RUN";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException)
        {
            experimentStatus = "LOAD FAILED / CHECK SCENARIO.JSON";
        }
    }

    private void DrawReadouts()
    {
        var climate = session.Climate;
        var mint = new Color(139, 216, 191);
        var muted = new Color(159, 177, 193);
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        text.Draw(spriteBatch, "PLANETSIMULATOR / MILESTONE 3", new Vector2(24, 24), mint);
        var lines = new[]
        {
            $"TEMPERATURE: {F(climate.TemperatureKelvin)} K / {F(climate.TemperatureKelvin - 273.15)} C",
            $"EQUILIBRIUM: {F(climate.EquilibriumTemperatureKelvin)} K",
            $"ABSORBED: {F(climate.AbsorbedWattsPerSquareMeter)} W/M2",
            $"EMITTED: {F(climate.EmittedWattsPerSquareMeter)} W/M2",
            $"NET: {F(climate.NetWattsPerSquareMeter)} W/M2",
            $"DISTANCE: {F(climate.Parameters.DistanceAstronomicalUnits)} AU / ALBEDO: {F(climate.Parameters.BondAlbedo * 100)}%",
            $"DAY: {F(Clock.ElapsedSeconds / 86400)} / SPEED: {Clock.Speed:G}X",
            Clock.IsPaused ? "PAUSED" : "RUNNING",
        };
        for (var index = 0; index < lines.Length; index++) text.Draw(spriteBatch, lines[index], new Vector2(24, 54 + index * 23), muted);
        text.Draw(spriteBatch, experimentStatus, new Vector2(24, 254), mint, 1);
        if (result is not null && GraphicsDevice.Viewport.Width >= 900)
        {
            var width = Math.Min(470, GraphicsDevice.Viewport.Width / 2 - 40);
            graph.Draw(spriteBatch, text, new Rectangle(GraphicsDevice.Viewport.Width - width - 24, 54, width, 260), result, previousResult);
        }
        var bottom = GraphicsDevice.Viewport.Height - 92;
        text.Draw(spriteBatch, "DRAG: ORBIT / WHEEL: ZOOM / SPACE: PAUSE / R: RESET", new Vector2(24, bottom), muted);
        text.Draw(spriteBatch, "1-4: SPEED / A-Z: ALBEDO / PAGE UP-DOWN: DISTANCE / W: WIREFRAME", new Vector2(24, bottom + 23), muted);
        text.Draw(spriteBatch, "UNIFORM BLACKBODY MODEL / NO ATMOSPHERE / DECORATIVE SURFACE", new Vector2(24, bottom + 46), mint);
        spriteBatch.End();
    }

    private static string F(double value) => value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

    private bool Pressed(KeyboardState keyboard, Keys key) => keyboard.IsKeyDown(key) && previousKeyboard.IsKeyUp(key);
}
