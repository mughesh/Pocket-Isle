using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Diorama
{
    /// <summary>
    /// The mid-game menu (step 9g). Opened and closed by the left hand's menu gesture (palm up, pinch: Meta's app menu
    /// button for hand tracking, <c>OVRInput.Button.Start</c> on <c>Controller.LHand</c>; also the left controller's
    /// menu button) and Esc in the Editor. It pauses the game (<see cref="GamePause"/>: walking, battle tweens,
    /// encounters, the Spirit, puzzles), dims the island like the battle space and shows runes over the near rim:
    /// Resume, Settings, Title (saves, then reloads into the returning title), and Dev in the Editor and QA builds (the
    /// dev panel; F2 still opens it directly). Not available during the opening before the starter is picked, on the
    /// title or during a scene swap; while the island is grabbed the press waits for the release.
    /// Also says <c>menu.first</c> once the lessons are done (not in a battle).
    /// </summary>
    public class GameMenu : MonoBehaviour
    {
        public RuneBoard runes;
        public SettingsBoard settings;
        public IslandBuilder island;
        public IslandHandle handle;
        public GestureTestPiece explorer;
        public GameSave save;
        public GamePause pause;
        public EncounterDirector encounters;
        public Onboarding onboarding;
        [Tooltip("Island dimming while the menu is open (the battle space uses 0.25).")]
        [Range(0f, 0.8f)] public float dim = 0.45f;
        [Tooltip("The hint line once the lessons are done.")]
        public string firstLine = "menu.first";

        public static bool IsOpen { get; private set; }

        /// <summary>Holds the menu.first hint back while true (the Quick Journey's gift).</summary>
        public static Func<bool> HoldHint;

        bool _queued, _wasLocked, _hinted;
        float _dimBefore, _calmFor;

        void OnDestroy()
        {
            if (IsOpen) IsOpen = false;
        }

        void Update()
        {
            if (Pressed())
            {
                if (IsOpen) Close("menu gesture");
                else TryOpen();
            }
            if (_queued && (handle == null || !handle.IsGrabbed))
            {
                _queued = false;
                TryOpen();
            }
            if (IsOpen && island != null) island.clearingDim = Mathf.Max(_dimBefore, dim);
            Hint();
        }

        static bool Pressed()
        {
            if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LHand) || OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch))
                return true;
            var kb = Keyboard.current;
            return Application.isEditor && kb != null && kb.escapeKey.wasPressedThisFrame;
        }

        /// <summary>Opens now, or waits for the island's release; ignored during the opening, the title and swaps.</summary>
        public void TryOpen()
        {
            if (IsOpen) return;
            if (Opening.InProgress || TitleScreen.Showing || SceneSwap.IsBusy)
            {
                Debug.Log($"[GameMenu] Not now ({(Opening.InProgress ? "the opening" : TitleScreen.Showing ? "the title" : "a scene swap")}).");
                return;
            }
            if (handle != null && handle.IsGrabbed)
            {
                _queued = true;
                Debug.Log("[GameMenu] The island is held: the menu opens on release.");
                return;
            }
            Open();
        }

        void Open()
        {
            if (runes == null)
            {
                Debug.LogError("[GameMenu] No rune board (Set Up MR_Main (9g title, menus, quick)).", this);
                return;
            }
            IsOpen = true;
            GameAudio.Ui(Diorama.Audio.AudioIds.MenuOpen);
            if (pause != null) pause.SetMenu(true);
            if (explorer != null)
            {
                _wasLocked = explorer.Locked;
                explorer.Locked = true;
            }
            if (island != null)
            {
                _dimBefore = island.clearingDim;
                // Outside a battle there is no clearing: one far off the map dims the whole island.
                bool battle = encounters != null && encounters.InBattle;
                if (!battle) island.SetClearing(new Vector2(-999f, -999f), Vector2.up, 0.01f, 0.01f, new Vector2(-999f, -999f), 0f);
            }
            ShowRunes();
            Debug.Log("[GameMenu] Open (paused).");
        }

        void ShowRunes()
        {
            runes.Clear();
            var size = new Vector2(0.072f, 0.034f);
            int n = 4;
#if UNITY_EDITOR || POCKET_DEV
            n = 5;
#endif
            float x0 = -(n - 1) * 0.04f;
            var resume = runes.Add("Resume", new Vector2(x0, 0f), size, () => Close("Resume"));
            runes.Add("Settings", new Vector2(x0 + 0.08f, 0f), size, OpenSettings);
            runes.Add("Controls", new Vector2(x0 + 0.16f, 0f), size, OpenControls);
            runes.Add("Title", new Vector2(x0 + 0.24f, 0f), size, ToTitle);
#if UNITY_EDITOR || POCKET_DEV
            var dev = runes.Add("Dev", new Vector2(x0 + 0.32f, 0f), size, OpenDev);
            dev.tint = new Color(0.45f, 0.5f, 0.62f);
#endif
            runes.DefaultFocus = resume;
            runes.Show();
        }

        public void Close(string why)
        {
            if (!IsOpen) return;
            IsOpen = false;
            GameAudio.Ui(Diorama.Audio.AudioIds.MenuClose);
            if (settings != null && SettingsBoard.IsOpen) settings.Close();
            runes.Hide();
            runes.Clear();
            if (island != null) island.clearingDim = _dimBefore;
            if (explorer != null && !_wasLocked) explorer.Locked = false;
            if (pause != null) pause.SetMenu(false);
            Debug.Log($"[GameMenu] Closed ({why}).");
        }

        void OpenSettings()
        {
            if (settings == null) return;
            runes.Hide();
            settings.Open(() =>
            {
                if (IsOpen) runes.Show();
            });
        }

        /// <summary>Step 12a: the How to play card.</summary>
        void OpenControls()
        {
            if (settings == null) return;
            runes.Hide();
            settings.OpenControls(() =>
            {
                if (IsOpen) runes.Show();
            });
        }

        void ToTitle()
        {
            Debug.Log("[GameMenu] Title: saving, then back to the title.");
            if (save != null) save.Save("menu: title");
            Close("Title");
            if (save != null) save.Suspended = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OpenDev()
        {
            Close("Dev");
#if UNITY_EDITOR || POCKET_DEV
            DevPanel.DebugOpen(true);
#endif
        }

        /// <summary>menu.first once the lessons are done and nothing else is going on for a few seconds.</summary>
        void Hint()
        {
            if (_hinted || string.IsNullOrEmpty(firstLine)) return;
            bool calm = onboarding != null && onboarding.IsFinished && !Opening.InProgress && !TitleScreen.Showing && !IsOpen &&
                        !SceneSwap.IsBusy && (encounters == null || !encounters.InBattle) && (HoldHint == null || !HoldHint());
            _calmFor = calm ? _calmFor + Time.deltaTime : 0f;
            if (_calmFor < 3f) return;
            _hinted = true;
            Dialogue.Say(firstLine);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsOpen = false;
            HoldHint = null;
        }
    }
}
