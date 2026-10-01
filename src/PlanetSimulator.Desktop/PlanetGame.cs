using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PlanetSimulator.Simulation;

namespace PlanetSimulator.Desktop;

/// <summary>
/// Presents milestone one: a lit sphere, orbit camera and independent simulation clock.
/// </summary>
public sealed class PlanetGame : Game
{
    private readonly GraphicsDeviceManager graphics;
    private readonly SimulationClock clock = new();
    private readonly Planet planet = new();
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
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
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

        if (Pressed(keyboard, Keys.Space)) clock.IsPaused = !clock.IsPaused;
        if (Pressed(keyboard, Keys.D1)) clock.Speed = 1;
        if (Pressed(keyboard, Keys.D2)) clock.Speed = 3600;
        if (Pressed(keyboard, Keys.D3)) clock.Speed = 86400;
        if (Pressed(keyboard, Keys.W)) wireframe = !wireframe;
        if (Pressed(keyboard, Keys.R))
        {
            clock.Reset();
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

        clock.Advance(Math.Min(gameTime.ElapsedGameTime.TotalSeconds, 60), CancellationToken.None);
        Window.Title = $"PlanetSimulator | day {clock.ElapsedSeconds / 86400:F2} | {clock.Speed:G}x | {(clock.IsPaused ? "PAUSED" : "RUNNING")}"
            + " | drag: orbit / wheel: zoom / space: pause / 1-3: speed / W: mesh / R: reset";
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
        effect.World = Matrix.CreateRotationY((float)planet.GetRotationRadians(clock.ElapsedSeconds));
        effect.View = Matrix.CreateLookAt(camera, Vector3.Zero, Vector3.Up);
        effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.01f, 100);
        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, sphere.Vertices, 0, sphere.Vertices.Length, sphere.Indices, 0, sphere.Indices.Length / 3);
        }

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
        effect.Dispose();
        surface.Dispose();
        solid.Dispose();
        wire.Dispose();
        base.UnloadContent();
    }

    private bool Pressed(KeyboardState keyboard, Keys key) => keyboard.IsKeyDown(key) && previousKeyboard.IsKeyUp(key);
}
