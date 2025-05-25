using IdolOfMadderCrimson.Content.Subworlds;
using IdolOfMadderCrimson.Core.Physics.ClothManagement;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Tiles.TileEntities;
using NoxusBoss.Core.Graphics.LightingMask;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace IdolOfMadderCrimson.Content.Tiles.ForgottenShrine;

public class TEEnigmaticTapestry : ModTileEntity, IClientSideTileEntityUpdater
{
    private ClothSimulation cloth;

    /// <summary>
    /// The position at which this cloth is anchored, essentially its top-center in world coordinates.
    /// </summary>
    private Vector3 AnchorPosition => new Vector3(Position.ToWorldCoordinates(0f, 0f), 0f);

    /// <summary>
    /// The variant of this tapestry.
    /// </summary>
    public int TapestryVariant => ((int)(ID / MathHelper.PiOver2) + Position.X * 11) % tapestryTextures.Length;

    private static bool tapestriesAreSettling;

    private static float ClothPointSpacing => 13f;

    private static readonly Asset<Texture2D>[] tapestryTextures =
    [
        ModContent.Request<Texture2D>("IdolOfMadderCrimson/Content/Tiles/ForgottenShrine/EnigmaticTapestry1"),
        ModContent.Request<Texture2D>("IdolOfMadderCrimson/Content/Tiles/ForgottenShrine/EnigmaticTapestry2"),
        ModContent.Request<Texture2D>("IdolOfMadderCrimson/Content/Tiles/ForgottenShrine/EnigmaticTapestry3"),
        ModContent.Request<Texture2D>("IdolOfMadderCrimson/Content/Tiles/ForgottenShrine/EnigmaticTapestry4"),
    ];

    public static readonly SoundStyle InteractionSound = new SoundStyle("IdolOfMadderCrimson/Assets/Sounds/Environment/TapestryMove", 5);

    public override bool IsTileValidForEntity(int x, int y)
    {
        Tile tile = Main.tile[x, y];
        return tile.HasTile && tile.TileType == ModContent.TileType<EnigmaticTapestry>();
    }

