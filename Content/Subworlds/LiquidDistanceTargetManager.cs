using System;
using System.Buffers;
using System.Threading.Tasks;
using IdolOfMadderCrimson.Core;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using BitOperations = System.Numerics.BitOperations;

namespace IdolOfMadderCrimson.Content.Subworlds;

[Autoload(Side = ModSide.Client)]
public class LiquidDistanceTargetManager : ModSystem
{
    internal readonly record struct LiquidTargetRenderProfile(Rectangle TileArea)
    {
        /// <summary>
        ///     The amount of horizontal texture samples to take along the tile area.
        /// </summary>
        public int HorizontalSamples => TileArea.Width / 2 + 1;

        /// <summary>
        ///     The amount of vertical texture samples to take along the tile area.
        /// </summary>
        public int VerticalSamples => TileArea.Height / 2 + 1;

        /// <summary>
        ///     The width of the 2D mesh grid that composes the results.
        /// </summary>
        public int MeshWidth => TileArea.Width + 1;

        /// <summary>
        ///     The height of the 2D mesh grid that composes the results.
        /// </summary>
        public int MeshHeight => TileArea.Height + 1;

        /// <summary>
        ///     The amount of vertices that will compose the resulting mesh.
        /// </summary>
        public int VertexCount => MeshWidth * MeshHeight;

        /// <summary>
        ///     The amount of indices that will compose the resulting mesh.
        /// </summary>
        public int IndexCount => TileArea.Width * TileArea.Height * 6;

        /// <summary>
        ///     The amount of primitives (in this case triangles) that compose the overall mesh.
        /// </summary>
        public int TriangleCount => TileArea.Width * TileArea.Height * 2;
    };

    private static bool prepareLiquidDistanceTarget;

    /// <summary>
    ///     The backer for <see cref="LiquidDistanceTarget"/>.
    /// </summary>
    internal static ManagedRenderTarget? liquidDistanceTarget
    {
        get;
        private set;
    }

    /// <summary>
    ///     The vertex buffer responsible for liquid distance information.
    /// </summary>
    internal static DynamicVertexBuffer Vertices
    {
        get;
        private set;
    }

    /// <summary>
    ///     The index buffer responsible for liquid distance information.
    /// </summary>
    internal static DynamicIndexBuffer Indices
    {
        get;
        private set;
    }

    /// <summary>
    ///     The render target that holds vertical liquid distance information.
    /// </summary>
    public static ManagedRenderTarget? LiquidDistanceTarget
    {
        get
        {
            prepareLiquidDistanceTarget = true;
            return liquidDistanceTarget;
        }
    }

    public override void OnModLoad()
    {
        Main.RunOnMainThread(() =>
        {
            Indices = new DynamicIndexBuffer(Main.instance.GraphicsDevice, IndexElementSize.SixteenBits, 131072, BufferUsage.None);
            Vertices = new DynamicVertexBuffer(Main.instance.GraphicsDevice, typeof(VertexPositionColorTexture), 32768, BufferUsage.None);
            liquidDistanceTarget = new ManagedRenderTarget(true, (width, height) =>
            {
                return new RenderTarget2D(Main.instance.GraphicsDevice, width, height, true, SurfaceFormat.Vector4, DepthFormat.Depth24);
            });
        });

        Main.OnPreDraw += GenerateDistanceTargetContents;
    }

    public override void OnModUnload()
    {
        Main.RunOnMainThread(() =>
        {
            Indices.Dispose();
            Vertices.Dispose();
        });
        Main.OnPreDraw -= GenerateDistanceTargetContents;
    }

    private static void GenerateDistanceTargetContents(GameTime obj)
    {
        if (!prepareLiquidDistanceTarget)
            return;

        // Ensure that the liquid usage only occurs on the frames in which it's requested, and never otherwise.
        prepareLiquidDistanceTarget = false;

        GraphicsDevice gd = Main.instance.GraphicsDevice;
        gd.SetRenderTarget(liquidDistanceTarget);
        gd.Clear(Color.Transparent);

        GenerateTargetContents();

        gd.SetRenderTarget(null);
    }

