﻿using IdolOfMadderCrimson.Common.Graphics;
using IdolOfMadderCrimson.Content.Subworlds.Generation;
using IdolOfMadderCrimson.Content.Subworlds.Generation.Bridges;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.FastParticleSystems;
using NoxusBoss.Core.Graphics.LightingMask;
using ReLogic.Content;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IdolOfMadderCrimson.Content.Subworlds;

public class ForgottenShrineLotusSystem : ModSystem
{
    private static FastParticleSystem lotusParticleSystem;

    private static int LotusCount => 2600;

    private static readonly Asset<Texture2D> redLotus = ModContent.Request<Texture2D>("IdolOfMadderCrimson/Content/Subworlds/RedLotus");

    public override void OnModLoad()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            Main.QueueMainThreadAction(() =>
            {
                lotusParticleSystem = new FramedFastParticleSystem(8, LotusCount, PrepareLotusParticleRendering, UpdateLotusParticles);
            });
        }

        On_Main.DrawProjectiles += RenderLotuses;
        ForgottenShrineSystem.OnEnter += ScatterLotusesIfNecessary;
    }

    public override void OnModUnload() => Main.QueueMainThreadAction(lotusParticleSystem.Dispose);

    private static void ScatterLotusesIfNecessary()
    {
        for (int i = 0; i < lotusParticleSystem.particles.Length; i++)
            lotusParticleSystem.particles[i].Active = false;

        int groundLevelY = Main.maxTilesY - ForgottenShrineGenerationHelpers.GroundDepth;
        int waterLevelY = groundLevelY - ForgottenShrineGenerationHelpers.WaterDepth;
        for (int i = 0; i < LotusCount; i++)
        {
            float lotusScale = Main.rand.NextFloat(0.85f, 1f);
            Vector2 lotusSpawnPosition = new Vector2(Main.rand.NextFloat(Main.maxTilesX * 16f), waterLevelY * 16f);
            if (!Collision.SolidCollision(lotusSpawnPosition - Vector2.One * 8f, 16, 16) && lotusSpawnPosition.X >= BaseBridgePass.BridgeGenerator.Left * 16f)
                lotusParticleSystem.CreateNew(lotusSpawnPosition, Vector2.Zero, new Vector2(18f, 14f) * lotusScale * 0.5f, Color.Wheat);
        }
    }

    private void RenderLotuses(On_Main.orig_DrawProjectiles orig, Main self)
    {
        orig(self);

        if (SubworldSystem.IsActive<ForgottenShrineSubworld>())
            lotusParticleSystem.RenderAll();
    }

    private static void PrepareLotusParticleRendering()
    {
        Matrix world = Matrix.CreateTranslation(-Main.screenPosition.X, 2f - Main.screenPosition.Y, 0f);
        Matrix projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -400f, 400f);

        Main.instance.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

        Texture2D lotus = redLotus.Value;
        ManagedShader overlayShader = ShaderManager.GetShader("IdolOfMadderCrimson.LitPrimitiveOverlayShader");
        overlayShader.TrySetParameter("exposure", 1.4f);
        overlayShader.TrySetParameter("uWorldViewProjection", world * Main.GameViewMatrix.TransformationMatrix * projection);
        overlayShader.TrySetParameter("screenSize", WotGUtils.ViewportSize);
        overlayShader.TrySetParameter("zoom", Main.GameViewMatrix.Zoom);
        overlayShader.SetTexture(lotus, 1, SamplerState.LinearClamp);
        overlayShader.SetTexture(LightingMaskTargetManager.LightTarget, 2);
        overlayShader.Apply();
    }

    private static void UpdateLotusParticles(ref FastParticle particle)
    {
        if (Collision.WetCollision(particle.Position + Vector2.UnitY * (particle.Size.Y - 1f), 1, 1))
            particle.Velocity.Y = MathHelper.Clamp(particle.Velocity.Y - 0.04f, -0.8f, 0.8f);
        else
            particle.Velocity.Y = (particle.Velocity.Y + 0.025f) * 0.93f;

        float worldEdgeBoundary = 600f;
        float worldEdgePushForce = 0.11f;
        if (particle.Position.X < worldEdgeBoundary)
            particle.Velocity.Y += worldEdgePushForce;
        if (particle.Position.X > Main.maxTilesX * 16f - worldEdgeBoundary)
            particle.Velocity.Y -= worldEdgePushForce;

        float distanceInterpolant = LumUtils.InverseLerp(96f, 45f, Main.LocalPlayer.Distance(particle.Position));
        Vector2 pushForce = Main.LocalPlayer.velocity * distanceInterpolant * 0.02f;
        particle.Velocity += pushForce;
        particle.Velocity *= 0.99f;

        particle.Rotation = particle.Velocity.X * 0.3f;
    }

    public override void PreUpdateEntities() => lotusParticleSystem.UpdateAll();
}
