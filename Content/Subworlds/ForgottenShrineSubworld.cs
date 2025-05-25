﻿using System.Collections.Generic;
using IdolOfMadderCrimson.Common.Graphics;
using IdolOfMadderCrimson.Content.Subworlds.Generation;
using IdolOfMadderCrimson.Content.Subworlds.Generation.Bridges;
using Luminance.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Avatar.SecondPhaseForm;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.World.WorldSaving;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace IdolOfMadderCrimson.Content.Subworlds;

public class ForgottenShrineSubworld : Subworld
{
    public static TagCompound ClientWorldDataTag
    {
        get;
        internal set;
    }

    public override int Width => ForgottenShrineGenerationHelpers.SubworldWidth;

    public override int Height => ForgottenShrineGenerationHelpers.SubworldHeight;

    public override List<GenPass> Tasks =>
    [
        new DefineWorldLinePass(),
        new WestFieldPass(),
        new CreateWaterbedPass(),
        new BaseBridgePass(),
        new BridgeDockPass(),
        new ShrineIslandPass(),
        new ShrinePass(),
        new CreateUnderwaterVegetationPass(),
        new SmoothenPass(),
        new ReorientShrineIslandLilyPass(),
        new SetPlayerSpawnPointPass()
    ];

    public override void OnEnter() => ParticleEngine.Clear();

    public override void OnExit() => ParticleEngine.Clear();

    public override bool ChangeAudio()
    {
        // Get rid of the jarring title screen music when moving between subworlds.
        if (Main.gameMenu)
        {
            Main.newMusic = 0;
            return true;
        }

        return false;
    }

    public override void DrawMenu(GameTime gameTime)
    {
        Texture2D pixel = MiscTexturesRegistry.Pixel.Value;
        Main.spriteBatch.Draw(pixel, Main.ScreenSize.ToVector2() * 0.5f, null, Color.Black, 0f, pixel.Size() * 0.5f, WotGUtils.ViewportSize * 3f, 0, 0f);
    }

    public override bool GetLight(Tile tile, int x, int y, ref FastRandom rand, ref Vector3 color)
    {
        ForgottenShrineLiquidVisualsSystem.ApplySwagTileShadows(x, y, ref color);
        return false;
    }

    public static TagCompound SafeWorldDataToTag(string suffix, bool saveInCentralRegistry = true)
    {
        // Re-initialize the save data tag.
        TagCompound savedWorldData = [];

        // Save difficulty data. This is self-explanatory.
        bool revengeanceMode = CommonCalamityVariables.RevengeanceModeActive;
        bool deathMode = CommonCalamityVariables.DeathModeActive;
        if (revengeanceMode)
            savedWorldData["RevengeanceMode"] = revengeanceMode;
        if (deathMode)
            savedWorldData["DeathMode"] = deathMode;
        if (BossDownedSaveSystem.HasDefeated<AvatarOfEmptiness>())
            savedWorldData["AvatarDefeated"] = true;
        if (BossDownedSaveSystem.HasDefeated<NamelessDeityBoss>())
            savedWorldData["NamelessDeityDefeated"] = true;
        if (Main.zenithWorld)
            savedWorldData["GFB"] = Main.zenithWorld;
        savedWorldData["WorldVersionText"] = WorldVersionSystem.WorldVersionText;

        // Save Calamity's boss defeat data.
        CommonCalamityVariables.SaveDefeatStates(savedWorldData);

        // Store the tag.
        if (saveInCentralRegistry)
            SubworldSystem.CopyWorldData($"ShrineSavedWorldData_{suffix}", savedWorldData);

        return savedWorldData;
    }

    public static void LoadWorldDataFromTag(string suffix, TagCompound? specialTag = null)
    {
        TagCompound savedWorldData = specialTag ?? SubworldSystem.ReadCopiedWorldData<TagCompound>($"ShrineSavedWorldData_{suffix}");

        if (savedWorldData.ContainsKey("AvatarDefeated"))
            BossDownedSaveSystem.SetDefeatState<AvatarOfEmptiness>(true);
        if (savedWorldData.ContainsKey("NamelessDeityDefeated"))
            BossDownedSaveSystem.SetDefeatState<NamelessDeityBoss>(true);

        CommonCalamityVariables.RevengeanceModeActive = savedWorldData.ContainsKey("RevengeanceMode");
        CommonCalamityVariables.DeathModeActive = savedWorldData.ContainsKey("DeathMode");
        Main.zenithWorld = savedWorldData.ContainsKey("GFB");

        if (savedWorldData.TryGet("WorldVersionText", out string version))
            WorldVersionSystem.WorldVersionText = version;

        CommonCalamityVariables.LoadDefeatStates(savedWorldData);
    }

    public override void CopyMainWorldData() => SafeWorldDataToTag("Main");

    public override void ReadCopiedMainWorldData() => LoadWorldDataFromTag("Main");

    public override void CopySubworldData() => SafeWorldDataToTag("Subworld");

    public override void ReadCopiedSubworldData() => LoadWorldDataFromTag("Subworld");
}
