namespace Diorama
{
    /// <summary>
    /// Step 16: the credits, one place for the finale's plaque and the Settings card. No brand names except what a licence
    /// requires: the ElevenLabs free plan requires its line until the paid regeneration (step 15b); then switch
    /// <see cref="PaidSoundsDone"/> and the line becomes <see cref="SoundsAfterRegen"/>. No Unity / Meta XR SDK lines.
    /// </summary>
    public static class CreditsText
    {
        /// <summary>Step 15b (paid sound regeneration) done: the free-plan line goes.</summary>
        public static readonly bool PaidSoundsDone = false;

        public const string SoundsFreePlan = "Sounds made with elevenlabs.io (free plan)";
        public const string SoundsAfterRegen = "Sounds: synthesised and generated for Pocket Isle; some sounds by Kenney (CC0)";

        public static string[] Lines => PaidSoundsDone
            ? new[]
            {
                "Pocket Isle", "Made by Mugesh", "Islings, realms and Keepers: original designs", SoundsAfterRegen,
                "Font: Liberation Sans (SIL Open Font License)",
            }
            : new[]
            {
                "Pocket Isle", "Made by Mugesh", "Islings, realms and Keepers: original designs", SoundsFreePlan,
                "Some sounds by Kenney (CC0)", "Font: Liberation Sans (SIL Open Font License)",
            };

        /// <summary>The Settings card's text (blank line after the title).</summary>
        public static string Block
        {
            get
            {
                var l = Lines;
                return l[0] + "\n\n" + string.Join("\n", l, 1, l.Length - 1);
            }
        }
    }
}
