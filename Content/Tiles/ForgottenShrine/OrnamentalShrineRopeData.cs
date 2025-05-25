using System;
using System.Collections.Generic;
using System.Linq;
using IdolOfMadderCrimson.Content.Tiles.Generic;
using IdolOfMadderCrimson.Core.Physics;
using Luminance.Assets;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Assets;
using NoxusBoss.Core.DataStructures;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace IdolOfMadderCrimson.Content.Tiles.ForgottenShrine;

public class OrnamentalShrineRopeData : WorldOrientedTileObject
{
    private Point end;

    private static readonly Asset<Texture2D> spiralTexture = ModContent.Request<Texture2D>("IdolOfMadderCrimson/Content/Tiles/ForgottenShrine/RopeSpiral");

    /// <summary>
    ///     A general-purpose timer used for wind movement on the baubles attached to this rope.
    /// </summary>
    public float WindTime
    {
        get;
        set;
    }

    /// <summary>
    ///     The amount by which this rope should sag when completely at rest.
    /// </summary>
    public float Sag
    {
        get;
        set;
    }

    /// <summary>
    ///     The starting position of the rope.
    /// </summary>
    public Point Start => Position;

    /// <summary>
    ///     The end position of the rope.
    /// </summary>
    public Point End
    {
        get => end;
        set
        {
            end = value;
            Vector2 endVector = end.ToVector2();
            ClampToMaxLength(ref endVector);

            if (VerletRope is RopeHandle rope)
                rope.End = endVector;
        }
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
    public static float Gravity => 0.65f;

    /// <summary>
    ///     The asset for the paper lantern texture used by this rope.
    /// </summary>
    public static readonly Asset<Texture2D> PaperLanternTexture = ModContent.Request<Texture2D>("IdolOfMadderCrimson/Content/Tiles/ForgottenShrine/PaperLantern");

    public OrnamentalShrineRopeData() { }

    public OrnamentalShrineRopeData(Point start, Point end, float sag)
    {
        Vector2 startVector = start.ToVector2();
        Vector2 endVector = end.ToVector2();
        Sag = sag;

        Position = start;
        this.end = end;

        MaxLength = RopeManagerSystem.CalculateSegmentLength(Vector2.Distance(Start.ToVector2(), End.ToVector2()), Sag);

        int segmentCount = 30;
        VerletRope = ModContent.GetInstance<RopeManagerSystem>().RequestNew(startVector, endVector, segmentCount, MaxLength / segmentCount, Vector2.UnitY * Gravity, new RopeSettings()
        {
            TileColliderArea = Vector2.One * 5f,
            StartIsFixed = true,
            EndIsFixed = true,
            RespondToEntityMovement = true,
            RespondToWind = false // Handled manually.
        }, 15);
    }

    private void ClampToMaxLength(ref Vector2 end)
    {
        Vector2 startVector = Start.ToVector2();
        if (!end.WithinRange(startVector, MaxLength))
            end = startVector + (end - startVector).SafeNormalize(Vector2.Zero) * MaxLength;
    }

    /// <summary>
    ///     Updates this rope.
    /// </summary>
    public override void Update()
    {
        bool startHasNoTile = !Framing.GetTileSafely(Start.ToVector2().ToTileCoordinates()).HasTile;
        bool endHasNoTile = !Framing.GetTileSafely(End.ToVector2().ToTileCoordinates()).HasTile;
        if (startHasNoTile || endHasNoTile)
        {
            ModContent.GetInstance<OrnamentalShrineRopeSystem>().Remove(this);
            VerletRope?.Dispose();
            return;
        }

        WindTime = (WindTime + MathF.Abs(Main.windSpeedCurrent) * 0.11f) % (MathHelper.TwoPi * 5000f);
    }

    private void DrawProjectionButItActuallyWorks(Texture2D projection, Vector2 drawOffset, bool flipHorizontally, Func<float, Color> colorFunction, int? projectionWidth = null, int? projectionHeight = null, float widthFactor = 1f, bool unscaledMatrix = false)
    {
        if (VerletRope is not RopeHandle rope)
            return;

        ManagedShader shader = ShaderManager.GetShader("NoxusBoss.PrimitiveProjection");
        Main.instance.GraphicsDevice.Textures[1] = projection;
        Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.AnisotropicClamp;
        Main.instance.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
        shader.TrySetParameter("horizontalFlip", flipHorizontally);
        shader.TrySetParameter("heightRatio", (float)projection.Height / projection.Width);
        shader.TrySetParameter("lengthRatio", 1f);
        List<Vector2> positions = rope.Positions.ToList();
        positions.Add(End.ToVector2());

        PrimitiveSettings settings = new PrimitiveSettings((float _) => projection.Width * widthFactor, colorFunction.Invoke, (float _) => drawOffset + Main.screenPosition, Smoothen: true, Pixelate: false, shader, projectionWidth, projectionHeight, unscaledMatrix);
        PrimitiveRenderer.RenderTrail(positions, settings, 36);
    }

    /// <summary>
    ///     Renders this rope.
    /// </summary>
    public override void Render()
    {
        if (VerletRope is not RopeHandle rope)
            return;

        static Color ropeColorFunction(float completionRatio) => new Color(63, 22, 32);
        DrawProjectionButItActuallyWorks(MiscTexturesRegistry.Pixel.Value, -Main.screenPosition, false, ropeColorFunction, widthFactor: 2f);

        DeCasteljauCurve positionCurve = new DeCasteljauCurve(rope.Positions.ToArray());

        Main.instance.LoadProjectile(ProjectileID.ReleaseLantern);

        int ornamentCount = 7;
        Texture2D glowTexture = GennedAssets.Textures.GreyscaleTextures.BloomCirclePinpoint;
        for (int i = 0; i < ornamentCount; i++)
        {
            float sampleInterpolant = MathHelper.Lerp(0.06f, 0.8f, i / (float)(ornamentCount - 1f));
            Vector2 ornamentWorldPosition = positionCurve.Evaluate(sampleInterpolant);

            Lighting.AddLight(ornamentWorldPosition, Color.Wheat.ToVector3() * 0.4f);

            int windGridTime = 33;
            Point ornamentTilePosition = ornamentWorldPosition.ToTileCoordinates();
            if (!WorldGen.InWorld(ornamentTilePosition.X, ornamentTilePosition.Y))
                continue;

            Main.instance.TilesRenderer.Wind.GetWindTime(ornamentTilePosition.X, ornamentTilePosition.Y, windGridTime, out int windTimeLeft, out int direction, out _);
            float windGridInterpolant = windTimeLeft / (float)windGridTime;
            float windGridRotation = Utils.GetLerpValue(0f, 0.5f, windGridInterpolant, true) * Utils.GetLerpValue(1f, 0.5f, windGridInterpolant, true) * direction * -0.93f;

            // Draw ornamental spirals.
            float windForceWave = LumUtils.AperiodicSin(WindTime * 0.4f + ornamentWorldPosition.X * 0.095f);
            float windForce = windForceWave * LumUtils.InverseLerp(0f, 0.75f, MathF.Abs(Main.windSpeedCurrent)) * 0.4f;
            float spiralRotation = WindTime + ornamentWorldPosition.X * 0.02f;
            Vector2 spiralDrawPosition = ornamentWorldPosition - Main.screenPosition + Vector2.UnitY * 3f;
            Main.spriteBatch.Draw(spiralTexture.Value, spiralDrawPosition, null, Color.White, spiralRotation, spiralTexture.Size() * 0.5f, 1f, 0, 0f);

            // Draw lanterns.
            Texture2D lanternTexture = TextureAssets.Projectile[ProjectileID.ReleaseLantern].Value;
            sampleInterpolant = MathHelper.Lerp(0.06f, 0.8f, (i + 0.5f) / (float)(ornamentCount - 1f));
            float lanternRotation = LumUtils.AperiodicSin(WindTime * 0.23f) * 0.45f + windGridRotation + (positionCurve.Evaluate(sampleInterpolant + 0.001f) - positionCurve.Evaluate(sampleInterpolant)).ToRotation();
            Vector2 lanternWorldPosition = positionCurve.Evaluate(sampleInterpolant);
            Vector2 lanternDrawPosition = lanternWorldPosition - Main.screenPosition;
            Vector2 lanternGlowDrawPosition = lanternDrawPosition + Vector2.UnitY.RotatedBy(lanternRotation) * 8f;
            Rectangle lanternFrame = lanternTexture.Frame(1, 4, 0, i % 4);
            Color lanternGlowColor = new Color(1f, 1f, 0.4f, 0f);
            float lanternGlowOpacity = 0.36f;
            float lanternGlowScaleFactor = 1f;
            float lanternScale = 0.8f;
            if (i == ornamentCount / 2)
            {
                lanternTexture = PaperLanternTexture.Value;
                lanternFrame = lanternTexture.Frame();
                lanternGlowColor = new Color(1f, 0.2f, 0f, 0f);
                lanternGlowOpacity = 0.5f;
                lanternRotation *= 0.33f;
                lanternGlowDrawPosition = lanternDrawPosition + Vector2.UnitY.RotatedBy(lanternRotation) * 26f;
                lanternGlowScaleFactor = 1.6f;
                lanternScale = 0.8f;
            }

            Main.spriteBatch.Draw(lanternTexture, lanternDrawPosition, lanternFrame, Color.White, lanternRotation, lanternFrame.Size() * new Vector2(0.5f, 0f), lanternScale, 0, 0f);
            Main.spriteBatch.Draw(glowTexture, lanternGlowDrawPosition, null, lanternGlowColor * lanternGlowOpacity, 0f, glowTexture.Size() * 0.5f, lanternGlowScaleFactor * 0.5f, 0, 0f);
            Main.spriteBatch.Draw(glowTexture, lanternGlowDrawPosition, null, lanternGlowColor * lanternGlowOpacity * 0.6f, 0f, glowTexture.Size() * 0.5f, lanternGlowScaleFactor * 1.1f, 0, 0f);
        }
    }

    /// <summary>
    ///     Serializes this rope data as a tag compound for world saving.
    /// </summary>
    public override TagCompound Serialize()
    {
        return new TagCompound()
        {
            ["Start"] = Start,
            ["End"] = End,
            ["Sag"] = Sag,
            ["MaxLength"] = MaxLength
        };
    }

    /// <summary>
    ///     Deserializes a tag compound containing data for a rope back into said rope.
    /// </summary>
    public override OrnamentalShrineRopeData Deserialize(TagCompound tag)
    {
        OrnamentalShrineRopeData rope = new OrnamentalShrineRopeData(tag.Get<Point>("Start"), tag.Get<Point>("End"), tag.GetFloat("Sag"))
        {
            MaxLength = tag.GetFloat("MaxLength")
        };
        return rope;
    }
}
