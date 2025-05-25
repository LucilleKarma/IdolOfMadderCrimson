using IdolOfMadderCrimson.Common.Graphics;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

namespace IdolOfMadderCrimson.Content.Particles;

public class CartoonWaveParticle : BaseParticle
{
    /// <summary>
    /// The particle pool responsible for wave particles.
    /// </summary>
    public static ParticlePool<CartoonWaveParticle> Pool
    {
        get;
        private set;
    } = new ParticlePool<CartoonWaveParticle>(256, GetNewParticle<CartoonWaveParticle>);

    /// <summary>
    /// The position of this wave.
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// The velocity of this wave.
    /// </summary>
    public Vector2 Velocity;

    /// <summary>
    /// The rotation of this wave.
    /// </summary>
    public float Rotation;

    /// <summary>
    /// The base color of this wave.
    /// </summary>
    public Color Color;

    /// <summary>
    /// How long this wave has existed for, in frames.
    /// </summary>
    public int Time;

    /// <summary>
    /// How long this wave should exist for, in frames.
    /// </summary>
    public int Lifetime;

    /// <summary>
    /// The scale of this wave.
    /// </summary>
    public Vector2 Scale;

    private static readonly Asset<Texture2D> waveTexture = ModContent.Request<Texture2D>("IdolOfMadderCrimson/Content/Particles/CartoonWave");

    public void Prepare(Vector2 position, Vector2 velocity, int lifetime, Color color, Vector2 scale)
    {
        Position = position;
        Velocity = velocity;
        Color = color;
        Scale = scale;
        Lifetime = lifetime;
    }

    public override void FetchFromPool()
    {
        base.FetchFromPool();
        Time = 0;
    }

    public override void Update(ref ParticleRendererSettings settings)
    {
        Position += Velocity;
        Scale *= 1.03f;
        Velocity *= 0.967f;
        Rotation = Velocity.ToRotation() - MathHelper.Pi;

        Time++;
        if (Time >= Lifetime)
            ShouldBeRemovedFromRenderer = true;
    }

    public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
    {
        float opacity = LumUtils.InverseLerpBump(0f, 0.1f, 0.25f, 1f, Time / (float)Lifetime).Squared();
        Texture2D texture = waveTexture.Value;
        Main.spriteBatch.Draw(texture, Position + settings.AnchorPosition, null, Color * opacity, Rotation, texture.Size() * 0.5f, Scale, 0, 0);
    }
}
