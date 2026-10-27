using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>Samples of any audio asset as mono 48 kHz: .wav straight from disk, anything else through Unity's decoder.</summary>
    public static class ClipReader
    {
        public static float[] ReadMono48k(AudioClip clip)
        {
            if (clip == null) return null;
            var path = AssetDatabase.GetAssetPath(clip);
            if (Path.GetExtension(path).ToLowerInvariant() == ".wav")
            {
                var wav = WavIO.ReadMono(path, out int rate);
                if (wav != null) return Resample(wav, rate);
            }
            var data = Decode(clip, path, out int ch, out int freq);
            if (data == null) return null;
            int frames = data.Length / ch;
            var mono = new float[frames];
            for (int i = 0; i < frames; i++)
            {
                float s = 0f;
                for (int c = 0; c < ch; c++) s += data[i * ch + c];
                mono[i] = s / ch;
            }
            return Resample(mono, freq);
        }

        /// <summary>Interleaved samples; compressed or streaming clips are reimported as PCM for a moment.</summary>
        public static float[] Decode(AudioClip clip, string path, out int channels, out int rate)
        {
            bool temp = clip.loadType != AudioClipLoadType.DecompressOnLoad;
            if (temp)
            {
                AudioImportRules.ReadablePcm.Add(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            try
            {
                channels = clip.channels;
                rate = clip.frequency;
                clip.LoadAudioData();
                var data = new float[clip.samples * clip.channels];
                if (clip.GetData(data, 0)) return data;
                Debug.LogError($"[AudioLab] Couldn't read samples from {path}.");
                return null;
            }
            finally
            {
                if (temp)
                {
                    AudioImportRules.ReadablePcm.Remove(path);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        /// <summary>Linear resample to 48 kHz (sources here are 44.1 or 48 kHz one-shots; good enough for SFX).</summary>
        public static float[] Resample(float[] mono, int rate)
        {
            if (rate == SynthPatch.SampleRate || rate <= 0) return mono;
            float ratio = rate / (float)SynthPatch.SampleRate;
            int outLen = Mathf.FloorToInt(mono.Length / ratio);
            var res = new float[outLen];
            for (int i = 0; i < outLen; i++)
            {
                float x = i * ratio;
                int a = Mathf.Min(mono.Length - 1, (int)x), b = Mathf.Min(mono.Length - 1, a + 1);
                res[i] = Mathf.Lerp(mono[a], mono[b], x - a);
            }
            return res;
        }
    }
}
