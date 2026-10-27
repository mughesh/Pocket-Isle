using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static Diorama.Audio.EditorTools.Syn;
using static Diorama.Audio.SynthWave;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Isling cries. Tools/Pocket Isle/Audio/Make Cry Profiles reads the species registry (read-only) and makes a CryProfile
    /// per species in Data/Cries/ (existing profiles are kept). The "cry" recipe set turns each profile into the patch and
    /// cue cry.&lt;speciesId&gt;, so Revert in the Lab rebuilds from the profile. Also the dialogue voices voice.keeper and
    /// voice.heart (the Spirit speaks with spirit.blip).
    /// </summary>
    [InitializeOnLoad]
    public static class CryTools
    {
        public const string Set = "cry";
        public const string CryRoot = SynthBaker.AudioRoot + "Data/Cries/";
        const string RegistryPath = "Assets/_Project/Data/Creatures/SpeciesRegistry.asset";

        static CryTools() => SynthRecipes.AddProvider(Recipes);

        // ---- syllable words ----

        /// <summary>A named syllable: vowel glide, length, pitch bend, trill.</summary>
        static CrySyllable W(string word)
        {
            switch (word)
            {
                case "mew": return S(CryVowel.EE, CryVowel.OO, 0.16f, 3f, -2f);
                case "pwee": return S(CryVowel.OO, CryVowel.EE, 0.14f, -3f, 4f);
                case "brrp": return S(CryVowel.ER, CryVowel.OH, 0.12f, 0f, -4f, 28f, 0.6f);
                case "pip": return S(CryVowel.EE, CryVowel.EE, 0.06f, 2f, 2f);
                case "coo": return S(CryVowel.OH, CryVowel.OO, 0.2f, 0f, -3f);
                case "yip": return S(CryVowel.EH, CryVowel.EE, 0.08f, 5f, 1f);
                case "hum": return S(CryVowel.OO, CryVowel.OO, 0.24f, 0f, 0f, 6f, 0.3f);
                case "wah": return S(CryVowel.OO, CryVowel.AH, 0.18f, -2f, 1f);
                case "chirr": return S(CryVowel.EE, CryVowel.EH, 0.12f, 4f, 2f, 35f, 0.5f);
                case "bwoh": return S(CryVowel.OO, CryVowel.OH, 0.2f, 2f, -5f);
                default: throw new ArgumentException("Unknown cry syllable " + word);
            }
        }

        static CrySyllable S(CryVowel a, CryVowel b, float len, float p0, float p1, float trill = 0f, float depth = 0f) =>
            new CrySyllable { from = a, to = b, length = len, pitchStart = p0, pitchEnd = p1, trill = trill, trillDepth = depth };

        /// <summary>Hand-picked words for base forms (evolved forms reuse their base). Unlisted species get "mew pip".</summary>
        static readonly Dictionary<string, string[]> Words = new Dictionary<string, string[]>
        {
            { "Kilnpot", new[] { "pip", "brrp" } },
            { "Pebbloop", new[] { "bwoh", "pip" } },
            { "Pufflet", new[] { "pwee", "coo" } },
            { "Ashmole", new[] { "hum", "pip" } },
            { "Cindercap", new[] { "pip", "pip", "mew" } },
            { "Coalbeetle", new[] { "chirr", "chirr" } },
            { "Embercrane", new[] { "pwee", "yip" } },
            { "Glazegolem", new[] { "bwoh", "hum" } },
            { "Kettlewing", new[] { "pwee", "pwee" } },
            { "Mapadillo", new[] { "brrp", "yip" } },
            { "Steambara", new[] { "coo", "hum" } },
            { "Toastling", new[] { "yip", "mew" } },
            { "Waxle", new[] { "mew", "pip" } },
            { "LanternSpirit", new[] { "pip", "pwee", "yip" } },
            { "Hearthstag", new[] { "bwoh", "coo" } },
        };

        static (float f1, float f2) Formants(CryVowel v)
        {
            switch (v)
            {
                case CryVowel.EE: return (270f, 2300f);
                case CryVowel.EH: return (530f, 1850f);
                case CryVowel.AH: return (730f, 1100f);
                case CryVowel.OH: return (570f, 840f);
                case CryVowel.OO: return (300f, 870f);
                default: return (490f, 1350f); // ER
            }
        }

        // ---- profiles ----

        [MenuItem("Tools/Pocket Isle/Audio/Make Cry Profiles (missing only)", priority = 93)]
        public static void MakeProfiles()
        {
            var registry = AssetDatabase.LoadAssetAtPath<SpeciesRegistry>(RegistryPath);
            if (registry == null)
            {
                Debug.LogError($"[AudioLab] No species registry at {RegistryPath}.");
                return;
            }
            Directory.CreateDirectory(CryRoot);
            // Base form of every evolution, by following evolvesTo.
            var baseOf = new Dictionary<string, string>();
            foreach (var s in registry.species)
                if (s != null && s.evolvesTo != null) baseOf[s.evolvesTo.name] = s.name;

            int made = 0;
            foreach (var s in registry.species)
            {
                if (s == null) continue;
                var path = $"{CryRoot}{s.name}.asset";
                if (AssetDatabase.LoadAssetAtPath<CryProfile>(path) != null) continue;
                var p = ScriptableObject.CreateInstance<CryProfile>();
                p.speciesId = s.name;
                p.element = (CryElement)(int)s.element;
                p.bodyHeight = s.recipe != null ? s.recipe.height : 0.045f;
                p.keeper = s.keeper;
                p.basePitch = PitchForSize(p.bodyHeight);
                if (baseOf.TryGetValue(s.name, out var from))
                {
                    p.evolvedFrom = from;
                    p.semitoneShift = -4f;
                    p.slow = 1.2f;
                    p.sub = true;
                }
                else
                {
                    var words = Words.TryGetValue(s.name, out var w) ? w : new[] { "mew", "pip" };
                    foreach (var word in words) p.syllables.Add(W(word));
                    if (p.keeper)
                    {
                        p.sub = true;
                        p.slow = 1.3f;
                    }
                }
                AssetDatabase.CreateAsset(p, path);
                made++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[AudioLab] Cry profiles: {made} new, {registry.species.Count} species.");
        }

        /// <summary>Tiny (3.4 cm) ~800 Hz down to huge (11 cm, the Keeper) ~180 Hz, snapped to D major pentatonic.</summary>
        public static float PitchForSize(float height)
        {
            float hz = 800f * Mathf.Pow(Mathf.Max(0.01f, height) / 0.034f, -1.27f);
            float midi = 69f + 12f * Mathf.Log(hz / 440f, 2f);
            int best = 0;
            float bestD = 999f;
            for (int m = 36; m <= 96; m++)
            {
                if (!SynthNotes.InScale(m)) continue;
                float d = Mathf.Abs(m - midi);
                if (d < bestD)
                {
                    bestD = d;
                    best = m;
                }
            }
            return SynthNotes.Hz(best);
        }

        public static List<CryProfile> Profiles()
        {
            var list = new List<CryProfile>();
            if (!AssetDatabase.IsValidFolder(CryRoot.TrimEnd('/'))) return list;
            foreach (var guid in AssetDatabase.FindAssets("t:CryProfile", new[] { CryRoot.TrimEnd('/') }))
            {
                var p = AssetDatabase.LoadAssetAtPath<CryProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if (p != null && !string.IsNullOrEmpty(p.speciesId)) list.Add(p);
            }
            list.Sort((a, b) => string.CompareOrdinal(a.speciesId, b.speciesId));
            return list;
        }

        // ---- recipes ----

        static IEnumerable<SynthRecipes.Recipe> Recipes()
        {
            var profiles = Profiles();
            var byId = new Dictionary<string, CryProfile>();
            foreach (var p in profiles) byId[p.speciesId] = p;
            foreach (var p in profiles)
            {
                var profile = p;
                yield return new SynthRecipes.Recipe
                {
                    set = Set, group = "Cries", id = "cry." + p.speciesId,
                    build = patch => Build(patch, profile, byId),
                    cue = SynthBatch1.Voice(p.keeper ? 0.75f : 0.6f).And2(c =>
                    {
                        c.maxDistance = p.keeper ? 2.5f : 1.5f;
                        c.priority = 50;
                        c.notes = "Hurt: SetPitch(1.25) and stop after ~60%. Faint: SetPitch(0.8).";
                    }),
                };
            }

            // Dialogue voices (the Spirit uses spirit.blip).
            yield return new SynthRecipes.Recipe
            {
                set = Set, group = "Cries", id = "voice.keeper",
                build = patch => patch.One(0.2f,
                        L(Buzz).Note("F#3").Pitch(1f, -1f, 0.15f).Env(0.02f, 0.14f, 0f, 0.05f, 0.02f).Formant(570f, 730f, 840f, 1100f, 0.15f, 7f, 0.85f).LP(2500f),
                        L(Sine).Note("F#2").Env(0.02f, 0.15f).Gain(0.4f),
                        L(Silence).Env(0.01f, 0.18f).Crackle(40f, 2f, 0.3f))
                    .Vars(6, 2f, 0.1f, 0.3f).Master(1f, 7000f),
                cue = SynthBatch1.Voice(0.5f),
            };
            yield return new SynthRecipes.Recipe
            {
                set = Set, group = "Cries", id = "voice.heart",
                build = patch => patch.One(0.34f,
                        L(Buzz).Note("D3").Pitch(0f, -2f, 0.3f).Env(0.05f, 0.25f, 0f, 0.05f, 0.04f).Vib(3f, 0.2f).Formant(300f, 570f, 870f, 840f, 0.28f, 6f, 0.85f).LP(1800f),
                        L(Sine).Note("D2").Env(0.05f, 0.3f).Gain(0.5f))
                    .Vars(6, 1.5f, 0.1f, 0.25f).Master(1f, 6000f),
                cue = SynthBatch1.Voice(0.55f),
            };
        }

        static Action<AudioCue> And2(this Action<AudioCue> a, Action<AudioCue> b) => c =>
        {
            a(c);
            b(c);
        };

        /// <summary>One profile to a patch: syllable layers (formant glides), an element layer, a sub for evolved forms.</summary>
        public static void Build(SynthPatch patch, CryProfile profile, Dictionary<string, CryProfile> byId)
        {
            var src = profile;
            if (!string.IsNullOrEmpty(profile.evolvedFrom))
            {
                if (byId.TryGetValue(profile.evolvedFrom, out var b) && string.IsNullOrEmpty(b.evolvedFrom)) src = b;
                else Debug.LogError($"[AudioLab] cry.{profile.speciesId}: base form '{profile.evolvedFrom}' has no profile; using its own syllables.");
            }
            float hz = src.basePitch * Mathf.Pow(2f, profile.semitoneShift / 12f);
            float slow = profile.slow;
            var el = profile.element;
            // Smaller voices have smaller mouths: formants rise a little with pitch.
            float fs = Mathf.Clamp(Mathf.Pow(hz / 400f, 0.2f), 0.85f, 1.25f);
            float attack = profile.keeper ? 0.04f : 0.012f;

            var layers = new List<SynthLayer>();
            float t = 0.01f;
            foreach (var s in src.syllables)
            {
                float len = s.length * slow;
                var a = Formants(s.from);
                var z = Formants(s.to);
                var wave = el == CryElement.Fire ? Buzz : el == CryElement.Neutral ? Buzz : SoftSaw;
                var l = L(wave, hz, "syllable")
                    .Pitch(s.pitchStart, s.pitchEnd, len, 0.8f)
                    .Env(attack, len, 0.7f, 0.06f)
                    .Gate(len)
                    .At(t)
                    .Formant(a.f1 * fs, z.f1 * fs, a.f2 * fs, z.f2 * fs, len, 7f, el == CryElement.Wind ? 0.6f : 0.85f);
                if (el == CryElement.Water) l.Vib(9f, 0.7f);
                else l.Vib(5.5f, 0.15f, 0.05f);
                if (el == CryElement.Neutral) l.amp = new SynthEnvelope { attack = 0.004f, decay = len * 0.8f, sustain = 0.2f, release = 0.05f };
                if (s.trill > 0f) l.Trem(s.trill, s.trillDepth);
                if (el == CryElement.Fire) l.LP(2800f);
                layers.Add(l);
                t += len + s.gap * slow;
            }
            float total = t;

            switch (el)
            {
                case CryElement.Fire:
                    layers.Add(L(Silence, 0f, "crackle tail").At(Mathf.Max(0f, total - 0.06f)).Env(0.005f, 0.25f).Crackle(140f, 1.5f, 0.7f).Gain(0.5f));
                    break;
                case CryElement.Water:
                    layers.Add(L(Sine, hz * 2f, "bubbles").Pitch(0f, 10f, 0.03f, 0.6f).Env(0.003f, 0.05f).Inst().Gain(0.5f));
                    break;
                case CryElement.Wind:
                    layers.Add(L(PinkNoise, 0f, "breath").BP(Mathf.Min(6000f, hz * 2.5f), -1f, 0.1f, 0.75f).Env(0.02f, total * 0.8f).Gain(0.35f));
                    break;
                default:
                    layers.Add(L(Triangle, hz * 2f, "wood pluck").Pitch(4f, 0f, 0.015f).Env(0.003f, 0.05f).BP(hz * 2.4f, -1f, 0.1f, 0.45f).Gain(0.5f));
                    break;
            }
            if (profile.sub)
                layers.Add(L(Sine, hz * 0.5f, "sub").Pitch(src.syllables.Count > 0 ? src.syllables[0].pitchStart : 0f, 0f, total).Env(0.02f, total, 0.6f, 0.1f).Gate(total).Gain(0.45f));
            if (profile.keeper)
                layers.Add(L(Silence, 0f, "crackle bed").Env(0.05f, total + 0.3f, 0.6f, 0.2f).Crackle(60f, 2.5f, 0.6f).Gain(0.4f));

            patch.One(total + 0.35f, layers.ToArray()).Vars(2, 0.4f, 0.06f, 0.08f).Master(profile.gain, 8000f);
            if (el == CryElement.Water)
            {
                int bubbles = layers.FindIndex(x => x.label == "bubbles");
                patch.notes = new List<SynthNote>();
                string[] pool = { "A5", "D6", "E6" };
                for (int i = 0; i < 3; i++)
                    patch.notes.Add(new SynthNote { note = pool[i], start = total - 0.08f + i * 0.05f, length = 0.05f, layer = bubbles, velocity = 0.6f - i * 0.1f });
            }
            uint h = 2166136261; // FNV-1a: a stable seed per species
            foreach (var ch in profile.speciesId) h = (h ^ ch) * 16777619;
            patch.seed = (int)(h & 0xffff);
        }

        /// <summary>Bridge: profiles, patches, bake, cues.</summary>
        public static void MakeAll()
        {
            MakeProfiles();
            SynthBaker.CreateAndBakeAll();
            AudioCueTools.SyncAll();
        }
    }
}
