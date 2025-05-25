using IdolOfMadderCrimson.Content.Subworlds;
using IdolOfMadderCrimson.Content.Tiles.ForgottenShrine;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Assets;
using System;
using Terraria;
using Terraria.ModLoader;

namespace IdolOfMadderCrimson.Content.NPCs.Bosses.Idol;

public partial class IdolSummoningRitualSystem : ModSystem
{
    private static float MaxDarknessFactor => 1.33f;

    private void Perform_OpenStatueEye()
    {
        int eyeOpenTime = 150;
        float animationCompletion = Timer / (float)eyeOpenTime;
        ForgottenShrineDarknessSystem.Darkness = MathHelper.Lerp(ForgottenShrineDarknessSystem.StandardDarkness, MaxDarknessFactor, animationCompletion);
        IdolStatueManager.WaterFlowCutoffInterpolant = LumUtils.InverseLerp(0f, 0.26f, animationCompletion);
        IdolStatueManager.ExtraDrawAction = p => DrawGlowOnStatueEye(animationCompletion, p);

        if (animationCompletion >= 1f)
            SwitchState(IdolSummoningRitualState.BatheWorldInCrimson);
    }

    private static void DrawGlowOnStatueEye(float animationCompletion, Vector2 drawPosition)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        float shineInterpolant = LumUtils.InverseLerp(0.26f, 0.54f, animationCompletion);
        float pulse = 1f + MathF.Cos(Main.GlobalTimeWrappedHourly * 93f) * shineInterpolant * 0.1f;
        ManagedShader shineShader = ShaderManager.GetShader("NoxusBoss.RadialShineShader");
        shineShader.Apply();

        Texture2D noise = GennedAssets.Textures.Noise.WavyBlotchNoise.Value;
        Vector2 eyePosition = drawPosition - Vector2.UnitY * 96f;
        Main.spriteBatch.Draw(noise, eyePosition, null, Color.Red * shineInterpolant * 0.1f, 0f, noise.Size() * 0.5f, shineInterpolant * pulse * 0.61f, 0, 0f);

        Main.spriteBatch.PrepareForShaders();
    }
}
