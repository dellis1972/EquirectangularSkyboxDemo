using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EquirectangularSkyboxDemo;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;

    private QuaternionCamera?              _camera;
    private EquirectangularSkyboxRenderer? _skybox;
    private Texture2D?                     _skyTexture;
    private SpriteBatch?                   _spriteBatch;

    // Simple reference cube drawn with BasicEffect
    private BasicEffect?    _basicEffect;
    private VertexBuffer?   _cubeVB;
    private IndexBuffer?    _cubeIB;
    private const int       CubePrimitiveCount = 12;

    private MouseState _prevMouse;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth  = 1280,
            PreferredBackBufferHeight = 720,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible        = false;
        Window.Title          = "Equirectangular Skybox Demo — WASD + Mouse | ESC to quit";
    }

    protected override void Initialize()
    {
        _camera = new QuaternionCamera(
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height);

        // Centre the mouse to avoid a large first-frame delta.
        Mouse.SetPosition(
            GraphicsDevice.Viewport.Width  / 2,
            GraphicsDevice.Viewport.Height / 2);
        _prevMouse = Mouse.GetState();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        // --- Skybox effect (compiled by MGCB) ---------------------------
        var skyEffect = Content.Load<Effect>("Effects/EquirectangularSkybox");

        // --- Procedural equirectangular sky texture ---------------------
        //_skyTexture = Content.Load<Texture2D>("Textures/HDR_blue_nebulae-1"); //earthlike_planet_close //GenerateSpaceTexture(2048, 1024);
        _skyTexture = Content.Load<Texture2D>("Textures/earthlike_planet_close");

        // --- Skybox renderer --------------------------------------------
        _skybox = new EquirectangularSkyboxRenderer(GraphicsDevice, skyEffect);
        _skybox.SkyTexture = _skyTexture;
        _skybox.ShowWireframe = false;

        // --- Reference cube (BasicEffect + hand-built buffers) ----------
        BuildReferenceCube();
    }

    protected override void Update(GameTime gameTime)
    {
        var kb    = Keyboard.GetState();
        var mouse = Mouse.GetState();

        if (kb.IsKeyDown(Keys.Escape))
            Exit();

        _camera!.Update(gameTime, kb, mouse, IsActive);

        // Re-centre mouse so delta accumulation is always relative to centre
        if (IsActive)
            Mouse.SetPosition(
                GraphicsDevice.Viewport.Width  / 2,
                GraphicsDevice.Viewport.Height / 2);

        _prevMouse = mouse;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // 1. Draw skybox FIRST (renderer clears depth buffer afterward).
        _skybox!.Draw(_camera!.View, _camera.Projection);

        // 2. Draw a small reference cube at the origin so you can verify
        //    camera movement and depth work correctly.
       //DrawReferenceCube();

        base.Draw(gameTime);
    }

    // -----------------------------------------------------------------------
    // Procedural space texture (2048×1024 equirectangular)
    // -----------------------------------------------------------------------

    private Texture2D GenerateSpaceTexture(int width, int height)
    {
        var rng    = new Random(12345);
        var pixels = new Color[width * height];

        // --- Dark background with a slight nebula-blue tint ---------------
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(2, 3, 8);

        // --- Milky-way band (subtle horizontal gradient) ------------------
        for (int y = 0; y < height; y++)
        {
            // Normalise to -1..+1 (centre of texture = equator)
            float ny = (y / (float)(height - 1)) * 2f - 1f;
            // Gaussian-like band centred slightly above equator
            float bandStrength = MathF.Exp(-ny * ny * 8f) * 0.06f;

            for (int x = 0; x < width; x++)
            {
                // Add a slight horizontal variation using a cheap noise
                float noise = (float)rng.NextDouble() * bandStrength;
                int   idx   = y * width + x;
                pixels[idx] = new Color(
                    pixels[idx].R + (byte)(noise * 30),
                    pixels[idx].G + (byte)(noise * 20),
                    pixels[idx].B + (byte)(noise * 50));
            }
        }

        // --- Stars ----------------------------------------------------------
        int starCount = 8000;
        for (int s = 0; s < starCount; s++)
        {
            int   sx   = rng.Next(width);
            int   sy   = rng.Next(height);
            float brightness = 0.3f + (float)rng.NextDouble() * 0.7f;

            // Randomly tint: white, yellow, or blue-white
            int tint = rng.Next(3);
            Color starColor = tint switch
            {
                0 => new Color(brightness, brightness, brightness),             // white
                1 => new Color(brightness, brightness * 0.9f, brightness * 0.6f), // yellow
                _ => new Color(brightness * 0.8f, brightness * 0.9f, brightness), // blue-white
            };

            // Core pixel
            pixels[sy * width + sx] = starColor;

            // Soft cross glow for bright stars
            if (brightness > 0.8f)
            {
                Color dim = new Color(starColor.R / 3, starColor.G / 3, starColor.B / 3);
                if (sx > 0)         pixels[sy * width + sx - 1] = Blend(pixels[sy * width + sx - 1], dim);
                if (sx < width - 1) pixels[sy * width + sx + 1] = Blend(pixels[sy * width + sx + 1], dim);
                if (sy > 0)         pixels[(sy - 1) * width + sx] = Blend(pixels[(sy - 1) * width + sx], dim);
                if (sy < height - 1) pixels[(sy + 1) * width + sx] = Blend(pixels[(sy + 1) * width + sx], dim);
            }
        }

        var tex = new Texture2D(GraphicsDevice, width, height);
        tex.SetData(pixels);
        return tex;
    }

    private static Color Blend(Color a, Color b) =>
        new Color(
            Math.Max(a.R, b.R),
            Math.Max(a.G, b.G),
            Math.Max(a.B, b.B));

    // -----------------------------------------------------------------------
    // Reference cube
    // -----------------------------------------------------------------------

    private void BuildReferenceCube()
    {
        // A small 0.5-unit cube at the origin, coloured faces
        var verts = new VertexPositionColor[]
        {
            // Front (+Z) — red
            new(new Vector3(-0.25f, -0.25f,  0.25f), Color.Red),
            new(new Vector3( 0.25f, -0.25f,  0.25f), Color.Red),
            new(new Vector3( 0.25f,  0.25f,  0.25f), Color.Red),
            new(new Vector3(-0.25f,  0.25f,  0.25f), Color.Red),
            // Back (-Z) — green
            new(new Vector3( 0.25f, -0.25f, -0.25f), Color.Green),
            new(new Vector3(-0.25f, -0.25f, -0.25f), Color.Green),
            new(new Vector3(-0.25f,  0.25f, -0.25f), Color.Green),
            new(new Vector3( 0.25f,  0.25f, -0.25f), Color.Green),
            // Right (+X) — blue
            new(new Vector3( 0.25f, -0.25f,  0.25f), Color.Blue),
            new(new Vector3( 0.25f, -0.25f, -0.25f), Color.Blue),
            new(new Vector3( 0.25f,  0.25f, -0.25f), Color.Blue),
            new(new Vector3( 0.25f,  0.25f,  0.25f), Color.Blue),
            // Left (-X) — yellow
            new(new Vector3(-0.25f, -0.25f, -0.25f), Color.Yellow),
            new(new Vector3(-0.25f, -0.25f,  0.25f), Color.Yellow),
            new(new Vector3(-0.25f,  0.25f,  0.25f), Color.Yellow),
            new(new Vector3(-0.25f,  0.25f, -0.25f), Color.Yellow),
            // Top (+Y) — cyan
            new(new Vector3(-0.25f,  0.25f,  0.25f), Color.Cyan),
            new(new Vector3( 0.25f,  0.25f,  0.25f), Color.Cyan),
            new(new Vector3( 0.25f,  0.25f, -0.25f), Color.Cyan),
            new(new Vector3(-0.25f,  0.25f, -0.25f), Color.Cyan),
            // Bottom (-Y) — magenta
            new(new Vector3(-0.25f, -0.25f, -0.25f), Color.Magenta),
            new(new Vector3( 0.25f, -0.25f, -0.25f), Color.Magenta),
            new(new Vector3( 0.25f, -0.25f,  0.25f), Color.Magenta),
            new(new Vector3(-0.25f, -0.25f,  0.25f), Color.Magenta),
        };

        short[] idx = new short[CubePrimitiveCount * 3];
        for (int face = 0; face < 6; face++)
        {
            int vBase = face * 4;
            int iBase = face * 6;
            idx[iBase + 0] = (short)(vBase + 0);
            idx[iBase + 1] = (short)(vBase + 1);
            idx[iBase + 2] = (short)(vBase + 2);
            idx[iBase + 3] = (short)(vBase + 0);
            idx[iBase + 4] = (short)(vBase + 2);
            idx[iBase + 5] = (short)(vBase + 3);
        }

        _cubeVB = new VertexBuffer(GraphicsDevice,
            VertexPositionColor.VertexDeclaration, verts.Length, BufferUsage.WriteOnly);
        _cubeVB.SetData(verts);

        _cubeIB = new IndexBuffer(GraphicsDevice,
            IndexElementSize.SixteenBits, idx.Length, BufferUsage.WriteOnly);
        _cubeIB.SetData(idx);

        _basicEffect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled    = false,
        };
    }

    private void DrawReferenceCube()
    {
        if (_basicEffect == null || _cubeVB == null || _cubeIB == null) return;

        _basicEffect.View       = _camera!.View;
        _basicEffect.Projection = _camera.Projection;
        _basicEffect.World      = Matrix.Identity;

        GraphicsDevice.SetVertexBuffer(_cubeVB);
        GraphicsDevice.Indices = _cubeIB;

        foreach (var pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                baseVertex: 0,
                startIndex: 0,
                primitiveCount: CubePrimitiveCount);
        }
    }

    protected override void UnloadContent()
    {
        _skybox?.Dispose();
        _skyTexture?.Dispose();
        _basicEffect?.Dispose();
        _cubeVB?.Dispose();
        _cubeIB?.Dispose();
        base.UnloadContent();
    }
}
