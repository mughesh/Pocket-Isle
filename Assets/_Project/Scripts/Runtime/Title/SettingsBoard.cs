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

        public static bool IsOpen { get; private set; }

        static readonly Vector2 Pebble = new Vector2(0.024f, 0.024f);
        static readonly Vector2 Choice = new Vector2(0.052f, 0.027f);
        static readonly Vector2 Wide = new Vector2(0.078f, 0.03f);
        const float RowGap = 0.037f, LabelX = -0.15f, FirstX = -0.045f;

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
            runes.AddBacking(new Vector2(0f, top * 0.5f + 0.004f), new Vector2(0.34f, top + 0.06f));
            runes.AddText("<b>Settings</b>", new Vector2(0f, top + 0.004f), runes.fontSize * 1.1f);
            Volume("Music", VolumeSettings.Music, top - RowGap);
            Volume("Effects", VolumeSettings.Sfx, top - 2f * RowGap);
            Volume("Ambience", VolumeSettings.Ambience, top - 3f * RowGap);
            Choices("Dominant hand", top - 4f * RowGap, new[] { "Auto", "Left", "Right" },
                () => (int)GameSettings.DominantHand, i => GameSettings.DominantHand = (GameSettings.Hand)i);
            Choices("Spirit hints", top - 5f * RowGap, new[] { "On", "Fewer" },
                () => GameSettings.FewerHints ? 1 : 0, i => GameSettings.FewerHints = i == 1);
            Choices("Touch tiles to walk", top - 6f * RowGap, new[] { "On", "Off" },
                () => GameSettings.TouchWalk ? 0 : 1, i => GameSettings.TouchWalk = i == 0);
            runes.Add("Recenter island", new Vector2(-0.09f, 0f), Wide, Recenter);
            runes.Add("Credits", new Vector2(0f, 0f), Wide, BuildCredits);
            var back = runes.Add("Back", new Vector2(0.09f, 0f), Wide, Close);
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
                var r = runes.Add(i == 0 ? "off" : i.ToString(), new Vector2(FirstX + i * 0.034f, y), Pebble, () =>
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
                row[i] = runes.Add(names[i], new Vector2(FirstX + 0.005f + i * 0.058f, y), Choice, () =>
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
