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
            new Control("Walk", "<b>Swipe</b> your thumb along your finger: a few steps", "thumb_swipe_forward", 0.6f),
            new Control("Keep going", "<b>Swipe twice</b> quickly: keep walking. <b>Tap</b>: stop", "thumb_swipe_twice_tap", 0.6f),
            new Control("Go there", "<b>Look</b> at a spot and <b>pinch</b>: go there", "pinch", 0.6f),
            new Control("Pick up", "<b>Pinch</b> things on the island to pick them up", "pinch_drop", 0.9f),
            new Control("Move isle", "<b>Grab</b> a corner to move the island. Two hands: turn and resize", "grab_rim_slide", 0.9f),
        };

        TextMeshPro _controlText;
        readonly List<RuneBoard.Rune> _controlRunes = new List<RuneBoard.Rune>();

        public static bool IsOpen { get; private set; }

        // Nov 4 (Mugesh: the buttons sat too close): smaller runes, wider steps and rows, a wider board.
        static readonly Vector2 Pebble = new Vector2(0.022f, 0.022f);
        static readonly Vector2 Choice = new Vector2(0.048f, 0.025f);
        static readonly Vector2 Wide = new Vector2(0.074f, 0.028f);
        const float RowGap = 0.043f, LabelX = -0.16f, FirstX = -0.045f, PebbleStep = 0.036f, ChoiceStep = 0.062f, WideStep = 0.09f;

        Action _closed;
        TextMeshPro _creditText;
        string[] _creditLines;
        int _creditTop;
        float _creditNext;
        readonly List<RuneBoard.Rune> _pebbles = new List<RuneBoard.Rune>();

        void OnEnable() => TitleScreen.SettingsRequested += Open;

        void OnDisable() => TitleScreen.SettingsRequested -= Open;

        void OnDestroy() => IsOpen = false;

        Action _toTitle;

        /// <summary>
        /// Shows the board; <paramref name="closed"/> runs on Back. Step 14h: from the game menu, <paramref name="toTitle"/>
        /// adds a small Title tablet by the heading (the menu keeps only Resume, Journal, Settings).
        /// </summary>
        public void Open(Action closed) => Open(closed, null);

        public void Open(Action closed, Action toTitle)
        {
            _toTitle = toTitle;
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
            float top = 7f * RowGap;
            runes.AddBacking(new Vector2(0f, top * 0.5f + 0.004f), new Vector2(0.37f, top + 0.06f));
            runes.AddText("<b>Settings</b>", new Vector2(0f, top + 0.004f), runes.fontSize * 1.1f);
            if (_toTitle != null)
            {
                var toTitle = _toTitle;
                var t = runes.Add("Title", new Vector2(0.135f, top + 0.004f), new Vector2(0.06f, 0.026f), () =>
                {
                    Close();
                    toTitle();
                });
                t.tint = new Color(0.62f, 0.55f, 0.4f);
            }
            Volume("Music", VolumeSettings.Music, top - RowGap);
            Volume("Effects", VolumeSettings.Sfx, top - 2f * RowGap);
            Volume("Ambience", VolumeSettings.Ambience, top - 3f * RowGap);
            Choices("Dominant hand", top - 4f * RowGap, new[] { "Auto", "Left", "Right" },
                () => (int)GameSettings.DominantHand, i => GameSettings.DominantHand = (GameSettings.Hand)i);
            Choices("Spirit hints", top - 5f * RowGap, new[] { "On", "Fewer" },
                () => GameSettings.FewerHints ? 1 : 0, i => GameSettings.FewerHints = i == 1);
            // Step 14g: "Touch tiles to walk" ran into its first option.
            Choices("Touch to walk", top - 6f * RowGap, new[] { "On", "Off" },
                () => GameSettings.TouchWalk ? 0 : 1, i => GameSettings.TouchWalk = i == 0);
            runes.Add("Recenter island", new Vector2(-1.5f * WideStep, 0f), Wide, Recenter);
            runes.Add("Controls", new Vector2(-0.5f * WideStep, 0f), Wide, () => BuildControls(BuildMain));
            runes.Add("Credits", new Vector2(0.5f * WideStep, 0f), Wide, BuildCredits);
            var back = runes.Add("Back", new Vector2(1.5f * WideStep, 0f), Wide, Close);
            back.tint = new Color(0.62f, 0.55f, 0.4f);
            runes.Show();
        }

        void Row(string label, float y) =>
            runes.AddText(label, new Vector2(LabelX + 0.05f, y), runes.fontSize * 0.85f, TextAlignmentOptions.Left, 0.1f);

        /// <summary>Five pebbles: the first is off (0), the others light up to the level.</summary>
        void Volume(string label, string bus, float y)
        {
            Row(label, y);
            var row = new RuneBoard.Rune[VolumeSettings.MaxLevel + 1];
            for (int i = 0; i <= VolumeSettings.MaxLevel; i++)
            {
                int level = i;
                var r = runes.Add(i == 0 ? "off" : i.ToString(), new Vector2(FirstX + i * PebbleStep, y), Pebble, () =>
                {
                    VolumeSettings.SetLevel(bus, level);
                    PaintVolume(row, bus);
                });
                r.radius = 0.5f;
                r.SetLabel(i == 0 ? "<size=70%>off</size>" : "");
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
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                row[i] = runes.Add(names[i], new Vector2(FirstX + 0.005f + i * ChoiceStep, y), Choice, () =>
                {
                    set(index);
                    for (int k = 0; k < row.Length; k++) row[k].selected = k == current();
                });
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
            int n = Controls.Length;
            var size = new Vector2(0.078f, 0.034f);
            const float step = 0.083f;
            for (int i = 0; i < n; i++)
            {
                int k = i;
                _controlRunes.Add(runes.Add(Controls[i].rune, new Vector2((i - (n - 1) * 0.5f) * step, 0.012f), size, () => PickControl(k)));
            }
            runes.AddBacking(new Vector2(0f, -0.026f), new Vector2(n * step + 0.01f, 0.03f));
            _controlText = runes.AddText("", new Vector2(0f, -0.026f), runes.fontSize * 0.82f, TextAlignmentOptions.Center, n * step);
            _controlText.textWrappingMode = TextWrappingModes.Normal;
            _controlText.rectTransform.sizeDelta = new Vector2(n * step, 0.03f);
            if (ghosts == null) Debug.LogError("[Settings] No ghost hands (Set Up MR_Main (14b)): the Controls card can't show the gestures.", this);
            var b = runes.Add("Back", new Vector2(0f, -0.064f), Wide, () =>
            {
                StopControlGhost();
                back();
            });
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
            float h = creditLines * 0.026f + 0.03f;
            runes.AddBacking(new Vector2(0f, 0.025f + h * 0.5f), new Vector2(0.3f, h + 0.02f));
            _creditText = runes.AddText("", new Vector2(0f, 0.025f + h * 0.5f), runes.fontSize * 1.05f, TextAlignmentOptions.Center, 0.28f);
            _creditText.rectTransform.sizeDelta = new Vector2(0.28f, h);
            _creditText.textWrappingMode = TextWrappingModes.Normal;
            ShowCredits();
            runes.Add("Back", new Vector2(0f, 0f), Wide, BuildMain);
            runes.Show();
        }

        void ShowCredits()
        {
            if (_creditText == null) return;
            int end = Mathf.Min(_creditLines.Length, _creditTop + creditLines);
            _creditText.text = string.Join("\n", _creditLines, _creditTop, end - _creditTop);
        }

        void Update()
        {
            if (_creditText == null || _creditLines == null || _creditLines.Length <= creditLines || Time.unscaledTime < _creditNext) return;
            _creditNext = Time.unscaledTime + creditSeconds;
            // Scrolls up a line at a time, then starts over from the top.
            _creditTop = _creditTop + creditLines >= _creditLines.Length ? 0 : _creditTop + 1;
            ShowCredits();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsOpen = false;
    }
}