    /// <summary>
    ///     Generates the contents of the <see cref="liquidDistanceTarget"/> as needed.
    /// </summary>
    private static void GenerateTargetContents()
    {
        GraphicsDevice gd = Main.instance.GraphicsDevice;

        int left = (int)(Main.screenPosition.X / 16f);
        int top = (int)(Main.screenPosition.Y / 16f);
        int right = (int)(left + gd.Viewport.Width / 16f);
        int bottom = (int)(top + gd.Viewport.Height / 16f);
        Rectangle tileArea = new Rectangle(left, top, right - left, bottom - top);
        LiquidTargetRenderProfile renderProfile = new LiquidTargetRenderProfile(tileArea);

        // Request index and vertex arrays, using array pools to reduce garbage.
        short[] indices = ArrayPool<short>.Shared.Rent(renderProfile.IndexCount);
        VertexPositionVectorTexture[] vertices = ArrayPool<VertexPositionVectorTexture>.Shared.Rent(renderProfile.VertexCount);
        try
        {
            FillBuffers(renderProfile, vertices, indices);
            EnsureBufferSizes(renderProfile);
            Indices.SetData(indices, 0, renderProfile.IndexCount, SetDataOptions.Discard);
            Vertices.SetData(vertices, 0, renderProfile.VertexCount, SetDataOptions.Discard);
        }
        finally
        {
            // Return rented buffers.
            ArrayPool<VertexPositionVectorTexture>.Shared.Return(vertices);
            ArrayPool<short>.Shared.Return(indices);
        }

        ManagedShader shader = ShaderManager.GetShader("Luminance.StandardPrimitiveShader");
        shader.TrySetParameter("uWorldViewProjection", Matrix.CreateOrthographicOffCenter(0f, WotGUtils.ViewportSize.X, WotGUtils.ViewportSize.Y, 0f, 0f, 1f));
        shader.Apply();

        gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, renderProfile.TriangleCount);
    }

    /// <summary>
    ///     Fills vertex and index buffers for the liquid distance data.
    /// </summary>
    /// <param name="renderProfile">The render profile that dictates what should be examined.</param>
    /// <param name="vertices">The vertex cache to push contents to.</param>
    /// <param name="indices">The index cache to push contents to.</param>
    private static void FillBuffers(LiquidTargetRenderProfile renderProfile, VertexPositionVectorTexture[] vertices, short[] indices)
    {
        Vector3 screenPosition3 = new Vector3(Main.screenPosition, 0f);

        int horizontalSamples = renderProfile.HorizontalSamples;
        int verticalSamples = renderProfile.VerticalSamples;
        int meshWidth = renderProfile.MeshWidth;
        int meshHeight = renderProfile.MeshHeight;
        Rectangle tileArea = renderProfile.TileArea;

        // Calculate samples.
        float[] waterLines = new float[horizontalSamples * 2];
        float[] tileLines = new float[horizontalSamples * 2];
        FillScanLines(renderProfile, waterLines, tileLines);

        // Determine depth information in two-indexed sweeps, taking advantage of natural primitive blending to ensure that the results smoothly interpolate in the final texture, rather than being
        // discrete "blocks" of color on the tile grid.
        for (int j = 0; j < verticalSamples; j++)
        {
            float yInterpolant = j / (float)(verticalSamples - 1f);
            float nextYInterpolant = (j + 0.5f) / (float)(verticalSamples - 1f);
            for (int i = 0; i < horizontalSamples; i++)
            {
                float leftLine = waterLines[i * 2];
                float rightLine = waterLines[i * 2 + 1];
                float topLeftDistance = leftLine - yInterpolant;
                float topRightDistance = rightLine - yInterpolant;
                float bottomLeftDistance = leftLine - nextYInterpolant;
                float bottomRightDistance = rightLine - nextYInterpolant;

                float depthToGroundLeft = tileLines[i * 2];
                float depthToGroundRight = tileLines[i * 2 + 1];

                Vector4 topLeftColor = new Vector4(topLeftDistance, depthToGroundLeft, leftLine, 1f);
                Vector4 topRightColor = new Vector4(topRightDistance, depthToGroundRight, rightLine, 1f);
                Vector4 bottomLeftColor = new Vector4(bottomLeftDistance, depthToGroundLeft, leftLine, 1f);
                Vector4 bottomRightColor = new Vector4(bottomRightDistance, depthToGroundRight, rightLine, 1f);

                bool rightEdge = i * 2 == tileArea.Width;
                bool bottomEdge = j * 2 == tileArea.Height;

                Vector2 topLeftUv = new Vector2(i * 2f / (meshWidth - 1), j * 2f / (meshHeight - 1));
                Vector2 bottomRightUv = new Vector2((i * 2f + 1) / (meshWidth - 1), (j * 2f + 1) / (meshHeight - 1));

                // Send vertices into the cache.
                vertices[i * 2 + j * 2 * meshWidth] = new VertexPositionVectorTexture(new Vector3(tileArea.X + i * 2, tileArea.Y + j * 2, 0f) * 16f - screenPosition3, topLeftColor, topLeftUv);
                if (!rightEdge)
                    vertices[i * 2 + 1 + j * 2 * meshWidth] = new VertexPositionVectorTexture(new Vector3(tileArea.X + i * 2 + 1, tileArea.Y + j * 2, 0f) * 16f - screenPosition3, topRightColor, new Vector2(bottomRightUv.X, topLeftUv.Y));
                if (!bottomEdge)
                    vertices[i * 2 + (j * 2 + 1) * meshWidth] = new VertexPositionVectorTexture(new Vector3(tileArea.X + i * 2, tileArea.Y + j * 2 + 1, 0f) * 16f - screenPosition3, bottomLeftColor, new Vector2(topLeftUv.X, bottomRightUv.Y));
                if (!bottomEdge && !rightEdge)
                    vertices[i * 2 + 1 + (j * 2 + 1) * meshWidth] = new VertexPositionVectorTexture(new Vector3(tileArea.X + i * 2 + 1, tileArea.Y + j * 2 + 1, 0f) * 16f - screenPosition3, bottomRightColor, bottomRightUv);
            }
        }

        // Construct index mappings.
        int currentIndex = 0;
        for (int j = 0; j < meshHeight - 1; j++)
        {
            for (int i = 0; i < meshWidth - 1; i++)
            {
                indices[currentIndex] = (short)(i + j * meshWidth);
                indices[currentIndex + 1] = (short)(i + 1 + j * meshWidth);
                indices[currentIndex + 2] = (short)(i + (j + 1) * meshWidth);
                indices[currentIndex + 3] = (short)(i + 1 + j * meshWidth);
                indices[currentIndex + 4] = (short)(i + 1 + (j + 1) * meshWidth);
                indices[currentIndex + 5] = (short)(i + (j + 1) * meshWidth);
                currentIndex += 6;
            }
        }
    }

    /// <summary>
    ///     Performs the vertical downward scans across the horizontal span of a given render profile, evaluating how far down a water surface (and its waterbed) are.
    /// </summary>
    /// <param name="renderProfile">The render profile that dictates what should be examined.</param>
    /// <param name="waterLines">The 0-1 interpolants relative to the screen height which indicate where liquid surfaces are.</param>
    /// <param name="tileLines">The 0-1 interpolants relative to the screen height which indicate where waterbeds beneath the water surface are.</param>
    private static void FillScanLines(LiquidTargetRenderProfile renderProfile, float[] waterLines, float[] tileLines)
    {
        Parallel.For(0, renderProfile.HorizontalSamples * 2, i =>
        {
            int x = renderProfile.TileArea.X + i;

            // Make the water line as being at the bottom of the screen by default.
            waterLines[i] = 1f;

            // Scan from the top to the bottom of the screen in search of open water.
            int? waterLineTileY = null;
            for (float yWorld = Main.screenPosition.Y; yWorld < Main.screenPosition.Y + Main.screenHeight + 32f; yWorld += 16f)
            {
                int y = (int)(yWorld / 16f);
                Tile t = Framing.GetTileSafely(x, y);
                bool solidTile = t.HasTile && Main.tileSolid[t.TileType];
                bool openWater = t.LiquidAmount >= 100 && !solidTile;
                if (openWater)
                {
                    waterLineTileY = y;
                    waterLines[i] = LumUtils.InverseLerp(Main.screenPosition.Y, Main.screenPosition.Y + Main.screenHeight, yWorld);
                    break;
                }
            }

            // If water is found, scan down further in search of solid tiles below said water line.
            if (waterLineTileY.HasValue)
            {
                for (int dy = 0; dy < renderProfile.TileArea.Height; dy++)
                {
                    int y = waterLineTileY.Value + dy;
                    Tile t = Framing.GetTileSafely(x, y);
                    bool solidTile = t.HasTile && Main.tileSolid[t.TileType];
                    if (solidTile)
                    {
                        tileLines[i] = LumUtils.InverseLerp(0f, Main.screenHeight / 16f, dy);
                        break;
                    }
                }
            }

            else
                tileLines[i] = 0f;
        });
    }

    /// <summary>
    ///     Ensures that the vertex and index buffer are of sufficient size for necessary rendering operations, increasing their sizes by powers of two if they're too small.
    /// </summary>
    /// <param name="renderProfile">The render profile that indicates buffer size requirements.</param>
    private static void EnsureBufferSizes(LiquidTargetRenderProfile renderProfile)
    {
        int vertexCount = renderProfile.VertexCount;
        int indexCount = renderProfile.IndexCount;

        if (Vertices.VertexCount < vertexCount)
        {
            int vertexBufferCapacity = (int)Math.Min(BitOperations.RoundUpToPowerOf2((uint)vertexCount), int.MaxValue);
            Vertices.Dispose();
            Vertices = new DynamicVertexBuffer(Main.instance.GraphicsDevice, typeof(VertexPositionColorTexture), vertexBufferCapacity, BufferUsage.None);
        }
        if (Indices.IndexCount < indexCount)
        {
            int indexBufferCapacity = (int)Math.Min(BitOperations.RoundUpToPowerOf2((uint)indexCount), int.MaxValue);
            Indices.Dispose();
            Indices = new DynamicIndexBuffer(Main.instance.GraphicsDevice, IndexElementSize.SixteenBits, indexBufferCapacity, BufferUsage.None);
        }
    }
}
