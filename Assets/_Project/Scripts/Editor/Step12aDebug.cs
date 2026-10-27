using System.IO;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 12a checks through the Claude bridge (exec:Diorama.EditorTools.Step12aDebug.X), in Play. Swipe (arg f/b/l/r,
    /// "ff" = two quick swipes), Tap, State. Start with GalewickPlay.Start, spots with GalewickPlay.Go.
    /// </summary>
    static class Step12aDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";
        static GestureTestPiece Explorer => Object.FindFirstObjectByType<GestureTestPiece>();

        static ThumbGesture Of(char c) => c switch
        {
            'f' => ThumbGesture.SwipeForward,
            'b' => ThumbGesture.SwipeBack,
            'l' => ThumbGesture.SwipeLeft,
            _ => ThumbGesture.SwipeRight,
        };

        public static void Swipe()
        {
            var input = Object.FindFirstObjectByType<MicrogestureInput>();
            if (input == null)
            {
                Debug.LogWarning("[Step12aDebug] No MicrogestureInput.");
                return;
            }
            foreach (char c in Arg) input.DebugRaise(Of(c), GestureSource.RightHand);
            State();
        }

        public static void Tap()
        {
            var input = Object.FindFirstObjectByType<MicrogestureInput>();
            if (input != null) input.DebugRaise(ThumbGesture.Tap, GestureSource.RightHand);
            State();
        }

        /// <summary>
        /// Standalone "Initialize XR on Startup" through Unity's settings object (arg 1 = on, 0 = off): flat checks need it
        /// off (XrSessionGuard stops Play without a headset); put it back as found afterwards.
        /// </summary>
        public static void XrInit()
        {
            // By reflection: this assembly doesn't reference XR Management's editor assembly.
            var per = System.Type.GetType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget, Unity.XR.Management.Editor");
            var get = per != null ? per.GetMethod("XRGeneralSettingsForBuildTarget") : null;
            var s = get != null ? get.Invoke(null, new object[] { UnityEditor.BuildTargetGroup.Standalone }) as Object : null;
            var prop = s != null ? s.GetType().GetProperty("InitManagerOnStart") : null;
            if (prop == null)
            {
                Debug.LogError("[Step12aDebug] No Standalone XR settings.");
                return;
            }
            prop.SetValue(s, Arg == "1");
            UnityEditor.EditorUtility.SetDirty(s);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"[Step12aDebug] Standalone Initialize XR on Startup: {prop.GetValue(s)}.");
        }

        /// <summary>Pokes the Lantern Spirit (repeat the objective, or the idle line and the trail).</summary>
        public static void Poke()
        {
            var p = Object.FindFirstObjectByType<SpiritPoke>();
            if (p != null) p.DebugPoke();
            else Debug.LogWarning("[Step12aDebug] No SpiritPoke.");
        }

        /// <summary>Says the trigger in arg.txt (even if once).</summary>
        public static void Say() => Dialogue.Repeat(Arg);

        /// <summary>Ends the tutorial (no praise).</summary>
        public static void FinishTutorial()
        {
            var o = Object.FindFirstObjectByType<Onboarding>();
            if (o != null) o.DebugFinish();
        }

        /// <summary>Shows the Spirit's trail now and logs its goal.</summary>
        public static void Trail()
        {
            var t = Object.FindFirstObjectByType<SpiritTrail>();
            if (t == null) return;
            Debug.Log($"[Step12aDebug] quick {QuickJourney.Active}, title {TitleScreen.Showing}, opening {Opening.InProgress}, tutorial {Onboarding.Running}, menu {GameMenu.IsOpen}, swap {SceneSwap.IsBusy}/{SceneSwap.IsInside}.");
            bool on = t.Show("bridge");
            Debug.Log($"[Step12aDebug] Trail {(on ? "on" : "off")}, goal {(t.Goal.HasValue ? $"c{t.Goal.Value.x} r{t.Goal.Value.y}" : "none")}.");
        }

        /// <summary>The last played slot (arg 0 = adventure, 1 = quick), as the title's Continue reads it.</summary>
        public static void LastSlot()
        {
            if (int.TryParse(Arg, out int v)) PlayerPrefs.SetInt("pocketisle.lastSlot", v);
            PlayerPrefs.Save();
            Debug.Log($"[Step12aDebug] Last played slot: {PlayerPrefs.GetInt("pocketisle.lastSlot", -1)} (0 adventure, 1 quick).");
        }

        public static void State()
        {
            var e = Explorer;
            if (e == null)
            {
                Debug.Log("[Step12aDebug] No explorer.");
                return;
            }
            var preview = e.GetComponent<WalkPreview>();
            var plan = string.Join(" ", System.Linq.Enumerable.Select(e.Plan, t => $"{t.x},{t.y}"));
            Debug.Log($"[Step12aDebug] mode {e.moveMode}, tile c{e.Tile.x} r{e.Tile.y}, heading {e.Heading}, can move {e.CanMove}, locked {e.Locked}, " +
                      $"plan [{plan}] index {e.PlanIndex} ends {e.PlanEnd}, walk on {e.Continuous}, smart steps {e.SmartStepsTaken}, " +
                      $"footprints shown {(preview != null ? preview.Shown : -1)}, onboarding {Onboarding.Running}, opening {Opening.InProgress}.");
        }
    }
}
