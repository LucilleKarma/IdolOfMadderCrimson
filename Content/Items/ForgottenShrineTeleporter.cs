using IdolOfMadderCrimson.Content.Subworlds;
using NoxusBoss.Core.World.Subworlds;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IdolOfMadderCrimson.Content.Items.Misc
{
    public class ForgottenShrineTeleporter : ModItem
    {
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 0;

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = null;
            Item.rare = ItemRarityID.Blue;
            Item.value = 0;
        }

        public override bool? UseItem(Player p)
        {
            if (Main.myPlayer == NetmodeID.MultiplayerClient || p.itemAnimation != p.itemAnimationMax - 1)
                return false;

            if (SubworldSystem.IsActive<ForgottenShrineSubworld>())
                SubworldSystem.Exit();
            else
            {
                ForgottenShrineSubworld.ClientWorldDataTag = EternalGardenNew.SafeWorldDataToTag("Client", false);
                SubworldSystem.Enter<ForgottenShrineSubworld>();
            }
            return null;
        }
    }
}