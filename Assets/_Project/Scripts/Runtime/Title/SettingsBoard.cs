using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Settings (step 9g): one stone board of runes above the near rim, opened from the title or the game menu; Back
    /// closes it. Rows: Music, Effects, Ambience (five pebbles each: off, 1-4; <see cref="VolumeSettings"/>), Dominant
    /// hand (Auto / Left / Right), Spirit hints (On / Fewer), Touch tiles to walk (On / Off, step 11d) (<see cref="GameSettings"/>), then Recenter island (the
    /// placer's standard re-place in front of the head), Credits (a stone card that scrolls) and Back. Everything is
    /// saved in PlayerPrefs at once.
    /// </summary>
    public class SettingsBoard : MonoBehaviour
    {
        public RuneBoard runes;
        public IslandPlacer placer;
        [Tooltip("Credits card lines; the setup appends Assets/Docs/AudioCredits.md when it exists.")]
        [TextArea(4, 12)] public string credits = "Pocket Isle\nby Mugesh\n\nBuilt with the Meta XR SDK\nand Unity\n\nFont: Liberation Sans (SIL Open Font License)";
        [Tooltip("Credits: lines shown at once; longer credits scroll up a line every few seconds.")]
        public int creditLines = 7;
        public float creditSeconds = 2.2f;
        [Tooltip("Step 12a: Diorama/Glyph (Materials/Glyph). Unused since Nov 3 (the Controls card shows ghost hands).")]
        public Material glyphMaterial;
        [Tooltip("Nov 3: the Controls card plays the picked control's ghost hand over the island.")]
        public GhostHands ghosts;

        /// <summary>One control on the Controls card: its rune, the line under the runes, the ghost clip and its size.</summary>
        public struct Control
        {
            public string rune, text, clip;
            public float scale;
            public Control(string rune, string text, string clip, float scale)
            {
                this.rune = rune;
                this.text = text;
                this.clip = clip;
                this.scale = scale;
            }
        }

        /// <summary>Step 12a, Nov 3: the Controls card's controls, in order.</summary>
        public static readonly Control[] Controls =
        {
            // Step 14f: shortened to two lines at the readable size.
            new Control("Walk", "<b>Swipe</b> your thumb along your finger", "thumb_swipe_forward", 0.6f),
            new Control("Keep going", "<b>Swipe twice</b>: keep walking. <b>Tap</b>: stop", "thumb_swipe_twice_tap", 0.6f),
            new Control("Go there", "<b>Look</b> at a spot and <b>pinch</b>", "pinch", 0.6f),
            new Control("Pick up", "<b>Pinch</b> things to pick them up", "pinch_drop", 0.9f),
            new Control("Move isle", "<b>Grab</b> a corner: move. Two hands: turn, size", "grab_rim_slide", 0.9f),
        };

        TextMeshPro _controlText;
        readonly List<RuneBoard.Rune> _controlRunes = new List<RuneBoard.Rune>();

        public static bool IsOpen { get; private set; }

        // Step 14f: everything at the readable size. Row names in a right-aligned column ending at LabelX; the
        // pebbles and choices start at FirstX. Shortened: Hand (Dominant hand), Hints (Spirit hints), Touch walk
        // (Touch tiles to walk), Recenter (Recenter island).
        static float RowGap => RuneBoard.TabletH + RuneBoard.Gap;
        static float Pebble => RuneBoard.TabletH;
        const float LabelX = -0.105f, FirstX = -0.085f;

        Action _closed;
        TextMeshPro _creditText;
        string[] _creditLines;
        int _creditTop;
        float _creditNext;
        readonly List<RuneBoard.Rune> _pebbles = new List<RuneBoard.Rune>();

        void OnEnable() => TitleScreen.SettingsRequested += Open;

        void OnDisable() => TitleScreen.SettingsRequested -= Open;

        void OnDestroy() => IsOpen = false;

        /// <summary>Shows the board; <paramref name="closed"/> runs on Back.</summary>
        public void Open(Action closed)
        {
            if (runes == null)
            {
                Debug.LogError("[Settings] No rune board (Set Up MR_Main (9g title, menus, quick)).", this);
                closed?.Invoke();
                return;
            }
            _closed = closed;
            IsOpen = true;
            BuildMain();
            Debug.Log("[Settings] Open.");
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _creditText = null;
            StopControlGhost();
            runes.Hide();
            runes.Clear();
            Debug.Log("[Settings] Closed.");
            var c = _closed;
            _closed = null;
            c?.Invoke();
        }

        void BuildMain()
        {
            runes.Clear();
            _creditText = null;
            _pebbles.Clear();
            var bottom = new[]
            {
                new RuneBoard.Item("Recenter", Recenter), new RuneBoard.Item("Controls", () => BuildControls(BuildMain)),
                new RuneBoard.Item("Credits", BuildCredits), new RuneBoard.Item("Back", Close),
            };
            float width = Mathf.Max(RuneBoard.RowWidth(bottom), 0.64f);
            float top = 7f * RowGap;
            runes.AddBacking(new Vector2(0f, top * 0.5f), new Vector2(width + 0.04f, top + RuneBoard.TabletH + 0.03f));
            runes.AddText("<b>Settings</b>", new Vector2(0f, top), RuneBoard.LabelFont);
            Volume("Music", VolumeSettings.Music, top - RowGap);
            Volume("Effects", VolumeSettings.Sfx, top - 2f * RowGap);
            Volume("Ambience", VolumeSettings.Ambience, top - 3f * RowGap);
            Choices("Hand", top - 4f * RowGap, new[] { "Auto", "Left", "Right" },
                () => (int)GameSettings.DominantHand, i => GameSettings.DominantHand = (GameSettings.Hand)i);
            Choices("Hints", top - 5f * RowGap, new[] { "On", "Fewer" },
                () => GameSettings.FewerHints ? 1 : 0, i => GameSettings.FewerHints = i == 1);
            Choices("Touch walk", top - 6f * RowGap, new[] { "On", "Off" },
                () => GameSettings.TouchWalk ? 0 : 1, i => GameSettings.TouchWalk = i == 0);
            var back = runes.Row(0f, bottom)[3];
            back.tint = new Color(0.62f, 0.55f, 0.4f);
            runes.Show();
        }

        void Row(string label, float y)
        {
            const float w = 0.24f;
            runes.AddText(label, new Vector2(LabelX - w * 0.5f, y), RuneBoard.LabelFont, TextAlignmentOptions.Right, w);
        }

        /// <summary>Five pebbles: the first is off (0), the others light up to the level.</summary>
        void Volume(string label, string bus, float y)
        {
            Row(label, y);
            var row = new RuneBoard.Rune[VolumeSettings.MaxLevel + 1];
            for (int i = 0; i <= VolumeSettings.MaxLevel; i++)
            {
                int level = i;
                // The first pebble says "off" (sized to it); the others are plain stones that light up to the level.
                var size = i == 0 ? RuneBoard.Fit("off") : new Vector2(Pebble, Pebble);
                float x = i == 0 ? FirstX + size.x * 0.5f : FirstX + RuneBoard.Fit("off").x + RuneBoard.Gap + (i - 1) * (Pebble + RuneBoard.Gap) + Pebble * 0.5f;
                var r = runes.Add(i == 0 ? "off" : i.ToString(), new Vector2(x, y), size, () =>
                {
                    VolumeSettings.SetLevel(bus, level);
                    PaintVolume(row, bus);
                });
                r.radius = 0.5f;
                r.SetLabel(i == 0 ? "off" : "");
                row[i] = r;
            }
            PaintVolume(row, bus);
        }

        static void PaintVolume(RuneBoard.Rune[] row, string bus)
        {
            int level = VolumeSettings.Level(bus);
            for (int i = 0; i < row.Length; i++)
            {
                row[i].selected = i == 0 ? level == 0 : i <= level;
                row[i].tint = i > 0 && i <= level ? RuneBoard.Gold : new Color(0.5f, 0.5f, 0.5f);
            }
        }

        void Choices(string label, float y, string[] names, Func<int> current, Action<int> set)
        {
            Row(label, y);
            var row = new RuneBoard.Rune[names.Length];
            float x = FirstX;
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                var size = RuneBoard.Fit(names[i]);
                row[i] = runes.Add(names[i], new Vector2(x + size.x * 0.5f, y), size, () =>
                {
                    set(index);
                    for (int k = 0; k < row.Length; k++) row[k].selected = k == current();
                });
                x += size.x + RuneBoard.Gap;
            }
            for (int k = 0; k < row.Length; k++)
            {
                row[k].selected = k == current();
                row[k].tint = new Color(0.45f, 0.62f, 0.5f);
            }
        }

        void Recenter()
        {
            if (placer == null)
            {
                Debug.LogWarning("[Settings] No island placer: can't recenter.");
                return;
            }
            placer.Place();
            Debug.Log("[Settings] Recentered the island in front of the head.");
        }

        // ---- controls (step 12a) ----

        /// <summary>Opens the Controls card on its own (the game menu's Controls rune); <paramref name="closed"/> runs on Back.</summary>
        public void OpenControls(Action closed)
        {
            if (runes == null)
            {
                closed?.Invoke();
                return;
            }
            _closed = closed;
            IsOpen = true;
            BuildControls(Close);
            Debug.Log("[Settings] Controls card.");
        }

        /// <summary>
        /// Nov 3: one row of control runes over the near rim, the picked one lit with its line under the row, and its
        /// ghost hand playing over the island (the drawn glyphs didn't look like the gestures). Kept low so the ghost
        /// above the island stays in view.
        /// </summary>
        void BuildControls(Action back)
        {
            runes.Clear();
            _creditText = null;
            _controlRunes.Clear();
            // Step 14f: at the readable size the five runes don't fit one row: three over two, the picked one's line
            // (up to two lines) under them, Back at the bottom.
            int n = Controls.Length;
            var items = new RuneBoard.Item[n];
            for (int i = 0; i < n; i++)
            {
                int k = i;
                items[i] = new RuneBoard.Item(Controls[i].rune, () => PickControl(k));
            }
            var rowA = new[] { items[0], items[1], items[2] };
            var rowB = new[] { items[3], items[4] };
            float width = RuneBoard.RowWidth(rowA);
            const float textH = 0.08f;
            var b = runes.Row(0f, new RuneBoard.Item("Back", () =>
            {
                StopControlGhost();
                back();
            }))[0];
            float textY = RuneBoard.Above(0f, RuneBoard.TabletH, textH);
            runes.AddBacking(new Vector2(0f, textY), new Vector2(width + 0.02f, textH + 0.01f));
            _controlText = runes.AddText("", new Vector2(0f, textY), RuneBoard.LabelFont, TextAlignmentOptions.Center, width);
            _controlText.textWrappingMode = TextWrappingModes.Normal;
            _controlText.rectTransform.sizeDelta = new Vector2(width, textH);
            float yB = RuneBoard.Above(textY, textH, RuneBoard.TabletH);
            _controlRunes.AddRange(runes.Row(yB, rowB));
            _controlRunes.InsertRange(0, runes.Row(RuneBoard.Above(yB, RuneBoard.TabletH, RuneBoard.TabletH), rowA));
            if (ghosts == null) Debug.LogError("[Settings] No ghost hands (Set Up MR_Main (14b)): the Controls card can't show the gestures.", this);
            b.tint = new Color(0.62f, 0.55f, 0.4f);
            runes.DefaultFocus = _controlRunes[0];
            PickControl(0);
            runes.Show();
        }

        void PickControl(int i)
        {
            var c = Controls[i];
            for (int k = 0; k < _controlRunes.Count; k++) _controlRunes[k].selected = k == i;
            if (_controlText != null) _controlText.text = c.text;
            if (ghosts != null) ghosts.BeginPreview(c.clip, GhostPlace.AboveIsland, c.scale);
            Debug.Log($"[Settings] Controls: {c.rune} (ghost {c.clip}).");
        }

        void StopControlGhost()
        {
            _controlText = null;
            _controlRunes.Clear();
            if (ghosts != null) ghosts.EndPreview();
        }

        // ---- credits ----

        void BuildCredits()
        {
            runes.Clear();
            _creditLines = credits.Replace("\r", "").Split('\n');
            _creditTop = 0;
            _creditNext = Time.unscaledTime + creditSeconds * 1.5f;
            // Step 14f: the readable size shows fewer lines at once (they scroll as before).
            float lineH = RuneBoard.LabelFont * 0.1f * 1.25f;
            float h = Lines * lineH + 0.02f, w = 0.6f;
            float cy = RuneBoard.Above(0f, RuneBoard.TabletH, h);
            runes.AddBacking(new Vector2(0f, cy), new Vector2(w + 0.02f, h + 0.02f));
            _creditText = runes.AddText("", new Vector2(0f, cy), RuneBoard.LabelFont, TextAlignmentOptions.Center, w);
            _creditText.rectTransform.sizeDelta = new Vector2(w, h);
            _creditText.textWrappingMode = TextWrappingModes.Normal;
            ShowCredits();
            runes.Row(0f, new RuneBoard.Item("Back", BuildMain));
            runes.Show();
        }

        void ShowCredits()
        {
            if (_creditText == null) return;
            int end = Mathf.Min(_creditLines.Length, _creditTop + Lines);
            _creditText.text = string.Join("\n", _creditLines, _creditTop, end - _creditTop);
        }

        /// <summary>Credit lines shown at once (at most 5 at the readable size).</summary>
        int Lines => Mathf.Clamp(creditLines, 1, 5);

        void Update()
        {
            if (_creditText == null || _creditLines == null || _creditLines.Length <= Lines || Time.unscaledTime < _creditNext) return;
            _creditNext = Time.unscaledTime + creditSeconds;
            // Scrolls up a line at a time, then starts over from the top.
            _creditTop = _creditTop + Lines >= _creditLines.Length ? 0 : _creditTop + 1;
            ShowCredits();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsOpen = false;
    }
}
