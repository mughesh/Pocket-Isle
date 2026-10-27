using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Ambience profiles (Data/Ambience/*.asset, made from the defaults below) and the Audio Lab's Ambience tab: it renders a
    /// minute of a profile offline (bed + sweeteners scheduled like AmbienceDirector does, panned by where they land on the
    /// island) into one stereo clip and plays it in Edit mode, with a play head over the event list.
    /// </summary>
    public static class AmbienceTools
    {
        public const string ProfileRoot = SynthBaker.AudioRoot + "Data/Ambience/";
        const float IslandRadius = 0.35f;

        static SweetenerSet S(string cue, float min, float max, float volume = 1f, int maxAtOnce = 1) =>
            new SweetenerSet { cue = cue, minInterval = min, maxInterval = max, volume = volume, maxAtOnce = maxAtOnce };

        /// <summary>Default profile contents from the step B brief (SoundList "P0 Ambience").</summary>
        static readonly (string name, string bed, SweetenerSet[] sets, string notes)[] Defaults =
        {
            ("StillIsle", "amb.stillisle", new[] { S("sw.bird", 4f, 12f), S("sw.leaves", 6f, 15f), S("sw.gust", 8f, 20f, 0.8f), S("sw.drip", 5f, 15f, 0.6f) },
                "Calm meadow breeze; birds, leaf flutter, soft gusts, pond drips."),
            ("CinderwoodFaded", "amb.cinderwood", new[] { S("sw.crackle", 3f, 10f), S("sw.creak", 8f, 20f), S("sw.leaves", 8f, 18f), S("sw.bird", 20f, 45f, 0.5f) },
                "Hushed warm forest: ember pops near vents, creaks, a rare bird."),
            ("CinderwoodRestored", "amb.cinderwood.restored", new[] { S("sw.bird", 3f, 8f, 1f, 2), S("sw.crackle", 4f, 10f), S("sw.drip", 6f, 14f), S("sw.leaves", 8f, 16f) },
                "Brighter: birds back, warm crackle, springs."),
            ("ForgeHall", "amb.forgehall", new[] { S("sw.crackle", 3f, 9f), S("sw.creak", 10f, 25f, 0.7f), S("sw.drip", 6f, 15f, 0.8f) },
                "Big quiet stone room, low ember glow; single pops, chain creak, drips."),
            ("Lodge", "amb.lodge", new[] { S("sw.crackle", 4f, 10f), S("sw.creak", 10f, 20f, 0.7f) },
                "Hearth room."),
            ("Battle", "amb.battle", new SweetenerSet[0],
                "Wind only, quiet under the battle music. Also Duck(-6, 0.5, pauseSweeteners: true) while a battle runs."),
        };

        [MenuItem("Tools/Pocket Isle/Audio/Create Ambience Profiles", priority = 97)]
        public static void CreateProfiles()
        {
            Directory.CreateDirectory(ProfileRoot);
            int made = 0;
            foreach (var d in Defaults)
            {
                var path = ProfileRoot + d.name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<AmbienceProfile>(path) != null) continue;
                var p = ScriptableObject.CreateInstance<AmbienceProfile>();
                p.bed = d.bed;
                p.sweeteners = d.sets.Select(s => JsonUtility.FromJson<SweetenerSet>(JsonUtility.ToJson(s))).ToList();
                p.notes = d.notes;
                AssetDatabase.CreateAsset(p, path);
                made++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[AudioLab] Ambience profiles: {made} new in {ProfileRoot}");
        }

        public static List<AmbienceProfile> Profiles() =>
            AssetDatabase.FindAssets("t:AmbienceProfile", new[] { ProfileRoot.TrimEnd('/') })
                .Select(g => AssetDatabase.LoadAssetAtPath<AmbienceProfile>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null).OrderBy(p => p.name).ToList();

        // ---- offline preview ----

        public struct Event
        {
            public float time, pan, volume;
            public string cue, clip;
        }

        /// <summary>A stereo mix (interleaved) of the bed plus scheduled sweeteners. bedClip = index into the bed cue's clips.</summary>
        public static float[] Render(AmbienceProfile profile, float seconds, int seed, int bedClip, float duckDb, List<Event> events)
        {
            const int rate = SynthPatch.SampleRate;
            int frames = Mathf.CeilToInt(seconds * rate);
            var mix = new float[frames * 2];
            var cues = AudioCueTools.AllCues().ToDictionary(c => c.id, c => c);
            var cache = new Dictionary<AudioClip, float[]>();

            float[] Samples(AudioClip c)
            {
                if (!cache.TryGetValue(c, out var s)) cache[c] = s = ClipReader.ReadMono48k(c);
                return s;
            }

            if (!string.IsNullOrEmpty(profile.bed) && cues.TryGetValue(profile.bed, out var bedCue) && bedCue.clips.Length > 0)
            {
                var clip = bedCue.clips[Mathf.Clamp(bedClip, 0, bedCue.clips.Length - 1)];
                var bed = clip != null ? Samples(clip) : null;
                if (bed != null && bed.Length > 0)
                {
                    float g = bedCue.volume * profile.bedVolume * Mathf.Pow(10f, duckDb / 20f) * 0.7071f;
                    for (int i = 0; i < frames; i++)
                    {
                        float x = bed[i % bed.Length] * g;
                        mix[2 * i] += x;
                        mix[2 * i + 1] += x;
                    }
                }
            }

            var rng = new System.Random(seed);
            foreach (var set in profile.sweeteners)
            {
                if (!cues.TryGetValue(set.cue, out var cue)) continue;
                var clips = cue.clips.Where(c => c != null).ToArray();
                if (clips.Length == 0) continue;
                var ends = new List<float>();
                int last = -1;
                float t = Range(rng, set.minInterval * 0.5f, set.maxInterval);
                while (t < seconds)
                {
                    ends.RemoveAll(e => e <= t);
                    if (ends.Count < set.maxAtOnce)
                    {
                        int k = clips.Length == 1 ? 0 : rng.Next(clips.Length - 1);
                        if (clips.Length > 1 && k >= last && last >= 0) k++; // never the same clip twice in a row
                        last = k;
                        var src = Samples(clips[k]);
                        if (src != null)
                        {
                            float pitch = Range(rng, cue.pitchMin, cue.pitchMax) * (1f + set.pitchJitter * Range(rng, -1f, 1f));
                            float vol = cue.volume * set.volume * (1f - set.volumeJitter * (float)rng.NextDouble());
                            // A random point on the island disc, panned by its x (the island sits in front of the player).
                            float ang = (float)(rng.NextDouble() * Mathf.PI * 2), r = Mathf.Sqrt((float)rng.NextDouble()) * IslandRadius;
                            float pan = Mathf.Clamp(Mathf.Cos(ang) * r / IslandRadius * 0.7f, -1f, 1f);
                            float gl = Mathf.Cos((pan + 1f) * Mathf.PI / 4f) * vol, gr = Mathf.Sin((pan + 1f) * Mathf.PI / 4f) * vol;
                            int start = Mathf.RoundToInt(t * rate), len = Mathf.FloorToInt(src.Length / pitch);
                            for (int i = 0; i < len && start + i < frames; i++)
                            {
                                float x = i * pitch;
                                int a = (int)x, b = Mathf.Min(src.Length - 1, a + 1);
                                float s = Mathf.Lerp(src[a], src[b], x - a);
                                mix[2 * (start + i)] += s * gl;
                                mix[2 * (start + i) + 1] += s * gr;
                            }
                            ends.Add(t + len / (float)rate);
                            events?.Add(new Event { time = t, cue = set.cue, clip = clips[k].name, pan = pan, volume = vol });
                        }
                    }
                    t += Range(rng, set.minInterval, Mathf.Max(set.minInterval, set.maxInterval));
                }
            }
            events?.Sort((a, b) => a.time.CompareTo(b.time));
            for (int i = 0; i < mix.Length; i++) mix[i] = Mathf.Clamp(mix[i], -1f, 1f);
            return mix;
        }

        static float Range(System.Random rng, float lo, float hi) => lo + (float)rng.NextDouble() * (hi - lo);

        /// <summary>Bridge: a minute of every profile (mono sum) on one contact sheet, Temp/audio-analysis/AmbienceProfiles.png.</summary>
        public static void AnalyseProfiles()
        {
            var renders = new List<KeyValuePair<string, float[]>>();
            foreach (var p in Profiles())
            {
                var events = new List<Event>();
                var st = Render(p, 60f, 1, 0, 0f, events);
                var mono = new float[st.Length / 2];
                for (int i = 0; i < mono.Length; i++) mono[i] = 0.5f * (st[2 * i] + st[2 * i + 1]);
                renders.Add(new KeyValuePair<string, float[]>($"{p.name} ({events.Count} sweeteners)", mono));
            }
            AudioScope.Sheet("AmbienceProfiles", renders);
            Debug.Log($"[AudioLab] Ambience profiles analysed: {AudioScope.OutDir}AmbienceProfiles.png");
        }

        // ---- the Lab tab ----

        static int _profile, _bedClip, _seed = 1;
        static float _seconds = 60f, _duck;
        static AudioClip _clip;
        static List<Event> _events = new List<Event>();
        static double _startedAt = -1;
        static Vector2 _scroll;

        public static void DrawTab(EditorWindow window)
        {
            var profiles = Profiles();
            if (profiles.Count == 0)
            {
                EditorGUILayout.HelpBox("No ambience profiles yet.", MessageType.Info);
                if (GUILayout.Button("Create the six profiles")) CreateProfiles();
                return;
            }
            EditorGUILayout.HelpBox("A minute of an area: the bed plus sweeteners scheduled like AmbienceDirector does in game (random times, random " +
                                    "points on the island, panned left / right). Listen on headphones and on laptop speakers.", MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                _profile = Mathf.Clamp(EditorGUILayout.Popup("Profile", _profile, profiles.Select(p => p.name).ToArray()), 0, profiles.Count - 1);
                if (GUILayout.Button("Select", GUILayout.Width(60))) Selection.activeObject = profiles[_profile];
            }
            var profile = profiles[_profile];
            var bedCue = AudioCueTools.AllCues().Find(c => c.id == profile.bed);
            if (bedCue != null && bedCue.clips.Length > 0)
                _bedClip = EditorGUILayout.Popup("Bed variant", Mathf.Clamp(_bedClip, 0, bedCue.clips.Length - 1),
                    bedCue.clips.Select(c => c != null ? c.name : "(missing)").ToArray());
            else EditorGUILayout.LabelField("Bed variant", $"'{profile.bed}' has no clips");
            _seconds = EditorGUILayout.Slider("Seconds", _seconds, 10f, 120f);
            _seed = EditorGUILayout.IntField("Seed (another take)", _seed);
            _duck = EditorGUILayout.Slider(new GUIContent("Bed duck dB", "Battle / dialogue: -6"), _duck, -12f, 0f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play", GUILayout.Height(24)))
                {
                    _events.Clear();
                    var data = Render(profile, _seconds, _seed, _bedClip, _duck, _events);
                    if (_clip != null) Object.DestroyImmediate(_clip);
                    _clip = AudioPreview.MakeClip("ambience." + profile.name, data, 2);
                    AudioPreview.Play(_clip);
                    _startedAt = EditorApplication.timeSinceStartup;
                    EditorApplication.update -= Tick;
                    EditorApplication.update += Tick;
                    _window = window;
                }
                if (GUILayout.Button("Stop", GUILayout.Height(24), GUILayout.Width(60)))
                {
                    AudioPreview.Stop();
                    _startedAt = -1;
                }
                if (GUILayout.Button("Next take", GUILayout.Height(24), GUILayout.Width(80))) _seed++;
            }
            float now = _startedAt >= 0 ? (float)(EditorApplication.timeSinceStartup - _startedAt) : -1f;
            if (now > _seconds + 0.5f) _startedAt = -1;
            EditorGUILayout.LabelField(now >= 0 ? $"Playing {now:0.0} / {_seconds:0} s" : "Stopped", EditorStyles.miniBoldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var e in _events)
            {
                bool past = now >= 0 && e.time <= now;
                var style = past ? EditorStyles.boldLabel : EditorStyles.label;
                string side = e.pan < -0.2f ? "left" : e.pan > 0.2f ? "right" : "centre";
                EditorGUILayout.LabelField($"{e.time,6:0.0} s   {e.cue,-12} {e.clip}   {side}   vol {e.volume:0.00}", style);
            }
            EditorGUILayout.EndScrollView();
            if (profile.sweeteners.Any(s => AudioCueTools.AllCues().Find(c => c.id == s.cue) is AudioCue c && c.clips.Length == 0))
                EditorGUILayout.HelpBox("Some sweetener cues have no clips yet (birds, leaves, insects come from the gen batch); they are skipped.", MessageType.Info);
        }

        static EditorWindow _window;

        static void Tick()
        {
            if (_startedAt < 0)
            {
                EditorApplication.update -= Tick;
                if (_window != null) _window.Repaint();
                return;
            }
            if (_window != null) _window.Repaint();
        }
    }
}
