using System;
using System.Collections;
using System.Collections.Generic;
using Diorama.Audio;
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
    /// Step 10e: one component per realm's Keeper. The ids, names and dialogue targets are fields; the defaults are
    /// Cinderwood's (Hearthstag, the fire shard), so its scene data stays as it was.
    /// </summary>
    public class KeeperHall : MonoBehaviour
    {
        /// <summary>Cinderwood's Keeper (Hearthstag).</summary>
        public const string KeeperId = "cinder.keeper";

        [Header("Which Keeper (step 10e)")]
        [Tooltip("Saved id: solved = calmed.")]
        public string keeperId = KeeperId;
        [Tooltip("Realm name for the save note once restored.")]
        public string realmName = "Cinderwood";
        [Tooltip("Dialogue targets: the Keeper and its floating shard.")]
        public string keeperTarget = "hearthstag";
        public string shardTarget = "fireShard";
        [Tooltip("The battle's opening caption ({0} = the Keeper's name).")]
        public string captionFormat = "{0} blocks the way, crown ablaze!";
        [Tooltip("Sparks as the shard reaches the Spirit.")]
        public Color shardColor = new Color(1f, 0.7f, 0.3f);
        [Tooltip("Step 10e: a hand puzzle to solve before the meeting (Galeowl's knots); empty = it meets you on the way in.")]
        public KeeperUntangle untangle;
        [Tooltip("Its hall has a forge that lights on victory (Hearthstag).")]
        public bool forgeFlame = true;
        [Tooltip("It stomps (rears and slams down, lava cracks); off = it flaps up instead.")]
        public bool stomps = true;
        [Tooltip("Sits this many tiles above the floor (Galeowl on its perch).")]
        public float keeperLift;
        [Tooltip("Calmed, it rests (lies down); off = it stays up and preens now and then.")]
        public bool restsWhenCalm = true;
        [Tooltip("Poked while calm: a hoot (an owl) instead of a snort.")]
        public bool hoots;
        [Tooltip("After a lost or fled battle, walking up to it (this many tiles) or poking it starts the battle again; 0 = only on the way in.")]
        public float approachRestart;
        [Tooltip("Fury while it's still tangled (the aura flares to full when it breaks free).")]
        [Range(0f, 1f)] public float tangledFury = 0.35f;

        [Header("Look (defaults: Hearthstag)")]
        public Color frightenedFur = new Color(0.55f, 0.52f, 0.5f);
        public Color frightenedBelly = new Color(0.8f, 0.78f, 0.74f);
        public Color smokeLight = new Color(0.36f, 0.31f, 0.42f, 0.62f);
        public Color smokeDark = new Color(0.15f, 0.13f, 0.19f, 0.8f);
        public Color sparks = new Color(1f, 0.45f, 0.12f);
        public Color eyeFrightened = new Color(1f, 0.22f, 0.05f, 0.9f);
        [Tooltip("Crown flares, cracks and the shield's bursts.")]
        public Color glowColor = new Color(1f, 0.6f, 0.25f);
        public Color shellCore = new Color(1f, 0.75f, 0.35f);
        public Color shellEdge = new Color(1f, 0.45f, 0.15f);

        /// <summary>Step 10e: the hall lights up on victory (Hearthstag's forge; Galeowl's window clears).</summary>
        public event Action HallLit;

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
        [Tooltip("Step 10a: this Keeper's own shard (saved id); the other shards held raise its level.")]
        public string ownShard = "shard.fire";
        [Tooltip("Diorama/Beacon material: the forge's flame once lit.")]
        public Material beaconMaterial;
        public PropRecipe shardRecipe;
        [Tooltip("Diorama/Smoke material: the frightened aura (step 9e).")]
        public Material smokeMaterial;
        [Header("Taking the shard (step 11d: it was too hard)")]
        [Tooltip("A pinch, or a fist closing, this close to the floating shard takes it (m, real hands).")]
        public float shardGrabReach = 0.09f;
        [Tooltip("Not taken after this long (s): it floats to the Spirit by itself.")]
        public float shardAutoSeconds = 4f;
        /// <summary>Step 11d: the shard's ghost pinches it (thumb + index), not a fist.</summary>
        public const string ShardGhost = "pinch_lift";
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
        readonly bool[] _wasFist = { true, true };
        float _shardWait;
        GuideRings _shardRing;
        float _snortAt;

        /// <summary>The scene's Keeper hall for <paramref name="id"/>, inactive ones too (editor tools, the bridge).</summary>
        public static KeeperHall FindById(string id)
        {
            foreach (var k in FindObjectsByType<KeeperHall>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (k.keeperId == id) return k;
            return null;
        }

        public bool Calmed => PuzzleState.IsSolved(keeperId);
        string KeeperName => keeperSpecies != null ? keeperSpecies.displayName : keeperId;
        /// <summary>Its model in the hall (null until the hall first shows).</summary>
        public StageActor Model => _model;
        /// <summary>Not meeting, fighting or in its victory.</summary>
        public bool Idle => _phase == Phase.Idle;
        bool Tangled => untangle != null && !untangle.IsSolved;
        float _preenAt, _restartAt;
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
            if (shardRecipe != null) _shard = new PropView($"{ownShard} (flying)", island, shardRecipe) { restoredSelf = 1f };
            swap.Finished += OnSwapFinished;
            swap.Returned += OnReturned;
            if (untangle != null) untangle.Solved += OnUntangled;
            encounters.BattleEnded += OnBattleEnded;
            stage.Presented += OnPresented;
            PuzzleState.Restored += ApplyState;
            Dialogue.RegisterTarget(keeperTarget, () => _model != null && _model.gameObject.activeSelf ? _model.WorldCenter : (Vector3?)null, k => { },
                () => 1.2f * TileWorld);
            Dialogue.RegisterTarget(shardTarget, () => _shardHovering ? island.transform.TransformPoint(_shardLocal) : (Vector3?)null, k => { },
                () => 0.05f * island.transform.lossyScale.x);
            ApplyState();
            StartCoroutine(EnforceRestoredAfterLoad());
            if (Active && !Calmed)
            {
                if (Tangled) ShowTangled();
                else StartCoroutine(Meet());
            }
        }

        void OnDestroy()
        {
            if (swap != null)
            {
                swap.Finished -= OnSwapFinished;
                swap.Returned -= OnReturned;
            }
            if (encounters != null) encounters.BattleEnded -= OnBattleEnded;
            if (untangle != null) untangle.Solved -= OnUntangled;
            if (stage != null) stage.Presented -= OnPresented;
            PuzzleState.Restored -= ApplyState;
            if (_model != null) Destroy(_model.gameObject);
            if (_flame != null) Destroy(_flame.gameObject);
            if (_quad != null) Destroy(_quad);
            _shard?.Destroy();
            _shardRing?.Destroy();
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
            f.smokeLight = smokeLight;
            f.smokeDark = smokeDark;
            f.ember = sparks;
            f.eyeFrightened = eyeFrightened;
            return f;
        }

        KeeperLook AddLook(GameObject go)
        {
            var l = go.GetComponent<KeeperLook>();
            if (l == null) l = go.AddComponent<KeeperLook>();
            l.frightenedFur = frightenedFur;
            l.frightenedBelly = frightenedBelly;
            return l;
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
            go.name = $"{KeeperName} (hall)";
            _model = go.AddComponent<StageActor>();
            _model.island = island;
            _model.tile = keeperSpot;
            _model.yaw = 0f;
            _model.baseLift = keeperLift * island.map.tileSize;
            _look = AddLook(go);
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
            UpdateFlame(forgeFlame && active && (Calmed || _phase == Phase.Victory || _phase == Phase.Shard));
            if (_phase == Phase.Fighting) UpdateBattleLook();
            if (_phase == Phase.Shard) UpdateShardPickup();
            else if (_shard != null && !_shardHovering) _shard.Hide();
            if (show && Calmed && _phase == Phase.Idle)
            {
                UpdatePoke();
                if (!restsWhenCalm) Preen();
            }
            if (show && !Calmed && _phase == Phase.Idle && approachRestart > 0f && !Tangled && !encounters.InBattle && Time.time > _restartAt) Approach();
        }

        /// <summary>Calmed and awake: it ruffles and bobs now and then.</summary>
        void Preen()
        {
            if (_model == null || Time.time < _preenAt) return;
            _preenAt = Time.time + UnityEngine.Random.Range(4f, 8f);
            StartCoroutine(PreenOnce());
        }

        IEnumerator PreenOnce()
        {
            for (float t = 0f; t < 0.9f && _model != null; t += Time.deltaTime)
            {
                _model.pitch = 12f * Mathf.Sin(t / 0.9f * Mathf.PI) * Mathf.Sin(t * 14f) * 0.5f + 6f * Mathf.Sin(t / 0.9f * Mathf.PI);
                yield return null;
            }
            if (_model != null) _model.pitch = 0f;
        }

        /// <summary>Free but frightened after a lost or fled fight: walking up to it or poking it starts the battle again.</summary>
        void Approach()
        {
            if (_model == null) return;
            bool near = (explorer.Tile - keeperSpot).magnitude <= approachRestart;
            if (!near)
            {
                Vector3 c = _model.WorldCenter;
                float r = _model.View.height * 0.6f * island.Zoom * island.transform.lossyScale.x;
                for (int h = 0; h < 2 && !near; h++)
                {
                    var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                    near = HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip) && (tip - c).magnitude <= r;
                }
            }
            if (!near) return;
            Debug.Log($"[KeeperHall] Walked up to (or poked) {KeeperName}: the battle again.");
            StartCoroutine(Meet());
        }

        /// <summary>Still wrapped in strings (the untangle piece runs): it sits dazed and trembling, the aura low.</summary>
        void ShowTangled()
        {
            EnsureModel();
            if (_model == null) return;
            _look.calm = 0f;
            _model.View.Daze();
            _fury.fury = tangledFury;
            _fury.idleStomps = false;
            Debug.Log($"[KeeperHall] {KeeperName} sits wrapped in strings ({untangle.Undone} / {untangle.knots.Length} knots undone).");
        }

        void OnUntangled(HandPuzzle p)
        {
            if (!Active || Calmed) return;
            Debug.Log($"[KeeperHall] {KeeperName} is free of the strings.");
            StartCoroutine(Meet());
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
                if (restsWhenCalm) _model.View.Daze(); // resting by its forge
                else _model.View.Revive();
                Debug.Log($"[KeeperHall] {KeeperName} {(restsWhenCalm ? "rests" : "preens")} in its hall (calmed).");
                return;
            }
            if (Tangled)
            {
                ShowTangled();
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
            explorer.Locked = true;
            if (untangle != null)
            {
                // Just untangled: it shakes free, flaps and the aura flares before it speaks.
                _model.View.Revive();
                _model.View.Hop();
                _fury.fury = 1f;
                if (fx != null) fx.Burst(_model.WorldCenter, 36, sparks, TileWorld * 1.2f, 0.8f, TileWorld * 0.05f, Vector3.up * TileWorld * 0.4f);
                Debug.Log($"[KeeperHall] {KeeperName} shakes free and flares up.");
            }
            else
            {
                _model.View.Daze();
                Debug.Log($"[KeeperHall] {KeeperName} lies in its hall, frightened.");
            }
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
            _fury.idleStomps = stomps;
            if (!Active)
            {
                // Left during the meeting (the dev panel's teleport): no battle outside the hall.
                _phase = Phase.Idle;
                explorer.Locked = false;
                Debug.Log("[KeeperHall] Left the hall during the meeting: no battle.");
                yield break;
            }
            if (fx != null) fx.Burst(_look.CrystalCount > 0 ? _look.CrownWorld : _model.WorldCenter, 30, glowColor, TileWorld * 0.8f, 0.8f, TileWorld * 0.05f, Vector3.up * TileWorld * 0.6f);
            explorer.Locked = false;
            // Step 10a: Keepers grow with the other realms' shards the player holds.
            int level = ShardScaling.KeeperLevel(keeperLevel, ownShard);
            // Nov 3: easier in the Quick Journey (no gift any more; its realm only).
            level = QuickJourney.KeeperLevel(level);
            // Step 14h: the full game's Keeper follows the team at the door (KeeperScaling; the Quick Journey keeps its fixed one).
            int shardLevel = level;
            var party = FindFirstObjectByType<Party>();
            if (!QuickJourney.Active && party != null) level = KeeperScaling.Level(level, party.members, keeperSpecies);
            // Nov 5: the quick Keeper stays easy but a strong team gets a little more of a fight (at most +3).
            else if (party != null) level += QuickJourney.StrongTeamExtra(KeeperScaling.TeamLevel(party.members));
            if (level != shardLevel)
                Debug.Log($"[KeeperHall] Tuned to the team: {KeeperScaling.Fighters(party.members)} fighter(s), top-3 Lv {KeeperScaling.TeamLevel(party.members):0.0}: Keeper Lv {shardLevel} -> {level}.");
            var fight = new EncounterDirector.KeeperFight
            {
                keeper = new IslingInstance(keeperSpecies, level),
                keeperPad = keeperPad,
                myPad = partyPad,
                caption = string.Format(captionFormat, keeperSpecies.displayName),
            };
            _phase = Phase.Fighting;
            _battleLook = null;
            if (!encounters.StartKeeper(fight))
            {
                Debug.LogError("[KeeperHall] The Keeper battle didn't start (a battle on, an empty party or a zoom).", this);
                _phase = Phase.Idle;
                yield break;
            }
            Debug.Log($"[KeeperHall] The Keeper battle starts: {keeperSpecies.displayName} Lv {level} (base {keeperLevel}, {ShardScaling.Held(ownShard)} other shard(s)).");
        }

        // ---- the battle's look ----

        void UpdateBattleLook()
        {
            var wild = stage.Wild;
            if (wild == null || encounters.Keeper == null) return;
            if (_battleLook == null || _battleLook.gameObject != wild.gameObject)
            {
                _battleLook = AddLook(wild.gameObject);
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
            if (!stomps)
            {
                // It flaps up with a gust; the Spirit flinches back.
                var view = fury.GetComponent<CreatureView>();
                if (view != null) view.Hop();
                var actor = fury.GetComponent<StageActor>();
                Vector3 at = actor != null ? actor.WorldCenter : fury.transform.position;
                if (fx != null) fx.Burst(at, 30, sparks, TileWorld * 1.2f, 0.8f, TileWorld * 0.05f, Vector3.up * TileWorld * 0.3f);
                if (spirit != null) spirit.Flinch(at);
                Debug.Log($"[KeeperHall] Battle intro: {KeeperName} rears up (a hop and a burst, no stomp).");
                yield break;
            }
            fury.Stomped += OnBattleStomp;
            fury.Stomp(26f);
            Debug.Log($"[KeeperHall] Battle intro: {KeeperName} rears up and stomps.");
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
                if (_shell != null) _shell.Set(shellCore, shellEdge, 0.35f, 2.5f, 0.15f, 0.5f, 0.3f);
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
                    GameAudio.At(AudioIds.KeeperCrownShield, stage.Wild.WorldCenter);
                    GameAudio.At(AudioIds.KeeperCrownCrack, _battleLook.CrystalCount > 0 ? _battleLook.CrystalWorld(crystal) : stage.Wild.WorldCenter);
                    if (fx != null) fx.Burst(_battleLook.CrystalCount > 0 ? _battleLook.CrystalWorld(crystal) : _shell != null ? _shell.transform.position : stage.Wild.WorldCenter,
                        10, glowColor, tile * 0.5f, 0.5f, tile * 0.04f);
                    if (!string.IsNullOrEmpty(shieldLine)) Dialogue.Say(shieldLine);
                    break;
                case BattleEventKind.ShieldBroken:
                    GameAudio.At(AudioIds.KeeperCrownBreak, _shell != null ? _shell.transform.position : stage.Wild.WorldCenter);
                    if (fx != null && _shell != null) fx.Burst(_shell.transform.position, 40, shellCore, tile * 1.2f, 0.7f, tile * 0.05f);
                    SetShell(false);
                    _battleLook.SetAllCrystals(0.2f);
                    if (!string.IsNullOrEmpty(brokenLine)) Dialogue.Say(brokenLine);
                    break;
                case BattleEventKind.ShieldRaised:
                    GameAudio.At(AudioIds.KeeperCrownShield, stage.Wild.WorldCenter);
                    SetShell(true);
                    _battleLook.SetAllCrystals(1f);
                    if (fx != null) fx.Burst(_battleLook.CrystalCount > 0 ? _battleLook.CrownWorld : stage.Wild.WorldCenter, 24, glowColor, tile * 0.7f, 0.6f, tile * 0.05f, Vector3.up * tile * 0.5f);
                    break;
                case BattleEventKind.KeeperPhase2:
                    _battleLook.crownBoost = 1.7f;
                    GameAudio.At(AudioIds.KeeperFury, stage.Wild.WorldCenter);
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
                    _restartAt = Time.time + 3f;
                    explorer.Teleport(hallEntry, new Vector2Int(0, -1));
                    Debug.Log("[KeeperHall] Ran from the Keeper: back at the hall door; it lies down again.");
                    if (_model != null) _model.View.Daze();
                    if (_fury != null) _fury.idleStomps = false;
                    break;
                default:
                    _phase = Phase.Idle;
                    _restartAt = Time.time + 3f;
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
            Debug.Log($"[KeeperHall] {KeeperName}'s fury burns off into gold motes.");
            GameAudio.At(AudioIds.KeeperCalm, _model.transform.position);
            GameAudio.Ui(AudioIds.JingleKeeper);
            MusicDirector.Jingle(4f);
            yield return _fury.BurnOff(1.8f);
            Debug.Log($"[KeeperHall] {KeeperName} lowers its head and calms.");
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
            PuzzleState.Solve(keeperId, 1); // saved: calmed
            // Nods.
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                _model.pitch = 14f + 10f * Mathf.Sin(t * Mathf.PI * 2f);
                yield return null;
            }
            _model.pitch = 0f;
            if (!string.IsNullOrEmpty(winLine)) Dialogue.Say(winLine);
            for (float t = 0f; t < 6f && Dialogue.IsPending(winLine); t += Time.deltaTime) yield return null;
            // The forge ignites (Galeowl: the window clears).
            Vector3 mouth = island.transform.TransformPoint(island.TileToLocal(forgeMouth, island.Content.capHeight + forgeMouthHeight * island.map.tileSize));
            if (fx != null) fx.Burst(mouth, 50, glowColor, TileWorld * 1.4f, 1f, TileWorld * 0.07f, Vector3.up * TileWorld);
            HallLit?.Invoke();
            Debug.Log($"[KeeperHall] The {(forgeFlame ? "forge ignites" : "hall lights up")}; {ownShard} rises.");
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
            _shardFloat.Stop(0f);
            _shardFloat = AudioService.PlayLoop(AudioIds.ShardFloat, island.transform.TransformPoint(hover), 0.6f);
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
            _shardWait = 0f;
            if (!string.IsNullOrEmpty(shardLine)) Dialogue.Say(shardLine);
            if (ghosts != null) ghosts.Show(ShardGhost, GhostPlace.AtTarget, shardTarget, 1f, DominantHand.Left, false);
            FocusHighlight.SetPending(() => _shardHovering ? island.transform.TransformPoint(_shardLocal) : (Vector3?)null, 0.05f * island.transform.lossyScale.x);
            explorer.Locked = false;
            Debug.Log($"[KeeperHall] {ownShard} floats in front of the player: grab it (pinch, or look + pinch).");
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
            DrawShardRing(true);
            // Step 11d: not taken within a few seconds, it goes to the Spirit by itself.
            _shardWait += Time.deltaTime;
            if (_shardWait >= shardAutoSeconds)
            {
                CollectShard($"by itself after {shardAutoSeconds:0.#} s");
                return;
            }
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
                bool fist = hand != null && hand.IsTrackedDataValid && HandLookup.TryGetJoint(hand, HandJointId.HandPalm, out var palm) &&
                            HandPuzzle.IsFistPose(hand, palm) && (palm - world).magnitude < shardGrabReach * 1.2f;
                bool fistStarted = fist && !_wasFist[h];
                _wasFist[h] = fist;
                if (fistStarted)
                {
                    CollectShard($"{(h == 0 ? "left" : "right")} hand grab");
                    return;
                }
                if (!started) continue;
                bool near = IslandHandle.TryGetPinchPoint(hand, out var p) && (p - world).magnitude < shardGrabReach;
                if (near || gazed)
                {
                    CollectShard(near ? $"{(h == 0 ? "left" : "right")} pinch" : "look + pinch");
                    return;
                }
            }
        }

        /// <summary>Step 11d: a gold ring and glow round the floating shard while it waits.</summary>
        void DrawShardRing(bool on)
        {
            if (beaconMaterial == null || island == null || island.map == null || island.Content == null) return;
            if (_shardRing == null) _shardRing = new GuideRings("Keeper Shard Focus", island, beaconMaterial, 1, fx);
            float tileMetres = Mathf.Max(1e-5f, island.map.tileSize * island.Zoom);
            Vector2 tile = island.LocalToTile(_shardLocal);
            float height = (_shardLocal.y - island.CapTopLocal) / tileMetres - 0.04f / tileMetres;
            float pulse = 0.85f + 0.2f * Mathf.Sin(Time.time * 3f);
            _shardRing.Draw(0, on, new Vector3(tile.x, height, tile.y), 0.1f / tileMetres, 1.4f * pulse, 0.05f / tileMetres * pulse);
        }

        /// <summary>The shard is taken (a hand, look + pinch, or the bridge): it flies into the holder.</summary>
        public void CollectShard(string how)
        {
            if (_phase != Phase.Shard || !_shardHovering) return;
            Debug.Log($"[KeeperHall] {ownShard} taken ({how}): it flies to the Spirit.");
            _shardFloat.Stop(0.5f);
            StartCoroutine(ShardToHolder());
        }

        IEnumerator ShardToHolder()
        {
            _phase = Phase.Leaving;
            if (ghosts != null && ghosts.ClipName == ShardGhost) ghosts.Hide();
            FocusHighlight.Clear();
            DrawShardRing(false);
            Vector3 from = _shardLocal, to = from;
            if (holder != null) holder.SetOut(ownShard, true);
            float small = holder != null ? holder.carryMetres : 0.016f;
            for (float t = 0f; t < 0.9f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.9f);
                // The Spirit bobs: aim at where its shard spot is now; shrink on the way.
                if (holder != null) to = holder.CupLocal(ownShard);
                _shardLocal = Vector3.Lerp(from, to, k) + Vector3.up * (0.05f * Mathf.Sin(k * Mathf.PI));
                DrawShard(Mathf.Lerp(BigShard, small, k));
                yield return null;
            }
            _shardHovering = false;
            if (_shard != null) _shard.Hide();
            PuzzleState.SetStage(ownShard, 1); // saved: held
            if (holder != null) holder.SetOut(ownShard, false);
            if (fx != null) fx.Burst(island.transform.TransformPoint(to), 20, shardColor, 0.03f, 0.6f, 0.004f);
            GameAudio.At(AudioIds.ShardDock, island.transform.TransformPoint(to));
            GameAudio.Ui(AudioIds.JingleShard);
            MusicDirector.Jingle(3f);
            yield return new WaitForSeconds(1.2f);
            _phase = Phase.Idle;
            if (Active && swap.Return()) Debug.Log("[KeeperHall] Back outside for the restoration.");
            _waveDue = true;
        }

        bool _waveDue;
        AudioHandle _shardFloat;

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
            GameAudio.Ui(AudioIds.RestoreSwell);
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
            if (save != null) save.Save($"{realmName} restored", true);
            yield return Thanks();
        }

        /// <summary>
        /// Step 14h: the first Keeper calmed (once per save, either slot), back on the realm map after the wave and the
        /// return gate: if the starter's line hasn't evolved, this Keeper thanks it (its own voice) and the starter becomes
        /// ready to evolve (its pod glows; GrowthDirector does the rest).
        /// </summary>
        IEnumerator Thanks()
        {
            if (PuzzleState.IsSolved(KeeperThanks.Flag)) yield break;
            var party = FindFirstObjectByType<Party>();
            if (party == null) yield break;
            for (float t = 0f; t < 10f && (Dialogue.IsTalking || SceneSwap.IsBusy); t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(0.6f);
            var opening = FindFirstObjectByType<Opening>(FindObjectsInactive.Include);
            var starters = opening != null ? opening.starters : FinaleDirector.Instance != null ? FinaleDirector.Instance.starters : null;
            var c = KeeperThanks.Apply(PuzzleState.IsSolved, id => PuzzleState.Solve(id, 1), party.members, party.storage, starters, out int was);
            if (c == null)
            {
                Debug.Log($"[KeeperHall] {KeeperName} calmed first: the starter has already evolved (or isn't here), no thanks line.");
                yield break;
            }
            party.NotifyChanged();
            Dialogue.SetVar("name", c.Name);
            Dialogue.Say(KeeperThanks.Line(ownShard));
            Debug.Log($"[KeeperHall] {KeeperName} thanks {c.Name}: Lv {was} -> {c.level}, ready to evolve into {c.species.evolvesTo.displayName}.");
            if (save != null) save.Save("keeper thanks");
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

        /// <summary>Step 13: the calm Keeper was poked (the post-game rematch asks "Spar?").</summary>
        public event Action Snorted;

        /// <summary>A happy snort (poked while calm; also the bridge).</summary>
        public void Snort()
        {
            if (_model == null) return;
            Snorted?.Invoke();
            _snortAt = Time.time + 1.5f;
            _model.View.Revive();
            _model.View.Hop();
            if (hoots)
            {
                if (_audio == null)
                {
                    _audio = gameObject.AddComponent<AudioSource>();
                    _audio.playOnAwake = false;
                    _audio.spatialBlend = 0f;
                }
                GameAudio.Play(AudioIds.VoiceGaleowl, _model.WorldCenter, _audio, PuzzleSounds.Hoot, 0.8f);
            }
            if (fx != null) fx.Burst(_model.WorldCenter + island.transform.forward * (_model.View.height * 0.4f * island.Zoom * island.transform.lossyScale.x),
                12, new Color(1f, 0.9f, 0.75f, 0.8f), TileWorld * 0.3f, 0.6f, TileWorld * 0.05f, Vector3.up * TileWorld * 0.3f);
            Debug.Log($"[KeeperHall] {KeeperName} {(hoots ? "hoots" : "snorts")} happily.");
            if (restsWhenCalm) StartCoroutine(BackToRest());
        }

        AudioSource _audio;

        IEnumerator BackToRest()
        {
            yield return new WaitForSeconds(1.2f);
            if (_model != null && Calmed && _phase == Phase.Idle) _model.View.Daze();
        }

        /// <summary>Debug (bridge): the shard is in front of the player.</summary>
        public bool ShardWaiting => _phase == Phase.Shard && _shardHovering;
    }

    /// <summary>Step 14h: the first calmed Keeper's thanks: the starter becomes ready to evolve (pure parts, tested).</summary>
    public static class KeeperThanks
    {
        /// <summary>Puzzle flag: the thanks happened (once per save).</summary>
        public const string Flag = "keeper.thanks";

        /// <summary>The Keeper's line for its shard ("shard.wind" -> "keeper.thanks.evolve.wind").</summary>
        public static string Line(string shard) =>
            "keeper.thanks.evolve." + (!string.IsNullOrEmpty(shard) && shard.StartsWith("shard.") ? shard.Substring(6) : "wind");

        /// <summary>
        /// Once per save (<paramref name="isSolved"/> / <paramref name="solve"/> keep the <see cref="Flag"/>): the starter
        /// whose line hasn't evolved becomes ready to evolve. Returns it (its level before in <paramref name="was"/>), or null.
        /// </summary>
        public static IslingInstance Apply(Func<string, bool> isSolved, Action<string> solve, IList<IslingInstance> pods,
            IList<IslingInstance> storage, IList<IslingSpecies> starters, out int was)
        {
            was = 0;
            if (isSolved(Flag)) return null;
            solve(Flag);
            var c = Starter(pods, storage, starters);
            if (c == null) return null;
            was = c.level;
            Progression.MakeReady(c);
            return c;
        }

        /// <summary>
        /// The starter (an Isling whose species is one of <paramref name="starters"/>, so its line hasn't evolved yet),
        /// pods first, then storage; null when there is none.
        /// </summary>
        public static IslingInstance Starter(IList<IslingInstance> pods, IList<IslingInstance> storage, IList<IslingSpecies> starters)
        {
            if (starters == null) return null;
            foreach (var list in new[] { pods, storage })
                if (list != null)
                    foreach (var c in list)
                        if (c != null && c.species != null && c.species.evolvesTo != null && starters.Contains(c.species)) return c;
            return null;
        }
    }
}
