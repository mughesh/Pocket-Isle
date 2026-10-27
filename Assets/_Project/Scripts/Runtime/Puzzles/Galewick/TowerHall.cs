using System.Collections;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Galewick's tower hall look (step 10e): the big round window's sky pane. While Galeowl holds the wind it is a flat,
    /// still grey; <see cref="clear"/> (0..1, the Keeper hall raises it on victory) fills it with bright blue sky, light and
    /// cloud wisps drifting across. Saved through the Keeper: calmed = clear. Doors in and out are the
    /// <see cref="ForgeHall"/> component beside it and <see cref="TowerDoor"/>.
    /// </summary>
    public class TowerHall : MonoBehaviour
    {
        public SceneSwap swap;
        public WorldMap hall;
        public IslandBuilder island;
        public PropRecipe skyRecipe;
        public PropRecipe wispRecipe;
        [Tooltip("The window's 2 x 1 anchor (top-left tile) on the hall map.")]
        public Vector2Int windowAnchor = new Vector2Int(4, 0);
        [Tooltip("Saved Keeper id: calmed = the sky is clear.")]
        public string keeperId = "gw.keeper";
        [Tooltip("Its victory clears the sky (KeeperHall.HallLit).")]
        public KeeperHall keeper;
        public float clearSeconds = 2.5f;
        [Range(0f, 1f)] public float clear;
        [Tooltip("Cloud wisps drifting across the clear sky; seconds to cross.")]
        [Min(1)] public int wisps = 3;
        public float crossSeconds = 9f;

        PropView _sky;
        PropView[] _wisps;

        public bool Active => swap != null && hall != null && swap.Inside && swap.Interior == hall;

        void Start()
        {
            if (swap == null || hall == null || island == null || skyRecipe == null)
            {
                Debug.LogError("[TowerHall] Needs the scene swap, the tower hall map, the island and the sky recipe (Set Up MR_Main (10e tower hall)).", this);
                enabled = false;
                return;
            }
            _sky = new PropView("Tower Hall Sky", island, skyRecipe);
            _wisps = new PropView[wispRecipe != null ? wisps : 0];
            for (int i = 0; i < _wisps.Length; i++) _wisps[i] = new PropView($"Tower Hall Wisp {i}", island, wispRecipe);
            PuzzleState.Restored += ApplyState;
            if (keeper != null) keeper.HallLit += OnHallLit;
            ApplyState();
        }

        void OnDestroy()
        {
            PuzzleState.Restored -= ApplyState;
            if (keeper != null) keeper.HallLit -= OnHallLit;
            _sky?.Destroy();
            if (_wisps != null)
                foreach (var w in _wisps) w?.Destroy();
        }

        void ApplyState() => clear = PuzzleState.IsSolved(keeperId) ? 1f : 0f;

        void OnHallLit() => StartCoroutine(Clear());

        /// <summary>The window fills with sky, light and drifting clouds.</summary>
        IEnumerator Clear()
        {
            Debug.Log("[TowerHall] The window clears: sky, light and clouds.");
            float from = clear;
            for (float t = 0f; t < clearSeconds; t += Time.deltaTime)
            {
                clear = Mathf.Lerp(from, 1f, Mathf.SmoothStep(0f, 1f, t / clearSeconds));
                yield return null;
            }
            clear = 1f;
        }

        Vector2 WindowCenter => windowAnchor + new Vector2(0.5f, 0f);

        void Update()
        {
            if (_sky == null) return;
            bool on = Active;
            _sky.restoredSelf = clear;
            _sky.Place(WindowCenter, 0f, Quaternion.identity, on);
            for (int i = 0; i < _wisps.Length; i++)
            {
                // Wisps drift across the pane left to right, bobbing; they fade in with the clear sky.
                float u = Mathf.Repeat(Time.time / crossSeconds + i / (float)_wisps.Length, 1f);
                float x = Mathf.Lerp(-0.52f, 0.52f, u);
                float y = 1.02f + (i - 1) * 0.27f + 0.03f * Mathf.Sin(Time.time * 0.7f + i * 2f);
                float edge = Mathf.Clamp01(Mathf.Min(u, 1f - u) * 6f);
                float size = clear * edge * (i % 2 == 0 ? 1f : 0.8f);
                _wisps[i].restoredSelf = clear;
                // Map space: columns run toward -X, so a later column is further left from the room.
                _wisps[i].Place(WindowCenter + new Vector2(-x, -0.02f), y, Quaternion.identity, on && size > 0.05f, size);
            }
        }
    }
}
