using System;
using System.Collections;
using System.Collections.Generic;
using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Shows a battle on the zoomed-in island: spawns the two creatures, plays the battle events as the four animation
    /// templates (Shoot, Lunge, Slam, Shield) recoloured by element, hit flashes, HP bars and pop-up text.
    /// Knows nothing about rules; it only animates what <see cref="BattleSystem"/> reports.
    /// Sizes are map metres (scaled by the zoom), so the look holds at any zoom and island scale.
    /// </summary>
    public class BattleStage : MonoBehaviour
    {
        public IslandBuilder island;
        public BattleFx fx;
        public BattleHud hud;
        [Tooltip("Round earth disc each creature stands on.")]
        public BattlePad padPrefab;
        public float padSeconds = 0.4f;
        [Tooltip("XP curve, for the party creature's XP bar.")]
        public ProgressionConfig progression;

        [Header("Timing (s)")]
        public float pauseBetweenMoves = 0.45f;

        [Header("Step 16: battle spectacle")]
        [Tooltip("Every move winds up this long first: crouch, squash, the element's glow building, a charge sound (s).")]
        public float windUpSeconds = 0.25f;
        [Tooltip("Every damaging hit holds the stage this long (hit-stop, s); super effective uses the larger of this and PolishConfig.hitStop.")]
        public float hitStopSeconds = 0.06f;
        [Tooltip("The projectile's flight (s) and its size (map metres).")]
        public float shotSeconds = 0.35f, shotRadius = 0.011f;
        [Tooltip("Idle on the pad: bob (share of the height), sway (degrees), a glance every few seconds.")]
        public float idleBob = 0.03f, idleSway = 0f, glanceYaw = 6f; // Nov 6: no side-to-side sway (Mugesh), a smaller glance
        [Tooltip("A lunge hops this high (share of the height) on its way in.")]
        public float lungeHop = 0.35f;

        static readonly Color Gold = new Color(1f, 0.84f, 0.35f);
        static readonly Color Leaf = new Color(0.55f, 0.8f, 0.35f);
        static readonly Color Smoke = new Color(0.6f, 0.64f, 0.61f);
        static readonly Color SuperColor = new Color(1f, 0.85f, 0.3f);
        static readonly Color WeakColor = new Color(0.75f, 0.8f, 0.78f);
        static readonly Color HealColor = new Color(0.55f, 0.95f, 0.55f);

        StageActor _player, _wild;
        BattlePad _playerPad, _wildPad;
        BattleHud.Bar _playerBar, _wildBar;
        BattleFx.Item _playerShield, _wildShield;

        public StageActor Player => _player;
        public StageActor Wild => _wild;

        /// <summary>
        /// A move's outcome landed (at the moment of impact), or a Keeper event played (shield broken / raised, phase 2):
        /// the Keeper's look follows (step 9c: cracked crystals, the shell, the crown flaring).
        /// </summary>
        public event Action<BattleEvent> Presented;

        void Start()
        {
            // The gold focus outline marks them (FocusHighlight); white flashes are for gaze hover only.
            Dialogue.RegisterTarget("nearPad", () => PadPoint(_playerPad), k => { }, () => World(TileSize * 0.9f));
            Dialogue.RegisterTarget("wildPad", () => PadPoint(_wildPad), k => { }, () => World(TileSize * 0.9f));
        }

        Vector3? PadPoint(BattlePad pad) =>
            this != null && pad != null ? island.transform.TransformPoint(island.TileToLocal(pad.tile, Cap + pad.TopLift)) : (Vector3?)null;

        // The creature on the pad flashes softly (the shared _Flash highlight).
        float Zoom => island.Zoom;
        float TileSize => island.map.tileSize;
        float Cap => island.Content.capHeight;
        /// <summary>Map metres to world metres right now.</summary>
        float World(float mapMetres) => mapMetres * Zoom * island.transform.lossyScale.x;
        Vector3 WorldAt(Vector2 tile, float height) => island.transform.TransformPoint(island.TileToLocal(tile, Cap + height));

        // ---- audio-D: cries and the low-HP pulse ----

        string _playerCry, _wildCry;
        AudioHandle _lowHp;

        /// <summary>The Isling's cry (cry.&lt;species&gt;) on it; 0.8 pitch = the faint (slower and lower). Silent for a species without one.</summary>
        static void Cry(StageActor a, string id, float pitch = 1f)
        {
            if (a == null || string.IsNullOrEmpty(id) || !AudioService.Has(id)) return;
            GameAudio.On(id, a.transform).SetPitch(pitch);
        }

        IEnumerator CryLater(StageActor a, string id, float delay)
        {
            yield return new WaitForSeconds(delay);
            Cry(a, id);
        }

        /// <summary>hp.low: a soft pulse while the player's Isling is under a quarter of its HP; stops on a heal or when the battle ends.</summary>
        void LowHp()
        {
            bool low = _playerBar != null && _player != null && _playerBar.target > 0f && _playerBar.target < 0.25f * _playerBar.max;
            if (low && !_lowHp.IsValid && AudioService.Has(AudioIds.HpLow)) _lowHp = AudioService.PlayLoop2D(AudioIds.HpLow, 0.3f);
            else if (!low && _lowHp.IsValid)
            {
                _lowHp.Stop(0.3f);
                _lowHp = default;
            }
        }

        void OnEnable() => ObjectiveArea.Collect += KeepClearOfCreatures;

        void OnDisable()
        {
            ObjectiveArea.Collect -= KeepClearOfCreatures;
            _lowHp.Stop(0.1f);
            _lowHp = default;
        }

        /// <summary>Step 16: the Spirit and its bubble keep off both creatures (your own Isling, the wild one) in a battle.</summary>
        void KeepClearOfCreatures(System.Collections.Generic.List<Vector4> into)
        {
            foreach (var a in new[] { _player, _wild })
            {
                if (a == null || !a.TryGetVisualBounds(out var b)) continue;
                into.Add(ObjectiveArea.Sphere(b.center, Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z)) * 1.1f));
            }
        }

        // ---- setup ----

        /// <summary>Puts a creature on the stage, hidden (grow 0) until an entrance plays.</summary>
        public StageActor Spawn(IslingInstance creature, BattleSide side, Vector2 tile, Vector2 facing)
        {
            var prefab = creature.species != null ? creature.species.Prefab : null;
            if (prefab == null)
            {
                Debug.LogError($"[BattleStage] {creature.Name} has no baked prefab.");
                return null;
            }
            var go = Instantiate(prefab, island.transform);
            go.name = $"{(side == BattleSide.Player ? "Player" : "Wild")} {creature.Name}";
            var actor = go.AddComponent<StageActor>();
            actor.island = island;
            actor.tile = tile;
            actor.yaw = StageActor.YawToward(tile, facing);
            actor.grow = 0f;
            var pad = side == BattleSide.Player ? _playerPad : _wildPad;
            if (pad != null) actor.baseLift = pad.TopLift;
            actor.Snap();
            if (side == BattleSide.Player) _player = actor;
            else
            {
                _wild = actor;
                _wildKeeper = creature.species != null && creature.species.keeper;
            }
            FitPads(true);
            if (side == BattleSide.Player) _playerCry = AudioIds.Cry(SpeciesRegistry.IdOf(creature.species));
            else _wildCry = AudioIds.Cry(SpeciesRegistry.IdOf(creature.species));
            return actor;
        }

        public void ShowBars(IslingInstance player, IslingInstance wild)
        {
            ShowPlayerBar(player);
            ShowWildBar(wild);
        }

        public void ShowWildBar(IslingInstance wild)
        {
            hud.RemoveBar(_wildBar);
            // Step 14k: in front of its pad (between the pads), clear of the near Isling.
            _wildBar = hud.AddBar(_wild, wild.Name, wild.level, wild.currentHp, wild.MaxHp, 0f, false, () => PadRadiusLocal(_wildPad));
        }

        bool _wildKeeper;

        /// <summary>
        /// Step 14h: readable battles (<see cref="PadFit"/>): with both actors on their pads, scales them down if the near one
        /// would hide the wild one from the player's eyes, or one is over 1.3x the other. <paramref name="snap"/> = at once
        /// (an actor just spawned, still hidden); otherwise it eases.
        /// </summary>
        void FitPads(bool snap)
        {
            if (_player == null || _wild == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            var t = island.transform;
            // Island-local: heights in map metres times the zoom.
            Vector3 eye = t.InverseTransformPoint(cam.transform.position);
            Vector3 nearFeet = island.TileToLocal(_player.tile, Cap + _player.baseLift), wildFeet = island.TileToLocal(_wild.tile, Cap + _wild.baseLift);
            _player.MeasureCached(out float nearH, out _);
            _wild.MeasureCached(out float wildH, out _);
            var r = PadFit.Fit(eye, nearFeet, nearH, wildFeet, wildH, _wildKeeper);
            bool changed = Mathf.Abs(_player.fitTarget - r.near) > 0.03f || Mathf.Abs(_wild.fitTarget - r.wild) > 0.03f;
            _player.fitTarget = r.near;
            _wild.fitTarget = r.wild;
            if (snap && _player.grow <= 0.001f) _player.fit = r.near;
            if (snap && _wild.grow <= 0.001f) _wild.fit = r.wild;
            if ((snap || changed) && (r.near < 0.999f || r.wild < 0.999f))
                Debug.Log($"[BattleStage] Pad fit: near {_player.name} x{r.near:0.00} (drawn {nearH:0.000}), wild {_wild.name} x{r.wild:0.00} (drawn {wildH:0.000})" +
                          $"{(r.sightline ? " (sightline)" : "")}{(r.ratio ? " (1.3x rule)" : "")}.");
        }

        /// <summary>HP bar for the party creature on the field (after a switch).</summary>
        public void ShowPlayerBar(IslingInstance player)
        {
            hud.RemoveBar(_playerBar);
            // Step 14k: the near one's label in front of its pad, toward the runes.
            _playerBar = hud.AddBar(_player, player.Name, player.level, player.currentHp, player.MaxHp, 0f, true, () => PadRadiusLocal(_playerPad));
            if (progression != null) hud.SetXp(_playerBar, progression.XpShare(player));
        }

        /// <summary>Step 14j: a pad's radius, island-local (0 without one).</summary>
        float PadRadiusLocal(BattlePad pad)
        {
            if (pad == null || island == null) return 0f;
            float tile = (island.TileToLocal(pad.tile + Vector2.right, Cap) - island.TileToLocal(pad.tile, Cap)).magnitude;
            return pad.widthTiles * tile * 0.5f;
        }

        /// <summary>Island-local point on top of a pad (where a thrown pod lands).</summary>
        public Vector3 PadTopLocal(BattleSide side)
        {
            var pad = side == BattleSide.Player ? _playerPad : _wildPad;
            if (pad == null) return island.TileToLocal(Vector2.zero, Cap);
            return island.TileToLocal(pad.tile, Cap + pad.TopLift);
        }

        /// <summary>Removes both creatures, shields and labels.</summary>
        public void Cleanup()
        {
            if (_playerPad != null) Destroy(_playerPad.gameObject);
            if (_wildPad != null) Destroy(_wildPad.gameObject);
            _playerPad = _wildPad = null;
            if (_player != null) Destroy(_player.gameObject);
            if (_wild != null) Destroy(_wild.gameObject);
            _player = _wild = null;
            DropShield(ref _playerShield, false);
            DropShield(ref _wildShield, false);
            hud.Clear();
            _playerBar = _wildBar = null;
        }

        // ---- pads ----

        /// <summary>Puts both pads in the ground (risen with <see cref="RisePads"/>), tinted for the habitat.</summary>
        public void SpawnPads(Vector2 playerTile, Vector2 wildTile, Habitat habitat)
        {
            if (padPrefab == null)
            {
                Debug.LogError("[BattleStage] No pad prefab assigned (Tools > Diorama > Step 5 > 2. Add Battle to MR_Main).", this);
                return;
            }
            _playerPad = MakePad("Pad (player)", playerTile, habitat);
            _wildPad = MakePad("Pad (wild)", wildTile, habitat);
        }

        BattlePad MakePad(string name, Vector2 tile, Habitat habitat)
        {
            var pad = Instantiate(padPrefab, island.transform);
            pad.name = name;
            pad.island = island;
            pad.tile = tile;
            pad.Rise = 0f;
            pad.Paint(habitat);
            pad.Snap();
            return pad;
        }

        /// <summary>Pads rise out of the ground (or sink back) over <see cref="padSeconds"/>.</summary>
        public IEnumerator RisePads(bool up)
        {
            float from = up ? 0f : 1f, to = up ? 1f : 0f;
            var padAt = _wildPad != null ? island.transform.TransformPoint(island.TileToLocal(_wildPad.tile, Cap)) : island.transform.position;
            GameAudio.At(up ? AudioIds.PadsRise : AudioIds.ClearingRumble, padAt);
            yield return Tween(padSeconds, p =>
            {
                float k = Mathf.Lerp(from, to, p);
                if (_playerPad != null) _playerPad.Rise = k;
                if (_wildPad != null) _wildPad.Rise = k;
            });
        }

        // ---- entrances ----

        /// <summary>Grass shakes around a tile (before the creature shows); gold sparkles if it is rare.</summary>
        public IEnumerator Rustle(Vector2Int tile, bool rare, float seconds)
        {
            island.FlashTile(tile, seconds);
            Vector3 p = WorldAt(tile, 0.01f);
            GameAudio.At(AudioIds.EncRustle, p);
            fx.Burst(p, 10, Leaf, World(0.03f), 0.5f, World(0.004f), Vector3.up * World(0.03f));
            float t = 0f;
            while (t < seconds)
            {
                IslandBuilder.SetGrassPush(p, 0.9f * Mathf.Abs(Mathf.Sin(t * 26f)) * (1f - t / seconds));
                if (rare && Time.frameCount % 3 == 0) fx.Burst(WorldAt(tile, 0.02f), 2, Gold, World(0.03f), 0.6f, World(0.004f), Vector3.up * World(0.04f));
                t += Time.deltaTime;
                yield return null;
            }
            IslandBuilder.SetGrassPush(p, 0f);
        }

        /// <summary>Rings spreading on the water where something is about to jump out.</summary>
        public IEnumerator Ripple(Vector2Int tile, float seconds)
        {
            var color = ElementStyle.Of(Element.Water).burst;
            var rings = new List<(BattleFx.Item item, float start)>();
            float t = 0f, next = 0f;
            while (t < seconds || rings.Count > 0)
            {
                if (t < seconds && t >= next)
                {
                    var r = fx.Get(BattleFx.Shape.Ring);
                    r.Set(color, color, 1f);
                    rings.Add((r, t));
                    next += seconds / 3f;
                }
                for (int i = rings.Count - 1; i >= 0; i--)
                {
                    float u = (t - rings[i].start) / 0.7f;
                    if (u >= 1f)
                    {
                        fx.Release(rings[i].item);
                        rings.RemoveAt(i);
                        continue;
                    }
                    var tr = rings[i].item.transform;
                    tr.localPosition = island.TileToLocal(tile, Cap + 0.0015f);
                    tr.localRotation = Quaternion.identity;
                    tr.localScale = Vector3.one * (TileSize * Zoom * Mathf.Lerp(0.1f, 0.6f, u));
                    rings[i].item.SetAlpha(1f - u);
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>The wild creature pops out of the grass with a hop, or leaps out of the water in an arc.</summary>
        /// <param name="waterTile">Pond battles: the water tile it leaps out of, onto its pad.</param>
        public IEnumerator WildAppears(bool rare, Vector2? waterTile)
        {
            var a = _wild;
            if (a == null) yield break;
            Vector3 burstAt = WorldAt(a.tile, 0.01f);
            GameAudio.At(rare ? AudioIds.EncRare : AudioIds.EncSting, burstAt);
            StartCoroutine(CryLater(a, _wildCry, 0.45f));
            if (waterTile.HasValue)
            {
                GameAudio.At(AudioIds.PondSplash, WorldAt(waterTile.Value, 0f));
                var water = ElementStyle.Of(Element.Water);
                Vector2 from = waterTile.Value, to = a.tile;
                fx.Burst(WorldAt(from, 0.004f), 24, water.burst, World(0.06f), 0.6f, World(0.004f), Vector3.up * World(0.08f));
                float h = a.Height, rest = a.baseLift;
                a.grow = 1f;
                yield return Tween(0.6f, p =>
                {
                    a.tile = Vector2.Lerp(from, to, p);
                    // Starts below the water surface, arcs up and lands on the pad.
                    a.lift = Mathf.Lerp(-h - rest, 0f, p) + h * 1.8f * Mathf.Sin(p * Mathf.PI);
                    a.squash = new Vector3(1f - 0.1f * (1f - p), 1f + 0.15f * (1f - p), 1f - 0.1f * (1f - p));
                });
                a.tile = to;
                a.lift = 0f;
                a.squash = Vector3.one;
                fx.Burst(burstAt, 16, water.trail, World(0.04f), 0.4f, World(0.003f), Vector3.up * World(0.03f));
            }
            else
            {
                fx.Burst(burstAt, 18, Leaf, World(0.05f), 0.5f, World(0.004f), Vector3.up * World(0.05f));
                yield return Tween(0.3f, p =>
                {
                    a.grow = EaseOutBack(p);
                    a.lift = a.Height * 0.5f * Mathf.Sin(p * Mathf.PI);
                });
                a.grow = 1f;
                a.lift = 0f;
            }
            if (rare) fx.Burst(a.WorldCenter, 30, Gold, World(0.05f), 0.9f, World(0.005f), Vector3.up * World(0.03f));
            a.View.Hop();
        }

        /// <summary>The thrown pod opens on the pad: a burst of light and the party creature grows in.</summary>
        public IEnumerator PodBurst()
        {
            var a = _player;
            if (a == null) yield break;
            Vector3 c = WorldAt(a.tile, a.CenterHeight);
            GameAudio.At(AudioIds.PodOpen, c);
            StartCoroutine(CryLater(a, _playerCry, 0.3f));
            fx.Burst(c, 26, new Color(1f, 0.88f, 0.54f), World(0.05f), 0.5f, World(0.004f), Vector3.up * World(0.02f));
            var ring = fx.Get(BattleFx.Shape.Ring);
            var col = ElementStyle.Of(Element.Neutral).burst;
            ring.Set(col, col, 1f);
            yield return Tween(0.35f, p =>
            {
                a.grow = EaseOutBack(p);
                ring.transform.localPosition = island.TileToLocal(a.tile, Cap + 0.002f);
                ring.transform.localScale = Vector3.one * (TileSize * Zoom * (0.15f + 0.5f * p));
                ring.SetAlpha(1f - p);
            });
            fx.Release(ring);
            a.grow = 1f;
            a.View.Hop();
            // Step 16: the wild one reacts to your Isling's arrival (a hop; a Keeper keeps its fury).
            if (_wild != null && !_wildKeeper && PolishConfig.BattleFeelOn) _wild.View.Hop();
        }

        // ---- pods: recall, capture ----

        /// <summary>The party creature turns to light and streams back into its pod at <paramref name="podWorld"/>; then it is gone.</summary>
        public IEnumerator RecallPlayer(Vector3 podWorld)
        {
            var a = _player;
            if (a == null) yield break;
            var light = new Color(1f, 0.88f, 0.54f);
            Vector3 from = a.WorldCenter;
            GameAudio.At(AudioIds.PodReturn, from);
            yield return Tween(0.4f, p =>
            {
                a.View.SetFlash(Mathf.Min(1f, p * 2.5f));
                a.grow = 1f - EaseInOut(p);
                if (Time.frameCount % 2 == 0)
                    fx.Burst(Vector3.Lerp(from, podWorld, p), 3, light, World(0.01f), 0.3f, World(0.004f));
            });
            fx.Burst(podWorld, 10, light, World(0.02f), 0.35f, World(0.003f));
            RemovePlayer();
        }

        /// <summary>Removes the party creature and its HP bar at once.</summary>
        public void RemovePlayer()
        {
            DropShield(ref _playerShield, false);
            hud.RemoveBar(_playerBar);
            _playerBar = null;
            if (_player != null) Destroy(_player.gameObject);
            _player = null;
        }

        /// <summary>The wild creature flashes white and shrinks into the pod at <paramref name="podWorld"/>.</summary>
        public IEnumerator SuckIntoPod(Vector3 podWorld)
        {
            var a = _wild;
            if (a == null) yield break;
            DropShieldOf(BattleSide.Wild, true);
            Vector3 from = a.WorldCenter;
            GameAudio.At(AudioIds.CatchHit, from);
            var light = new Color(1f, 0.95f, 0.8f);
            yield return Tween(0.45f, p =>
            {
                a.View.SetFlash(Mathf.Min(1f, p * 3f));
                a.grow = 1f - EaseInOut(p);
                if (Time.frameCount % 2 == 0) fx.Burst(Vector3.Lerp(from, podWorld, p), 3, light, World(0.008f), 0.25f, World(0.004f));
            });
            a.grow = 0f;
        }

        /// <summary>The wild creature bursts back out of the pod with a squash and stretch and an angry little shake.</summary>
        public IEnumerator BreakFree()
        {
            var a = _wild;
            if (a == null) yield break;
            yield return Tween(0.35f, p =>
            {
                a.grow = EaseOutBack(p);
                a.View.SetFlash(1f - p);
                float s = Mathf.Sin(p * Mathf.PI);
                a.squash = new Vector3(1f - 0.22f * s, 1f + 0.35f * s, 1f - 0.22f * s);
            });
            a.grow = 1f;
            a.View.SetFlash(0f);
            // Lands: a quick squash, then shakes it off.
            yield return Tween(0.12f, p =>
            {
                float s = Mathf.Sin(p * Mathf.PI);
                a.squash = new Vector3(1f + 0.2f * s, 1f - 0.25f * s, 1f + 0.2f * s);
            });
            float yaw0 = a.yaw;
            var huff = new Color(0.95f, 0.45f, 0.35f);
            fx.Burst(island.transform.TransformPoint(a.LocalTop), 6, huff, World(0.015f), 0.35f, World(0.004f), Vector3.up * World(0.02f));
            yield return Tween(0.4f, p =>
            {
                float w = Mathf.Sin(p * Mathf.PI * 8f) * (1f - p);
                a.yaw = yaw0 + w * 14f;
                a.squash = new Vector3(1f + 0.05f * w, 1f, 1f);
            });
            a.yaw = yaw0;
            a.squash = Vector3.one;
        }

        /// <summary>Caught: the wild creature and its bar go away.</summary>
        public void RemoveWild()
        {
            DropShield(ref _wildShield, false);
            hud.RemoveBar(_wildBar);
            _wildBar = null;
            if (_wild != null) Destroy(_wild.gameObject);
            _wild = null;
        }

        /// <summary>Sparkles and a "Caught!" pop at a world point.</summary>
        public void CatchSparkle(Vector3 world, string text)
        {
            var gold = new Color(1f, 0.84f, 0.35f);
            fx.Burst(world, 30, gold, World(0.05f), 0.8f, World(0.004f), Vector3.up * World(0.03f));
            hud.PopText(island.transform.InverseTransformPoint(world) + Vector3.up * 0.03f, text, gold, 0.9f, 1.4f);
            if (PolishConfig.BattleFeelOn) StartCoroutine(CatchStars(world));
        }

        // ---- Nov 5: battle feel (PolishConfig.battleFeel) ----

        /// <summary>The element's own touch on a hit: fire sparks fly up, water splashes a ring on the ground, wind swirls, neutral kicks up dust.</summary>
        void ElementHit(StageActor target, Element element)
        {
            Vector3 at = target.WorldCenter;
            Vector3 feet = island.transform.TransformPoint(island.TileToLocal(target.tile, Cap));
            Vector3 up = island.transform.up;
            switch (element)
            {
                case Element.Fire:
                    fx.Burst(at, 16, new Color(1f, 0.55f, 0.15f), World(0.035f), 0.7f, World(0.0025f), up * World(0.07f));
                    fx.Burst(at, 8, new Color(1f, 0.9f, 0.5f), World(0.05f), 0.4f, World(0.002f), up * World(0.05f));
                    break;
                case Element.Water:
                    StartCoroutine(Splash(feet, new Color(0.55f, 0.85f, 1f)));
                    fx.Burst(at, 14, new Color(0.6f, 0.88f, 1f), World(0.04f), 0.6f, World(0.003f), up * World(0.04f), World(0.25f));
                    break;
                case Element.Wind:
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i / 12f * Mathf.PI * 2f;
                        Vector3 r = island.transform.rotation * new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        Vector3 tangent = Vector3.Cross(up, r);
                        fx.Burst(at + r * World(0.022f) + up * World(0.006f * (i % 3)), 1, new Color(0.85f, 1f, 0.92f), World(0.004f), 0.55f, World(0.0025f),
                            tangent * World(0.09f) + up * World(0.02f));
                    }
                    break;
                default:
                    fx.Burst(feet + up * World(0.004f), 12, new Color(0.72f, 0.66f, 0.58f), World(0.03f), 0.75f, World(0.005f), up * World(0.012f));
                    break;
            }
        }

        /// <summary>A flat ring spreading on the ground under the target (water).</summary>
        IEnumerator Splash(Vector3 feet, Color color)
        {
            var ring = fx.Get(BattleFx.Shape.Ring);
            ring.Set(color, Color.white, 1f);
            yield return Tween(0.45f, p =>
            {
                ring.transform.SetPositionAndRotation(feet + island.transform.up * World(0.002f), island.transform.rotation);
                ring.transform.localScale = Vector3.one * (TileSize * Zoom * (0.15f + 0.9f * p));
                ring.SetAlpha((1f - p) * 0.9f);
            });
            fx.Release(ring);
        }

        /// <summary>Super effective: a gold ring flashes out round the target (facing the player).</summary>
        IEnumerator GoldFlash(Vector3 world)
        {
            var gold = new Color(1f, 0.82f, 0.3f);
            var ring = fx.Get(BattleFx.Shape.Ring);
            ring.Set(gold, Color.white, 1f, 1.4f);
            var cam = Camera.main;
            fx.Burst(world, 12, gold, World(0.06f), 0.45f, World(0.003f));
            // Real time: it flashes during the hit-stop.
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                float p = t / 0.4f;
                ring.transform.position = world;
                if (cam != null) ring.transform.rotation = Quaternion.FromToRotation(Vector3.up, cam.transform.position - world);
                ring.transform.localScale = Vector3.one * (TileSize * Zoom * (0.2f + 0.6f * Mathf.Sqrt(p)));
                ring.SetAlpha(1f - p * p);
                yield return null;
            }
            fx.Release(ring);
        }

        /// <summary>A catch: the pod clicks shut (a tight white ring, the pod's return click), then three gold stars pop up in an arc.</summary>
        IEnumerator CatchStars(Vector3 world)
        {
            var cam = Camera.main;
            var snap = fx.Get(BattleFx.Shape.Ring);
            snap.Set(Color.white, Color.white, 1f, 1.6f);
            GameAudio.At(AudioIds.PodReturn, world);
            yield return Tween(0.15f, p =>
            {
                snap.transform.position = world;
                if (cam != null) snap.transform.rotation = Quaternion.FromToRotation(Vector3.up, cam.transform.position - world);
                snap.transform.localScale = Vector3.one * (TileSize * Zoom * (0.22f - 0.12f * p));
                snap.SetAlpha(1f - p);
            });
            fx.Release(snap);
            var gold = new Color(1f, 0.85f, 0.3f);
            var stars = new BattleFx.Item[3];
            Vector3 up = cam != null ? cam.transform.up : Vector3.up, right = cam != null ? cam.transform.right : Vector3.right;
            for (int i = 0; i < 3; i++)
            {
                stars[i] = fx.Get(BattleFx.Shape.Orb);
                stars[i].Set(Color.white, gold, 1f, 1.4f, 1f, 0.1f);
            }
            yield return Tween(0.65f, p =>
            {
                for (int i = 0; i < 3; i++)
                {
                    float lag = Mathf.Clamp01(p * 1.3f - i * 0.12f);
                    float s = Mathf.Sin(lag * Mathf.PI);
                    Vector3 dir = up * 1f + right * ((i - 1) * 0.9f);
                    stars[i].transform.position = world + dir * World(0.045f) * EaseOutBack(Mathf.Min(1f, lag * 1.6f));
                    stars[i].transform.localScale = Vector3.one * World(0.007f) * s;
                    stars[i].SetAlpha(s);
                    if (lag > 0f && lag < 0.1f) fx.Burst(stars[i].transform.position, 2, gold, World(0.02f), 0.4f, World(0.002f));
                }
            });
            foreach (var s in stars) fx.Release(s);
        }

        // ---- turns ----

        /// <summary>Animates one turn's events in order. <paramref name="caption"/> receives the battle text lines.</summary>
        public IEnumerator Play(IReadOnlyList<BattleEvent> events, BattleSystem battle, Action<string> caption)
        {
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                var self = Actor(e.side);
                string who = battle.Get(e.side).isling.Name;
                switch (e.kind)
                {
                    case BattleEventKind.MoveUsed:
                        caption($"{(e.side == BattleSide.Wild ? "Wild " : "")}{who} used {e.move.Name}!");
                        // The move's outcome is the next event; it plays at the moment of impact.
                        bool hasOutcome = i + 1 < events.Count && IsOutcome(events[i + 1].kind);
                        var outcome = hasOutcome ? events[i + 1] : default;
                        yield return PlayMove(e.side, e.move, hasOutcome ? (BattleEvent?)outcome : null, battle);
                        if (hasOutcome) i++;
                        yield return Wait(pauseBetweenMoves);
                        break;
                    case BattleEventKind.Fainted:
                        bool dazed = e.side == BattleSide.Wild && battle.WildDazed;
                        caption(dazed ? $"Wild {who} is dazed!" : $"{(e.side == BattleSide.Wild ? "Wild " : "")}{who} fainted!");
                        if (self != null)
                        {
                            fx.Burst(self.WorldCenter, 20, Smoke, World(0.03f), 0.7f, World(0.005f), Vector3.up * World(0.02f));
                            GameAudio.At(AudioIds.Faint, self.WorldCenter);
                            Cry(self, e.side == BattleSide.Player ? _playerCry : _wildCry, 0.8f);
                            DropShieldOf(e.side, true);
                            if (dazed)
                            {
                                self.View.Daze();
                                StartCoroutine(DizzyStars(self));
                            }
                            else if (PolishConfig.BattleFeelOn) yield return FaintSpin(self);
                            else self.View.Faint();
                            // Step 16: the winner does a happy hop.
                            var winner = Actor(e.side == BattleSide.Player ? BattleSide.Wild : BattleSide.Player);
                            if (winner != null && PolishConfig.BattleFeelOn && !(winner == _wild && _wildKeeper)) winner.View.Hop();
                        }
                        yield return Wait(dazed || !PolishConfig.BattleFeelOn ? 0.8f : 0.5f);
                        break;
                    case BattleEventKind.FleeFailed:
                        caption("Couldn't get away!");
                        if (_player != null) _player.View.Hop();
                        yield return Wait(0.7f);
                        break;
                    case BattleEventKind.Fled:
                        caption("Got away safely!");
                        yield return Wait(0.5f);
                        break;
                    case BattleEventKind.ShieldBroken:
                        caption($"{who}'s crown shield shattered!");
                        Presented?.Invoke(e);
                        if (self != null) hud.PopText(self.LocalTop + Vector3.up * 0.06f, "Shield down!", SuperColor, 0.8f, 1.4f);
                        yield return Wait(0.9f);
                        break;
                    case BattleEventKind.ShieldRaised:
                        // A standalone raise (not a move outcome): just the look.
                        Presented?.Invoke(e);
                        yield return Wait(0.4f);
                        break;
                    case BattleEventKind.KeeperPhase2:
                        caption($"{who}'s crown burns brighter!");
                        Presented?.Invoke(e);
                        yield return Wait(0.8f);
                        break;
                }
            }
        }

        /// <summary>Step 16: a little spin and flop with sparkles, then the faint pose (eyes shut, tipped over).</summary>
        IEnumerator FaintSpin(StageActor a)
        {
            float yaw0 = a.yaw;
            yield return Tween(0.4f, p =>
            {
                a.yaw = yaw0 + 360f * EaseInOut(p);
                a.lift = Mathf.Sin(p * Mathf.PI) * a.Height * 0.25f;
                if (Time.frameCount % 2 == 0) fx.Burst(a.WorldCenter, 2, Gold, World(0.02f), 0.5f, World(0.003f), Vector3.up * World(0.02f));
            });
            a.yaw = yaw0;
            a.lift = 0f;
            a.View.Faint();
            fx.Burst(a.WorldCenter, 18, Gold, World(0.04f), 0.7f, World(0.0035f), Vector3.up * World(0.03f));
            yield return Tween(0.2f, p => a.squash = new Vector3(1f + 0.15f * Mathf.Sin(p * Mathf.PI), 1f - 0.15f * Mathf.Sin(p * Mathf.PI), 1f + 0.15f * Mathf.Sin(p * Mathf.PI)));
            a.squash = Vector3.one;
        }

        /// <summary>Three little gold stars circling a dazed creature's head while it stays dazed.</summary>
        IEnumerator DizzyStars(StageActor a)
        {
            float t = 0f;
            var loop = AudioService.PlayLoop(AudioIds.CatchDazed, a.transform, 0.3f);
            while (a != null && a.View != null && a.View.IsDazed && a.grow > 0.5f)
            {
                t += Time.deltaTime;
                if (Time.frameCount % 3 == 0)
                {
                    Vector3 top = island.transform.TransformPoint(a.LocalTop);
                    for (int i = 0; i < 3; i++)
                    {
                        float ang = t * 4f + i * Mathf.PI * 2f / 3f;
                        Vector3 off = (island.transform.right * Mathf.Cos(ang) + island.transform.forward * Mathf.Sin(ang)) * World(0.012f);
                        fx.Burst(top + off, 1, Gold, World(0.002f), 0.25f, World(0.0035f));
                    }
                }
                yield return null;
            }
            loop.Stop(0.3f);
        }

        /// <summary>No pod reached the dazed wild creature in time: it fades out (the outro then has nothing left to do for it).</summary>
        public IEnumerator FadeWild()
        {
            var a = _wild;
            if (a == null) yield break;
            fx.Burst(a.WorldCenter, 18, new Color(1f, 0.88f, 0.54f), World(0.04f), 0.5f, World(0.004f));
            yield return Tween(0.5f, p => a.grow = 1f - EaseInOut(p));
            RemoveWild();
        }

        static bool IsOutcome(BattleEventKind k) =>
            k == BattleEventKind.Damage || k == BattleEventKind.Missed || k == BattleEventKind.Shielded ||
            k == BattleEventKind.AttackRaised || k == BattleEventKind.Healed || k == BattleEventKind.ShieldRaised;

        StageActor Actor(BattleSide side) => side == BattleSide.Player ? _player : _wild;

        IEnumerator PlayMove(BattleSide side, MoveData move, BattleEvent? outcome, BattleSystem battle)
        {
            var user = Actor(side);
            var target = Actor(side == BattleSide.Player ? BattleSide.Wild : BattleSide.Player);
            if (user == null || target == null) yield break;
            var style = ElementStyle.Of(move.element);
            var userStyle = ElementStyle.Of(battle.Get(side).isling.Element);
            Action impact = () => { if (outcome.HasValue) Impact(outcome.Value, user, target, userStyle); };
            int who = side == BattleSide.Player ? 0 : 1;
            _acting[who] = true;
            float started = Time.time;
            try
            {
                bool feel = PolishConfig.BattleFeelOn;
                // Step 16: a Keeper's signature flourish, then every move winds up (crouch, glow, charge sound).
                if (feel && side == BattleSide.Wild && _wildKeeper) yield return Flourish(user, target, battle.Get(side).isling.Element, userStyle);
                // A slam winds up shorter (its leap is a wind-up of its own): it stays within +25% of the old pace.
                if (feel) yield return WindUp(user, move.kind == MoveKind.Support ? userStyle : style,
                    move.kind != MoveKind.Support && move.animation == MoveAnimation.Slam ? windUpSeconds * 0.5f : windUpSeconds);
                else GameAudio.At(AudioIds.MoveCharge, user.WorldCenter);

                if (move.kind == MoveKind.Support)
                {
                    yield return Support(user, move, userStyle, impact);
                    yield break;
                }
                switch (move.animation)
                {
                    case MoveAnimation.Lunge: yield return Lunge(user, target, impact); break;
                    case MoveAnimation.Slam: yield return Slam(user, target, style, impact); break;
                    default: yield return Shoot(user, target, move, style, impact); break;
                }
            }
            finally
            {
                _acting[who] = false;
                LastMoveSeconds = Time.time - started;
                Debug.Log($"[BattleStage] {move.Name} ({(move.kind == MoveKind.Support ? "support" : move.animation.ToString().ToLowerInvariant())}): {LastMoveSeconds:0.00} s (+ {pauseBetweenMoves:0.00} s pause).");
            }
        }

        /// <summary>Step 16: seconds the last move took (wind-up to recover, without the pause after it).</summary>
        public static float LastMoveSeconds { get; private set; }

        readonly bool[] _acting = new bool[2];
        readonly float[] _glanceAt = { 2f, 3.4f }, _glanceStart = { -9f, -9f };

        /// <summary>Step 16: crouch and squash, the element's glow building on the body, motes drawn in, a charge sound.</summary>
        IEnumerator WindUp(StageActor user, ElementStyle style, float seconds)
        {
            GameAudio.At(AudioIds.MoveCharge, user.WorldCenter);
            yield return Tween(seconds, p =>
            {
                float k = EaseInOut(p);
                user.squash = new Vector3(1f + 0.14f * k, 1f - 0.16f * k, 1f + 0.14f * k);
                user.View.SetFlash(0.3f * k);
                if (Time.frameCount % 2 == 0)
                {
                    Vector3 c = user.WorldCenter, r = UnityEngine.Random.onUnitSphere * World(0.035f);
                    fx.Burst(c + r, 1, style.edge, World(0.003f), 0.22f, World(0.003f), -r * 4f);
                }
            });
            user.View.SetFlash(0f);
        }

        /// <summary>
        /// Step 16: a Keeper's signature before its move: Hearthstag's crown flares (fire), Galeowl's wing gust sweeps
        /// the arena (wind), Tidemother's wave splashes across it (water).
        /// </summary>
        IEnumerator Flourish(StageActor user, StageActor target, Element element, ElementStyle style)
        {
            Vector3 top = island.transform.TransformPoint(user.LocalTop);
            Vector3 up = island.transform.up;
            switch (element)
            {
                case Element.Fire:
                    GameAudio.At(AudioIds.MoveCharge, top);
                    yield return Tween(0.3f, p =>
                    {
                        user.View.SetFlash(0.45f * Mathf.Sin(p * Mathf.PI));
                        if (Time.frameCount % 2 == 0)
                        {
                            fx.Burst(top, 4, new Color(1f, 0.55f, 0.15f), World(0.03f), 0.6f, World(0.004f), up * World(0.08f));
                            fx.Burst(top, 2, new Color(1f, 0.92f, 0.5f), World(0.04f), 0.4f, World(0.003f), up * World(0.05f));
                        }
                    });
                    StartCoroutine(Pop(top, style.burst));
                    break;
                case Element.Wind:
                {
                    var ring = fx.Get(BattleFx.Shape.Ring);
                    ring.Set(style.edge, style.edge, 1f);
                    float dist = (target.tile - user.tile).magnitude + 1f;
                    Vector2 dir = (target.tile - user.tile).normalized;
                    yield return Tween(0.4f, p =>
                    {
                        float r = 0.2f + dist * 1.2f * EaseInOut(p);
                        ring.transform.localPosition = island.TileToLocal(user.tile, Cap + 0.003f);
                        ring.transform.localScale = Vector3.one * (r * TileSize * Zoom);
                        ring.SetAlpha(0.9f * (1f - p));
                        // The gust's leaves and grass bits sweep across the pads.
                        Vector2 at = Vector2.Lerp(user.tile, target.tile, p) + new Vector2(-dir.y, dir.x) * UnityEngine.Random.Range(-1f, 1f);
                        fx.Burst(WorldAt(at, 0.006f), 2, new Color(0.6f, 0.85f, 0.45f), World(0.01f), 0.5f, World(0.003f),
                            island.transform.TransformDirection(new Vector3(dir.x, 0f, dir.y)) * World(0.25f) + up * World(0.03f));
                    });
                    fx.Release(ring);
                    if (_playerPad != null) StartCoroutine(ShakePad(_playerPad, 0.2f, 0.0015f));
                    break;
                }
                case Element.Water:
                {
                    var ring = fx.Get(BattleFx.Shape.Ring);
                    ring.Set(style.edge, style.core, 1f);
                    yield return Tween(0.45f, p =>
                    {
                        Vector2 at = Vector2.Lerp(user.tile, target.tile, EaseInOut(p));
                        ring.transform.localPosition = island.TileToLocal(at, Cap + 0.004f);
                        ring.transform.localScale = Vector3.one * (TileSize * Zoom * (0.6f + 0.6f * Mathf.Sin(p * Mathf.PI)));
                        ring.SetAlpha(0.85f);
                        if (Time.frameCount % 2 == 0)
                            fx.Burst(WorldAt(at, 0.01f), 4, new Color(0.6f, 0.88f, 1f), World(0.04f), 0.5f, World(0.003f), up * World(0.06f), World(0.3f));
                    });
                    fx.Release(ring);
                    StartCoroutine(Splash(island.transform.TransformPoint(island.TileToLocal(target.tile, Cap)), new Color(0.55f, 0.85f, 1f)));
                    break;
                }
            }
            user.View.SetFlash(0f);
        }

        IEnumerator ShakePad(BattlePad pad, float seconds, float amount)
        {
            for (float t = 0f; t < seconds && pad != null; t += Time.deltaTime)
            {
                float k = 1f - t / seconds;
                pad.shake = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)) * (amount * k);
                yield return null;
            }
            if (pad != null) pad.shake = Vector3.zero;
        }

        /// <summary>Step 16: alive on the pad: a bob, a little sway, a glance (eyes off the player, a small turn) and a blink every few seconds.</summary>
        void Idle()
        {
            for (int i = 0; i < 2; i++)
            {
                var a = i == 0 ? _player : _wild;
                if (a == null || a.View == null) continue;
                bool keeper = i == 1 && _wildKeeper;
                bool still = PolishConfig.BattleFeelOn && !keeper && !_acting[i] && a.grow > 0.95f && !a.View.IsFainted && !a.View.IsDazed;
                if (!still)
                {
                    a.idleLift = Mathf.MoveTowards(a.idleLift, 0f, Time.deltaTime * 0.05f);
                    a.idleRoll = Mathf.MoveTowards(a.idleRoll, 0f, Time.deltaTime * 30f);
                    a.idleYaw = Mathf.MoveTowards(a.idleYaw, 0f, Time.deltaTime * 60f);
                    continue;
                }
                float t = Time.time + i * 1.7f;
                a.idleLift = a.Height * idleBob * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f * 0.9f));
                a.idleRoll = Mathf.Sin(t * Mathf.PI * 2f * 0.45f) * idleSway;
                if (Time.time >= _glanceAt[i])
                {
                    _glanceStart[i] = Time.time;
                    _glanceAt[i] = Time.time + 3f + UnityEngine.Random.value * 3f;
                    a.View.BlinkNow();
                }
                float g = (Time.time - _glanceStart[i]) / 0.9f;
                bool glancing = g >= 0f && g <= 1f;
                a.View.lookAtPlayer = !glancing;
                a.idleYaw = glancing ? Mathf.Sin(g * Mathf.PI) * glanceYaw * (i == 0 ? 1f : -1f) : 0f;
            }
        }

        void Impact(BattleEvent o, StageActor user, StageActor target, ElementStyle userStyle)
        {
            Presented?.Invoke(o);
            switch (o.kind)
            {
                case BattleEventKind.ShieldRaised:
                    hud.PopText(user.LocalTop + Vector3.up * 0.06f, "Shield up!", new Color(1f, 0.7f, 0.3f), 0.8f, 1.2f);
                    break;
                case BattleEventKind.Damage:
                    Hit(target, user, o);
                    break;
                case BattleEventKind.Missed:
                    hud.PopText(target.LocalTop + Vector3.up * 0.03f, "Miss", WeakColor);
                    break;
                case BattleEventKind.Shielded:
                    RaiseShield(user, userStyle);
                    break;
                case BattleEventKind.AttackRaised:
                    hud.PopText(user.LocalTop + Vector3.up * 0.03f, "Attack up!", new Color(1f, 0.6f, 0.35f), 0.8f);
                    break;
                case BattleEventKind.Healed:
                    hud.PopText(user.LocalTop + Vector3.up * 0.03f, $"+{o.amount}", HealColor);
                    hud.SetHp(o.side == BattleSide.Player ? _playerBar : _wildBar, o.hpAfter);
                    break;
            }
        }

        void Hit(StageActor target, StageActor user, BattleEvent o)
        {
            var bar = o.side == BattleSide.Player ? _playerBar : _wildBar;
            hud.SetHp(bar, o.hpAfter);
            GameAudio.At(o.effectiveness == Effectiveness.Super ? AudioIds.HitSuper : o.effectiveness == Effectiveness.NotVery || o.blocked ? AudioIds.HitWeak : AudioIds.HitNormal,
                target.WorldCenter);
            // Step 16: the number in the move's element colour, a bit bigger.
            hud.PopText(target.LocalTop + Vector3.up * 0.042f, $"-{o.amount}", DamageColor(o.move != null ? o.move.element : Element.Neutral), 1.2f);
            if (o.blocked) hud.PopText(target.LocalTop + Vector3.up * 0.075f, "Blocked!", new Color(1f, 0.7f, 0.3f), 0.75f, 1.4f);
            else if (o.effectiveness == Effectiveness.Super) hud.PopText(target.LocalTop + Vector3.up * 0.075f, "Super effective!", SuperColor, 0.65f, 1.4f);
            else if (o.effectiveness == Effectiveness.NotVery) hud.PopText(target.LocalTop + Vector3.up * 0.075f, "Not very effective", WeakColor, 0.65f, 1.4f);
            if (PolishConfig.BattleFeelOn)
            {
                ElementHit(target, o.move != null ? o.move.element : Element.Neutral);
                // Step 16: every hit holds the stage a moment (hit-stop); super effective a little longer, with the gold flash.
                bool super = o.effectiveness == Effectiveness.Super && !o.blocked;
                _hitStopUntil = Time.time + (super ? Mathf.Max(hitStopSeconds, PolishConfig.Get().hitStop) : hitStopSeconds);
                if (super) StartCoroutine(GoldFlash(target.WorldCenter));
            }
            if (o.shieldAbsorbed) DropShieldOf(o.side, true);
            StartCoroutine(Knockback(target, (target.tile - user.tile).normalized));
        }

        /// <summary>Step 16: damage numbers in the element's colour (neutral stays white).</summary>
        public static Color DamageColor(Element e) => e switch
        {
            Element.Fire => new Color(1f, 0.55f, 0.2f),
            Element.Water => new Color(0.45f, 0.78f, 1f),
            Element.Wind => new Color(0.5f, 0.92f, 0.7f),
            _ => Color.white,
        };

        IEnumerator Knockback(StageActor target, Vector2 dir)
        {
            float seed = UnityEngine.Random.value * 10f;
            yield return Tween(0.36f, p =>
            {
                // Step 16: a full white flash for the first 0.1 s, then it fades.
                target.View.SetFlash(p < 0.28f ? 1f : (1f - p) * 0.85f);
                float k = Mathf.Sin(p * Mathf.PI) * (1f - p) * 0.35f;
                Vector2 side = new Vector2(-dir.y, dir.x) * (Mathf.Sin(p * 45f + seed) * 0.04f * (1f - p));
                target.nudge = dir * k + side;
            });
            target.View.SetFlash(0f);
            target.nudge = Vector2.zero;
        }

        // ---- templates ----

        IEnumerator Shoot(StageActor user, StageActor target, MoveData move, ElementStyle style, Action impact)
        {
            bool feel = PolishConfig.BattleFeelOn;
            if (!feel)
            {
                GameAudio.At(AudioIds.MoveCharge, user.WorldCenter);
                yield return Tween(0.26f, p => user.squash = new Vector3(1f + 0.12f * p, 1f - 0.12f * p, 1f + 0.12f * p));
            }
            else yield return Tween(0.06f, p => user.squash = Vector3.Lerp(new Vector3(1.14f, 0.84f, 1.14f), new Vector3(0.92f, 1.12f, 0.92f), p));
            user.squash = Vector3.one;
            GameAudio.At(ElementCue("move.shoot.", move.element), user.WorldCenter);
            Vector2 dir = (target.tile - user.tile).normalized;
            Vector2 from = user.tile + dir * 0.35f, to = target.tile;
            float h0 = user.CenterHeight, h1 = target.CenterHeight;
            var orb = fx.Get(BattleFx.Shape.Orb);
            orb.Set(style.core, style.edge, 1f, 1f, 1f, move.element == Element.Water ? 0.12f : 0.35f);
            // Step 16: bigger and readable, ~0.35 s with a slight arc; each element moves its own way.
            float radius = feel ? shotRadius : 0.007f; // map metres
            float flight = feel ? shotSeconds : 0.42f;
            var element = move.element;
            float spin = 0f;
            yield return Tween(flight, p =>
            {
                Vector2 t = Vector2.Lerp(from, to, p);
                float arc = move.arc ? 0.03f : feel ? 0.015f : 0f;
                float h = Mathf.Lerp(h0, h1, p) + Mathf.Sin(p * Mathf.PI) * arc;
                orb.transform.localPosition = island.TileToLocal(t, Cap + h);
                Vector3 size = Vector3.one * (radius * Zoom * (1f + Mathf.Sin(Time.time * 30f) * 0.08f));
                Vector3 world = island.transform.TransformPoint(orb.transform.localPosition);
                if (feel && element == Element.Water)
                {
                    // A wobbling bubble.
                    float w = Mathf.Sin(Time.time * 22f) * 0.16f;
                    size = Vector3.Scale(size, new Vector3(1f + w, 1f - w, 1f + w));
                }
                orb.transform.localScale = size;
                if (!feel)
                {
                    fx.Burst(world, 2, style.trail, World(0.012f), 0.3f, World(0.003f));
                    return;
                }
                switch (element)
                {
                    case Element.Fire:
                        // An ember ball with a rising trail.
                        fx.Burst(world, 3, style.trail, World(0.012f), 0.4f, World(0.004f), Vector3.up * World(0.03f));
                        break;
                    case Element.Wind:
                    {
                        // A spinning crescent: two motes circling the orb.
                        spin += Time.deltaTime * 25f;
                        Vector3 r = island.transform.rotation * new Vector3(Mathf.Cos(spin), Mathf.Sin(spin), 0f) * World(radius * 1.6f);
                        fx.Burst(world + r, 1, style.core, World(0.002f), 0.25f, World(0.003f));
                        fx.Burst(world - r, 1, style.edge, World(0.002f), 0.25f, World(0.003f));
                        break;
                    }
                    case Element.Water:
                        fx.Burst(world, 2, style.trail, World(0.01f), 0.35f, World(0.0035f), Vector3.zero, World(0.2f));
                        break;
                    default:
                        // A star pellet: sparkles.
                        fx.Burst(world, 2, new Color(1f, 0.95f, 0.75f), World(0.02f), 0.3f, World(0.003f));
                        break;
                }
            });
            fx.Release(orb);
            Vector3 hitAt = target.WorldCenter;
            GameAudio.At(ElementCue("move.impact.", move.element), hitAt);
            fx.Burst(hitAt, feel ? 51 : 34, style.burst, World(feel ? 0.12f : 0.08f), 0.5f, World(0.004f), Vector3.up * World(0.02f));
            StartCoroutine(Pop(hitAt, style.burst));
            impact();
            yield return Wait(0.25f);
        }

        IEnumerator Lunge(StageActor user, StageActor target, Action impact)
        {
            Vector2 d = target.tile - user.tile;
            bool feel = PolishConfig.BattleFeelOn;
            if (!feel) yield return Tween(0.14f, p => user.squash = new Vector3(1f + 0.12f * p, 1f - 0.12f * p, 1f + 0.12f * p));
            GameAudio.At(AudioIds.MoveLunge, user.WorldCenter);
            float hop = feel ? user.Height * lungeHop : 0f;
            yield return Tween(0.15f, p =>
            {
                float q = EaseInOut(p);
                user.nudge = d * (0.6f * q);
                // Step 16: a real hop toward the target, stretched along the way.
                user.lift = Mathf.Sin(q * Mathf.PI * 0.85f) * hop;
                user.squash = new Vector3(1.12f - 0.2f * q, 0.88f + 0.2f * q, 1.12f - 0.2f * q);
            });
            // Contact: squashed against it.
            user.squash = feel ? new Vector3(1.15f, 0.85f, 1.15f) : Vector3.one;
            fx.Burst(target.WorldCenter, feel ? 24 : 16, Color.white, World(feel ? 0.1f : 0.07f), 0.3f, World(0.0035f));
            impact();
            // Bounce back with a little hop.
            yield return Tween(feel ? 0.24f : 0.26f, p =>
            {
                float q = EaseInOut(p);
                user.nudge = d * (0.6f * (1f - q));
                user.lift = Mathf.Sin(q * Mathf.PI) * hop * 0.4f;
                user.squash = Vector3.Lerp(user.squash, Vector3.one, p);
            });
            user.nudge = Vector2.zero;
            user.lift = 0f;
            user.squash = Vector3.one;
        }

        IEnumerator Slam(StageActor user, StageActor target, ElementStyle style, Action impact)
        {
            float top = user.Height * 0.8f;
            yield return Tween(0.26f, p => user.lift = Mathf.Sin(p * Mathf.PI * 0.5f) * top);
            yield return Tween(0.11f, p => user.lift = top * (1f - p * p));
            user.lift = 0f;
            StartCoroutine(Tween(0.22f, p =>
            {
                float k = Mathf.Sin(p * Mathf.PI) * 0.25f;
                user.squash = new Vector3(1f + k, 1f - k, 1f + k);
            }, () => user.squash = Vector3.one));
            GameAudio.At(AudioIds.MoveSlam, WorldAt(user.tile, 0.004f));
            fx.Burst(WorldAt(user.tile, 0.004f), 24, style.burst, World(0.06f), 0.45f, World(0.004f), Vector3.up * World(0.03f));
            if (PolishConfig.BattleFeelOn)
            {
                // Step 16: dust and the pads shake.
                fx.Burst(WorldAt(user.tile, 0.004f), 18, new Color(0.72f, 0.66f, 0.58f), World(0.05f), 0.7f, World(0.005f), Vector3.up * World(0.01f));
                if (_playerPad != null) StartCoroutine(ShakePad(_playerPad, 0.25f, 0.002f));
                if (_wildPad != null) StartCoroutine(ShakePad(_wildPad, 0.25f, 0.002f));
            }
            var ring = fx.Get(BattleFx.Shape.Ring);
            ring.Set(style.edge, style.edge, 1f);
            float dist = (target.tile - user.tile).magnitude;
            bool done = false;
            yield return Tween(0.42f, p =>
            {
                float r = 0.1f + dist * 1.2f * EaseInOut(p); // tiles
                ring.transform.localPosition = island.TileToLocal(user.tile, Cap + 0.002f);
                ring.transform.localScale = Vector3.one * (r * TileSize * Zoom);
                ring.SetAlpha(1f - p * 0.85f);
                if (!done && r >= dist)
                {
                    done = true;
                    impact();
                }
            });
            if (!done) impact();
            fx.Release(ring);
        }

        IEnumerator Support(StageActor user, MoveData move, ElementStyle style, Action impact)
        {
            switch (move.effect)
            {
                case SupportEffect.Shield:
                    impact(); // raises the bubble
                    fx.Burst(user.WorldCenter, 22, style.shield, World(0.04f), 0.45f, World(0.004f));
                    yield return Wait(PolishConfig.BattleFeelOn ? 0.45f : 0.5f);
                    break;
                case SupportEffect.RaiseAttack:
                    yield return Tween(0.4f, p =>
                    {
                        float k = Mathf.Sin(p * Mathf.PI);
                        user.squash = new Vector3(1f + 0.1f * k, 1f + 0.12f * k, 1f + 0.1f * k);
                        user.View.SetFlash(0.35f * k);
                        if (Time.frameCount % 2 == 0)
                            fx.Burst(WorldAt(user.tile, 0.004f), 3, new Color(1f, 0.55f, 0.3f), World(0.015f), 0.6f, World(0.004f), Vector3.up * World(0.06f));
                    });
                    user.squash = Vector3.one;
                    user.View.SetFlash(0f);
                    impact();
                    yield return Wait(0.3f);
                    break;
                case SupportEffect.Heal:
                    yield return Tween(0.5f, p =>
                    {
                        if (Time.frameCount % 2 == 0)
                            fx.Burst(WorldAt(user.tile + UnityEngine.Random.insideUnitCircle * 0.3f, 0.004f), 2, HealColor, World(0.01f), 0.7f, World(0.004f), Vector3.up * World(0.05f));
                    });
                    impact();
                    yield return Wait(0.3f);
                    break;
                default:
                    impact();
                    break;
            }
        }

        void RaiseShield(StageActor user, ElementStyle style)
        {
            bool isPlayer = user == _player;
            var shield = (isPlayer ? _playerShield : _wildShield) ?? fx.Get(BattleFx.Shape.Orb);
            if (isPlayer) _playerShield = shield;
            else _wildShield = shield;
            shield.Set(style.shield, style.shield, 1f, 2.2f, 0.05f, 0f, 0.06f);
            GameAudio.At(AudioIds.MoveShieldUp, user.WorldCenter);
            StartCoroutine(GrowShield(shield, user));
        }

        IEnumerator GrowShield(BattleFx.Item shield, StageActor user)
        {
            yield return Tween(0.32f, p => PlaceShield(shield, user, 0.2f + 0.8f * EaseInOut(p)));
        }

        void PlaceShield(BattleFx.Item shield, StageActor user, float size)
        {
            if (shield == null || shield.transform == null || !shield.transform.gameObject.activeSelf || user == null) return;
            shield.transform.localPosition = user.LocalCenter;
            shield.transform.localScale = Vector3.one * (user.Height * 0.72f * Zoom * size);
            // Step 14j: its label clears the shield.
            if (shield.renderer != null && !user.attached.Contains(shield.renderer)) user.attached.Add(shield.renderer);
        }

        void DropShieldOf(BattleSide side, bool pop)
        {
            if (side == BattleSide.Player) DropShield(ref _playerShield, pop);
            else DropShield(ref _wildShield, pop);
        }

        void DropShield(ref BattleFx.Item slot, bool pop)
        {
            if (slot == null) return;
            var item = slot;
            slot = null;
            if (_player != null) _player.attached.Remove(item.renderer);
            if (_wild != null) _wild.attached.Remove(item.renderer);
            if (pop && isActiveAndEnabled) StartCoroutine(PopShield(item));
            else fx.Release(item);
        }

        IEnumerator PopShield(BattleFx.Item item)
        {
            Vector3 s0 = item.transform.localScale;
            GameAudio.At(AudioIds.MoveShieldPop, item.transform.position);
            fx.Burst(item.transform.position, 26, Color.white, World(0.07f), 0.4f, World(0.004f));
            yield return Tween(0.3f, p =>
            {
                item.transform.localScale = s0 * (1f + p * 0.35f);
                item.SetAlpha(1f - p);
            });
            fx.Release(item);
        }

        IEnumerator Pop(Vector3 world, Color color)
        {
            var ring = fx.Get(BattleFx.Shape.Ring);
            ring.Set(color, color, 1f);
            var cam = Camera.main;
            yield return Tween(0.3f, p =>
            {
                ring.transform.position = world;
                // The quad lies in XZ; turn its up axis toward the camera.
                if (cam != null) ring.transform.rotation = Quaternion.FromToRotation(Vector3.up, cam.transform.position - world);
                ring.transform.localScale = Vector3.one * (TileSize * Zoom * (0.05f + 0.25f * p));
                ring.SetAlpha(1f - p);
            });
            fx.Release(ring);
        }

        float _refitAt;

        void LateUpdate()
        {
            LowHp();
            Idle();
            // Step 14j: re-fit while both stand on their pads (the battle zoom and the head move after the spawn fit).
            if (_player != null && _wild != null && _player.grow > 0.9f && _wild.grow > 0.9f && Time.time >= _refitAt)
            {
                _refitAt = Time.time + 0.3f;
                FitPads(false);
            }
            if (_playerShield != null && _player != null) PlaceShieldIdle(_playerShield, _player);
            if (_wildShield != null && _wild != null) PlaceShieldIdle(_wildShield, _wild);
        }

        // After it has grown, the bubble just follows its creature.
        void PlaceShieldIdle(BattleFx.Item shield, StageActor user)
        {
            shield.transform.localPosition = user.LocalCenter;
            float target = user.Height * 0.72f * Zoom;
            // Step 16: the bubble pulses softly while it stands.
            if (PolishConfig.BattleFeelOn) target *= 1f + 0.04f * Mathf.Sin(Time.time * 4f);
            if (shield.transform.localScale.x > target * 0.9f) shield.transform.localScale = Vector3.one * target;
        }

        // ---- growth: XP, level-ups, evolution ----

        /// <summary>
        /// On the pad after a win: "+21 XP" floats up and the XP bar fills; each level-up (about 0.4 s each) fills it
        /// to the end, bursts a golden ring at the creature's feet, pops "Lv 6!", hops, and raises the HP bar.
        /// <paramref name="startShare"/> is the bar before the XP; the creature already holds the result.
        /// </summary>
        public IEnumerator PlayXp(IslingInstance c, int amount, float startShare, IReadOnlyList<LevelUp> ups)
        {
            var a = _player;
            if (a == null || _playerBar == null || c == null) yield break;
            // Step 9f: a big "+34 XP", the XP bar at twice its size filling over about a second, a banner per level.
            hud.PopText(a.LocalTop + Vector3.up * 0.05f, $"+{amount} XP", Gold, 1.9f, 1.8f);
            float share = startShare;
            int hp = c.currentHp - SumHp(ups);
            hud.SetXp(_playerBar, share);
            yield return Tween(0.2f, p => hud.SetXpScale(_playerBar, Mathf.Lerp(1f, XpBarLarge, EaseInOut(p))));
            float fill = Mathf.Max(0.35f, 1f / (ups.Count + 1));
            _xpStep = 0;
            for (int i = 0; i < ups.Count; i++)
            {
                float from = share;
                StartCoroutine(XpTicks(a, fill));
                yield return Tween(fill, p => hud.SetXp(_playerBar, Mathf.Lerp(from, 1f, EaseInOut(p))));
                var up = ups[i];
                hp = c.IsFainted ? 0 : hp + up.hpGain;
                int max = MaxHpAt(c, up.level);
                hud.SetLevel(_playerBar, c.Name, up.level, hp, max);
                LevelBurst(a, up.level);
                share = 0f;
                hud.SetXp(_playerBar, 0f);
                yield return Wait(0.15f);
            }
            float to = progression != null ? progression.XpShare(c) : 1f;
            float start = share;
            StartCoroutine(XpTicks(a, fill));
            yield return Tween(fill, p => hud.SetXp(_playerBar, Mathf.Lerp(start, to, EaseInOut(p))));
            yield return Wait(ups.Count > 0 ? 0.6f : 0.5f);
            yield return Tween(0.2f, p => hud.SetXpScale(_playerBar, Mathf.Lerp(XpBarLarge, 1f, EaseInOut(p))));
        }

        /// <summary>Step 9f: how much bigger the pad's XP bar gets while the XP after a win fills.</summary>
        const float XpBarLarge = 2f;

        /// <summary>Step 9f: "+34" in gold rising from a world point (a benched Isling's pod on the rim).</summary>
        public void PopXpAt(Vector3 world, int amount) =>
            hud.PopText(hud.transform.InverseTransformPoint(world) + Vector3.up * 0.012f, $"+{amount}", Gold, 1.2f, 1.8f);

        int _xpStep;

        /// <summary>Audio hookup: rising ticks while the XP bar fills (one step higher each tick, across level-ups).</summary>
        IEnumerator XpTicks(StageActor a, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.07f)
            {
                if (a == null) yield break;
                GameAudio.At(AudioIds.XpTick, a.WorldCenter, Mathf.Min(_xpStep++, 24));
                yield return Wait(0.07f);
            }
        }

        /// <summary>move.shoot.fire etc.; neutral when the element has no cue.</summary>
        static string ElementCue(string prefix, Element e)
        {
            var id = prefix + e.ToString().ToLowerInvariant();
            return AudioService.Has(id) ? id : prefix + "neutral";
        }

        static int SumHp(IReadOnlyList<LevelUp> ups)
        {
            int s = 0;
            foreach (var u in ups) s += u.hpGain;
            return s;
        }

        static int MaxHpAt(IslingInstance c, int level) => BattleMath.MaxHp(c.species != null ? c.species.baseStats.hp : 1, level);

        void LevelBurst(StageActor a, int level)
        {
            var feet = island.TileToLocal(a.tile, Cap + a.baseLift + 0.002f);
            fx.Burst(island.transform.TransformPoint(feet), 22, Gold, World(0.045f), 0.6f, World(0.004f), Vector3.up * World(0.03f));
            StartCoroutine(GoldRing(a));
            GameAudio.Ui(AudioIds.JingleLevelup);
            MusicDirector.Jingle(2f);
            hud.PopText(a.LocalTop + Vector3.up * 0.03f, $"Lv {level}!", Gold, 2.2f, 1.6f);
            a.View.Hop();
        }

        IEnumerator GoldRing(StageActor a)
        {
            var ring = fx.Get(BattleFx.Shape.Ring);
            ring.Set(Color.white, Gold, 1f);
            yield return Tween(0.45f, p =>
            {
                if (a == null) return;
                ring.transform.localPosition = island.TileToLocal(a.tile, Cap + a.baseLift + 0.002f);
                ring.transform.localScale = Vector3.one * (TileSize * Zoom * (0.2f + 0.7f * p));
                ring.SetAlpha(1f - p);
            });
            fx.Release(ring);
        }

        /// <summary>
        /// Evolution on the pad: the creature glows white, then swaps between its old and new body at the same height,
        /// faster and faster (about 2 s), a light burst, and the new form grows to its own size and hops.
        /// Applies <see cref="Progression.Evolve"/> to <paramref name="c"/> (HP ratio, level, XP and nickname kept).
        /// </summary>
        public IEnumerator Evolve(IslingInstance c)
        {
            var old = _player;
            var next = c != null && c.species != null ? c.species.evolvesTo : null;
            if (old == null || next == null || next.Prefab == null)
            {
                if (next != null && next.Prefab == null) Debug.LogError($"[BattleStage] {next.displayName} has no baked prefab; evolving without the show.");
                Progression.Evolve(c);
                yield break;
            }
            string oldName = c.Name;
            var go = Instantiate(next.Prefab, island.transform);
            go.name = $"Player {next.displayName}";
            var nu = go.AddComponent<StageActor>();
            nu.island = island;
            nu.tile = old.tile;
            nu.yaw = old.yaw;
            nu.baseLift = old.baseLift;
            nu.grow = 0f;
            var was = _player;
            _player = nu;
            FitPads(true);
            _player = was;
            nu.Snap();
            float same = old.Height / Mathf.Max(1e-4f, nu.Height);

            var rise = AudioService.PlayLoop(AudioIds.EvoRise, old.transform, 0.3f);
            yield return Tween(0.5f, p => old.View.SetFlash(p));
            nu.View.SetFlash(1f);
            float[] holds = { 0.5f, 0.42f, 0.34f, 0.26f, 0.2f, 0.14f, 0.1f };
            bool showNew = false;
            foreach (float h in holds)
            {
                showNew = !showNew;
                old.grow = showNew ? 0f : 1f;
                nu.grow = showNew ? same : 0f;
                yield return Wait(h);
            }
            // Ends on the new form.
            old.grow = 0f;
            nu.grow = same;
            Vector3 c0 = nu.WorldCenter;
            rise.Stop(0.1f);
            GameAudio.At(AudioIds.EvoBurst, c0);
            GameAudio.Ui(AudioIds.JingleEvolve);
            MusicDirector.Jingle(3f);
            fx.Burst(c0, 34, Color.white, World(0.07f), 0.6f, World(0.005f));
            fx.Burst(c0, 24, Gold, World(0.05f), 0.8f, World(0.004f), Vector3.up * World(0.03f));
            StartCoroutine(GoldRing(nu));
            Progression.Evolve(c);
            Destroy(old.gameObject);
            _player = nu;
            if (_playerBar != null) _playerBar.actor = nu;
            yield return Tween(0.4f, p =>
            {
                nu.grow = Mathf.Lerp(same, 1f, EaseOutBack(p));
                nu.View.SetFlash(1f - p);
            });
            nu.grow = 1f;
            nu.View.SetFlash(0f);
            // Step 14h: the new form's cry.
            _playerCry = AudioIds.Cry(SpeciesRegistry.IdOf(next));
            Cry(nu, _playerCry);
            nu.View.Hop();
            hud.SetLevel(_playerBar, c.Name, c.level, c.currentHp, c.MaxHp);
            hud.PopText(nu.LocalTop + Vector3.up * 0.03f, c.Name + "!", Gold, 1.2f, 1.4f);
            Debug.Log($"[BattleStage] {oldName} evolved into {next.displayName} (Lv {c.level}, HP {c.currentHp}/{c.MaxHp}).");
            yield return Wait(0.8f);
        }

        // ---- endings ----

        /// <summary>Winner hops; both creatures shrink away (the loser has already fainted).</summary>
        public IEnumerator Outro(BattleResult result)
        {
            var winner = result == BattleResult.Won ? _player : result == BattleResult.Lost ? _wild : null;
            if (winner != null)
            {
                winner.View.Hop();
                yield return Wait(0.45f);
                winner.View.Hop();
                yield return Wait(0.6f);
            }
            DropShield(ref _playerShield, true);
            DropShield(ref _wildShield, true);
            var leaving = new List<StageActor>();
            if (_player != null) leaving.Add(_player);
            if (_wild != null) leaving.Add(_wild);
            foreach (var a in leaving)
                fx.Burst(a.WorldCenter, 18, result == BattleResult.Fled && a == _wild ? Leaf : new Color(1f, 0.88f, 0.54f), World(0.04f), 0.5f, World(0.004f));
            yield return Tween(0.3f, p =>
            {
                foreach (var a in leaving) a.grow = 1f - EaseInOut(p);
            });
        }

        // ---- helpers ----

        /// <summary>Nov 5: a super-effective hit holds the stage's tweens this long (hit-stop); the rest of the game runs on.</summary>
        static float _hitStopUntil;

        static float StageDelta => Time.time < _hitStopUntil ? 0f : Time.deltaTime;

        static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += StageDelta) yield return null;
        }

        static IEnumerator Tween(float seconds, Action<float> step, Action done = null)
        {
            for (float t = 0f; t < seconds; t += StageDelta)
            {
                step(t / seconds);
                yield return null;
            }
            step(1f);
            done?.Invoke();
        }

        static float EaseInOut(float p) => p * p * (3f - 2f * p);

        static float EaseOutBack(float p)
        {
            const float c = 1.7f;
            float q = p - 1f;
            return 1f + (c + 1f) * q * q * q + c * q * q;
        }
    }
}
