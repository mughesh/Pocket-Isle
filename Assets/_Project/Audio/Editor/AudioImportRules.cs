using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Import settings for everything under Assets/_Project/Audio/Clips/ (Quest / Android):
    /// - short SFX (under 2 s): mono, ADPCM, Decompress On Load. Tiny on disk, PCM in RAM, no decode cost when played.
    /// - longer loops and ambience: mono, Vorbis 0.6, Compressed In Memory. A 30 s mono loop is ~2.9 MB as PCM, ~0.2 MB as Vorbis;
    ///   Quest decodes Vorbis on the CPU per playing voice, which is fine for the 1-3 loops that run at once.
    /// - Clips/Music/: stereo kept, Vorbis 0.5, Streaming (one disk stream per track, almost no RAM; never more than two at once
    ///   during a crossfade).
    /// Sample rate is preserved: our clips are 48 kHz, which is Quest's output rate, so nothing is resampled.
    /// </summary>
    public class AudioImportRules : AssetPostprocessor
    {
        public const string Root = "Assets/_Project/Audio/Clips/";
        public const string MusicRoot = Root + "Music/";
        public const float ShortSeconds = 2f;

        /// <summary>Paths the Loop tool reimports as plain PCM for a moment so it can read the samples.</summary>
        public static readonly HashSet<string> ReadablePcm = new HashSet<string>();

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (AudioImporter)assetImporter;
            var s = importer.defaultSampleSettings;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;

            if (ReadablePcm.Contains(assetPath))
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.PCM;
                s.preloadAudioData = true;
                importer.forceToMono = false;
            }
            else if (assetPath.StartsWith(MusicRoot))
            {
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.5f;
                s.preloadAudioData = false;
                importer.forceToMono = false;
                importer.loadInBackground = true;
            }
            else
            {
                float seconds = Path.GetExtension(assetPath).ToLowerInvariant() == ".wav" ? WavIO.Duration(assetPath) : 0f;
                bool shortSfx = seconds < ShortSeconds; // non-wav files here are Kenney one-shots
                s.loadType = shortSfx ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = shortSfx ? AudioCompressionFormat.ADPCM : AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
                s.preloadAudioData = true;
                importer.forceToMono = true;
                importer.loadInBackground = false;
            }
            importer.defaultSampleSettings = s;
            importer.ClearSampleSettingOverride("Android");
        }
    }
}
