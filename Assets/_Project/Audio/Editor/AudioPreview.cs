using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Edit-mode playback for the Audio Lab. Uses UnityEditor.AudioUtil (internal, reached by reflection; it is what the
    /// AudioClip inspector's play button uses and works without Play mode). If that API is missing in a future Unity,
    /// falls back to a hidden HideAndDontSave AudioSource.
    /// </summary>
    public static class AudioPreview
    {
        static MethodInfo _play, _stopAll, _isPlaying;
        static bool _looked;
        static AudioSource _fallback;

        /// <summary>"AudioUtil" or "AudioSource fallback": which path playback takes.</summary>
        public static string Backend
        {
            get
            {
                Find();
                return _play != null ? "AudioUtil" : "AudioSource fallback";
            }
        }

        static void Find()
        {
            if (_looked) return;
            _looked = true;
            var t = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (t == null) return;
            const BindingFlags F = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            _play = t.GetMethod("PlayPreviewClip", F, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            _stopAll = t.GetMethod("StopAllPreviewClips", F, null, Type.EmptyTypes, null);
            _isPlaying = t.GetMethod("IsPreviewClipPlaying", F, null, Type.EmptyTypes, null);
        }

        public static void Play(AudioClip clip, bool loop = false)
        {
            if (clip == null) return;
            Stop();
            Find();
            if (_play != null)
            {
                try
                {
                    _play.Invoke(null, new object[] { clip, 0, loop });
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AudioLab] AudioUtil.PlayPreviewClip failed ({e.InnerException?.Message ?? e.Message}); using an AudioSource.");
                    _play = null;
                }
            }
            if (_fallback == null)
            {
                var go = EditorUtility.CreateGameObjectWithHideFlags("AudioLab Preview", HideFlags.HideAndDontSave, typeof(AudioSource));
                _fallback = go.GetComponent<AudioSource>();
                _fallback.playOnAwake = false;
                _fallback.spatialBlend = 0f;
            }
            _fallback.clip = clip;
            _fallback.loop = loop;
            _fallback.Play();
        }

        public static void Stop()
        {
            Find();
            if (_stopAll != null) _stopAll.Invoke(null, null);
            if (_fallback != null) _fallback.Stop();
        }

        public static bool IsPlaying
        {
            get
            {
                Find();
                if (_fallback != null && _fallback.isPlaying) return true;
                return _isPlaying != null && (bool)_isPlaying.Invoke(null, null);
            }
        }

        /// <summary>A throwaway clip for samples that aren't baked yet.</summary>
        public static AudioClip MakeClip(string name, float[] samples, int channels = 1, int rate = SynthPatch.SampleRate)
        {
            var clip = AudioClip.Create(name, Mathf.Max(1, samples.Length / channels), channels, rate, false);
            clip.SetData(samples, 0);
            clip.hideFlags = HideFlags.HideAndDontSave;
            return clip;
        }
    }
}
