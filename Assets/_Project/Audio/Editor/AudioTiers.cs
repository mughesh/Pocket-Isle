using System.Collections.Generic;

namespace Diorama.Audio.EditorTools
{
    /// <summary>Which loudness tier a cue belongs to (from its group and id), and which cues are hits (short tails).</summary>
    public static class AudioTiers
    {
        static readonly HashSet<string> HitIds = new HashSet<string>
        {
            "move.slam", "walk.bump", "catch.hit", "catch.wobble", "log.drop", "keeper.stomp", "pads.rise", "island.release",
            "keeper.crown.shield", "shard.dock", "statue.socket", "rune.select", "walk.stop",
        };

        static readonly HashSet<string> BigIds = new HashSet<string>
        {
            "catch.success", "evo.burst", "restore.swell", "statue.socket",
        };

        static readonly HashSet<string> UiIds = new HashSet<string>
        {
            "xp.tick", "zoom.in", "zoom.out", "island.recenter", "lodge.page", "lodge.podTick", "ui.saved", "ui.lesson", "ui.pebble",
            "path.gold",
        };

        public static bool IsHit(string id) => id.StartsWith("hit.") || id.StartsWith("move.impact.") || HitIds.Contains(id);

        public static LoudnessTier Default(string group, string id, bool loop)
        {
            if (group == "Music" || id.StartsWith("music.")) return LoudnessTier.Music;
            if (group == "Ambience" || id.StartsWith("amb.")) return LoudnessTier.Bed;
            if (id.StartsWith("sw.")) return LoudnessTier.Sweetener;
            if (group == "Cries" || id.StartsWith("cry.")) return LoudnessTier.Voice;
            if (loop) return LoudnessTier.Hum;
            if (group == "Jingles" || id.StartsWith("jingle.") || BigIds.Contains(id)) return LoudnessTier.Big;
            if (group == "Runes" || id.StartsWith("rune.") || id.StartsWith("menu.") || id.StartsWith("ui.") || UiIds.Contains(id))
                return LoudnessTier.UI;
            if (group == "Walk") return LoudnessTier.Steps;
            if (group == "Spirit" || group == "Voices" || id == "explorer.stretch") return LoudnessTier.Voice;
            if (group == "Moves" || group == "Hits" || group == "Catch" || group == "Keeper" || id.StartsWith("pod.throw")) return LoudnessTier.Moves;
            return LoudnessTier.Island;
        }

        /// <summary>The tier a cue's clips are levelled to: its patch's when it has one, otherwise the default.</summary>
        public static LoudnessTier ForCue(AudioCue cue, SynthPatch patch)
        {
            if (patch != null && patch.levelMode == LevelMode.Tier) return patch.tier;
            var r = SynthRecipes.Find(cue.id);
            if (r != null && r.tier.HasValue) return r.tier.Value;
            return Default(AudioCueTools.Group(cue), cue.id, cue.loop);
        }

        public static float TrimFor(AudioCue cue, SynthPatch patch)
        {
            if (patch != null && patch.levelMode == LevelMode.Tier) return patch.tierTrimDb;
            var r = SynthRecipes.Find(cue.id);
            return r != null ? r.trimDb : 0f;
        }
    }
}
