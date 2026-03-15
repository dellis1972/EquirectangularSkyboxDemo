using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EquirectangularSkyboxDemo;

/// <summary>
/// Generates a unit sphere mesh (radius 1) procedurally.
/// Uses <see cref="VertexPosition"/> vertices — only the POSITION semantic is
/// needed because the skybox shader derives its view direction from that.
/// </summary>
public class SphereMesh : IDisposable
{
    public VertexBuffer VertexBuffer { get; }
    public IndexBuffer IndexBuffer { get; }
    public int PrimitiveCount { get; }

    /// <summary>
    /// Creates a unit sphere centred at the origin.
    /// </summary>
    /// <param name="device">The graphics device.</param>
    /// <param name="slices">Longitudinal subdivisions (default 32).</param>
    /// <param name="stacks">Latitudinal subdivisions (default 16).</param>
    public SphereMesh(GraphicsDevice device, int slices = 32, int stacks = 16)
    {
        // ---------------------------------------------------------------
        // Vertices
        // Layout: top pole | (stacks-1) rings of (slices+1) verts | bottom pole
        // ---------------------------------------------------------------
        int vertexCount = (stacks - 1) * (slices + 1) + 2;
        var vertices = new VertexPosition[vertexCount];
        int v = 0;

        // Top pole (y = +1)
        vertices[v++] = new VertexPosition(Vector3.Up);

        // Intermediate rings
        for (int stack = 1; stack < stacks; stack++)
        {
            float phi    = MathHelper.Pi * stack / stacks; // 0 … π (top → bottom)
            float sinPhi = MathF.Sin(phi);
            float cosPhi = MathF.Cos(phi);

            for (int slice = 0; slice <= slices; slice++)
            {
                float theta    = MathHelper.TwoPi * slice / slices; // 0 … 2π
                float sinTheta = MathF.Sin(theta);
                float cosTheta = MathF.Cos(theta);

                vertices[v++] = new VertexPosition(new Vector3(
                    sinPhi * cosTheta,
                    cosPhi,
                    sinPhi * sinTheta));
            }
        }

        // Bottom pole (y = -1)
        int bottomPoleIndex = v;
        vertices[v] = new VertexPosition(Vector3.Down);

        // ---------------------------------------------------------------
        // Indices (16-bit)
        // ---------------------------------------------------------------
        int indexCount = slices * 3                    // top cap
                       + slices * 3                    // bottom cap
                       + (stacks - 2) * slices * 6;   // middle quads (2 tris)

        var indices = new short[indexCount];
        int idx = 0;

        // Top cap — triangle fan from pole to first ring
        for (int slice = 0; slice < slices; slice++)
        {
            indices[idx++] = 0;
            indices[idx++] = (short)(1 + slice + 1);
            indices[idx++] = (short)(1 + slice);
        }

        // Middle quads
        for (int stack = 0; stack < stacks - 2; stack++)
        {
            int topRing = 1 + stack * (slices + 1);
            int botRing = 1 + (stack + 1) * (slices + 1);

            for (int slice = 0; slice < slices; slice++)
            {
                indices[idx++] = (short)(topRing + slice);
                indices[idx++] = (short)(botRing + slice + 1);
                indices[idx++] = (short)(botRing + slice);

                indices[idx++] = (short)(topRing + slice);
                indices[idx++] = (short)(topRing + slice + 1);
                indices[idx++] = (short)(botRing + slice + 1);
            }
        }

        // Bottom cap — triangle fan from last ring to bottom pole
        {
            int lastRing = 1 + (stacks - 2) * (slices + 1);
            for (int slice = 0; slice < slices; slice++)
            {
                indices[idx++] = (short)bottomPoleIndex;
                indices[idx++] = (short)(lastRing + slice);
                indices[idx++] = (short)(lastRing + slice + 1);
            }
        }

        // ---------------------------------------------------------------
        // Upload to GPU
        // ---------------------------------------------------------------
        VertexBuffer = new VertexBuffer(device, VertexPosition.VertexDeclaration,
                                        vertexCount, BufferUsage.WriteOnly);
        VertexBuffer.SetData(vertices);

        IndexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits,
                                      indexCount, BufferUsage.WriteOnly);
        IndexBuffer.SetData(indices);

        PrimitiveCount = indexCount / 3;
    }

    public void Dispose()
    {
        VertexBuffer?.Dispose();
        IndexBuffer?.Dispose();
    }
}
