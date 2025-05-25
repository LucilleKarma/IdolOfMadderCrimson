using IdolOfMadderCrimson.Content.Tiles.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Utilities;
using System;
using Terraria;

namespace IdolOfMadderCrimson.Content.Tiles.ForgottenShrine;

public class IdolStatueManager : WorldOrientedTileObjectManager<IdolStatueData>
{
    /// <summary>
    ///     The 0-1 interpolant which dictates how much water flows are cut off.
    /// </summary>
    public static float WaterFlowCutoffInterpolant
    {
        get;
        set;
    }

    /// <summary>
    ///     An optional extra draw action to perform on all statues.
    /// </summary>
    public static Action<Vector2>? ExtraDrawAction
    {
        get;
        set;
    }

    public override void PreUpdatePlayers()
    {
        WaterFlowCutoffInterpolant = WaterFlowCutoffInterpolant.StepTowards(0f, 0.01f);
        ExtraDrawAction = null;
    }

    public override void PostDrawTiles()
    {
        if (TileObjects.Count <= 0)
            return;

        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        foreach (IdolStatueData statue in TileObjects)
            statue.Render();

        Main.spriteBatch.End();
    }
}
