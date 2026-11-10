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
        /// <summary>Nov 11: the Spirit names it the first time the walk stops beside it (and when someone is hurt).</summary>
        public const string MeetLine = "heal.bloom.meet";

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
        public float heightTiles = 1.8f;
        [Tooltip("Nov 11: a fingertip this close (m) wakes it: it swells, glows green and its motes quicken.")]
        public float hoverRadius = 0.08f;
        [Tooltip("Nov 11: the walk stops at its closest tile when it passes this close (tiles, straight-line).")]
        public float stopRadiusTiles = 2.3f;
        [Tooltip("Nov 11: the heal's shockwave reach in tiles.")]
        public float waveTiles = 2.5f;
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
        float _open = 1f, _lastUse = -99f, _hover, _squash, _nextMote, _nextPlus;
        BattleFx.Item _halo;
        Vector2Int? _stopTile;
        readonly HashSet<string> _met = new HashSet<string>();

        /// <summary>The name of a realm's bloom (for the Spirit's line).</summary>
        public static string NameFor(string realm) =>
            realm == "galewick" ? "dandelion" : realm == "cinderwood" ? "ember-lily" : realm == "lilymere" ? "lotus" : "healing flower";

        /// <summary>
        /// Nov 11 (Mugesh: it was a little off the road, the walk never stopped beside it): stop on <paramref name="next"/>
        /// when it is the walk's closest tile to the bloom within <paramref name="radius"/> tiles: the walk gets nearer
        /// with this step, and one more step the same way would not (pure).
        /// </summary>
        public static bool ClosestApproach(Vector2Int from, Vector2Int next, Vector2Int bloom, float radius)
        {
            float d0 = (from - bloom).sqrMagnitude, d1 = (next - bloom).sqrMagnitude, d2 = (next + (next - from) - bloom).sqrMagnitude;
            return d1 <= radius * radius && d1 < d0 && d2 >= d1 && next != bloom;
        }

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

        void OnEnable()
        {
            if (explorer != null) explorer.walkStops.Add(WalkStopHere);
        }

        void OnDisable()
        {
            if (explorer != null) explorer.walkStops.Remove(WalkStopHere);
            if (_halo != null && stage != null && stage.fx != null) stage.fx.Release(_halo);
            _halo = null;
        }

        /// <summary>Nov 11: stops the walk at its closest tile to the bloom, the first time in a realm, or with anyone hurt.</summary>
        WalkStop WalkStopHere(Vector2Int from, Vector2Int next)
        {
            var world = island != null ? island.world : null;
            if (world == null || SceneSwap.IsInside || !TryGetTile(world, out var t)) return WalkStop.None;
            if (_met.Contains(world.realm ?? "") && !(AnyHurt(party) && IsOpen(world.realm))) return WalkStop.None;
            if (!ClosestApproach(from, next, t, stopRadiusTiles)) return WalkStop.None;
            _stopTile = next;
            return WalkStop.On;
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
            if (!TryGetTile(island.world, out var t)) return;
            if (t == tile)
            {
                Use("walked onto it");
                return;
            }
            if (_stopTile == tile)
            {
                _stopTile = null;
                Meet();
            }
        }

        /// <summary>Nov 11: the walk stopped beside it: the Spirit says what it is and it wakes up a little.</summary>
        void Meet()
        {
            string realm = island.world != null ? island.world.realm ?? "" : "";
            bool first = _met.Add(realm);
            Dialogue.SetVar("bloom", NameFor(realm));
            Dialogue.Repeat(MeetLine);
            _hover = Mathf.Max(_hover, 0.6f);
            Debug.Log($"[HealBloom] {realm}: the walk stopped beside the {NameFor(realm)} ({(first ? "first time" : "someone is hurt")}); the Spirit names it.");
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
            if (!show)
            {
                if (_halo != null) _halo.SetAlpha(0f);
                return;
            }
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
            // Nov 11: a fingertip near wakes it (swells, glows); a poke squashes it before the puff.
            _squash = Mathf.MoveTowards(_squash, 0f, Time.deltaTime / 0.18f);
            float swell = 1f + 0.12f * _hover * _open, squash = 1f - 0.3f * Mathf.Sin(_squash * Mathf.PI);
            // Closed: folded down and dark; open: up, breathing, glowing.
            _view.localPosition = ground;
            _view.localRotation = Quaternion.Euler(0f, Time.time * 8f * _open, 0f);
            float wide = scale * Mathf.Lerp(0.75f, 1f, _open) * swell * (2f - squash);
            _view.localScale = new Vector3(wide, scale * Mathf.Lerp(0.45f, 1f, _open) * breathe * swell * squash, wide);
            float gold = Time.time < _goldUntil && open ? 0.5f + 0.5f * Mathf.Sin(Time.time * 5f) : 0f;
            _block.Clear();
            recipe.ApplyTo(_block);
            _block.SetFloat(WindowClipId, 0f);
            _block.SetFloat(RestoredSelfId, 1f);
            _block.SetFloat(ClearSinkId, 0f);
            if (gold > 0f) _block.SetColor(OutlineTintId, new Color(gold * this.gold.r + (1f - gold) * 0.1f, Mathf.Lerp(0.08f, this.gold.g, gold), Mathf.Lerp(0.05f, this.gold.b, gold), 1f));
            else if (_hover > 0.01f && open) _block.SetColor(OutlineTintId, Color.Lerp(new Color(0.1f, 0.08f, 0.05f, 1f), HealFx.Green, _hover));
            _renderer.SetPropertyBlock(_block);
            Cues(world.realm, open, Head());
            bool near = false;
            if (!GameMenu.IsOpen && !GamePause.IsPaused) near = Hands(_view.position + Vector3.up * (bounds.size.y * scale * 0.5f));
            _hover = Mathf.MoveTowards(_hover, near && open ? 1f : 0f, Time.deltaTime / (near ? 0.15f : 0.6f));
        }

        float TileWorldSize => (island.map != null ? island.map.tileSize : 0.05f) * island.Zoom * island.transform.lossyScale.x;

        Vector3 Head()
        {
            var mesh = _filter != null ? _filter.sharedMesh : null;
            float h = mesh != null ? mesh.bounds.size.y * _view.lossyScale.y : TileWorldSize;
            return _view.position + _view.up * (h * 0.75f);
        }

        /// <summary>
        /// Nov 11 (Mugesh: nothing said it heals): while open, soft green motes rise from the flower, a green "+" floats up
        /// now and then, and a faint green halo breathes on the ground round it. Gone while closed.
        /// </summary>
        void Cues(string realm, bool open, Vector3 head)
        {
            var fx = stage != null ? stage.fx : null;
            if (fx == null) return;
            float tile = TileWorldSize;
            var up = island.transform.up;
            if (_halo == null)
            {
                _halo = fx.Get(BattleFx.Shape.Ring);
                _halo.Set(HealFx.Green, Color.white, 1f, 1.2f);
            }
            float haloAlpha = open ? (0.28f + 0.14f * Mathf.Sin(Time.time * 2.2f)) * (1f + 1.6f * _hover) * _open : 0f;
            _halo.transform.SetPositionAndRotation(_view.position + up * (tile * 0.03f), island.transform.rotation);
            _halo.transform.localScale = Vector3.one * (tile * (1.1f + 0.25f * _hover) / Mathf.Max(1e-5f, fx.transform.lossyScale.x));
            _halo.SetAlpha(Mathf.Clamp01(haloAlpha));
            if (!open) return;
            if (Time.time >= _nextMote)
            {
                _nextMote = Time.time + (_hover > 0.3f ? 0.06f : 0.22f);
                HealFx.Mote(fx, head, up, tile, realm, _hover > 0.3f);
            }
            if (Time.time >= _nextPlus)
            {
                _nextPlus = Time.time + 2.6f;
                stage.PopHealAt(head + up * (tile * 0.2f), 0.8f, 1.6f);
            }
        }

        /// <summary>Pokes and look + pinch; true while a fingertip is within <see cref="hoverRadius"/>.</summary>
        bool Hands(Vector3 at)
        {
            bool any = false;
            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
                bool tracked = hand != null && hand.IsTrackedDataValid;
                bool busy = grabGuard != null && grabGuard.IsHandBusy(left);
                Vector3 tip = default;
                bool hasTip = tracked && !busy && HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out tip);
                float dist = hasTip ? (tip - at).magnitude : float.MaxValue;
                if (dist < hoverRadius) any = true;
                bool near = dist < pokeRadius;
                if (near && !_wasNear[h]) Use("poke");
                _wasNear[h] = near;
                bool pinch = tracked && hand.GetIndexFingerIsPinching();
                if (pinch && !_wasPinching[h] && !busy && HeadGaze.TryGetRay(out var ray) && HeadGaze.InCone(ray, at, Mathf.Max(gazeRadius, HeadGaze.MinRadius), out _))
                    Use("look + pinch");
                _wasPinching[h] = pinch;
            }
            return any;
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
            _squash = 1f;
            if (!AnyHurt(party))
            {
                Debug.Log($"[HealBloom] {world.realm}: nobody hurt ({how}), a small puff only.");
                if (stage != null && stage.fx != null && _view != null)
                {
                    var p = HealFx.For(world.realm);
                    float tile = TileWorldSize;
                    stage.fx.Burst(Head(), p.count / 4, p.main, tile * 0.8f, p.life * 0.7f, tile * 0.05f, island.transform.up * (tile * p.rise));
                }
                return;
            }
            Heals++;
            party.HealAll();
            _closedUntil[world.realm] = Time.time + reopenSeconds;
            StartCoroutine(HealMoment(world.realm));
            Debug.Log($"[HealBloom] {world.realm}: healed ({how}); big puff, heal wave; closed for {reopenSeconds:0} s.");
        }

        /// <summary>
        /// Nov 11: the poke squashes it, then it bursts in the realm's puff (seeds, embers or petals); a moment later the
        /// heal wave washes over the explorer (flash, two shockwaves, a rising halo, sparkles and "+"), the pod rings
        /// refill with a sweep, the chime and the Spirit's line.
        /// </summary>
        IEnumerator HealMoment(string realm)
        {
            yield return new WaitForSeconds(0.12f);
            var fx = stage != null ? stage.fx : null;
            float tile = TileWorldSize;
            var up = island.transform.up;
            GameAudio.Ui(Diorama.Audio.AudioIds.ShrineHeal);
            if (fx != null && _view != null) StartCoroutine(HealFx.Puff(fx, Head(), up, tile, realm));
            yield return new WaitForSeconds(0.35f);
            if (pods != null) pods.HealSweep(0.12f);
            Dialogue.Say(HealedLine);
            if (fx != null) yield return HealFx.Wave(fx, stage, explorer.WorldPosition, island.transform.rotation, up, tile, waveTiles);
        }
    }
}
