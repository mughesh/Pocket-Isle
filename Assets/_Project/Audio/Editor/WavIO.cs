using System;
using System.IO;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>16-bit PCM .wav writing, plus a header probe for the import rules.</summary>
    public static class WavIO
    {
        /// <summary>Writes interleaved samples (-1..1) as 16-bit PCM. Creates the folder.</summary>
        public static void Write(string path, float[] samples, int channels, int sampleRate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var w = new BinaryWriter(fs))
            {
                int dataBytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + dataBytes);
                w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)channels);
                w.Write(sampleRate);
                w.Write(sampleRate * channels * 2);
                w.Write((short)(channels * 2));
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(dataBytes);
                var bytes = new byte[dataBytes];
                // TPDF dither below one LSB keeps quiet tails from turning into grit. Seeded by the file name, so re-baking
                // an unchanged patch writes the same bytes (no Git LFS churn).
                uint h = 2166136261;
                foreach (var ch in Path.GetFileName(path)) h = (h ^ ch) * 16777619;
                var rng = new System.Random((int)h);
                for (int i = 0; i < samples.Length; i++)
                {
                    float d = (float)(rng.NextDouble() - rng.NextDouble()) / 32768f;
                    short v = (short)Mathf.Clamp(Mathf.RoundToInt((samples[i] + d) * 32767f), -32768, 32767);
                    bytes[2 * i] = (byte)(v & 0xff);
                    bytes[2 * i + 1] = (byte)((v >> 8) & 0xff);
                }
                w.Write(bytes);
            }
        }

        /// <summary>
        /// Reads a PCM (8/16/24/32-bit) or 32-bit float .wav as mono (channels averaged). Null if it isn't one.
        /// </summary>
        public static float[] ReadMono(string path, out int sampleRate)
        {
            sampleRate = 0;
            try
            {
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length < 12 || bytes[0] != 'R' || bytes[1] != 'I' || bytes[2] != 'F' || bytes[3] != 'F') return null;
                int pos = 12, format = 0, channels = 0, bits = 0;
                while (pos + 8 <= bytes.Length)
                {
                    string id = System.Text.Encoding.ASCII.GetString(bytes, pos, 4);
                    int size = BitConverter.ToInt32(bytes, pos + 4);
                    int body = pos + 8;
                    if (id == "fmt ")
                    {
                        format = BitConverter.ToInt16(bytes, body);
                        channels = BitConverter.ToInt16(bytes, body + 2);
                        sampleRate = BitConverter.ToInt32(bytes, body + 4);
                        bits = BitConverter.ToInt16(bytes, body + 14);
                        if (format == -2 && size >= 26) format = BitConverter.ToInt16(bytes, body + 24); // WAVE_FORMAT_EXTENSIBLE
                    }
                    else if (id == "data")
                    {
                        if (channels <= 0 || bits <= 0) return null;
                        int bps = bits / 8, frames = Math.Min(size, bytes.Length - body) / (bps * channels);
                        var mono = new float[frames];
                        for (int f = 0; f < frames; f++)
                        {
                            float sum = 0f;
                            for (int c = 0; c < channels; c++)
                            {
                                int o = body + (f * channels + c) * bps;
                                float v;
                                if (format == 3 && bits == 32) v = BitConverter.ToSingle(bytes, o);
                                else if (bits == 16) v = BitConverter.ToInt16(bytes, o) / 32768f;
                                else if (bits == 24) v = ((bytes[o] | (bytes[o + 1] << 8) | (bytes[o + 2] << 16)) << 8 >> 8) / 8388608f;
                                else if (bits == 32) v = BitConverter.ToInt32(bytes, o) / 2147483648f;
                                else v = (bytes[o] - 128) / 128f;
                                sum += v;
                            }
                            mono[f] = sum / channels;
                        }
                        return mono;
                    }
                    pos = body + size + (size & 1);
                }
            }
            catch (Exception) { }
            return null;
        }

        /// <summary>Duration in seconds from a .wav header, or -1 if it can't be read.</summary>
        public static float Duration(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (var r = new BinaryReader(fs))
                {
                    if (new string(r.ReadChars(4)) != "RIFF") return -1f;
                    r.ReadInt32();
                    if (new string(r.ReadChars(4)) != "WAVE") return -1f;
                    int byteRate = 0;
                    while (fs.Position + 8 <= fs.Length)
                    {
                        var id = new string(r.ReadChars(4));
                        int size = r.ReadInt32();
                        if (id == "fmt ")
                        {
                            r.ReadInt16();
                            r.ReadInt16();
                            r.ReadInt32();
                            byteRate = r.ReadInt32();
                            fs.Position += size - 12;
                        }
                        else if (id == "data") return byteRate > 0 ? size / (float)byteRate : -1f;
                        else fs.Position += size + (size & 1);
                    }
                }
            }
            catch (Exception) { }
            return -1f;
        }
    }
}
