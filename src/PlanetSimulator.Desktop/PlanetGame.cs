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
    private SimulationSession session = new(regional: new RegionalParameters(), surface: new SurfaceParameters());
    private readonly CancellationTokenSource lifetime = new();
    private SimulationClock Clock => session.Clock;
    private SpriteBatch spriteBatch = null!;
    private PixelTextRenderer text = null!;
    private ExperimentGraph graph = null!;
    private ExperimentRunner? experiment;
    private ExperimentResult? result, previousResult;
    private ExperimentScenario? loadedScenario;
    private string experimentStatus = "G: RUN EXPERIMENT / S: SAVE / L: LOAD";
    private readonly SphereMesh sphere = new();
    private BasicEffect effect = null!;
    private Texture2D surface = null!;
    private KeyboardState previousKeyboard;
    private MouseState previousMouse;
    private float yaw = 0.5f;
    private float pitch = 0.25f;
    private float distance = 3.5f;
    private bool wireframe;
    private bool temperatureMap;
    private bool atmosphere = true;
    private float relief = 1;
    private RealisticPlanetRenderer? realisticRenderer;
    private SurfaceReservoirs? renderedSurface;
    private Color[] decorativePixels = [];
    private Color[] mapPixels = [];
    private int[] textureCells = [];
    private bool[] textureBorders = [];
    private long textureTick = -1;
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
        if (Environment.GetEnvironmentVariable("PLANET_SIMULATOR_WARM_CAPTURE") == "1") RestartSurface(285);
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
        mapPixels = new Color[pixels.Length];
        textureCells = new int[pixels.Length];
        textureBorders = new bool[pixels.Length];
        var textureGrid = new SphericalGrid();
        for (var y = 0; y < 128; y++)
        {
            for (var x = 0; x < 256; x++)
            {
                var textureIndex = y * 256 + x;
                textureCells[textureIndex] = textureGrid.GetCellIndex(Math.PI / 2 - (y + 0.5) * Math.PI / 128, (x + 0.5) * Math.Tau / 256);
                textureBorders[textureIndex] = x > 0 && textureCells[textureIndex] != textureCells[textureIndex - 1]
                    || y > 0 && textureCells[textureIndex] != textureCells[textureIndex - 256];
                var grid = x % 16 == 0 || y % 16 == 0;
                var band = (float)(0.5 + 0.5 * Math.Sin(x * Math.Tau / 256 * 3 + y * 0.04));
                pixels[y * 256 + x] = grid ? new Color(50, 76, 92) : Color.Lerp(new Color(95, 133, 153), new Color(166, 133, 96), band);
            }
        }

        decorativePixels = pixels;
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
        if (Pressed(keyboard, Keys.D4)) Clock.Speed = session.Regional is null ? 604800 : 86400;
        if (Pressed(keyboard, Keys.A)) ChangeForcing(0, 0.05);
        if (Pressed(keyboard, Keys.Z)) ChangeForcing(0, -0.05);
        if (Pressed(keyboard, Keys.PageUp)) ChangeForcing(0.1, 0);
        if (Pressed(keyboard, Keys.PageDown)) ChangeForcing(-0.1, 0);
        if (Pressed(keyboard, Keys.C))
        {
            session.SelectModel(session.Regional is null ? new RegionalParameters() : null, lifetime.Token);
            if (session.Regional is not null) Clock.Speed = Math.Min(Clock.Speed, 86400);
            loadedScenario = null; textureTick = -1;
        }
        if (Pressed(keyboard, Keys.B) && session.Regional is { } surfaceModel)
        {
            session.SelectSurface(surfaceModel.Surface is null ? new SurfaceParameters() : null, lifetime.Token);
            loadedScenario = null; textureTick = -1;
        }
        if (Pressed(keyboard, Keys.T)) { temperatureMap = !temperatureMap; textureTick = -1; }
        if (Pressed(keyboard, Keys.N)) atmosphere = !atmosphere;
        if (Pressed(keyboard, Keys.V)) relief = relief == 0 ? 1 : 0;
        if (Pressed(keyboard, Keys.P)) RestartSurface(session.Climate.Parameters.InitialTemperatureKelvin < 273.15 ? 285 : 230);
        if (Pressed(keyboard, Keys.O)) ChangeRegional(5, 0);
        if (Pressed(keyboard, Keys.K)) ChangeRegional(-5, 0);
        if (Pressed(keyboard, Keys.H)) ChangeRegional(0, 0.1);
        if (Pressed(keyboard, Keys.J)) ChangeRegional(0, -0.1);
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
            for (var day = 0; day < (experiment.Scenario.Regional is null ? 7 : 1) && !experiment.IsComplete; day++) experiment.AdvanceDay(lifetime.Token);
            experimentStatus = $"EXPERIMENT: {experiment.CompletedDays}/{experiment.Scenario.DurationDays} DAYS";
            if (experiment.IsComplete)
            {
                previousResult = result;
                result = experiment.GetResult();
                experiment = null;
                experimentStatus = "COMPLETE / G: RERUN / S: SAVE / L: LOAD";
            }
        }

        if (Environment.GetEnvironmentVariable("PLANET_SIMULATOR_REGIONAL_CAPTURE") == "1" && textureTick == -1)
            for (var day = 0; day < 10; day++) session.AdvanceClock(1, lifetime.Token);
        session.AdvanceClock(Math.Min(gameTime.ElapsedGameTime.TotalSeconds, 60), lifetime.Token);
        if (!ReferenceEquals(renderedSurface, session.Regional?.Surface))
        {
            realisticRenderer?.Dispose();
            renderedSurface = session.Regional?.Surface;
            realisticRenderer = renderedSurface is null ? null : new RealisticPlanetRenderer(GraphicsDevice, renderedSurface, lifetime.Token);
        }
        if (temperatureMap || realisticRenderer is null) UpdateTemperatureTexture();
        Window.Title = $"PlanetSimulator | {session.Snapshot().TemperatureKelvin:F2} K | day {Clock.ElapsedSeconds / 86400:F2} | {Clock.Speed:G}x";
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
        GraphicsDevice.SamplerStates[0] = temperatureMap && session.Regional is not null ? SamplerState.PointClamp : SamplerState.LinearWrap;
        var camera = new Vector3(MathF.Cos(pitch) * MathF.Sin(yaw), MathF.Sin(pitch), MathF.Cos(pitch) * MathF.Cos(yaw)) * distance;
        var regionalView = session.Regional is not null;
        effect.LightingEnabled = !(regionalView && temperatureMap);
        if (session.Regional is { } regional)
        {
            var declination = regional.SolarDeclinationRadians(Clock.ElapsedSeconds);
            effect.DirectionalLight0.Direction = new Vector3(-(float)Math.Cos(declination), -(float)Math.Sin(declination), 0);
        }
        else effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, -0.4f, -0.5f));
        effect.World = Matrix.CreateRotationY((float)session.Planet.GetRotationRadians(Clock.ElapsedSeconds));
        effect.View = Matrix.CreateLookAt(camera, Vector3.Zero, Vector3.Up);
        effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.01f, 100);
        if (!temperatureMap && realisticRenderer is not null && session.Regional?.Snapshot().Surface is { } water)
            realisticRenderer.Draw(water, effect.World, effect.View, effect.Projection,
                new AppearanceOptions(camera, -effect.DirectionalLight0.Direction, relief, atmosphere, wireframe), lifetime.Token);
        else
        {
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, sphere.Vertices, 0, sphere.Vertices.Length, sphere.Indices, 0, sphere.Indices.Length / 3);
            }
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
        realisticRenderer?.Dispose();
        solid.Dispose();
        wire.Dispose();
        base.UnloadContent();
    }

    private void RestartSurface(double kelvin)
    {
        var climate = session.Climate.Parameters;
        var paused = Clock.IsPaused; var speed = Clock.Speed;
        session = new SimulationSession(new ClimateParameters(climate.DistanceAstronomicalUnits, climate.BondAlbedo,
            climate.ArealHeatCapacity, kelvin, climate.StellarLuminositySolarUnits), session.Regional?.Parameters ?? new RegionalParameters(), new SurfaceParameters());
        Clock.IsPaused = paused; Clock.Speed = Math.Min(speed, 86400);
        loadedScenario = null; textureTick = -1;
    }

    private void ChangeForcing(double distanceChange, double albedoChange)
    {
        var parameters = session.Climate.Parameters;
        var starDistance = Math.Clamp(parameters.DistanceAstronomicalUnits + distanceChange, 0.5, 2);
        var albedo = Math.Clamp(parameters.BondAlbedo + albedoChange, 0, 0.8);
        session.SetForcing(starDistance, albedo, lifetime.Token);
        loadedScenario = null;
    }

    private ExperimentScenario CurrentScenario() => session.Regional is { Surface: { } reservoir } waterModel
        ? ExperimentScenario.CreateSurface("Desktop water experiment", 30, session.Climate.Parameters, waterModel.Parameters, reservoir.Parameters)
        : session.Regional is { } regional
        ? ExperimentScenario.CreateRegional("Desktop regional experiment", 30, session.Climate.Parameters, regional.Parameters)
        : ExperimentScenario.Create("Desktop experiment", 365, session.Climate.Parameters);

    private void ChangeRegional(double tiltChange, double diffusionChange)
    {
        if (session.Regional is not { } model) return;
        model.SetParameters(new RegionalParameters(Math.Clamp(model.Parameters.AxialTiltDegrees + tiltChange, 0, 90),
            Math.Clamp(model.Parameters.HeatDiffusionWattsPerSquareMeterKelvin + diffusionChange, 0, 2), model.Parameters.YearDays), lifetime.Token);
        loadedScenario = null;
    }

    private void UpdateTemperatureTexture()
    {
        if (textureTick == Clock.TickCount) return;
        textureTick = Clock.TickCount;
        if (session.Regional is not { } regional || !temperatureMap && regional.Surface is null) { surface.SetData(decorativePixels); return; }
        var water = regional.Snapshot().Surface;
        for (var index = 0; index < mapPixels.Length; index++)
        {
            var cell = textureCells[index];
            var colour = TemperaturePalette.ForKelvin(regional.TemperaturesKelvin[cell]);
            if (!temperatureMap && water is not null)
            {
                var mass = water.WaterMassPerSquareMeter[cell];
                colour = mass == 0 ? new Color(118, 137, 80)
                    : Color.Lerp(new Color(25, 89, 145), new Color(225, 239, 242), (float)(water.IceMassPerSquareMeter[cell] / mass));
            }
            mapPixels[index] = textureBorders[index] ? new Color(colour.ToVector3() * 0.75f) : colour;
        }
        surface.SetData(mapPixels);
    }

    private void SaveExperiment()
    {
        try
        {
            Directory.CreateDirectory("experiments");
            File.WriteAllText("experiments/scenario.json", ScenarioJson.Serialize(result?.Scenario ?? loadedScenario ?? CurrentScenario()));
            if (result is not null) File.WriteAllText("experiments/results.csv", result.ToCsv());
            if (result is { FinalRegions.Count: > 0 }) File.WriteAllText("experiments/regions.csv", result.ToRegionalCsv());
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
        var climate = session.Snapshot();
        var mint = new Color(139, 216, 191);
        var muted = new Color(159, 177, 193);
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        text.Draw(spriteBatch, "PLANETSIMULATOR / MILESTONE 6", new Vector2(24, 24), mint);
        var lines = new[]
        {
            $"TEMPERATURE: {F(climate.TemperatureKelvin)} K / {F(climate.TemperatureKelvin - 273.15)} C",
            $"RADIATIVE EQ: {F(climate.EquilibriumTemperatureKelvin)} K",
            $"ABSORBED: {F(climate.AbsorbedWattsPerSquareMeter)} W/M2",
            $"EMITTED: {F(climate.EmittedWattsPerSquareMeter)} W/M2",
            $"NET: {F(climate.NetWattsPerSquareMeter)} W/M2",
            $"DISTANCE: {F(session.Climate.Parameters.DistanceAstronomicalUnits)} AU / ALBEDO: {F(session.Climate.Parameters.BondAlbedo * 100)}%",
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
        if (climate.Regional is { } region)
        {
            text.Draw(spriteBatch, $"CELLS: 288 / MIN-MAX: {F(region.MinimumTemperatureKelvin)}-{F(region.MaximumTemperatureKelvin)} K", new Vector2(24, 275), mint, 1);
            text.Draw(spriteBatch, $"TILT: {F(session.Regional!.Parameters.AxialTiltDegrees)} / D: {F(session.Regional.Parameters.HeatDiffusionWattsPerSquareMeterKelvin)}",
                new Vector2(24, 292), mint, 1);
            text.Draw(spriteBatch, $"BUDGET ERROR: {region.BudgetErrorJoulesPerSquareMeter:G3} J/M2", new Vector2(24, 309), muted, 1);
        }
        if (climate.Regional?.Surface is { } water)
        {
            text.Draw(spriteBatch, $"WATER: {water.TotalWaterMassKilograms:G3} KG / LIQUID: {F(water.LiquidMassFraction * 100)}%",
                new Vector2(24, 326), mint, 1);
            text.Draw(spriteBatch, $"MASS ERROR: {water.WaterMassErrorKilograms:G3} KG", new Vector2(24, 343), muted, 1);
        }
        var bottom = GraphicsDevice.Viewport.Height - 113;
        text.Draw(spriteBatch, "DRAG: ORBIT / WHEEL: ZOOM / SPACE: PAUSE / R: RESET", new Vector2(24, bottom), muted);
        text.Draw(spriteBatch, "1-4: SPEED / A-Z: ALBEDO / PAGE UP-DOWN: DISTANCE / W: WIREFRAME", new Vector2(24, bottom + 23), muted);
        text.Draw(spriteBatch, "C: MODEL / B: WATER / T: MAP / O-K: TILT / H-J: TRANSPORT", new Vector2(24, bottom + 46), muted);
        text.Draw(spriteBatch, "N: OPTICAL ATMOSPHERE / V: RELIEF / P: COLD-WARM START", new Vector2(24, bottom + 69), mint);
        spriteBatch.End();
    }

    private static string F(double value) => value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

    private bool Pressed(KeyboardState keyboard, Keys key) => keyboard.IsKeyDown(key) && previousKeyboard.IsKeyUp(key);
}
