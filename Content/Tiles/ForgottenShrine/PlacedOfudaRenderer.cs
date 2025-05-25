using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.RenderTargets;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace IdolOfMadderCrimson.Content.Tiles.ForgottenShrine;

public class PlacedOfudaRenderer : ModSystem
{
    /// <summary>
    /// The render target in which ofuda are rendered into before being pixelated.
    /// </summary>
    public static InstancedRequestableTarget OfudaTarget
    {
        get;
        private set;
    }

    public override void OnModLoad() => Main.ContentThatNeedsRenderTargets.Add(OfudaTarget = new InstancedRequestableTarget());

    public override void PostDrawTiles()
    {
        List<TEPlacedOfuda> placedOfuda = [.. TileEntity.ByID.Values.Where(te => te is TEPlacedOfuda).Select(te => te as TEPlacedOfuda)];
        if (placedOfuda.Count <= 0)
            return;

        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        foreach (TEPlacedOfuda ofuda in placedOfuda)
            ofuda.Render();
        Main.spriteBatch.End();
    }
}
