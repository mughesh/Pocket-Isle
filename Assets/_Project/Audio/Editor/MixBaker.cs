using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Bakes a synth patch mixed with each clip of a prefix (catch.shatter = synth sparkle + Kenney light glass), so the
    /// cue plays both layers at once: Clips/Synth/&lt;group&gt;/&lt;id&gt;_mix_vN.wav, mono 48 kHz, levelled like the patch (tier or peak).
    /// </summary>
    public static class MixBaker
    {
        public static string MixPath(SynthPatch patch, int k)
        {
            var group = SynthBaker.Group(patch);
            return SynthBaker.SynthClipRoot + (group.Length > 0 ? group + "/" : "") + $"{patch.id}_mix_v{k + 1}.wav";
        }

        public static List<AudioClip> Clips(SynthPatch patch, SynthRecipes.Recipe recipe)
        {
            var list = new List<AudioClip>();
            int n = AudioCueTools.ExtraClips(new[] { recipe.mixWith }).Count;
            for (int k = 0; k < n; k++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MixPath(patch, k));
                if (clip == null)
                {
                    Bake(patch, recipe, true);
                    clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MixPath(patch, k));
                }
                if (clip != null) list.Add(clip);
            }
            return list;
        }

        public static void Bake(SynthPatch patch, SynthRecipes.Recipe recipe, bool import)
        {
            var layers = AudioCueTools.ExtraClips(new[] { recipe.mixWith });
            if (layers.Count == 0)
            {
                Debug.LogError($"[AudioLab] {patch.id}: no clips at {recipe.mixWith} to mix with.");
                return;
            }
            for (int k = 0; k < layers.Count; k++)
            {
                var synth = SynthRenderer.Render(patch, k % Mathf.Max(1, patch.variants));
                var other = ReadMono48k(layers[k]);
                if (other == null) continue;
                var mix = new float[Mathf.Max(synth.Length, other.Length)];
                for (int i = 0; i < mix.Length; i++)
                    mix[i] = (i < synth.Length ? synth[i] : 0f) + (i < other.Length ? other[i] * recipe.mixGain : 0f);
                if (patch.levelMode == LevelMode.Tier)
                {
                    mix = Loudness.TrimSilence(mix, SynthPatch.SampleRate);
                    Loudness.LevelTo(mix, SynthPatch.SampleRate, Loudness.TierDb(patch.tier) + patch.tierTrimDb);
                }
                else
                {
                    float peak = SynthRenderer.Peak(mix), target = Mathf.Pow(10f, patch.peakDb / 20f);
                    if (peak > 1e-6f)
                        for (int i = 0; i < mix.Length; i++) mix[i] *= target / peak;
                }
                int fade = Mathf.Min(mix.Length / 2, 144);
                for (int i = 0; i < fade; i++) mix[mix.Length - 1 - i] *= i / (float)fade;
                var path = MixPath(patch, k);
                WavIO.Write(path, mix, 1, SynthPatch.SampleRate);
                if (import) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        /// <summary>Samples of an imported clip as mono 48 kHz, normalised to a 0.9 peak (null if unreadable).</summary>
        static float[] ReadMono48k(AudioClip clip)
        {
            clip.LoadAudioData();
            var data = new float[clip.samples * clip.channels];
            if (!clip.GetData(data, 0))
            {
                Debug.LogError($"[AudioLab] Can't read {AssetDatabase.GetAssetPath(clip)} (needs Decompress On Load).");
                return null;
            }
            int ch = clip.channels, frames = clip.samples;
            var mono = new float[frames];
            for (int i = 0; i < frames; i++)
            {
                float s = 0f;
                for (int c = 0; c < ch; c++) s += data[i * ch + c];
                mono[i] = s / ch;
            }
            float ratio = clip.frequency / (float)SynthPatch.SampleRate;
            int outLen = Mathf.FloorToInt(frames / ratio);
            var res = new float[outLen];
            for (int i = 0; i < outLen; i++)
            {
                float x = i * ratio;
                int a = Mathf.Min(frames - 1, (int)x);
                int b = Mathf.Min(frames - 1, a + 1);
                res[i] = Mathf.Lerp(mono[a], mono[b], x - a);
            }
            float peak = SynthRenderer.Peak(res);
            if (peak > 1e-6f)
                for (int i = 0; i < res.Length; i++) res[i] *= 0.9f / peak;
            return res;
        }

        /// <summary>Bridge: bake the mixes, sync cues, and put a "compare" note on the cues with synth + Kenney variants.</summary>
        public static void KenneyPass()
        {
            foreach (var r in SynthRecipes.All())
            {
                if (string.IsNullOrEmpty(r.mixWith)) continue;
                var patch = SynthBaker.AllPatches().Find(p => p.id == r.id);
                if (patch != null) Bake(patch, r, true);
            }
            AudioCueTools.SyncAll();
            foreach (var id in new[] { "walk.bump", "move.lunge", "move.slam", "island.scale" })
            {
                var patch = SynthBaker.AllPatches().Find(p => p.id == id);
                if (patch == null || !string.IsNullOrEmpty(patch.review.note)) continue;
                patch.review.note = "Compare: synth and Kenney variants are both in the cue (Play picks at random). Say which to keep.";
                EditorUtility.SetDirty(patch);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
