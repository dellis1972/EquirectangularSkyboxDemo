using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EquirectangularSkyboxDemo;

/// <summary>
/// Renders an equirectangular panorama texture as a full-sphere skybox.
/// The camera is always treated as being at the origin — translation is
/// stripped from the view matrix before the shader is called.
/// </summary>
public class EquirectangularSkyboxRenderer : IDisposable
{
    private readonly GraphicsDevice _gd;
    private readonly Effect _effect;
    private readonly SphereMesh _sphere;

    private static readonly SamplerState SkyboxSampler = new SamplerState
    {
        AddressU = TextureAddressMode.Wrap,
        AddressV = TextureAddressMode.Clamp,
        Filter   = TextureFilter.Linear,
    };

    private static readonly RasterizerState WireframeRasterizer = new RasterizerState
    {
        FillMode = FillMode.WireFrame,
        CullMode = CullMode.None,
    };

    private BasicEffect? _wireEffect;

    /// <summary>
    /// The equirectangular panorama texture to display as the sky.
    /// </summary>
    public Texture2D? SkyTexture { get; set; }

    /// <summary>
    /// Optional yaw rotation (radians) applied to the panorama, allowing you
    /// to spin the sky without moving the camera.
    /// </summary>
    public float YawOffset { get; set; } = 0f;

    /// <summary>When true, draws a wireframe overlay of the sphere mesh.</summary>
    public bool ShowWireframe { get; set; } = false;

    /// <summary>Wireframe line colour.</summary>
    public Color WireframeColor { get; set; } = new Color(0f, 0f, 1f, 0.2f);

    public EquirectangularSkyboxRenderer(GraphicsDevice graphicsDevice, Effect effect,
                                          int slices = 32, int stacks = 16)
    {
        _gd     = graphicsDevice;
        _effect = effect;
        _sphere = new SphereMesh(graphicsDevice, slices, stacks);
    }

    /// <summary>
    /// Draws the skybox.  Call this FIRST in your Draw method, before any
    /// scene geometry.
    /// </summary>
    public void Draw(Matrix view, Matrix projection)
    {
        // --- Save render states -----------------------------------------
        var prevDepth   = _gd.DepthStencilState;
        var prevRaster  = _gd.RasterizerState;
        var prevSampler = _gd.SamplerStates[0];

        // --- Set render states ------------------------------------------
        _gd.DepthStencilState = DepthStencilState.None;
        _gd.RasterizerState   = RasterizerState.CullCounterClockwise; // inside sphere
        _gd.SamplerStates[0]  = SkyboxSampler;

        // Strip translation from view matrix so camera is always at origin.
        Matrix rotationOnly = view;
        rotationOnly.M41 = 0f;
        rotationOnly.M42 = 0f;
        rotationOnly.M43 = 0f;
        rotationOnly.M44 = 1f;

        // Apply optional yaw offset to the rotation matrix.
        if (YawOffset != 0f)
            rotationOnly = Matrix.CreateRotationY(YawOffset) * rotationOnly;

        Matrix rotProj = rotationOnly * projection;

        _effect.Parameters["RotationProjection"].SetValue(rotProj);
        _effect.Parameters["SkyMap"].SetValue(SkyTexture);

        _gd.SetVertexBuffer(_sphere.VertexBuffer);
        _gd.Indices = _sphere.IndexBuffer;

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _gd.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                baseVertex: 0,
                startIndex: 0,
                primitiveCount: _sphere.PrimitiveCount);
        }

        // --- Optional wireframe overlay ---------------------------------
        if (ShowWireframe)
        {
            _wireEffect ??= new BasicEffect(_gd)
            {
                VertexColorEnabled = false,
                TextureEnabled     = false,
                LightingEnabled    = false,
            };

            _wireEffect.DiffuseColor = WireframeColor.ToVector3();
            _wireEffect.World        = Matrix.Identity;
            _wireEffect.View         = rotationOnly;
            _wireEffect.Projection   = projection;

            _gd.RasterizerState = WireframeRasterizer;

            foreach (EffectPass pass in _wireEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _gd.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    baseVertex: 0,
                    startIndex: 0,
                    primitiveCount: _sphere.PrimitiveCount);
            }
        }

        // --- Clear depth buffer so scene geometry renders on top --------
        _gd.Clear(ClearOptions.DepthBuffer, Color.Black, 1f, 0);

        // --- Restore render states --------------------------------------
        _gd.DepthStencilState = prevDepth;
        _gd.RasterizerState   = prevRaster;
        _gd.SamplerStates[0]  = prevSampler;
    }

    public void Dispose()
    {
        _sphere?.Dispose();
        _wireEffect?.Dispose();
        // Note: SkyboxSampler is shared/static — it is intentionally not disposed here.
    }
}
