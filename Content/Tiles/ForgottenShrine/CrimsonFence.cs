using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace IdolOfMadderCrimson.Content.Tiles.ForgottenShrine;

public class CrimsonFence : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        AddMapEntry(new Color(166, 20, 38));
    }

    public override bool CanExplode(int i, int j) => false;

    public override bool CanReplace(int i, int j, int tileTypeBeingPlaced) => false;

    public override bool CanKillTile(int i, int j, ref bool blockDamaged) => false;
}
