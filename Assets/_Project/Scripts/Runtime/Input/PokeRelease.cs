using System.Collections.Generic;
using System.Reflection;
using Oculus.Interaction;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 33 (friend test: the hand's picture stayed pressed onto a menu button until the menu closed, while the real hand
    /// moved on): ISDK's <see cref="HandPokeLimiterVisual"/> pins the drawn hand to a button while its poke interactor counts
    /// the finger as past the surface, and that state can stick. When the real fingertip has been more than
    /// <see cref="releaseDistance"/> from the touch point for <see cref="releaseSeconds"/>, the limiter is switched off and on
    /// (it frees the wrist) and a stuck select is let go. Every menu (game menu, settings, title, Journal, choice boards).
    /// Made at start-up; plain polling, no scene wiring.
    /// Nov 18 (Mugesh: a fast poke on Settings sticks the finger, a slow one doesn't): a button that hides itself on the
    /// press (Settings swaps the panel) takes its interactable away mid-select; ISDK drops the select but leaves the poke's
    /// passed-surface flag on, so the limiter keeps the hand pinned to a button that's gone (and relocks after a release).
    /// An orphaned pass (passed, nothing selected, no live recoil target) is cleared at once.
    /// </summary>
    public class PokeRelease : MonoBehaviour
    {
        public float releaseDistance = 0.03f;
        public float releaseSeconds = 0.2f;

        static readonly FieldInfo PokeField = typeof(HandPokeLimiterVisual).GetField("_pokeInteractor", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo RecoilField = typeof(PokeInteractor).GetField("_recoilInteractable", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo HitField = typeof(PokeInteractor).GetField("_hitInteractable", BindingFlags.Instance | BindingFlags.NonPublic);

        readonly List<(HandPokeLimiterVisual limiter, PokeInteractor poke)> _pairs = new List<(HandPokeLimiterVisual, PokeInteractor)>();
        readonly Dictionary<PokeInteractor, float> _offSince = new Dictionary<PokeInteractor, float>();
        float _nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<PokeRelease>() != null) return;
            var go = new GameObject("Poke Release");
            DontDestroyOnLoad(go);
            go.AddComponent<PokeRelease>();
        }

        /// <summary>Pure rule (tests): free the drawn hand once the finger has been this far off for this long.</summary>
        public static bool ShouldRelease(float distance, float offFor, float releaseDistance, float releaseSeconds) =>
            distance > releaseDistance && offFor >= releaseSeconds;

        /// <summary>Pure rule (tests): the poke says it's past a surface, but nothing is pressed and no live button can recoil.</summary>
        public static bool IsOrphanedPass(bool passedSurface, bool hasSelected, bool recoilLive) =>
            passedSurface && !hasSelected && !recoilLive;

        /// <summary>Clears an orphaned pass; true when it did (the limiter frees the wrist on the change).</summary>
        static bool ClearOrphan(PokeInteractor poke)
        {
            var recoil = RecoilField != null ? RecoilField.GetValue(poke) as PokeInteractable : null;
            bool recoilLive = recoil != null && recoil.isActiveAndEnabled;
            if (!IsOrphanedPass(poke.IsPassedSurface, poke.HasSelectedInteractable, recoilLive)) return false;
            if (recoil != null && RecoilField != null) RecoilField.SetValue(poke, null);
            poke.IsPassedSurface = false;
            return true;
        }

        void Update()
        {
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 2f;
                _pairs.Clear();
                if (PokeField != null)
                    foreach (var l in FindObjectsByType<HandPokeLimiterVisual>(FindObjectsSortMode.None))
                        if (PokeField.GetValue(l) is PokeInteractor p && p != null) _pairs.Add((l, p));
            }
            foreach (var (limiter, poke) in _pairs)
            {
                if (limiter == null || poke == null || !limiter.isActiveAndEnabled) continue;
                if (ClearOrphan(poke))
                {
                    _offSince.Remove(poke);
                    Debug.Log($"[PokeRelease] orphaned pass cleared ({poke.name}: its button went away mid-press).");
                    continue;
                }
                bool held = poke.IsPassedSurface || poke.State == InteractorState.Select;
                float distance = held ? (poke.Origin - poke.TouchPoint).magnitude : 0f;
                if (!held || distance <= releaseDistance)
                {
                    _offSince.Remove(poke);
                    continue;
                }
                if (!_offSince.TryGetValue(poke, out float since))
                {
                    _offSince[poke] = Time.unscaledTime;
                    continue;
                }
                if (!ShouldRelease(distance, Time.unscaledTime - since, releaseDistance, releaseSeconds)) continue;
                _offSince.Remove(poke);
                if (poke.State == InteractorState.Select) poke.Unselect();
                // Nov 18: a recoil target keeps the pass on (and relocks the wrist on the next state change): drop it too.
                if (RecoilField != null) RecoilField.SetValue(poke, null);
                if (HitField != null) HitField.SetValue(poke, null);
                poke.IsPassedSurface = false;
                limiter.enabled = false;
                limiter.enabled = true;
                Debug.Log($"[PokeRelease] visual freed ({poke.name}: the fingertip {distance * 100f:0.0} cm off the button for {releaseSeconds:0.0} s).");
            }
        }
    }
}
