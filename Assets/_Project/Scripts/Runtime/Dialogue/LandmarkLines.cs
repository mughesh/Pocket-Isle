using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Says a landmark's first-visit line (<see cref="Landmark.line"/>) the first time the explorer steps within its
    /// radius (tiles, the larger of the column / row distance) on the current outdoor map: the kiln village, the hot
    /// springs, the fallen log, the forge door, the kiln ruins (step 9a). Not during the opening, the tutorial, a
    /// battle or a scene swap; the line itself is "once", so Dialogue remembers (and saves) that it was said.
    /// </summary>
    public class LandmarkLines : MonoBehaviour
    {
        public IslandBuilder island;
        public GestureTestPiece explorer;
        public EncounterDirector encounters;

        void OnEnable()
        {
            if (explorer != null) explorer.OnStep += OnStep;
            else Debug.LogError("[LandmarkLines] No explorer: landmark lines won't play.", this);
        }

        void OnDisable()
        {
            if (explorer != null) explorer.OnStep -= OnStep;
        }

        void OnStep(Vector2Int tile)
        {
            var map = island != null ? island.world : null;
            if (map == null || map.landmarks == null || SceneSwap.IsInside || SceneSwap.IsBusy || Onboarding.Running || Opening.InProgress) return;
            if (encounters != null && encounters.InBattle) return;
            foreach (var l in map.landmarks)
            {
                if (string.IsNullOrEmpty(l.line) || l.radius <= 0 || Dialogue.HasSaid(l.line) || Dialogue.IsPending(l.line)) continue;
                if (Mathf.Max(Mathf.Abs(tile.x - l.tile.x), Mathf.Abs(tile.y - l.tile.y)) > l.radius) continue;
                Debug.Log($"[LandmarkLines] {map.name} {l.name}: {l.line}");
                Dialogue.Say(l.line);
            }
        }
    }
}
