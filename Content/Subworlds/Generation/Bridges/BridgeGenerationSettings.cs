using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace IdolOfMadderCrimson.Content.Subworlds.Generation.Bridges;

public class BridgeGenerationSettings
{
    /// <summary>
    ///     The width of beams for the bridge before the shrine.
    /// </summary>
    public int BridgeBeamWidth
    {
        get
        {
            /*
             * The height of a bridge's arch is defined by the following equation: round(height * abs(sin(pi * x / width)))
             * Beams should be generated at points where this equation is sufficiently low, aka at a point where the arch is at its
             * base. Represented mathematically, this is asking where the above height equation is less than some height value.
             * For the sake of demonstration, a value of one will be used as the "sufficiently low" cutoff.
             * The following reductions exist to explain the process that lead to the math math below.
             * 
                round(height * abs(sin(pi * x / width))) < 1
                height * abs(sin(pi * x / width)) < 1
                abs(sin(pi * x / width)) < 1 / height
                sin(pi * x / width) < 1 / height
                sin(pi * x / width) = 1 / height
                pi * x / width = arcsin(1 / height)
                x = arcsin(1 / height) * width / pi
             */

            // For a bit of artistic preference, 0.5 will be used instead of 1 like in the original equation, making the beams a bit thinner.
            float beamThicknessCoefficient = 0.5f;
            float intermediateArcsine = MathF.Asin(beamThicknessCoefficient / BridgeArchHeight);
            int beamWidth = (int)MathF.Round(intermediateArcsine * BridgeArchWidth / MathHelper.Pi);

            // Account for the case in which the bridge is perfectly flat, wherein the above calculations would result in a division by zero.
            if (BridgeArchHeight == 0)
                beamWidth = BridgeArchWidth / 33;

            return beamWidth;
        }
    }

    /// <summary>
    ///     The height of beams for the bridge before the shrine.
    /// </summary>
    public int BridgeBeamHeight;

    /// <summary>
    ///     The width of arches on the bridge.
    /// </summary>
    public int BridgeArchWidth;

    /// <summary>
    ///     The amount of horizontal coverage that ropes underneath the bridge have, in tile coordinates.
    /// </summary>
    public int BridgeUndersideRopeWidth;

    /// <summary>
    ///     The amount of vertical coverage that ropes beneath the bridge should have due to sag, in tile coordinates.
    /// </summary>
    public int BridgeUndersideRopeSag;

    /// <summary>
    ///     The maximum height of arches on the bridge.
    /// </summary>
    public int BridgeArchHeight;

    /// <summary>
    ///     The factor by which arches are exaggerated for big bridges with a rooftop.
    /// </summary>
    public float BridgeArchHeightBigBridgeFactor;

    /// <summary>
    ///     The vertical thickness of the bridge.
    /// </summary>
    public int BridgeThickness;

    /// <summary>
    ///     The amount of bridges it takes on a cyclical basis in order for a bridge to be created with a roof.
    /// </summary>
    public int BridgeRooftopsPerBridge;

    /// <summary>
    ///     The amount of vertical space taken up by bottom of rooftops.
    /// </summary>
    public int BridgeRooftopDynastyWoodLayerHeight;

    /// <summary>
    ///     The amount of vertical space dedicated to walls above pillars but below rooftops.
    /// </summary>
    public int BridgeRoofWallUndersideHeight;

    /// <summary>
    ///     The baseline height of walls behind the bridge. Determines things such as how high rooftops are as a baseline.
    /// </summary>
    public int BridgeBackWallHeight;

    /// <summary>
    ///     The width of the dock.
    /// </summary>
    public int DockWidth;

    /// <summary>
    ///     The set of possible rooftops that can be selected for a given roof on the bridge.
    /// </summary>
    public List<ShrineRooftopSet> BridgeRooftopConfigurations = new List<ShrineRooftopSet>();
}
