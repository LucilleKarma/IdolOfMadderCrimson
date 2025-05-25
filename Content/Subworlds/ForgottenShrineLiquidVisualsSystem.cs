using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IdolOfMadderCrimson.Content.Subworlds.Generation;
using IdolOfMadderCrimson.Content.Subworlds.Generation.Bridges;
using IdolOfMadderCrimson.Content.Waters;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Assets;
using NoxusBoss.Core.Graphics.LightingMask;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.Utilities;
using SubworldLibrary;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace IdolOfMadderCrimson.Content.Subworlds;

public class ForgottenShrineLiquidVisualsSystem : ModSystem
{
    /// <summary>
    ///     A general purpose timer used for water perturbations that increments based on wind speed and direction.
    /// </summary>
    public static float WindTimer
    {
        get;
        private set;
    }

    /// <summary>
    ///     A queue of points that determines where points in space should be converted to ripples, in world coordinates.
    /// </summary>
    public static readonly Queue<Vector2> PointsToAddRipplesAt = new Queue<Vector2>(32);

    /// <summary>
    ///     The render target responsible for temporarily storing information to be swapped over back into the original target.
    /// </summary>
    public static ManagedRenderTarget? UpdateTarget
    {
        get;
        private set;
    }

    /// <summary>
    ///     The render target responsible for water ripple effects.
    /// </summary>
    public static ManagedRenderTarget? WaterStepRippleTarget
    {
        get;
        private set;
    }

    /// <summary>
    ///     Whether water effects for this system are active or not.
    /// </summary>
    public static bool WaterEffectsActive => ForgottenShrineSystem.WasInSubworldLastFrame;

    /// <summary>
    ///     The sound played when players walk on water and create ripples.
    /// </summary>
    public static readonly SoundStyle RippleStepSound = new SoundStyle("IdolOfMadderCrimson/Assets/Sounds/Environment/WaterRipple", 3);

    public override void OnModLoad()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        UpdateTarget = new ManagedRenderTarget(true, (width, height) =>
        {
            return new RenderTarget2D(Main.instance.GraphicsDevice, width / 2, height / 2, true, SurfaceFormat.Vector4, DepthFormat.Depth24);
        });
        WaterStepRippleTarget = new ManagedRenderTarget(true, (width, height) =>
        {
            return new RenderTarget2D(Main.instance.GraphicsDevice, width / 2, height / 2, true, SurfaceFormat.Vector4, DepthFormat.Depth24);
        });

