using System.Collections;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Hearthstag in the Forge Hall (step 9c). Frightened (ash fur, crystals flickering dull red) and lying by the cold
    /// forge until met: the first time in (also after a lost or fled fight) the Spirit explains, it rises with its crown
    /// flaring and the Keeper battle starts (<see cref="EncounterDirector.StartKeeper"/>, <see cref="KeeperRules"/>).
    /// In battle its look follows the fight: a shell of ember light while shielded, a crystal goes dark per blocked
    /// hit, the shell shatters and the crown dims when broken, flares back when it rises again, burns brighter in phase 2.
    /// Won: it kneels, calms (grey to chestnut, gold crown, ~2 s), nods; the forge ignites; the fire shard rises from the
    /// forge and floats to the player (big, easy to grab): grab it (pinch, or look + pinch) and it shrinks and flies to
    /// the Lantern Spirit, who carries it (<see cref="ShardHolder"/>). Then back
    /// outside, a restoration wave ripples out from the temple and the temple's return gate appears. Saved:
    /// <c>cinder.keeper</c> solved, the shard held (<see cref="ShardHolder.FireId"/> stage 1), the realm restored.
    /// Calmed, it rests by the burning forge; poke it for a happy snort. Run: back at the hall door; lost: the shrine.
    /// Step 9e: frightened, it rages (<see cref="KeeperFury"/>: smoke aura, crown flames, trembling, ember eyes, snorts,
    /// stomps); the battle opens with it rearing and stomping (the Spirit flinches back), phase 2 makes it all stronger,
    /// and on victory the fury burns off into gold motes before it lowers its head and calms.
    /// </summary>
    public class KeeperHall : MonoBehaviour
    {
        public const string KeeperId = "cinder.keeper";

        public SceneSwap swap;
        public WorldMap hall;
        [Tooltip("The realm it restores (the temple gate lives there).")]
        public WorldMap realmMap;
        public IslandBuilder island;
        public GestureTestPiece explorer;
        public EncounterDirector encounters;
        public BattleStage stage;
        public BattleFx fx;
        public RealmRestoration realm;
        public ShardHolder holder;
        public GameSave save;
        public GhostHands ghosts;
        public IslingSpecies keeperSpecies;
        [Min(1)] public int keeperLevel = 12;
        [Tooltip("Diorama/Beacon material: the forge's flame once lit.")]
        public Material beaconMaterial;
        public PropRecipe shardRecipe;
        [Tooltip("Diorama/Smoke material: the frightened aura (step 9e).")]
        public Material smokeMaterial;
        [Tooltip("Flinches back when it stomps at the start of the battle.")]
        public LanternSpirit spirit;

        [Header("Hall tiles")]
        public Vector2 keeperSpot = new Vector2(4.5f, 3.6f);
        public Vector2Int keeperPad = new Vector2Int(4, 3);
        public Vector2Int partyPad = new Vector2Int(4, 6);
        public Vector2Int hallEntry = new Vector2Int(4, 8);
        [Tooltip("The forge's fire box (tiles) and its height (tiles).")]
        public Vector2 forgeMouth = new Vector2(4.5f, 1.15f);
        public float forgeMouthHeight = 0.4f;

        [Header("Outside")]
        [Tooltip("The realm map tile the restoration wave ripples out from (the temple door).")]
        public Vector2 waveOrigin;
        public float waveSeconds = 5f;
        [Tooltip("The temple's return gate prop anchor on the realm map (hidden until the Keeper is calmed).")]
        public Vector2Int templeGate;

        [Header("Lines")]
        public string meetLine = "cinder.keeper.first";
        public string shieldLine = "keeper.shield.first";
        public string brokenLine = "keeper.shield.broken.first";
        public string phase2Line = "keeper.phase2";
        public string winLine = "cinder.keeper.win";
        public string shardLine = "cinder.shard.take";
        public string restoredLine = "cinder.restored";

        enum Phase { Idle, Meeting, Fighting, Victory, Shard, Leaving }

        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int ModeId = Shader.PropertyToID("_Mode");

        Phase _phase;
        StageActor _model;
        KeeperLook _look;
        KeeperLook _battleLook;
        KeeperFury _fury, _battleFury;
        BattleFx.Item _shell;
        Transform _flame;
        MeshRenderer _flameRenderer;
        MaterialPropertyBlock _block;
        Mesh _quad;
        PropView _shard;
        Vector3 _shardLocal;
        bool _shardHovering;
        readonly bool[] _wasPinching = { true, true };
        float _snortAt;

        public static bool Calmed => PuzzleState.IsSolved(KeeperId);
        bool Active => swap != null && swap.Inside && swap.Interior == hall;
        float TileWorld => island.map.tileSize * island.Zoom * island.transform.lossyScale.x;

        void Start()
        {
            if (swap == null || hall == null || island == null || explorer == null || encounters == null || stage == null || keeperSpecies == null)
            {
                Debug.LogError("[KeeperHall] Needs the scene swap, the hall, island, explorer, encounters, stage and the Keeper species (Set Up MR_Main (9c keeper)).", this);
                enabled = false;
                return;
            }
            _block = new MaterialPropertyBlock();
            if (beaconMaterial != null)
            {
                _quad = BattleHud.VerticalQuad();
                var go = new GameObject("Forge Hall Fire");
                go.layer = island.gameObject.layer;
                go.transform.SetParent(island.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = _quad;
                _flameRenderer = go.AddComponent<MeshRenderer>();
                _flameRenderer.sharedMaterial = beaconMaterial;
                _flameRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _flame = go.transform;
                go.SetActive(false);
            }
            if (shardRecipe != null) _shard = new PropView("Fire Shard (flying)", island, shardRecipe) { restoredSelf = 1f };
            swap.Finished += OnSwapFinished;
            swap.Returned += OnReturned;
            encounters.BattleEnded += OnBattleEnded;
            stage.Presented += OnPresented;
            PuzzleState.Restored += ApplyState;
            Dialogue.RegisterTarget("hearthstag", () => _model != null && _model.gameObject.activeSelf ? _model.WorldCenter : (Vector3?)null, k => { },
                () => 1.2f * TileWorld);
            Dialogue.RegisterTarget("fireShard", () => _shardHovering ? island.transform.TransformPoint(_shardLocal) : (Vector3?)null, k => { },
                () => 0.05f * island.transform.lossyScale.x);
            ApplyState();
            StartCoroutine(EnforceRestoredAfterLoad());
            if (Active && !Calmed) StartCoroutine(Meet());
        }

        void OnDestroy()
        {
            if (swap != null)
            {
                swap.Finished -= OnSwapFinished;
                swap.Returned -= OnReturned;
            }
            if (encounters != null) encounters.BattleEnded -= OnBattleEnded;
            if (stage != null) stage.Presented -= OnPresented;
            PuzzleState.Restored -= ApplyState;
            if (_model != null) Destroy(_model.gameObject);
            if (_flame != null) Destroy(_flame.gameObject);
            if (_quad != null) Destroy(_quad);
            _shard?.Destroy();
            if (realmMap != null)
            {
                realmMap.SetPropHidden(templeGate, false);
                realmMap.SetPropOpen(templeGate, false);
            }
        }

        /// <summary>The temple gate shows once calmed (hidden and walk-through before).</summary>
        void ApplyState()
        {
            if (realmMap != null)
            {
                realmMap.SetPropHidden(templeGate, !Calmed);
                realmMap.SetPropOpen(templeGate, !Calmed);
            }
            if (_look != null) _look.calm = Calmed ? 1f : 0f;
            if (_fury != null) _fury.fury = Calmed ? 0f : 1f;
        }

        KeeperFury AddFury(GameObject go)
        {
            var f = go.GetComponent<KeeperFury>();
            if (f == null) f = go.AddComponent<KeeperFury>();
            f.island = island;
            f.fx = fx;
            f.smokeMaterial = smokeMaterial;
            f.beaconMaterial = beaconMaterial;
            return f;
        }

        /// <summary>A save from mid-wave: once calmed, the realm is fully restored (after GameSave applied its value).</summary>
        IEnumerator EnforceRestoredAfterLoad()
        {
            yield return null;
            if (!Calmed || realm == null || realmMap == null) yield break;
            realm.Remember(realmMap, 1f);
            if (!SceneSwap.IsInside && island.world == realmMap && RealmState.Restored < 1f) realm.SetNow(1f);
        }

        // ---- the hall model ----

        void EnsureModel()
        {
            if (_model != null || keeperSpecies.Prefab == null) return;
            var go = Instantiate(keeperSpecies.Prefab, island.transform);
            go.name = "Hearthstag (hall)";
            _model = go.AddComponent<StageActor>();
            _model.island = island;
            _model.tile = keeperSpot;
            _model.yaw = 0f;
            _look = go.AddComponent<KeeperLook>();
            _look.calm = Calmed ? 1f : 0f;
            _fury = AddFury(go);
            _fury.fury = Calmed ? 0f : 1f;
            _fury.idleStomps = false;
        }

        void Update()
        {
            bool active = Active;
            if (active) EnsureModel();
            bool show = active && _phase != Phase.Fighting && !encounters.InBattle;
            if (_model != null) _model.gameObject.SetActive(show);
            UpdateFlame(active && (Calmed || _phase == Phase.Victory || _phase == Phase.Shard));
            if (_phase == Phase.Fighting) UpdateBattleLook();
            if (_phase == Phase.Shard) UpdateShardPickup();
            else if (_shard != null && !_shardHovering) _shard.Hide();
            if (show && Calmed && _phase == Phase.Idle) UpdatePoke();
        }

        void UpdateFlame(bool on)
        {
            if (_flame == null) return;
            _flame.gameObject.SetActive(on);
            if (!on) return;
            Vector3 local = island.TileToLocal(forgeMouth, island.Content.capHeight + forgeMouthHeight * island.map.tileSize);
            var cam = Camera.main;
            Quaternion face = Quaternion.identity;
            if (cam != null)
            {
                Vector3 to = island.transform.InverseTransformPoint(cam.transform.position) - local;
                to.y = 0f;
                if (to.sqrMagnitude > 1e-8f) face = Quaternion.LookRotation(-to.normalized, Vector3.up);
            }
            float t = island.map.tileSize * island.Zoom;
            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 7f) + 0.06f * Mathf.Sin(Time.time * 13f);
            _flame.localRotation = face;
            _flame.localScale = new Vector3(t * 1.2f, t * 0.9f * pulse, 1f);
            _flame.localPosition = local + Vector3.up * (t * 0.45f * pulse);
            _block.Clear();
            _block.SetFloat(ModeId, 0f);
            _block.SetFloat(IntensityId, 2.2f);
            _flameRenderer.SetPropertyBlock(_block);
        }

        // ---- the meeting ----

        void OnSwapFinished(bool inside)
        {
            if (!inside || !Active) return;
            EnsureModel();
            _look.calm = Calmed ? 1f : 0f;
            _model.tile = keeperSpot;
            _model.pitch = 0f;
            _fury.fury = Calmed ? 0f : 1f;
            _fury.phase2 = false;
            _fury.idleStomps = false;
            if (Calmed)
            {
                _model.View.Daze(); // resting by its forge
                Debug.Log("[KeeperHall] Hearthstag rests by the burning forge (calmed).");
                return;
            }
            StartCoroutine(Meet());
        }

        IEnumerator Meet()
        {
            if (_phase != Phase.Idle) yield break;
            _phase = Phase.Meeting;
            EnsureModel();
            _look.calm = 0f;
            _look.SetAllCrystals(0.6f);
            _model.View.Daze();
            explorer.Locked = true;
            Debug.Log("[KeeperHall] Hearthstag lies by the cold forge, frightened.");
            yield return new WaitForSeconds(0.6f);
            if (!string.IsNullOrEmpty(meetLine)) Dialogue.Say(meetLine);
            for (float t = 0f; t < 9f && Dialogue.IsPending(meetLine); t += Time.deltaTime) yield return null;
            // It rises, the crown flares.
            _model.View.Revive();
            _model.View.Hop();
            _look.SetAllCrystals(1f);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                _look.crownBoost = 1f + 1.5f * Mathf.Sin(t / 0.6f * Mathf.PI);
                yield return null;
            }
            _look.crownBoost = 1f;
            _fury.fury = 1f;
            _fury.idleStomps = true;
            if (!Active)
            {
                // Left during the meeting (the dev panel's teleport): no battle outside the hall.
                _phase = Phase.Idle;
                explorer.Locked = false;
                Debug.Log("[KeeperHall] Left the hall during the meeting: no battle.");
                yield break;
            }
            if (fx != null) fx.Burst(_look.CrownWorld, 30, new Color(1f, 0.55f, 0.2f), TileWorld * 0.8f, 0.8f, TileWorld * 0.05f, Vector3.up * TileWorld * 0.6f);
            explorer.Locked = false;
            var fight = new EncounterDirector.KeeperFight
            {
                keeper = new IslingInstance(keeperSpecies, keeperLevel),
                keeperPad = keeperPad,
                myPad = partyPad,
                caption = $"{keeperSpecies.displayName} blocks the way, crown ablaze!",
            };
            _phase = Phase.Fighting;
            _battleLook = null;
            if (!encounters.StartKeeper(fight))
            {
                Debug.LogError("[KeeperHall] The Keeper battle didn't start (a battle on, an empty party or a zoom).", this);
                _phase = Phase.Idle;
                yield break;
            }
            Debug.Log($"[KeeperHall] The Keeper battle starts: {keeperSpecies.displayName} Lv {keeperLevel}.");
        }

        // ---- the battle's look ----

        void UpdateBattleLook()
        {
            var wild = stage.Wild;
            if (wild == null || encounters.Keeper == null) return;
            if (_battleLook == null || _battleLook.gameObject != wild.gameObject)
            {
                _battleLook = wild.GetComponent<KeeperLook>();
                if (_battleLook == null) _battleLook = wild.gameObject.AddComponent<KeeperLook>();
                _battleLook.calm = 0f;
                _battleFury = AddFury(wild.gameObject);
                _battleFury.fury = 1f;
                _battleFury.phase2 = false;
                _battleFury.idleStomps = false;
                SetShell(true);
                StartCoroutine(BattleIntro(_battleFury));
            }
            if (_shell != null && fx != null)
            {
                _shell.transform.position = wild.WorldCenter + island.transform.up * (wild.View.height * 0.15f * island.Zoom * island.transform.lossyScale.x);
                float size = wild.View.height * 0.6f * island.Zoom * island.transform.lossyScale.x * wild.grow; // the orb mesh scales by radius
                _shell.transform.localScale = Vector3.one * (size / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
                _shell.SetAlpha(0.32f + 0.06f * Mathf.Sin(Time.time * 3f));
            }
        }

        /// <summary>The battle opens: it rears up and stomps, the Spirit flinches back; then it stomps now and then.</summary>
        IEnumerator BattleIntro(KeeperFury fury)
        {
            yield return new WaitForSeconds(0.9f);
            if (fury == null || _phase != Phase.Fighting) yield break;
            fury.Stomped += OnBattleStomp;
            fury.Stomp(26f);
            Debug.Log("[KeeperHall] Battle intro: Hearthstag rears up and stomps.");
            yield return new WaitForSeconds(1.2f);
            if (fury != null) fury.idleStomps = true;
        }

        void OnBattleStomp(Vector3 feet)
        {
            if (_battleFury != null) _battleFury.Stomped -= OnBattleStomp;
            if (spirit != null) spirit.Flinch(feet);
        }

        void SetShell(bool on)
        {
            if (fx == null) return;
            if (on && _shell == null)
            {
                _shell = fx.Get(BattleFx.Shape.Orb);
                if (_shell != null) _shell.Set(new Color(1f, 0.75f, 0.35f), new Color(1f, 0.45f, 0.15f), 0.35f, 2.5f, 0.15f, 0.5f, 0.3f);
            }
            else if (!on && _shell != null)
            {
                fx.Release(_shell);
                _shell = null;
            }
        }

        void OnPresented(BattleEvent e)
        {
            if (_phase != Phase.Fighting || encounters.Keeper == null || _battleLook == null) return;
            float tile = TileWorld;
            switch (e.kind)
            {
                case BattleEventKind.Damage when e.side == BattleSide.Wild && e.blocked:
                    int crystal = CrackOrder(e.cracks - 1);
                    _battleLook.SetCrystal(crystal, 0.15f);
                    if (fx != null) fx.Burst(_battleLook.CrystalWorld(crystal), 10, new Color(1f, 0.6f, 0.25f), tile * 0.5f, 0.5f, tile * 0.04f);
                    if (!string.IsNullOrEmpty(shieldLine)) Dialogue.Say(shieldLine);
                    break;
                case BattleEventKind.ShieldBroken:
                    if (fx != null && _shell != null) fx.Burst(_shell.transform.position, 40, new Color(1f, 0.75f, 0.4f), tile * 1.2f, 0.7f, tile * 0.05f);
                    SetShell(false);
                    _battleLook.SetAllCrystals(0.2f);
                    if (!string.IsNullOrEmpty(brokenLine)) Dialogue.Say(brokenLine);
                    break;
                case BattleEventKind.ShieldRaised:
                    SetShell(true);
                    _battleLook.SetAllCrystals(1f);
                    if (fx != null) fx.Burst(_battleLook.CrownWorld, 24, new Color(1f, 0.6f, 0.2f), tile * 0.7f, 0.6f, tile * 0.05f, Vector3.up * tile * 0.5f);
                    break;
                case BattleEventKind.KeeperPhase2:
                    _battleLook.crownBoost = 1.7f;
                    if (_battleFury != null) _battleFury.phase2 = true;
                    if (!string.IsNullOrEmpty(phase2Line)) Dialogue.Say(phase2Line);
                    break;
            }
        }

        /// <summary>Cracks go outside in: the outer crystals first, the tall middle one last.</summary>
        static int CrackOrder(int crack) => crack switch { 0 => 3, 1 => 4, 2 => 0, _ => Mathf.Clamp(crack, 0, 4) };

        void OnBattleEnded(BattleResult result)
        {
            if (_phase != Phase.Fighting) return;
            SetShell(false);
            _battleLook = null;
            if (_battleFury != null) _battleFury.Stomped -= OnBattleStomp;
            _battleFury = null;
            switch (result)
            {
                case BattleResult.Won:
                    StartCoroutine(Victory());
                    break;
                case BattleResult.Fled:
                    _phase = Phase.Idle;
                    explorer.Teleport(hallEntry, new Vector2Int(0, -1));
                    Debug.Log("[KeeperHall] Ran from the Keeper: back at the hall door; it lies down again.");
                    if (_model != null) _model.View.Daze();
                    if (_fury != null) _fury.idleStomps = false;
                    break;
                default:
                    _phase = Phase.Idle;
                    Debug.Log($"[KeeperHall] Keeper battle {result}: it resets.");
                    break;
            }
        }

        // ---- victory ----

        IEnumerator Victory()
        {
            _phase = Phase.Victory;
            EnsureModel();
            _model.gameObject.SetActive(true);
            _model.View.Revive();
            _model.tile = keeperSpot;
            _look.calm = 0f;
            _look.SetAllCrystals(0.3f);
            explorer.Locked = true;
            // The fury burns off into gold motes: flames shrink, eyes blink and soften, the trembling stops.
            _fury.phase2 = false;
            _fury.fury = 1f;
            Debug.Log("[KeeperHall] Hearthstag's fury burns off into gold motes.");
            yield return _fury.BurnOff(1.8f);
            Debug.Log("[KeeperHall] Hearthstag lowers its head and calms.");
            // Lowers its head (kneels).
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                _model.pitch = Mathf.SmoothStep(0f, 14f, t / 0.6f);
                yield return null;
            }
            // Calms: grey to chestnut, the crown to gold.
            _look.SetAllCrystals(1f);
            for (float t = 0f; t < 2f; t += Time.deltaTime)
            {
                _look.calm = Mathf.SmoothStep(0f, 1f, t / 2f);
                yield return null;
            }
            _look.calm = 1f;
            PuzzleState.Solve(KeeperId, 1); // saved: calmed
            // Nods.
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                _model.pitch = 14f + 10f * Mathf.Sin(t * Mathf.PI * 2f);
                yield return null;
            }
            _model.pitch = 0f;
            if (!string.IsNullOrEmpty(winLine)) Dialogue.Say(winLine);
            for (float t = 0f; t < 6f && Dialogue.IsPending(winLine); t += Time.deltaTime) yield return null;
            // The forge ignites.
            Vector3 mouth = island.transform.TransformPoint(island.TileToLocal(forgeMouth, island.Content.capHeight + forgeMouthHeight * island.map.tileSize));
            if (fx != null) fx.Burst(mouth, 50, new Color(1f, 0.6f, 0.2f), TileWorld * 1.4f, 1f, TileWorld * 0.07f, Vector3.up * TileWorld);
            Debug.Log("[KeeperHall] The forge ignites; the fire shard rises.");
            yield return new WaitForSeconds(0.6f);
            yield return ShardRise();
        }

        IEnumerator ShardRise()
        {
            _phase = Phase.Shard;
            Vector3 from = island.TileToLocal(forgeMouth, island.Content.capHeight + forgeMouthHeight * island.map.tileSize);
            Vector3 up = from + Vector3.up * 0.09f;
            var half = island.FootprintHalfSize;
            Vector3 hover = new Vector3(0f, island.CapTopLocal + 0.12f, half.y * 0.55f);
            _shardHovering = true;
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                _shardLocal = Vector3.Lerp(from, up, Mathf.SmoothStep(0f, 1f, t));
                DrawShard();
                yield return null;
            }
            for (float t = 0f; t < 1.4f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 1.4f);
                _shardLocal = Vector3.Lerp(up, hover, k) + Vector3.up * (0.03f * Mathf.Sin(k * Mathf.PI));
                DrawShard();
                yield return null;
            }
            _shardHome = hover;
            if (!string.IsNullOrEmpty(shardLine)) Dialogue.Say(shardLine);
            if (ghosts != null) ghosts.Show("grab_drop", GhostPlace.AtTarget, "fireShard", 1f, DominantHand.Left, false);
            FocusHighlight.SetPending(() => _shardHovering ? island.transform.TransformPoint(_shardLocal) : (Vector3?)null, 0.05f * island.transform.lossyScale.x);
            explorer.Locked = false;
            Debug.Log("[KeeperHall] The fire shard floats in front of the player: grab it (pinch, or look + pinch).");
        }

        Vector3 _shardHome;

        /// <summary>Metres per shape unit of the floating shard: big to grab (the holder's offer size).</summary>
        float BigShard => holder != null ? holder.offerMetres : 0.05f;

        void DrawShard() => DrawShard(BigShard);

        void DrawShard(float metres)
        {
            if (_shard == null) return;
            _shard.PlaceLocal(_shardLocal, Quaternion.Euler(0f, Time.time * 60f, 0f), metres, true);
        }

        void UpdateShardPickup()
        {
            if (!_shardHovering) return;
            _shardLocal = _shardHome + Vector3.up * (0.006f * Mathf.Sin(Time.time * 2f));
            DrawShard();
            Vector3 world = island.transform.TransformPoint(_shardLocal);
            bool gazed = false;
            if (HeadGaze.TryGetRay(out var ray))
            {
                var pick = new HeadGaze.ConePick(ray, -1);
                pick.Consider(0, world, Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.05f * island.transform.lossyScale.x));
                gazed = pick.Best == 0;
                if (gazed) FocusHighlight.Hover(world, 0.05f * island.transform.lossyScale.x);
            }
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                bool pinching = hand != null && hand.IsTrackedDataValid && hand.GetIndexFingerIsPinching();
                bool started = pinching && !_wasPinching[h];
                _wasPinching[h] = pinching;
                if (!started) continue;
                bool near = IslandHandle.TryGetPinchPoint(hand, out var p) && (p - world).magnitude < 0.09f;
                if (near || gazed)
                {
                    CollectShard(near ? $"{(h == 0 ? "left" : "right")} pinch" : "look + pinch");
                    return;
                }
            }
        }

        /// <summary>The shard is taken (a hand, look + pinch, or the bridge): it flies into the holder.</summary>
        public void CollectShard(string how)
        {
            if (_phase != Phase.Shard || !_shardHovering) return;
            Debug.Log($"[KeeperHall] Fire shard taken ({how}): it flies to the Spirit.");
            StartCoroutine(ShardToHolder());
        }

        IEnumerator ShardToHolder()
        {
            _phase = Phase.Leaving;
            if (ghosts != null && ghosts.ClipName == "grab_drop") ghosts.Hide();
            FocusHighlight.Clear();
            Vector3 from = _shardLocal, to = from;
            if (holder != null) holder.FireOut = true;
            float small = holder != null ? holder.carryMetres : 0.016f;
            for (float t = 0f; t < 0.9f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.9f);
                // The Spirit bobs: aim at where its shard spot is now; shrink on the way.
                if (holder != null) to = holder.CupLocal(0);
                _shardLocal = Vector3.Lerp(from, to, k) + Vector3.up * (0.05f * Mathf.Sin(k * Mathf.PI));
                DrawShard(Mathf.Lerp(BigShard, small, k));
                yield return null;
            }
            _shardHovering = false;
            if (_shard != null) _shard.Hide();
            PuzzleState.SetStage(ShardHolder.FireId, 1); // saved: held
            if (holder != null) holder.FireOut = false;
            if (fx != null) fx.Burst(island.transform.TransformPoint(to), 20, new Color(1f, 0.7f, 0.3f), 0.03f, 0.6f, 0.004f);
            yield return new WaitForSeconds(1.2f);
            _phase = Phase.Idle;
            if (Active && swap.Return()) Debug.Log("[KeeperHall] Back outside for the restoration.");
            _waveDue = true;
        }

        bool _waveDue;

        void OnReturned(WorldMap outdoor)
        {
            if (!_waveDue || outdoor != realmMap) return;
            _waveDue = false;
            StartCoroutine(Restore());
        }

        IEnumerator Restore()
        {
            while (SceneSwap.IsBusy) yield return null;
            yield return new WaitForSeconds(0.3f);
            if (realm != null) realm.Wave(waveOrigin, waveSeconds);
            Debug.Log("[KeeperHall] The restoration wave ripples out from the temple.");
            if (fx != null)
            {
                Vector3 door = island.transform.TransformPoint(island.TileToLocal(waveOrigin, island.Content.capHeight + 0.5f * island.map.tileSize));
                fx.Burst(door, 60, new Color(1f, 0.7f, 0.3f), TileWorld * 2f, 1.4f, TileWorld * 0.06f, Vector3.up * TileWorld);
            }
            yield return new WaitForSeconds(waveSeconds * 0.5f);
            if (!string.IsNullOrEmpty(restoredLine)) Dialogue.Say(restoredLine);
            ApplyState(); // the temple's return gate appears
            if (fx != null)
            {
                Vector3 gate = island.transform.TransformPoint(island.TileToLocal(templeGate + new Vector2(0.5f, 0f), island.Content.capHeight + 0.6f * island.map.tileSize));
                fx.Burst(gate, 40, new Color(0.7f, 0.85f, 1f), TileWorld, 1f, TileWorld * 0.05f, Vector3.up * TileWorld * 0.5f);
            }
            Debug.Log("[KeeperHall] The temple's return gate appears.");
            yield return new WaitForSeconds(waveSeconds * 0.5f + 0.2f);
            if (realm != null) realm.Remember(realmMap, 1f);
            if (save != null) save.Save("Cinderwood restored", true);
        }

        // ---- calmed: poke for a snort ----

        void UpdatePoke()
        {
            if (_model == null || Time.time < _snortAt) return;
            Vector3 c = _model.WorldCenter;
            float r = _model.View.height * 0.6f * island.Zoom * island.transform.lossyScale.x;
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (!HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip) || (tip - c).magnitude > r) continue;
                Snort();
                return;
            }
        }

        /// <summary>A happy snort (poked while calm; also the bridge).</summary>
        public void Snort()
        {
            if (_model == null) return;
            _snortAt = Time.time + 1.5f;
            _model.View.Revive();
            _model.View.Hop();
            if (fx != null) fx.Burst(_model.WorldCenter + island.transform.forward * (_model.View.height * 0.4f * island.Zoom * island.transform.lossyScale.x),
                12, new Color(1f, 0.9f, 0.75f, 0.8f), TileWorld * 0.3f, 0.6f, TileWorld * 0.05f, Vector3.up * TileWorld * 0.3f);
            Debug.Log("[KeeperHall] Hearthstag snorts happily.");
            StartCoroutine(BackToRest());
        }

        IEnumerator BackToRest()
        {
            yield return new WaitForSeconds(1.2f);
            if (_model != null && Calmed && _phase == Phase.Idle) _model.View.Daze();
        }

        /// <summary>Debug (bridge): the shard is in front of the player.</summary>
        public bool ShardWaiting => _phase == Phase.Shard && _shardHovering;
    }
}
