using System;
using System.Collections.Generic;
using System.Linq;
using IdolOfMadderCrimson.Content.Tiles.Generic;
using IdolOfMadderCrimson.Core.Physics;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.DataStructures;
using NoxusBoss.Core.Graphics.LightingMask;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace IdolOfMadderCrimson.Content.Tiles.ForgottenShrine;

public class ShrinePillarRopeData : WorldOrientedTileObject
{
    private Point end;

    private static readonly Asset<Texture2D> beadsTexture = ModContent.Request<Texture2D>("IdolOfMadderCrimson/Content/Tiles/ForgottenShrine/ShrineRopeBeads");

    /// <summary>
    ///     The amount of beads this rope should have.
    /// </summary>
    public int BeadCount
    {
        get;
        set;
    }

    /// <summary>
    ///     A general purpose identifier number used for this rope for RNG determinations.
    /// </summary>
    public int ID
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
    public static float Gravity => 0.5f;

    public ShrinePillarRopeData() { }

    public ShrinePillarRopeData(Point start, Point end, int beadCount, float sag)
    {
        Vector2 startVector = start.ToVector2();
        Vector2 endVector = end.ToVector2();
        BeadCount = beadCount;
        Sag = sag;
        ID = Main.rand.Next();

        Position = start;
        this.end = end;

        MaxLength = RopeManagerSystem.CalculateSegmentLength(Vector2.Distance(Start.ToVector2(), End.ToVector2()), Sag);

        int segmentCount = 30;
        VerletRope = ModContent.GetInstance<RopeManagerSystem>().RequestNew(startVector, endVector, segmentCount, MaxLength / segmentCount, Vector2.UnitY * Gravity, new RopeSettings()
        {
            TileColliderArea = Vector2.One * 5f,
            StartIsFixed = true,
            EndIsFixed = true,
            Mass = 0.5f,
            RespondToEntityMovement = true,
            RespondToWind = true
        }, 12);
    }

    private void DrawProjectionButItActuallyWorks(Texture2D projection, Vector2 drawOffset, Func<float, Color> colorFunction, int? projectionWidth = null, int? projectionHeight = null, float widthFactor = 1f, bool unscaledMatrix = false)
    {
        if (VerletRope is not RopeHandle rope)
            return;

        List<Vector2> positions = rope.Positions.ToList();
        positions.Add(End.ToVector2());

        ManagedShader overlayShader = ShaderManager.GetShader("IdolOfMadderCrimson.LitPrimitiveOverlayShader");
        overlayShader.TrySetParameter("exposure", 1f);
        overlayShader.TrySetParameter("screenSize", WotGUtils.ViewportSize);
        overlayShader.TrySetParameter("zoom", Main.GameViewMatrix.Zoom);
        overlayShader.SetTexture(MiscTexturesRegistry.Pixel.Value, 1, SamplerState.LinearClamp);
        overlayShader.SetTexture(LightingMaskTargetManager.LightTarget, 2);
        overlayShader.Apply();

        PrimitiveSettings settings = new PrimitiveSettings((float _) => projection.Width * widthFactor, colorFunction.Invoke, (float _) => drawOffset + Main.screenPosition, Smoothen: true, Pixelate: false, overlayShader, projectionWidth, projectionHeight, unscaledMatrix);
        PrimitiveRenderer.RenderTrail(positions, settings, 36);
    }

    /// <summary>
    ///     Renders this rope.
    /// </summary>
    public override void Render()
    {
        if (VerletRope is not RopeHandle rope)
            return;

        static Color ropeColorFunction(float completionRatio) => new Color(255, 28, 58);
        DrawProjectionButItActuallyWorks(MiscTexturesRegistry.Pixel.Value, -Main.screenPosition, ropeColorFunction, widthFactor: 2f);

        if (BeadCount >= 1)
        {
            UnifiedRandom rng = new UnifiedRandom(ID);
            DeCasteljauCurve positionCurve = new DeCasteljauCurve(rope.Positions.ToArray());
            Texture2D beadTexture = beadsTexture.Value;
            for (int i = 0; i < BeadCount; i++)
            {
                float positionInterpolant = MathHelper.SmoothStep(0.25f, 0.75f, i / (float)(BeadCount - 1f));
                if (BeadCount == 1)
                    positionInterpolant = 0.5f;

                int frameY = rng.Next(3);
                Rectangle frame = beadsTexture.Frame(1, 3, 0, frameY);
                Vector2 beadWorldPosition = positionCurve.Evaluate(positionInterpolant);
                Vector2 drawPosition = beadWorldPosition - Main.screenPosition;
                float beadRotation = beadWorldPosition.AngleTo(positionCurve.Evaluate(positionInterpolant + 0.001f));
                Main.spriteBatch.Draw(beadTexture, drawPosition, frame, Lighting.GetColor(beadWorldPosition.ToTileCoordinates()), beadRotation, frame.Size() * 0.5f, 0.5f, 0, 0f);
            }
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
            ["BeadCount"] = BeadCount,
            ["MaxLength"] = MaxLength,
            ["ID"] = ID
        };
    }

    /// <summary>
    ///     Deserializes a tag compound containing data for a rope back into said rope.
    /// </summary>
    public override ShrinePillarRopeData Deserialize(TagCompound tag)
    {
        ShrinePillarRopeData rope = new ShrinePillarRopeData(tag.Get<Point>("Start"), tag.Get<Point>("End"), tag.GetInt("BeadCount"), tag.GetFloat("Sag"))
        {
            MaxLength = tag.GetFloat("MaxLength"),
            ID = tag.GetInt("ID")
        };
        return rope;
    }
}
