using IdolOfMadderCrimson.Content.Tiles.Generic;
using Terraria;

namespace IdolOfMadderCrimson.Content.Tiles.ForgottenShrine;

public class SpiderLilyManager : WorldOrientedTileObjectManager<SpiderLilyData>
{
    public override void OnModLoad() => On_Main.DrawNPCs += RenderLilies;

    private void RenderLilies(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
    {
        orig(self, behindTiles);

        if (TileObjects.Count <= 0 || behindTiles)
            return;

        foreach (SpiderLilyData lily in TileObjects)
            lily.Render();
    }
}
