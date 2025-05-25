﻿using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

namespace IdolOfMadderCrimson.Common.Graphics;

public class ParticleEngine : ILoadable
{
    /// <summary>
    ///     Renders over dusts.
    /// </summary>
    public static readonly ParticleRenderer Particles = new ParticleRenderer();

    /// <summary>
    ///     Clears all particle contents.
    /// </summary>
    public static void Clear() => Particles.Clear();

    public void Load(Mod mod)
    {
        On_Main.UpdateParticleSystems += UpdateParticles;
        On_Main.DrawDust += DrawParticles;
    }

    private void UpdateParticles(On_Main.orig_UpdateParticleSystems orig, Main self)
    {
        orig(self);
        Particles.Update();
    }

    private void DrawParticles(On_Main.orig_DrawDust orig, Main self)
    {
        orig(self);

        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        Particles.Settings.AnchorPosition = -Main.screenPosition;
        Particles.Draw(Main.spriteBatch);
        Main.spriteBatch.End();
    }

    public void Unload() { }
}
