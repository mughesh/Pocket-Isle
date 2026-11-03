using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Diorama
{
    /// <summary>
    /// The title (step 9g): the logo and a row of runes over the near rim, at launch, before any gameplay. Driven by
    /// <see cref="Opening"/>:
    /// - A new player (no save in either slot): after the island unfolds and before the Spirit appears, the logo rises
    ///   out of the statue and the runes offer Full adventure / Quick journey ("about 10 min") / Settings, nothing gold
    ///   until looked at. A pick chooses the slot (<see cref="SaveSlots"/>), the logo floats away and the opening goes on.
    /// - A returning player: the logo rises with the island; Continue (gold, the last played slot) / New journey /
    ///   Settings (and "or: &lt;other slot&gt;" when both have a save). Gameplay waits (the explorer is locked) until
    ///   Continue. New journey offers Full / Quick / Back; a slot that has a save needs a 2 s hold ("Hold to start over"),
    ///   then that slot is deleted and the scene reloads into the opening from the shard (<see cref="SaveSlots.PendingNew"/>).
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        public IslandBuilder island;
        public TitleLogo logo;
        public RuneBoard runes;
        public GameSave save;
        [Tooltip("Seconds for the logo to rise out of the statue (new player).")]
        public float riseSeconds = 1f;
        public float leaveSeconds = 1.2f;

        /// <summary>The title shows (no game menu, no gameplay).</summary>
        public static bool Showing { get; private set; }

        /// <summary>The Settings rune: show the settings board, call the action when it closes (step 9g B wires it).</summary>
        public static event Action<Action> SettingsRequested;


        SaveSlot? _picked;
        bool _continue;

        void OnDestroy() => Showing = false;

        /// <summary>Opening.WarmUp: the logo drawn once, tiny, at <paramref name="world"/>.</summary>
        public void WarmUp(Vector3 world)
        {
            if (logo != null) logo.WarmUp(world);
        }

        public void EndWarmUp()
        {
            if (logo != null) logo.EndWarmUp();
        }

        // ---- new player ----

        /// <summary>The logo out of the statue, then Full adventure / Quick journey / Settings; returns once a slot is picked.</summary>
        public IEnumerator NewPlayer(Vector3 statueLocal)
        {
            Showing = true;
            if (logo != null) logo.Rise(statueLocal, riseSeconds, true);
            if (island != null) GameAudio.At(Diorama.Audio.AudioIds.LogoRise, island.transform.TransformPoint(statueLocal));
            yield return WaitUnscaled(riseSeconds * 0.6f);
            _picked = null;
            ShowSlotChoice(false);
            Debug.Log("[Title] New player: Full adventure / Quick journey.");
            while (!_picked.HasValue) yield return null;
            SaveSlots.Choose(_picked.Value);
            yield return Close();
        }

        // ---- returning player ----

        /// <summary>Called as the island starts rising: the logo rises with it.</summary>
        public void BeginReturning(float seconds)
        {
            Showing = true;
            if (logo != null && island != null)
                logo.Rise(new Vector3(island.LocalBounds.center.x, island.CapTopLocal - 0.05f, island.LocalBounds.center.z), seconds, false);
        }

        /// <summary>Continue / New journey / Settings; returns on Continue (New journey reloads the scene instead).</summary>
        public IEnumerator Returning()
        {
            Showing = true;
            _continue = false;
            ShowReturning();
            Debug.Log($"[Title] Returning player: Continue ({SaveSlots.Label(SaveSlots.Active)}).");
            while (!_continue) yield return null;
            yield return Close();
        }

        void ShowReturning()
        {
            if (runes == null)
            {
                _continue = true;
                return;
            }
            runes.Clear();
            var active = SaveSlots.Active;
            var other = SaveSlots.Other(active);
            // Step 14f: Continue (large, gold, its journey under it) over New journey and Settings; the other journey under those.
            float y = 0f, h = 0f;
            if (SaveSlots.Exists(other))
            {
                var or = new RuneBoard.Item($"or: {SaveSlots.Label(other)}", () => SwitchTo(other));
                runes.Row(0f, or);
                h = RuneBoard.RowHeight(or);
            }
            var middle = new[] { new RuneBoard.Item("New journey", () => ShowSlotChoice(true)), new RuneBoard.Item("Settings", OpenSettings) };
            if (h > 0f) y = RuneBoard.Above(y, h, RuneBoard.RowHeight(middle));
            runes.Row(y, middle);
            var top = new RuneBoard.Item("Continue", () => _continue = true, SaveSlots.Label(active), true);
            var cont = runes.Row(RuneBoard.Above(y, RuneBoard.RowHeight(middle), RuneBoard.RowHeight(top)), top)[0];
            runes.DefaultFocus = cont;
            runes.Show();
        }

        /// <summary>Full adventure / Quick journey (+ Settings for a new player, Back for New journey).</summary>
        void ShowSlotChoice(bool restart)
        {
            if (runes == null)
            {
                _picked = SaveSlot.Adventure;
                return;
            }
            runes.Clear();
            // Step 14f: the two journeys side by side over Back (or Settings for a new player). "Hold to start over" became
            // "Hold to restart" (it didn't fit at the readable size).
            var bottom = restart ? new RuneBoard.Item("Back", ShowReturning) : new RuneBoard.Item("Settings", OpenSettings);
            runes.Row(0f, bottom);
            var pair = new[]
            {
                new RuneBoard.Item("Full adventure", () => Pick(SaveSlot.Adventure, restart), restart && SaveSlots.Exists(SaveSlot.Adventure) ? "Hold to restart" : null),
                new RuneBoard.Item("Quick journey", () => Pick(SaveSlot.Quick, restart), restart && SaveSlots.Exists(SaveSlot.Quick) ? "Hold to restart" : "about 10 min"),
            };
            var two = runes.Row(RuneBoard.Above(0f, RuneBoard.TabletH, RuneBoard.RowHeight(pair)), pair);
            var full = two[0];
            var quick = two[1];
            full.hold = restart && SaveSlots.Exists(SaveSlot.Adventure);
            quick.hold = restart && SaveSlots.Exists(SaveSlot.Quick);
            runes.DefaultFocus = null;
            runes.Show();
        }

        void Pick(SaveSlot slot, bool restart)
        {
            if (!restart)
            {
                _picked = slot;
                return;
            }
            Debug.Log($"[Title] New journey: {SaveSlots.Label(slot)} from the start.");
            StartFresh(slot);
        }

        /// <summary>Deletes the slot's save and reloads into the opening from the shard (no slot question then).</summary>
        void StartFresh(SaveSlot slot)
        {
            if (save != null) save.Suspended = true;
            SaveSlots.Choose(slot);
            SaveSlots.Delete(slot);
            SaveSlots.PendingNew = slot;
            DominantHand.Restore(null);
            Reload();
        }

        /// <summary>"or: other slot": plays the other saved slot (a reload loads it like a launch).</summary>
        void SwitchTo(SaveSlot slot)
        {
            if (save != null) save.Suspended = true;
            SaveSlots.Choose(slot);
            Debug.Log($"[Title] Switching to {SaveSlots.Label(slot)}.");
            Reload();
        }

        static void Reload()
        {
            Showing = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OpenSettings()
        {
            if (SettingsRequested == null)
            {
                Debug.LogWarning("[Title] No settings board in the scene.");
                return;
            }
            runes.Hide();
            SettingsRequested.Invoke(() => runes.Show());
        }

        IEnumerator Close()
        {
            if (runes != null)
            {
                runes.Hide();
                runes.Clear();
            }
            if (logo != null) logo.Leave(leaveSeconds);
            yield return WaitUnscaled(leaveSeconds);
            Showing = false;
        }

        /// <summary>The N skip: everything away at once.</summary>
        public void Cancel()
        {
            if (runes != null)
            {
                runes.Hide();
                runes.Clear();
            }
            if (logo != null) logo.Hide();
            Showing = false;
        }

        static IEnumerator WaitUnscaled(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Showing = false;
            SettingsRequested = null;
        }
    }
}
