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
                for (int i = 0; i < samples.Length; i++)
                {
                    // TPDF dither below one LSB keeps quiet tails from turning into grit.
                    float d = (UnityEngine.Random.value - UnityEngine.Random.value) / 32768f;
                    short v = (short)Mathf.Clamp(Mathf.RoundToInt((samples[i] + d) * 32767f), -32768, 32767);
                    bytes[2 * i] = (byte)(v & 0xff);
                    bytes[2 * i + 1] = (byte)((v >> 8) & 0xff);
                }
                w.Write(bytes);
            }
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
