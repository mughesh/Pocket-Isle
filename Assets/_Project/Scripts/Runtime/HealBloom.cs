using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 29 C (Mugesh: no lodge or shrine crammed outside the towers): a small one-tile healing plant of the realm on
    /// the approach to each Keeper's door (Galewick a dandelion puff, Cinderwood an ember-lily, Lilymere a lotus bud),
    /// placed by the map's <see cref="Landmark"/> named <see cref="LandmarkName"/>. Walking onto it, poking it or looking
    /// at it and pinching: a burst of green "+" rises round the explorer, every pod Isling is healed, the rim rings refill
    /// with a sweep, a soft heal chime and the Spirit's "Your Islings are healed!". Then it closes and opens again after
    /// <see cref="reopenSeconds"/>. With nobody hurt it only shimmers. The walk stops beside it (a stop provider).
    /// Drawn from its baked prop recipe like <see cref="GateSigns"/>, only while its tile is in the window.
    /// </summary>
    public class HealBloom : MonoBehaviour
    {
        public const string LandmarkName = "healBloom";
        public const string HealedLine = "heal.bloom";

        [Serializable]
        public struct Kind
        {
            [Tooltip("Realm id (galewick / cinderwood / lilymere).")]
            public string realm;
            public PropRecipe recipe;
        }

        public IslandBuilder island;
        public GestureTestPiece explorer;
        public Party party;
        public PodRack pods;
        public BattleStage stage;
        public IslandHandle grabGuard;
        [Tooltip("Diorama/Prop, window clip off (Materials/Props/Logo.mat).")]
        public Material material;
        public Kind[] kinds = new Kind[0];

        [Tooltip("Height of the bloom in tiles.")]
        public float heightTiles = 1.4f;
        public float reopenSeconds = 60f;
        [Tooltip("A fingertip this close (m) pokes it.")]
        public float pokeRadius = 0.03f;
        public float gazeRadius = 0.03f;
        public Color gold = new Color(1f, 0.82f, 0.3f, 1f);

        static readonly int WindowClipId = Shader.PropertyToID("_WindowClip");
        static readonly int RestoredSelfId = Shader.PropertyToID("_RestoredSelf");
        static readonly int ClearSinkId = Shader.PropertyToID("_ClearSink");
        static readonly int OutlineTintId = Shader.PropertyToID("_OutlineTint");

        static HealBloom _instance;
        static float _goldUntil;

        Transform _view;
        MeshRenderer _renderer;
        MeshFilter _filter;
        string _realmShown;
        MaterialPropertyBlock _block;
        readonly Dictionary<string, float> _closedUntil = new Dictionary<string, float>();
        readonly bool[] _wasNear = new bool[2], _wasPinching = { true, true };
        float _open = 1f, _lastUse = -99f;

        /// <summary>The bloom's tile on a map, if it has one (pure over the map).</summary>
        public static bool TryGetTile(WorldMap map, out Vector2Int tile)
        {
            tile = default;
            return map != null && map.TryGetLandmark(LandmarkName, out tile);
        }

        /// <summary>Heals done (tests, the bridge).</summary>
        public int Heals { get; private set; }

        /// <summary>The current map's bloom is open (tests, the bridge).</summary>
        public bool IsOpenHere => island != null && island.world != null && IsOpen(island.world.realm);

        bool IsOpen(string realm) => !_closedUntil.TryGetValue(realm ?? "", out var until) || Time.time >= until;

        /// <summary>Step 29 C16: the bloom pulses gold for a while (the Spirit says the Islings are tired at a Keeper's door).</summary>
        public static void PulseGold(float seconds) => _goldUntil = Time.time + seconds;

        /// <summary>Anyone in the pods below full HP (or fainted).</summary>
        public static bool AnyHurt(Party party)
        {
            if (party == null) return false;
            foreach (var c in party.members)
                if (c != null && c.species != null && c.currentHp < c.MaxHp) return true;
            return false;
        }

        void Awake() => _instance = this;

        void OnEnable() => GestureTestPiece.StopProviders.Add(Footprints);

        void OnDisable() => GestureTestPiece.StopProviders.Remove(Footprints);

        static IEnumerable<RectInt> Footprints(WorldMap map)
        {
            if (TryGetTile(map, out var t)) yield return new RectInt(t.x, t.y, 1, 1);
        }

        void Start()
        {
            if (island == null || material == null || explorer == null || party == null)
            {
                Debug.LogError("[HealBloom] Missing references (Tools > Pocket Isle > Setup > Healing Blooms (29)): no healing blooms.", this);
                enabled = false;
                return;
            }
            _block = new MaterialPropertyBlock();
            var go = new GameObject("Heal Bloom");
            go.transform.SetParent(island.transform, false);
            var inner = new GameObject("Mesh");
            inner.transform.SetParent(go.transform, false);
            _filter = inner.AddComponent<MeshFilter>();
            _renderer = inner.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _view = go.transform;
            go.SetActive(false);
            explorer.OnStep += OnStep;
            foreach (var k in kinds)
                if (k.recipe == null || k.recipe.bakedMesh == null) Debug.LogError($"[HealBloom] The {k.realm} bloom isn't baked.", this);
        }

        void OnDestroy()
        {
            if (explorer != null) explorer.OnStep -= OnStep;
            if (_view != null) Destroy(_view.gameObject);
        }

        void OnStep(Vector2Int tile)
        {
            if (TryGetTile(island.world, out var t) && t == tile) Use("walked onto it");
        }

        PropRecipe RecipeFor(string realm)
        {
            foreach (var k in kinds)
                if (k.realm == realm) return k.recipe;
            return null;
        }

        void LateUpdate()
        {
            var world = island.world;
            var recipe = world != null ? RecipeFor(world.realm) : null;
            bool show = recipe != null && recipe.bakedMesh != null && TryGetTile(world, out var tile) && !SceneSwap.IsInside && !SceneSwap.IsBusy &&
                        !TitleScreen.Showing;
            Vector3 ground = default;
            if (show)
            {
                TryGetTile(world, out tile);
                ground = island.TileToLocal(tile, island.Content.capHeight);
                show = island.FootprintContains(ground);
            }
            if (_view.gameObject.activeSelf != show) _view.gameObject.SetActive(show);
            if (!show) return;
            if (_realmShown != world.realm)
            {
                _realmShown = world.realm;
                _filter.sharedMesh = recipe.bakedMesh;
                var b = recipe.bakedMesh.bounds;
                _filter.transform.localPosition = -new Vector3(b.center.x, b.min.y, b.center.z);
            }
            bool open = IsOpen(world.realm);
            _open = Mathf.MoveTowards(_open, open ? 1f : 0f, Time.deltaTime / 0.8f);
            float tileSize = island.map != null ? island.map.tileSize : 0.05f;
            var bounds = recipe.bakedMesh.bounds;
            float scale = heightTiles * tileSize * island.Zoom / Mathf.Max(1e-4f, bounds.size.y);
            float breathe = 1f + 0.04f * Mathf.Sin(Time.time * 2.2f) * _open;
            // Closed: folded down and dark; open: up, breathing, glowing.
            _view.localPosition = ground;
            _view.localRotation = Quaternion.Euler(0f, Time.time * 8f * _open, 0f);
            _view.localScale = new Vector3(scale * Mathf.Lerp(0.75f, 1f, _open), scale * Mathf.Lerp(0.45f, 1f, _open) * breathe, scale * Mathf.Lerp(0.75f, 1f, _open));
            float gold = Time.time < _goldUntil && open ? 0.5f + 0.5f * Mathf.Sin(Time.time * 5f) : 0f;
            _block.Clear();
            recipe.ApplyTo(_block);
            _block.SetFloat(WindowClipId, 0f);
            _block.SetFloat(RestoredSelfId, 1f);
            _block.SetFloat(ClearSinkId, 0f);
            if (gold > 0f) _block.SetColor(OutlineTintId, new Color(gold * this.gold.r + (1f - gold) * 0.1f, Mathf.Lerp(0.08f, this.gold.g, gold), Mathf.Lerp(0.05f, this.gold.b, gold), 1f));
            _renderer.SetPropertyBlock(_block);
            if (!GameMenu.IsOpen && !GamePause.IsPaused) Hands(_view.position + Vector3.up * (bounds.size.y * scale * 0.5f));
        }

        void Hands(Vector3 at)
        {
            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
                bool tracked = hand != null && hand.IsTrackedDataValid;
                bool busy = grabGuard != null && grabGuard.IsHandBusy(left);
                bool near = tracked && !busy && HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip) && (tip - at).magnitude < pokeRadius;
                if (near && !_wasNear[h]) Use("poke");
                _wasNear[h] = near;
                bool pinch = tracked && hand.GetIndexFingerIsPinching();
                if (pinch && !_wasPinching[h] && !busy && HeadGaze.TryGetRay(out var ray) && HeadGaze.InCone(ray, at, Mathf.Max(gazeRadius, HeadGaze.MinRadius), out _))
                    Use("look + pinch");
                _wasPinching[h] = pinch;
            }
        }

        /// <summary>Heals at the current map's bloom (bridge, tests: "debug").</summary>
        public void Use(string how)
        {
            var world = island.world;
            if (world == null || Time.time - _lastUse < 1f) return;
            _lastUse = Time.time;
            if (!IsOpen(world.realm))
            {
                Debug.Log($"[HealBloom] {world.realm}: closed ({how}), opens in {_closedUntil[world.realm] - Time.time:0} s.");
                return;
            }
            if (!AnyHurt(party))
            {
                Debug.Log($"[HealBloom] {world.realm}: nobody hurt ({how}), a shimmer only.");
                if (stage != null && _view != null) stage.PopHealAt(_view.position);
                return;
            }
            Heals++;
            party.HealAll();
            if (pods != null) pods.HealSweep(0f);
            _closedUntil[world.realm] = Time.time + reopenSeconds;
            GameAudio.Ui(Diorama.Audio.AudioIds.ShrineHeal);
            Dialogue.Say(HealedLine);
            StartCoroutine(Burst());
            Debug.Log($"[HealBloom] {world.realm}: healed ({how}); closed for {reopenSeconds:0} s.");
        }

        IEnumerator Burst()
        {
            if (stage == null) yield break;
            var rnd = new System.Random(Heals);
            float tile = island.map != null ? island.map.tileSize * island.transform.lossyScale.x : 0.03f;
            for (int i = 0; i < 12; i++)
            {
                var centre = explorer.WorldPosition;
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = tile * (0.3f + 0.9f * (float)rnd.NextDouble());
                stage.PopHealAt(centre + new Vector3(Mathf.Cos(a) * r, tile * 0.3f * (float)rnd.NextDouble(), Mathf.Sin(a) * r));
                yield return new WaitForSeconds(0.08f);
            }
        }
    }
}
