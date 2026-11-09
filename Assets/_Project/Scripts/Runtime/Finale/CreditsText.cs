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
        public static readonly bool PaidSoundsDone = true;

        public const string SoundsFreePlan = "Sounds made with elevenlabs.io (free plan)";
        public const string SoundsAfterRegen = "Sounds: synthesised and generated for Pocket Isle; some sounds by Kenney (CC0)";

        public static string[] Lines => PaidSoundsDone
            ? new[]
            {
                "Pocket Isle", "Made by Mugesh", "Music: Mugesh", "Islings, realms and Keepers: original designs", SoundsAfterRegen,
                "Font: Liberation Sans (SIL Open Font License)",
            }
            : new[]
            {
                "Pocket Isle", "Made by Mugesh", "Music: Mugesh", "Islings, realms and Keepers: original designs", SoundsFreePlan,
                "Some sounds by Kenney (CC0)", "Font: Liberation Sans (SIL Open Font License)",
            };

        /// <summary>Step 19: one sky lantern's credit (a bold first line, a smaller second); the last one is the big portrait lantern.</summary>
        public struct Lantern
        {
            public string title, subtitle;
            public bool big;

            public Lantern(string title, string subtitle, bool big = false)
            {
                this.title = title;
                this.subtitle = subtitle;
                this.big = big;
            }
        }

        /// <summary>
        /// Step 19: the finale's credits, one per sky lantern, in release order; the big one (the framed portrait) last.
        /// Same content as <see cref="Lines"/> (the Settings card keeps <see cref="Block"/>); the step 15b swap applies here too.
        /// </summary>
        public static Lantern[] Lanterns => new[]
        {
            new Lantern("Islings, realms and Keepers", "original designs"),
            new Lantern("Music", "Mugesh"),
            PaidSoundsDone
                ? new Lantern("Sounds: synthesised and generated for Pocket Isle", "Some sounds: Kenney (CC0)")
                : new Lantern("Sounds: elevenlabs.io (free plan)", "Some sounds: Kenney (CC0)"),
            new Lantern("Font: Liberation Sans", "SIL Open Font License"),
            new Lantern("Pocket Isle", "Made by Mugesh", true),
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
