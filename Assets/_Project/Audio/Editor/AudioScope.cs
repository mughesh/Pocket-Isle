using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Lets Claude see the sounds: per group a contact sheet PNG (spectrogram 0-8 kHz + waveform + label per clip) in
    /// Temp/audio-analysis/&lt;group&gt;.png, and report.md with the facts and rule failures per clip (FAIL lines on top).
    /// Rules: low share (&lt; 250 Hz) &lt;= 0.45, mid level within 1.5 dB of the tier, tail &lt;= 0.25 for hits,
    /// silence &lt;= 0.3 for one-shots, peak &lt;= -1 dBFS.
    /// </summary>
    public static class AudioScope
    {
        public const string OutDir = "Temp/audio-analysis/";
        public const float MaxLowShare = 0.45f, TierTolerance = 1.5f, MaxTail = 0.25f, MaxSilence = 0.3f;
        const int Fft = 1024, Hop = 256;
        const float TopHz = 8000f, RangeDb = 60f;

        public class Entry
        {
            public string cue, group, clipPath, kind;
            public AudioClip clip;
            public bool loop, hit;
            public float tierDb;
            public ClipStats stats;
            public List<string> fails = new List<string>();
            public float[] samples;
        }

        [MenuItem("Tools/Pocket Isle/Audio/Analyse All", priority = 95)]
        public static void AnalyseAll() => Analyse(null, OutDir);

        [MenuItem("Tools/Pocket Isle/Audio/Analyse Group (selected cue or patch)", priority = 96)]
        public static void AnalyseGroupMenu()
        {
            string group = null;
            if (Selection.activeObject is AudioCue c) group = AudioCueTools.Group(c);
            else if (Selection.activeObject is SynthPatch p) group = SynthBaker.Group(p);
            else if (AudioLabWindow.Selected != null) group = SynthBaker.Group(AudioLabWindow.Selected);
            if (string.IsNullOrEmpty(group))
            {
                Debug.LogError("[AudioScope] Select a cue or patch (Project window or Audio Lab) first.");
                return;
            }
            AnalyseGroup(group);
        }

        public static void AnalyseGroup(string group) => Analyse(new[] { group }, OutDir);

        /// <summary>Analyses the cues of the given groups (null = all but Cries and Music) and writes PNGs + report.md.</summary>
        public static List<Entry> Analyse(IEnumerable<string> groups, string outDir)
        {
            var only = groups != null ? new HashSet<string>(groups) : null;
            var patches = SynthBaker.AllPatches().GroupBy(p => p.id).ToDictionary(g => g.Key, g => g.First());
            var entries = new List<Entry>();
            var cues = AudioCueTools.AllCues();
            try
            {
                for (int ci = 0; ci < cues.Count; ci++)
                {
                    var cue = cues[ci];
                    var group = AudioCueTools.Group(cue);
                    if (only != null ? !only.Contains(group) : group == "Cries" || group == "Music") continue;
                    EditorUtility.DisplayProgressBar("AudioScope", cue.id, ci / (float)cues.Count);
                    patches.TryGetValue(cue.id, out var patch);
                    var tier = AudioTiers.ForCue(cue, patch);
                    float tierDb = Loudness.TierDb(tier) + AudioTiers.TrimFor(cue, patch);
                    foreach (var clip in cue.clips)
                    {
                        if (clip == null) continue;
                        var e = new Entry
                        {
                            cue = cue.id, group = group, clip = clip, clipPath = AssetDatabase.GetAssetPath(clip), loop = cue.loop,
                            hit = AudioTiers.IsHit(cue.id), tierDb = tier == LoudnessTier.Music ? float.NaN : tierDb,
                        };
                        e.kind = Kind(e.clipPath);
                        e.samples = ClipReader.ReadMono48k(clip);
                        if (e.samples == null)
                        {
                            e.fails.Add("unreadable");
                            entries.Add(e);
                            continue;
                        }
                        e.stats = Loudness.Analyse(e.samples, SynthPatch.SampleRate);
                        Check(e, tier);
                        entries.Add(e);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            Directory.CreateDirectory(outDir);
            foreach (var g in entries.GroupBy(e => e.group)) WriteSheet(g.Key, g.ToList(), outDir);
            WriteReport(entries, outDir);
            foreach (var e in entries) e.samples = null;
            int fails = entries.Count(e => e.fails.Count > 0);
            Debug.Log($"[AudioScope] {entries.Count} clips, {fails} failing. {outDir}report.md");
            return entries;
        }

        static string Kind(string path)
        {
            if (path.Contains("/Clips/Gen/")) return path.Contains("_FREE") ? "gen-free" : "gen";
            if (path.Contains("/Clips/Kenney/")) return "kenney";
            if (path.Contains("_mix_")) return "mix";
            return "synth";
        }

        static void Check(Entry e, LoudnessTier tier)
        {
            var s = e.stats;
            if (tier != LoudnessTier.Music && s.lowShare > MaxLowShare) e.fails.Add($"low share {s.lowShare:0.00} > {MaxLowShare:0.00}");
            if (!float.IsNaN(e.tierDb) && Mathf.Abs(s.midDb - e.tierDb) > TierTolerance) e.fails.Add($"mid {s.midDb:0.0} dB vs tier {e.tierDb:0.0}");
            if (e.hit && s.tailShare > MaxTail) e.fails.Add($"tail {s.tailShare:0.00} > {MaxTail:0.00}");
            if (!e.loop && s.silenceShare > MaxSilence) e.fails.Add($"silence {s.silenceShare:0.00} > {MaxSilence:0.00}");
            if (s.peakDb > Loudness.CeilingDb + 0.1f) e.fails.Add($"peak {s.peakDb:0.0} dBFS");
        }

        // ---- report ----

        static string F2(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);
        static string F1(float v) => v.ToString("0.0", CultureInfo.InvariantCulture);

        static void WriteReport(List<Entry> entries, string outDir)
        {
            var sb = new StringBuilder();
            int fails = entries.Count(e => e.fails.Count > 0);
            sb.AppendLine($"# AudioScope report {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
            sb.AppendLine($"{entries.Count} clips, {fails} failing. Rules: low <= {MaxLowShare}, mid within {TierTolerance} dB of tier, " +
                          $"tail <= {MaxTail} (hits), silence <= {MaxSilence} (one-shots), peak <= {Loudness.CeilingDb} dBFS.");
            sb.AppendLine();
            foreach (var rule in new[] { "low share", "mid", "tail", "silence", "peak", "unreadable" })
                sb.AppendLine($"- {rule}: {entries.Count(e => e.fails.Any(f => f.StartsWith(rule)))}");
            sb.AppendLine();
            foreach (var e in entries.Where(e => e.fails.Count > 0))
                sb.AppendLine($"FAIL {Path.GetFileNameWithoutExtension(e.clipPath)} ({e.cue}, {e.kind}): {string.Join("; ", e.fails)}");
            foreach (var g in entries.GroupBy(e => e.group))
            {
                sb.AppendLine();
                sb.AppendLine($"## {g.Key}");
                sb.AppendLine();
                sb.AppendLine("| clip | kind | s | peak dBFS | mid dB (tier) | low | tail | silence | result |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
                foreach (var e in g)
                {
                    var s = e.stats;
                    sb.AppendLine($"| {Path.GetFileNameWithoutExtension(e.clipPath)} | {e.kind} | {F2(s.seconds)} | {F1(s.peakDb)} | " +
                                  $"{F1(s.midDb)} ({(float.IsNaN(e.tierDb) ? "-" : F1(e.tierDb))}) | {F2(s.lowShare)} | {F2(s.tailShare)} | " +
                                  $"{F2(s.silenceShare)} | {(e.fails.Count > 0 ? "FAIL: " + string.Join("; ", e.fails) : "ok")} |");
                }
            }
            File.WriteAllText(outDir + "report.md", sb.ToString());
        }

        // ---- contact sheets ----

        const int TileW = 420, LabelH = 30, SpecH = 110, WaveH = 34, Pad = 6, Cols = 3;
        const int TileH = LabelH + SpecH + WaveH;

        /// <summary>One tile per cue and clip kind (first synth variant, first Kenney, first gen...), so a group fits one sheet.</summary>
        static void WriteSheet(string group, List<Entry> entries, string outDir)
        {
            var tiles = entries.GroupBy(e => e.cue + "|" + e.kind).Select(g => g.First()).Where(e => e.samples != null).ToList();
            if (tiles.Count == 0) return;
            int rows = (tiles.Count + Cols - 1) / Cols;
            int w = Cols * (TileW + Pad) + Pad, h = 16 + rows * (TileH + Pad) + Pad;
            var img = new Img(w, h, new Color32(18, 18, 22, 255));
            img.Text(Pad, 4, $"{group}  -  spectrogram 0-8 kHz linear (lines 250 1k 2k 4k 6k), {RangeDb} dB range; waveform below", new Color32(200, 200, 200, 255));
            for (int i = 0; i < tiles.Count; i++)
            {
                int x = Pad + (i % Cols) * (TileW + Pad), y = 16 + Pad + (i / Cols) * (TileH + Pad);
                DrawTile(img, x, y, tiles[i], entries.Count(e => e.cue == tiles[i].cue && e.kind == tiles[i].kind));
            }
            img.SavePng(outDir + Safe(group) + ".png");
        }

        static string Safe(string s) => string.Concat(s.Select(ch => char.IsLetterOrDigit(ch) || ch == '.' || ch == '-' ? ch : '_'));

        static void DrawTile(Img img, int x, int y, Entry e, int count)
        {
            bool fail = e.fails.Count > 0;
            var s = e.stats;
            var name = Path.GetFileNameWithoutExtension(e.clipPath);
            img.Text(x, y, $"{name} ({e.kind}{(count > 1 ? $", 1 of {count}" : "")})", fail ? new Color32(255, 110, 100, 255) : new Color32(235, 235, 235, 255));
            img.Text(x, y + 10, $"{F2(s.seconds)}s pk {F1(s.peakDb)} mid {F1(s.midDb)}/{(float.IsNaN(e.tierDb) ? "-" : F1(e.tierDb))} low {F2(s.lowShare)} tail {F2(s.tailShare)} sil {F2(s.silenceShare)}", new Color32(170, 190, 210, 255));
            if (fail) img.Text(x, y + 20, "FAIL " + string.Join("; ", e.fails), new Color32(255, 110, 100, 255));
            var spec = Spectrogram(e.samples, SynthPatch.SampleRate, TileW, SpecH);
            img.Blit(spec, TileW, SpecH, x, y + LabelH);
            var wave = Waveform(e.samples, TileW, WaveH);
            img.Blit(wave, TileW, WaveH, x, y + LabelH + SpecH);
            if (fail) img.Rect(x - 2, y + LabelH - 2, TileW + 4, SpecH + WaveH + 4, new Color32(220, 60, 50, 255));
        }

        /// <summary>Log-magnitude STFT (Hann 1024, hop 256), 0-8 kHz linear, top row = 8 kHz. Pixels row-major from the top.</summary>
        public static Color32[] Spectrogram(float[] buf, int rate, int width, int height)
        {
            int frames = Mathf.Max(1, (buf.Length - Fft) / Hop + 1);
            int bins = Mathf.Min(Fft / 2, Mathf.CeilToInt(TopHz / rate * Fft));
            var db = new float[frames, bins];
            var re = new float[Fft];
            var im = new float[Fft];
            var win = new float[Fft];
            for (int i = 0; i < Fft; i++) win[i] = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * i / (Fft - 1));
            float max = -200f;
            for (int f = 0; f < frames; f++)
            {
                int o = f * Hop;
                for (int i = 0; i < Fft; i++)
                {
                    re[i] = o + i < buf.Length ? buf[o + i] * win[i] : 0f;
                    im[i] = 0f;
                }
                Transform(re, im);
                for (int b = 0; b < bins; b++)
                {
                    float p = re[b] * re[b] + im[b] * im[b];
                    float d = 10f * Mathf.Log10(p + 1e-12f);
                    db[f, b] = d;
                    if (d > max) max = d;
                }
            }
            var px = new Color32[width * height];
            for (int x = 0; x < width; x++)
            {
                int fa = x * frames / width, fb = Mathf.Max(fa + 1, (x + 1) * frames / width);
                for (int y = 0; y < height; y++)
                {
                    float hzTop = TopHz * (height - y) / height, hzBot = TopHz * (height - y - 1) / height;
                    int ba = Mathf.Clamp(Mathf.FloorToInt(hzBot / rate * Fft), 0, bins - 1);
                    int bb = Mathf.Clamp(Mathf.CeilToInt(hzTop / rate * Fft), ba + 1, bins);
                    float v = -200f;
                    for (int f = fa; f < fb && f < frames; f++)
                        for (int b = ba; b < bb; b++) v = Mathf.Max(v, db[f, b]);
                    px[y * width + x] = Heat(Mathf.Clamp01((v - (max - RangeDb)) / RangeDb));
                }
            }
            foreach (var hz in new[] { 250f, 1000f, 2000f, 4000f, 6000f })
            {
                int y = height - 1 - Mathf.RoundToInt(hz / TopHz * height);
                for (int x = 0; x < width; x += 4) px[y * width + x] = new Color32(90, 160, 90, 255);
            }
            return px;
        }

        static Color32 Heat(float u)
        {
            Color a, b;
            float t;
            if (u < 0.25f) { a = new Color(0f, 0f, 0f); b = new Color(0.16f, 0.04f, 0.36f); t = u / 0.25f; }
            else if (u < 0.5f) { a = new Color(0.16f, 0.04f, 0.36f); b = new Color(0.6f, 0.12f, 0.47f); t = (u - 0.25f) / 0.25f; }
            else if (u < 0.75f) { a = new Color(0.6f, 0.12f, 0.47f); b = new Color(0.95f, 0.45f, 0.16f); t = (u - 0.5f) / 0.25f; }
            else { a = new Color(0.95f, 0.45f, 0.16f); b = new Color(1f, 0.95f, 0.6f); t = (u - 0.75f) / 0.25f; }
            return Color.Lerp(a, b, t);
        }

        public static Color32[] Waveform(float[] buf, int width, int height)
        {
            var px = new Color32[width * height];
            var bg = new Color32(28, 28, 34, 255);
            for (int i = 0; i < px.Length; i++) px[i] = bg;
            int mid = height / 2;
            for (int x = 0; x < width; x++) px[mid * width + x] = new Color32(70, 70, 80, 255);
            // -1 dBFS ceiling marks
            int top = Mathf.RoundToInt((1f - 0.891f) * 0.5f * (height - 1)), bot = height - 1 - top;
            for (int x = 0; x < width; x += 6)
            {
                px[top * width + x] = new Color32(120, 60, 60, 255);
                px[bot * width + x] = new Color32(120, 60, 60, 255);
            }
            for (int x = 0; x < width; x++)
            {
                int a = (int)((long)x * buf.Length / width), b = Mathf.Max(a + 1, (int)((long)(x + 1) * buf.Length / width));
                float lo = 0f, hi = 0f;
                for (int i = a; i < b && i < buf.Length; i++)
                {
                    lo = Mathf.Min(lo, buf[i]);
                    hi = Mathf.Max(hi, buf[i]);
                }
                int y0 = Mathf.Clamp(Mathf.RoundToInt((0.5f - hi * 0.5f) * (height - 1)), 0, height - 1);
                int y1 = Mathf.Clamp(Mathf.RoundToInt((0.5f - lo * 0.5f) * (height - 1)), 0, height - 1);
                for (int y = y0; y <= y1; y++) px[y * width + x] = new Color32(120, 200, 255, 255);
            }
            return px;
        }

        /// <summary>In-place radix-2 FFT.</summary>
        static void Transform(float[] re, float[] im)
        {
            int n = re.Length;
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;
                if (i < j)
                {
                    (re[i], re[j]) = (re[j], re[i]);
                    (im[i], im[j]) = (im[j], im[i]);
                }
            }
            for (int len = 2; len <= n; len <<= 1)
            {
                double ang = -2 * System.Math.PI / len;
                float wr = (float)System.Math.Cos(ang), wi = (float)System.Math.Sin(ang);
                for (int i = 0; i < n; i += len)
                {
                    float cr = 1f, ci = 0f;
                    for (int k = 0; k < len / 2; k++)
                    {
                        int a = i + k, b = a + len / 2;
                        float tr = re[b] * cr - im[b] * ci, ti = re[b] * ci + im[b] * cr;
                        re[b] = re[a] - tr;
                        im[b] = im[a] - ti;
                        re[a] += tr;
                        im[a] += ti;
                        float ncr = cr * wr - ci * wi;
                        ci = cr * wi + ci * wr;
                        cr = ncr;
                    }
                }
            }
        }

        /// <summary>A texture from top-down rows (for the Lab).</summary>
        public static Texture2D ToTexture(Color32[] px, int width, int height, Texture2D reuse = null)
        {
            var tex = reuse != null && reuse.width == width && reuse.height == height
                ? reuse
                : new Texture2D(width, height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var flipped = new Color32[px.Length];
            for (int y = 0; y < height; y++) System.Array.Copy(px, y * width, flipped, (height - 1 - y) * width, width);
            tex.SetPixels32(flipped);
            tex.Apply();
            return tex;
        }

        /// <summary>A plain top-down RGBA canvas with a 5x7 pixel font, saved as PNG.</summary>
        class Img
        {
            readonly int _w, _h;
            readonly Color32[] _px;

            public Img(int w, int h, Color32 bg)
            {
                _w = w;
                _h = h;
                _px = new Color32[w * h];
                for (int i = 0; i < _px.Length; i++) _px[i] = bg;
            }

            public void Set(int x, int y, Color32 c)
            {
                if (x >= 0 && y >= 0 && x < _w && y < _h) _px[y * _w + x] = c;
            }

            public void Blit(Color32[] src, int sw, int sh, int x, int y)
            {
                for (int j = 0; j < sh; j++)
                    for (int i = 0; i < sw; i++) Set(x + i, y + j, src[j * sw + i]);
            }

            public void Rect(int x, int y, int w, int h, Color32 c)
            {
                for (int i = 0; i < w; i++)
                {
                    Set(x + i, y, c);
                    Set(x + i, y + h - 1, c);
                }
                for (int j = 0; j < h; j++)
                {
                    Set(x, y + j, c);
                    Set(x + w - 1, y + j, c);
                }
            }

            public void Text(int x, int y, string s, Color32 c)
            {
                foreach (var ch in s)
                {
                    var g = PixelFont.Glyph(ch);
                    for (int col = 0; col < 5; col++)
                        for (int row = 0; row < 7; row++)
                            if ((g[col] >> row & 1) != 0) Set(x + col, y + row, c);
                    x += 6;
                    if (x > _w - 6) return;
                }
            }

            public void SavePng(string path)
            {
                var tex = ToTexture(_px, _w, _h);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
        }
    }

    /// <summary>Classic 5x7 font (columns, bit 0 = top). Lower case is drawn as upper case.</summary>
    static class PixelFont
    {
        static readonly Dictionary<char, byte[]> Glyphs = new Dictionary<char, byte[]>
        {
            [' '] = new byte[] { 0, 0, 0, 0, 0 },
            ['0'] = new byte[] { 0x3E, 0x51, 0x49, 0x45, 0x3E }, ['1'] = new byte[] { 0x00, 0x42, 0x7F, 0x40, 0x00 },
            ['2'] = new byte[] { 0x42, 0x61, 0x51, 0x49, 0x46 }, ['3'] = new byte[] { 0x21, 0x41, 0x45, 0x4B, 0x31 },
            ['4'] = new byte[] { 0x18, 0x14, 0x12, 0x7F, 0x10 }, ['5'] = new byte[] { 0x27, 0x45, 0x45, 0x45, 0x39 },
            ['6'] = new byte[] { 0x3C, 0x4A, 0x49, 0x49, 0x30 }, ['7'] = new byte[] { 0x01, 0x71, 0x09, 0x05, 0x03 },
            ['8'] = new byte[] { 0x36, 0x49, 0x49, 0x49, 0x36 }, ['9'] = new byte[] { 0x06, 0x49, 0x49, 0x29, 0x1E },
            ['A'] = new byte[] { 0x7E, 0x11, 0x11, 0x11, 0x7E }, ['B'] = new byte[] { 0x7F, 0x49, 0x49, 0x49, 0x36 },
            ['C'] = new byte[] { 0x3E, 0x41, 0x41, 0x41, 0x22 }, ['D'] = new byte[] { 0x7F, 0x41, 0x41, 0x22, 0x1C },
            ['E'] = new byte[] { 0x7F, 0x49, 0x49, 0x49, 0x41 }, ['F'] = new byte[] { 0x7F, 0x09, 0x09, 0x09, 0x01 },
            ['G'] = new byte[] { 0x3E, 0x41, 0x49, 0x49, 0x7A }, ['H'] = new byte[] { 0x7F, 0x08, 0x08, 0x08, 0x7F },
            ['I'] = new byte[] { 0x00, 0x41, 0x7F, 0x41, 0x00 }, ['J'] = new byte[] { 0x20, 0x40, 0x41, 0x3F, 0x01 },
            ['K'] = new byte[] { 0x7F, 0x08, 0x14, 0x22, 0x41 }, ['L'] = new byte[] { 0x7F, 0x40, 0x40, 0x40, 0x40 },
            ['M'] = new byte[] { 0x7F, 0x02, 0x0C, 0x02, 0x7F }, ['N'] = new byte[] { 0x7F, 0x04, 0x08, 0x10, 0x7F },
            ['O'] = new byte[] { 0x3E, 0x41, 0x41, 0x41, 0x3E }, ['P'] = new byte[] { 0x7F, 0x09, 0x09, 0x09, 0x06 },
            ['Q'] = new byte[] { 0x3E, 0x41, 0x51, 0x21, 0x5E }, ['R'] = new byte[] { 0x7F, 0x09, 0x19, 0x29, 0x46 },
            ['S'] = new byte[] { 0x46, 0x49, 0x49, 0x49, 0x31 }, ['T'] = new byte[] { 0x01, 0x01, 0x7F, 0x01, 0x01 },
            ['U'] = new byte[] { 0x3F, 0x40, 0x40, 0x40, 0x3F }, ['V'] = new byte[] { 0x1F, 0x20, 0x40, 0x20, 0x1F },
            ['W'] = new byte[] { 0x3F, 0x40, 0x38, 0x40, 0x3F }, ['X'] = new byte[] { 0x63, 0x14, 0x08, 0x14, 0x63 },
            ['Y'] = new byte[] { 0x07, 0x08, 0x70, 0x08, 0x07 }, ['Z'] = new byte[] { 0x61, 0x51, 0x49, 0x45, 0x43 },
            ['.'] = new byte[] { 0x00, 0x60, 0x60, 0x00, 0x00 }, ['-'] = new byte[] { 0x08, 0x08, 0x08, 0x08, 0x08 },
            ['_'] = new byte[] { 0x40, 0x40, 0x40, 0x40, 0x40 }, [':'] = new byte[] { 0x00, 0x36, 0x36, 0x00, 0x00 },
            ['/'] = new byte[] { 0x20, 0x10, 0x08, 0x04, 0x02 }, ['%'] = new byte[] { 0x23, 0x13, 0x08, 0x64, 0x62 },
            ['='] = new byte[] { 0x14, 0x14, 0x14, 0x14, 0x14 }, ['+'] = new byte[] { 0x08, 0x08, 0x3E, 0x08, 0x08 },
            ['('] = new byte[] { 0x00, 0x1C, 0x22, 0x41, 0x00 }, [')'] = new byte[] { 0x00, 0x41, 0x22, 0x1C, 0x00 },
            [','] = new byte[] { 0x00, 0x50, 0x30, 0x00, 0x00 }, [';'] = new byte[] { 0x00, 0x56, 0x36, 0x00, 0x00 },
            ['>'] = new byte[] { 0x00, 0x41, 0x22, 0x14, 0x08 }, ['<'] = new byte[] { 0x08, 0x14, 0x22, 0x41, 0x00 },
            ['#'] = new byte[] { 0x14, 0x7F, 0x14, 0x7F, 0x14 }, ['!'] = new byte[] { 0x00, 0x00, 0x5F, 0x00, 0x00 },
        };

        static readonly byte[] Unknown = { 0x7F, 0x41, 0x41, 0x41, 0x7F };

        public static byte[] Glyph(char c)
        {
            c = char.ToUpperInvariant(c);
            return Glyphs.TryGetValue(c, out var g) ? g : Unknown;
        }
    }
}
