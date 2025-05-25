using Luminance.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Metadata;
using Terraria.ID;
using Terraria.ModLoader;

namespace IdolOfMadderCrimson.Content.Tiles.ForgottenShrine;

public class UnderwaterTree : ModTile
{
    public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;

        // Prepare necessary setups to ensure that this tile is treated like grass.
        TileID.Sets.ReplaceTileBreakUp[Type] = true;
        TileID.Sets.SwaysInWindBasic[Type] = true;
        TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

        // Use plant destruction visuals and sounds.
        HitSound = SoundID.Grass;
        DustType = DustID.Grass;

        AddMapEntry(new Color(16, 16, 16));
    }

    public override bool CanExplode(int i, int j) => false;

    public override bool CanReplace(int i, int j, int tileTypeBeingPlaced) => false;

    public override bool CanKillTile(int i, int j, ref bool blockDamaged) => false;

    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => false;
}
