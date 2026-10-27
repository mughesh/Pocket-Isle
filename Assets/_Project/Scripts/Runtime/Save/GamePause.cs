using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Pause / resume (a competition rule). The headset coming off (application paused) or the system menu over the app
    /// (focus lost) freezes the game with Time.timeScale 0: battle timers (dazed window, auto send-out), walking,
    /// animations and dialogue all stop, and continue where they were on resume. Hand tracking keeps running.
    /// Editor: focus changes are ignored (the Editor loses focus all the time); P toggles the pause for testing.
    /// Step 9g: the game menu pauses too (<see cref="SetMenu"/>); runes and menus run on unscaled time.
    /// </summary>
    public class GamePause : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }

        bool _app, _focus, _key, _menu;

        void OnApplicationPause(bool paused)
        {
            _app = paused;
            Apply("app pause");
        }

        void OnApplicationFocus(bool focused)
        {
            if (Application.isEditor) return;
            _focus = !focused;
            Apply("focus");
        }

        void Update()
        {
            if (!Application.isEditor) return;
            var kb = Keyboard.current;
            if (kb == null || !kb.pKey.wasPressedThisFrame) return;
            _key = !_key;
            Apply("P key");
        }

        /// <summary>The game menu is open (step 9g).</summary>
        public void SetMenu(bool open)
        {
            _menu = open;
            Apply("menu");
        }

        /// <summary>Pauses or resumes from code (testing, the Claude bridge).</summary>
        public void SetPaused(bool paused)
        {
            _key = paused;
            Apply("code");
        }

        void Apply(string why)
        {
            bool paused = _app || _focus || _key || _menu;
            if (paused == IsPaused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            // Audio hookup: only the app pause silences everything; the game menu keeps its own sounds, the music dips
            // under it (MusicDirector) and the ambience goes on.
            AudioListener.pause = _app || _focus;
            Debug.Log($"[GamePause] {(paused ? "Paused" : "Resumed")} ({why}).");
        }

        void OnDisable()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsPaused = false;
    }
}
