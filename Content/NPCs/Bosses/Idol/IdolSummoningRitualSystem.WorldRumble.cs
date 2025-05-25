using Terraria.ModLoader;

namespace IdolOfMadderCrimson.Content.NPCs.Bosses.Idol;

public partial class IdolSummoningRitualSystem : ModSystem
{
    /// <summary>
    ///     The intensity of rumbles created by this ritual.
    /// </summary>
    public float RumbleInterpolant
    {
        get;
        set;
    }

    private void Perform_WorldRumble()
    {
        int rumbleBuildupTime = 180;
        RumbleInterpolant = LumUtils.InverseLerp(0f, rumbleBuildupTime, Timer);
        BaseWindSoundVolume = RumbleInterpolant;

        if (Timer >= rumbleBuildupTime)
            SwitchState(IdolSummoningRitualState.OpenStatueEye);
    }
}