        On_Main.CalculateWaterStyle += ForceShrineWater;
        On_WaterShaderData.Apply += DisableIdleLiquidDistortion;
        RenderTargetManager.RenderTargetUpdateLoopEvent += UpdateTargets;
    }

    /// <summary>
    ///     Modifies lighting such that shadows are applied in a way that suggests 3D perspective and doesn't become weirdly dark near the water surface line.
    /// </summary>
    /// <param name="x">The tile X position of evaluated light.</param>
    /// <param name="x">The tile Y position of evaluated light.</param>
    /// <param name="lightColor">The modifiable light color.</param>
    internal static void ApplySwagTileShadows(int x, int y, ref Vector3 lightColor)
    {
        if (!Main.tile[x, y].HasTile)
            return;

        int shrineIslandLeft = BaseBridgePass.BridgeGenerator.Right + ForgottenShrineGenerationHelpers.LakeWidth + BaseBridgePass.GenerationSettings.DockWidth;
        int shrineIslandWidth = ForgottenShrineGenerationHelpers.ShrineIslandWidth;
        float islandInterpolant = LumUtils.InverseLerpBump(0f, 16f, shrineIslandWidth - 16f, shrineIslandWidth, x - shrineIslandLeft);

        // Lucille's swag shadows ACTIVATE!
        if (islandInterpolant > 0f)
        {
            int distanceToSurface = 4;
            for (int dy = 0; dy < 4; dy++)
            {
                Tile t = Framing.GetTileSafely(x, y + dy);
                if (!t.HasTile && t.LiquidAmount >= 200)
                {
                    distanceToSurface = dy;
                    break;
                }

                // Check if the tile Y frame is less than or equal to 18 to determine if it's a grass layer.
                // This SHOULD check for the grass ID but I fear the potential performance penalties that could incur.
                if (t.HasTile && t.TileFrameY <= 18)
                {
                    distanceToSurface = dy;
                    break;
                }
            }

            float baseShadow = LumUtils.InverseLerp(3.5f, 0.5f, distanceToSurface);
            float easedShadow = MathF.Pow(baseShadow, 2.3f);
            lightColor = Vector3.One * easedShadow * islandInterpolant * 0.6f;
        }
    }

    // Not doing this results in beach water somehow having priority over shrine water in the outer parts of the subworld.
    private static int ForceShrineWater(On_Main.orig_CalculateWaterStyle orig, bool ignoreFountains)
    {
        if (SubworldSystem.IsActive<ForgottenShrineSubworld>())
            return ModContent.GetInstance<ForgottenShrineWater>().Slot;

        return orig(ignoreFountains);
    }

    private static void DisableIdleLiquidDistortion(On_WaterShaderData.orig_Apply orig, WaterShaderData self)
    {
        // Ensure that orig is still called, so as to not mess up any detours to this method made by other mods.
        orig(self);

        // However, at the same time, if the subworld is active, apply a separate water distortion shader, so that the water can be rendered completely still by default.
        if (SubworldSystem.IsActive<ForgottenShrineSubworld>())
        {
            float perturbationStrength = LumUtils.InverseLerp(0f, 1f, MathF.Abs(Main.windSpeedCurrent)) * 0.023f;
            Vector2 perturbationScroll = WindTimer * new Vector2(2.3f, 0.98f);
            Vector2 screenSize = Main.ScreenSize.ToVector2();
            RenderTarget2D distortionTarget = (RenderTarget2D)typeof(WaterShaderData).GetField("_distortionTarget", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(self)!;
            ManagedShader waterShader = ShaderManager.GetShader("IdolOfMadderCrimson.ShrineWaterShader");
            waterShader.TrySetParameter("zoom", Main.GameViewMatrix.Zoom);
            waterShader.TrySetParameter("screenOffset", (Main.screenPosition - Main.screenLastPosition) / screenSize);
            waterShader.TrySetParameter("targetSize", screenSize);
            waterShader.TrySetParameter("perturbationStrength", perturbationStrength);
            waterShader.TrySetParameter("perturbationScroll", perturbationScroll);
            waterShader.TrySetParameter("screenPosition", Main.screenPosition);
            waterShader.SetTexture(distortionTarget, 1);
            waterShader.SetTexture(TileTargetManagers.LiquidTarget, 2, SamplerState.LinearClamp);
            waterShader.SetTexture(GennedAssets.Textures.Noise.PerlinNoise, 3, SamplerState.LinearWrap);
            waterShader.Apply();

            Main.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
        }
    }

    private static void UpdateTargets()
    {
        if (!WaterEffectsActive || Main.gamePaused)
            return;

        // R = Pressure.
        // G = Pressure speed.
        // B = Gradient X.
        // A = Gradient Y.
        GraphicsDevice gd = Main.instance.GraphicsDevice;

        gd.SetRenderTarget(WaterStepRippleTarget);
        gd.Clear(Color.Transparent);
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
        RenderTargetWithUpdateLoop(UpdateTarget);
        Main.spriteBatch.End();

        gd.SetRenderTarget(UpdateTarget);
        gd.Clear(Color.Transparent);
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
        RenderTargetWithUpdateLoop(WaterStepRippleTarget);
        Main.spriteBatch.End();

        gd.SetRenderTarget(null);
    }

    private static void RenderTargetWithUpdateLoop(Texture2D texture)
    {
        int rippleIndex = 0;
        Vector2[] ripplePositions = [.. Enumerable.Repeat(Vector2.One * -99999f, 10)];
        while (PointsToAddRipplesAt.TryDequeue(out Vector2 rippleWorldPosition))
        {
            ripplePositions[rippleIndex] = (rippleWorldPosition - Main.screenPosition) / texture.Size() * 0.5f;

            rippleIndex++;
            if (rippleIndex >= ripplePositions.Length)
                break;
        }

        ManagedShader rippleShader = ShaderManager.GetShader("IdolOfMadderCrimson.ShrineWaterRippleUpdateShader");
        rippleShader.TrySetParameter("ripplePoints", ripplePositions);
        rippleShader.TrySetParameter("stepSize", Vector2.One / texture.Size());
        rippleShader.TrySetParameter("decayFactor", 0.996f);
        rippleShader.SetTexture(GennedAssets.Textures.Noise.PerlinNoise, 1, SamplerState.LinearWrap);
        rippleShader.Apply();

        Main.spriteBatch.Draw(texture, (Main.screenLastPosition - Main.screenPosition) * 0.25f, Color.White);
    }

    public override void PostUpdatePlayers()
    {
        foreach (Player p in Main.ActivePlayers)
        {
            bool headIsDry = !Collision.WetCollision(p.TopLeft, p.width, 16);
            bool waterAtFeet = Collision.WetCollision(p.TopLeft, p.width, p.height + 16);
            if (headIsDry && waterAtFeet && p.velocity.Length() >= 2f && Main.rand.NextBool(3))
            {
                SoundEngine.PlaySound(RippleStepSound with { MaxInstances = 1, Volume = 0.2f, PitchVariance = 0.15f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew }, p.Bottom);
                PointsToAddRipplesAt.Enqueue(p.Bottom + Vector2.UnitY * 5f + Main.rand.NextVector2Circular(4f, 0f));
            }
        }
        UpdateWaterShaders();
    }

    public override void PostDrawTiles()
    {
        ManagedScreenFilter mistShader = ShaderManager.GetFilter("IdolOfMadderCrimson.ForgottenShrineMistShader");
        ManagedScreenFilter reflectionShader = ShaderManager.GetFilter("IdolOfMadderCrimson.ForgottenShrineWaterReflectionShader");
        if (!WaterEffectsActive)
        {
            for (int i = 0; i < 30; i++)
            {
                mistShader.Update();
                reflectionShader.Update();
            }

            return;
        }

        UpdateWaterShaders();

        WindTimer += Main.windSpeedCurrent / 60f;
        if (MathF.Abs(WindTimer) >= 1000f)
            WindTimer = 0f;
    }

    private static void UpdateWaterShaders()
    {
        ManagedScreenFilter mistShader = ShaderManager.GetFilter("IdolOfMadderCrimson.ForgottenShrineMistShader");
        ManagedScreenFilter reflectionShader = ShaderManager.GetFilter("IdolOfMadderCrimson.ForgottenShrineWaterReflectionShader");
        mistShader.TrySetParameter("targetSize", Main.ScreenSize.ToVector2());
        mistShader.TrySetParameter("oldScreenPosition", Main.screenLastPosition);
        mistShader.TrySetParameter("zoom", Main.GameViewMatrix.Zoom);
        mistShader.TrySetParameter("mistColor", new Color(84, 74, 154).ToVector4());
        mistShader.TrySetParameter("noiseAppearanceThreshold", 0.3f);
        mistShader.TrySetParameter("noiseAppearanceSmoothness", 0.59f);
        mistShader.TrySetParameter("mistCoordinatesZoom", new Vector2(1f, 0.4f));
        mistShader.TrySetParameter("mistHeight", 160f);
        mistShader.SetTexture(GennedAssets.Textures.Noise.PerlinNoise, 1, SamplerState.LinearWrap);
        mistShader.SetTexture(TileTargetManagers.LiquidTarget, 2, SamplerState.LinearClamp);
        mistShader.SetTexture(LightingMaskTargetManager.LightTarget, 3, SamplerState.LinearClamp);
        mistShader.SetTexture(LiquidDistanceTargetManager.LiquidDistanceTarget, 4, SamplerState.LinearClamp);
        mistShader.SetTexture(TileTargetManagers.TileTarget, 5, SamplerState.LinearClamp);
        mistShader.Activate();

        float perturbationStrength = LumUtils.InverseLerp(0f, 1.2f, MathF.Abs(Main.windSpeedCurrent)) * 0.015f;
        reflectionShader.TrySetParameter("targetSize", Main.ScreenSize.ToVector2());
        reflectionShader.TrySetParameter("oldScreenPosition", Main.screenLastPosition);
        reflectionShader.TrySetParameter("zoom", Main.GameViewMatrix.Zoom);
        reflectionShader.TrySetParameter("reflectionStrength", 0.47f);
        reflectionShader.TrySetParameter("reflectionMaxDepth", 276f);
        reflectionShader.TrySetParameter("reflectionWaviness", 0.0023f);
        reflectionShader.TrySetParameter("ripplePerspectiveSquishFactor", 2.36f);
        reflectionShader.TrySetParameter("perturbationStrength", perturbationStrength);
        reflectionShader.TrySetParameter("perturbationScroll", Main.GlobalTimeWrappedHourly * new Vector2(Main.windSpeedCurrent.NonZeroSign() * 2f, 0.2f));
        reflectionShader.SetTexture(GennedAssets.Textures.Noise.PerlinNoise, 1, SamplerState.LinearWrap);
        reflectionShader.SetTexture(TileTargetManagers.LiquidTarget, 2, SamplerState.LinearClamp);
        reflectionShader.SetTexture(WaterStepRippleTarget, 3, SamplerState.LinearClamp);
        reflectionShader.SetTexture(TileTargetManagers.TileTarget, 4, SamplerState.LinearClamp);
        reflectionShader.SetTexture(LiquidDistanceTargetManager.LiquidDistanceTarget, 5, SamplerState.LinearClamp);
        reflectionShader.Activate();
    }
}