    public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
    {
        // If in multiplayer, tell the server to place the tile entity and DO NOT place it yourself. That would mismatch IDs.
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            NetMessage.SendTileSquare(Main.myPlayer, i, j, 1, 1);
            NetMessage.SendData(MessageID.TileEntityPlacement, -1, -1, null, i, j, Type);
            return -1;
        }
        return Place(i, j);
    }

    // Sync the tile entity the moment it is place on the server.
    // This is done to cause it to register among all clients.
    public override void OnNetPlace() => NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID, Position.X, Position.Y);

    public override void Load()
    {
        // Ensure that all tapestries settle upon entering the subworld.
        ForgottenShrineSystem.OnEnter += SettleClothOnEnteringWorldWrapper;
    }

    private static void SettleClothOnEnteringWorldWrapper()
    {
        new Thread(() =>
        {
            tapestriesAreSettling = true;

            try
            {
                SettleClothOnEnteringWorld();
            }
            finally
            {
                tapestriesAreSettling = false;
            }
        }).Start();
    }

    private static void SettleClothOnEnteringWorld()
    {
        // The ordering query is to ensure that tapestries that are closest to the player settle first, making it less likely that the player 
        // will see the process happening on the separate thread.
        List<TEEnigmaticTapestry> placedTapestries = [.. ByID.Values.Where(te => te is TEEnigmaticTapestry).
                    OrderByDescending(te => te.Position.ToWorldCoordinates().Distance(Main.LocalPlayer.Center)).
                    Select(te => te as TEEnigmaticTapestry)];
        foreach (TEEnigmaticTapestry tapestry in placedTapestries)
        {
            for (int i = 0; i < 1000; i++)
                tapestry.ApplySimulationStep();
        }
    }

    public void ClientSideUpdate()
    {
        if (!Main.LocalPlayer.WithinRange(Position.ToWorldCoordinates(), 3000f) || tapestriesAreSettling)
            return;

        for (int i = 0; i < 8; i++)
            ApplySimulationStep();
    }

    /// <summary>
    /// Applies a simulationn step to this tapestry's cloth.
    /// </summary>
    public void ApplySimulationStep()
    {
        cloth ??= new ClothSimulation(AnchorPosition, 21, 13, ClothPointSpacing, 80f, 0.0061f);

        for (int x = 0; x < cloth.Width; x++)
        {
            for (int y = 0; y < 2; y++)
                ConstrainParticle(AnchorPosition, cloth.particleGrid[x, y]);
        }

        bool interactedWith = false;
        for (int y = 0; y < cloth.Height; y++)
        {
            for (int x = 0; x < cloth.Width; x++)
            {
                Vector2 position2D = new Vector2(cloth.particleGrid[x, y].Position.X, cloth.particleGrid[x, y].Position.Y);
                float pushInterpolant = LumUtils.InverseLerp(36f, 19f, Main.LocalPlayer.Distance(position2D));
                Vector3 pushForce = new Vector3(Main.LocalPlayer.velocity * pushInterpolant * 0.75f, 0f);
                cloth.particleGrid[x, y].AddForce(pushForce);

                if (pushInterpolant >= 0.67f && Main.LocalPlayer.velocity.Length() >= 3f)
                    interactedWith = true;
            }
        }

        if (interactedWith)
            SoundEngine.PlaySound(InteractionSound with { MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew }, new Vector2(AnchorPosition.X, AnchorPosition.Y));

        cloth.Simulate(0.051f, false, Vector3.UnitY * 3f);
    }

    /// <summary>
    /// Locks certain cloth particles in place so that they don't fall infinitely.
    /// </summary>
    private void ConstrainParticle(Vector3 anchor, ClothPoint? point)
    {
        if (point is null)
            return;

        float width = cloth.Width * ClothPointSpacing * 0.9f;
        float xInterpolant = point.X / (float)cloth.Width;
        point.Position = anchor + new Vector3((xInterpolant - 0.5f) * width, 0f, LumUtils.Convert01To010(xInterpolant) * 130f);
        point.IsFixed = true;
    }

    /// <summary>
    /// Renders this tapestry.
    /// </summary>
    public void Render()
    {
        if (!Position.ToWorldCoordinates().WithinRange(WotGUtils.ViewportArea.Center() + Main.screenPosition, 3000f))
            return;
        if (cloth is null)
            return;

        RenderTapestry();
    }

    private void RenderTapestry()
    {
        bool newVariant = TapestryVariant == 1;
        Texture2D texture = tapestryTextures[TapestryVariant].Value;
        Vector2 textureZoom = Vector2.One;
        if (newVariant)
            textureZoom = new Vector2(cloth.Width / (float)cloth.Height, 1f) * 1.5f;

        EnigmaticTapestryRenderer.TapestryTarget.Request(400, 400, ID, () =>
        {
            Vector2 drawOffset = -Position.ToWorldCoordinates(0f, 0f) + WotGUtils.ViewportSize * 0.5f;
            Matrix world = Matrix.CreateTranslation(drawOffset.X, drawOffset.Y, 0f);
            Matrix projection = Matrix.CreateOrthographicOffCenter(0f, WotGUtils.ViewportSize.X, WotGUtils.ViewportSize.Y, 0f, -1000f, 1000f);

            ManagedShader clothShader = ShaderManager.GetShader("IdolOfMadderCrimson.ShrineTapestryShader");
            clothShader.SetTexture(texture, 1, SamplerState.PointWrap);
            clothShader.TrySetParameter("textureZoom", textureZoom);
            clothShader.TrySetParameter("transform", world * projection);
            clothShader.Apply();

            cloth.Render();
        });
        if (EnigmaticTapestryRenderer.TapestryTarget.TryGetTarget(ID, out RenderTarget2D? target) && target is not null)
        {
            if (newVariant)
            {
                ManagedShader pixelationShader = ShaderManager.GetShader("IdolOfMadderCrimson.ShrineTapestryPostProcessingNewShader");
                pixelationShader.TrySetParameter("gameZoom", Main.GameViewMatrix.Zoom);
                pixelationShader.TrySetParameter("screenSize", WotGUtils.ViewportSize);
                pixelationShader.TrySetParameter("pixelationFactor", Vector2.One * 2f / target.Size());
                pixelationShader.SetTexture(LightingMaskTargetManager.LightTarget, 1);
                pixelationShader.Apply();
            }
            else
            {
                ManagedShader pixelationShader = ShaderManager.GetShader("IdolOfMadderCrimson.ShrineTapestryPostProcessingShader");
                pixelationShader.TrySetParameter("zoom", Main.GameViewMatrix.Zoom);
                pixelationShader.TrySetParameter("screenSize", WotGUtils.ViewportSize);
                pixelationShader.TrySetParameter("pixelationFactor", Vector2.One * 1.5f / target.Size());
                pixelationShader.SetTexture(LightingMaskTargetManager.LightTarget, 1);
                pixelationShader.Apply();
            }

            Vector2 drawPosition = Position.ToWorldCoordinates() - Main.screenPosition + Vector2.UnitX * 4f;
            Main.spriteBatch.Draw(target, drawPosition, null, Color.White, 0f, target.Size() * 0.5f, 1f, 0, 0f);
        }
    }
}
