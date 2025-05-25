using System;
using IdolOfMadderCrimson.Content.Subworlds;
using IdolOfMadderCrimson.Content.Tiles.Generic;
using IdolOfMadderCrimson.Core.Physics;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Assets;
using NoxusBoss.Core.Graphics.LightingMask;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace IdolOfMadderCrimson.Content.Tiles.ForgottenShrine;

public class HangingLanternRopeData : WorldOrientedTileObject
{
    /// <summary>
    ///     The amount by which this rope should sag when completely at rest.
    /// </summary>
    public float Sag
    {
        get;
        set;
    }

    /// <summary>
    ///     The horizontal direction of this rope's lantern.
    /// </summary>
    public int Direction
    {
        get;
        set;
    }

    /// <summary>
    ///     A general-purpose timer used for wind movement on the baubles attached to this rope.
    /// </summary>
    public float WindTime
    {
        get;
        set;
    }

    /// <summary>
    ///     The verlet segments associated with this rope.
    /// </summary>
    public readonly RopeHandle? VerletRope;

    /// <summary>
    ///     The maximum length of this rope.
    /// </summary>
    public float MaxLength
    {
        get;
        private set;
    }

    /// <summary>
    ///     The amount of gravity imposed on this rope.
    /// </summary>
    public static float Gravity => 0.6f;

    /// <summary>
    ///     The asset for the knot texture used by this rope.
    /// </summary>
    public static readonly Asset<Texture2D> KnotTexture = ModContent.Request<Texture2D>("IdolOfMadderCrimson/Content/Tiles/ForgottenShrine/HangingLanternRopeKnot");

    public HangingLanternRopeData() { }

    public HangingLanternRopeData(Point anchorPosition, float ropeLength)
    {
        Vector2 startVector = anchorPosition.ToVector2();
        Position = anchorPosition;

        int segmentCount = 24;
        VerletRope = ModContent.GetInstance<RopeManagerSystem>().RequestNew(startVector, startVector + Vector2.UnitY * ropeLength, segmentCount, ropeLength / segmentCount, Vector2.UnitY * Gravity, new RopeSettings()
        {
            TileColliderArea = Vector2.One * 6f,
            StartIsFixed = true,
            RespondToEntityMovement = true,
            RespondToWind = true
        }, 12);
    }

    /// <summary>
    ///     Updates this rope.
    /// </summary>
    public override void Update()
    {
        if (VerletRope is not RopeHandle rope)
            return;

        Lighting.AddLight(rope.End, Color.Orange.ToVector3());
    }

    private void DrawProjectionButItActuallyWorks(Vector2 drawOffset, Func<float, Color> colorFunction, int? projectionWidth = null, int? projectionHeight = null, bool unscaledMatrix = false)
    {
        if (VerletRope is not RopeHandle rope)
            return;

        ManagedShader overlayShader = ShaderManager.GetShader("IdolOfMadderCrimson.LitPrimitiveOverlayShader");
        overlayShader.TrySetParameter("exposure", 1f);
        overlayShader.TrySetParameter("screenSize", WotGUtils.ViewportSize);
        overlayShader.TrySetParameter("zoom", Main.GameViewMatrix.Zoom);
        overlayShader.SetTexture(MiscTexturesRegistry.Pixel.Value, 1, SamplerState.LinearClamp);
        overlayShader.SetTexture(LightingMaskTargetManager.LightTarget, 2);
        overlayShader.Apply();

        PrimitiveSettings settings = new PrimitiveSettings((float _) => 2f, colorFunction.Invoke, (float _) => drawOffset + Main.screenPosition, Smoothen: true, Pixelate: false, overlayShader, projectionWidth, projectionHeight, unscaledMatrix);
        PrimitiveRenderer.RenderTrail(rope.Positions, settings, 36);

        // Draw the lantern at the bottom of the rope.
        Texture2D lantern = OrnamentalShrineRopeData.PaperLanternTexture.Value;
        Texture2D glowTexture = GennedAssets.Textures.GreyscaleTextures.BloomCirclePinpoint;
        float flickerInterpolant = LumUtils.Cos01(Main.GlobalTimeWrappedHourly * 5f + rope.Start.X * 0.1f);
        float flicker = MathHelper.Lerp(0.93f, 1.07f, flickerInterpolant);
        float lanternScale = 0.8f;
        float glowScale = lanternScale * flicker;
        float lanternRotation = rope.Start.AngleTo(rope.End);
        Vector2 lanternDrawPosition = rope.End - Main.screenPosition;
        Color lanternGlowColor = new Color(1f, 0.32f, 0f, 0f) * 0.33f;
        Main.spriteBatch.Draw(lantern, lanternDrawPosition, null, Color.White, lanternRotation - MathHelper.PiOver2, lantern.Size() * 0.5f, lanternScale, Direction.ToSpriteDirection(), 0f);
        Main.spriteBatch.Draw(glowTexture, lanternDrawPosition, null, lanternGlowColor, 0f, glowTexture.Size() * 0.5f, glowScale * 1.05f, 0, 0f);
        Main.spriteBatch.Draw(glowTexture, lanternDrawPosition, null, lanternGlowColor * 0.6f, 0f, glowTexture.Size() * 0.5f, glowScale * 1.4f, 0, 0f);

        // Draw the knot above the rope.
        Texture2D knot = KnotTexture.Value;
        Vector2 knotBottom = rope.Start;
        Color knotColor = Lighting.GetColor(knotBottom.ToTileCoordinates());
        Main.spriteBatch.Draw(knot, knotBottom - Main.screenPosition, null, knotColor, 0f, knot.Size() * new Vector2(0.5f, 1f), 1f, 0, 0f);

        // Make the glow target affected by the light emitted by the lantern.
        ForgottenShrineDarknessSystem.QueueGlowAction(() =>
        {
            Main.spriteBatch.Draw(glowTexture, lanternDrawPosition, null, new Color(1f, 1f, 1f, 0f) * 0.4f, 0f, glowTexture.Size() * 0.5f, glowScale * 1.97f, 0, 0f);
        });
    }

    /// <summary>
    ///     Renders this rope.
    /// </summary>
    public override void Render()
    {
        DrawProjectionButItActuallyWorks(-Main.screenPosition, _ => new Color(255, 28, 58));
    }

    /// <summary>
    ///     Serializes this rope data as a tag compound for world saving.
    /// </summary>
    public override TagCompound Serialize()
    {
        return new TagCompound()
        {
            ["Position"] = Position,
            ["Sag"] = Sag,
            ["MaxLength"] = MaxLength,
            ["Direction"] = Direction
        };
    }

    /// <summary>
    ///     Deserializes a tag compound containing data for a rope back into said rope.
    /// </summary>
    public override HangingLanternRopeData Deserialize(TagCompound tag)
    {
        HangingLanternRopeData ropeData = new HangingLanternRopeData(tag.Get<Point>("Position"), tag.GetFloat("Sag"))
        {
            MaxLength = tag.GetFloat("MaxLength"),
            Direction = tag.GetInt("Direction")
        };

        return ropeData;
    }
}
