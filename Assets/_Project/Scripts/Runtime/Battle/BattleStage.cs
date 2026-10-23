using System;
using System.Collections;
using System.Collections.Generic;
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

        [Header("Timing (s)")]
        public float pauseBetweenMoves = 0.45f;

        static readonly Color Gold = new Color(1f, 0.84f, 0.35f);
        static readonly Color Leaf = new Color(0.55f, 0.8f, 0.35f);
        static readonly Color Smoke = new Color(0.6f, 0.64f, 0.61f);
        static readonly Color SuperColor = new Color(1f, 0.85f, 0.3f);
        static readonly Color WeakColor = new Color(0.75f, 0.8f, 0.78f);
        static readonly Color HealColor = new Color(0.55f, 0.95f, 0.55f);

        StageActor _player, _wild;
        BattleHud.Bar _playerBar, _wildBar;
        BattleFx.Item _playerShield, _wildShield;

        public StageActor Player => _player;
        public StageActor Wild => _wild;

        float Zoom => island.Zoom;
        float TileSize => island.map.tileSize;
        float Cap => island.Content.capHeight;
        /// <summary>Map metres to world metres right now.</summary>
        float World(float mapMetres) => mapMetres * Zoom * island.transform.lossyScale.x;
        Vector3 WorldAt(Vector2 tile, float height) => island.transform.TransformPoint(island.TileToLocal(tile, Cap + height));

        // ---- setup ----

        /// <summary>Puts a creature on the stage, hidden (grow 0) until an entrance plays.</summary>
        public StageActor Spawn(CreatureInstance creature, BattleSide side, Vector2 tile, Vector2 facing)
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
            actor.Snap();
            if (side == BattleSide.Player) _player = actor;
            else _wild = actor;
            return actor;
        }

        public void ShowBars(CreatureInstance player, CreatureInstance wild)
        {
            _playerBar = hud.AddBar(_player, player.Name, player.level, player.currentHp, player.MaxHp);
            _wildBar = hud.AddBar(_wild, wild.Name, wild.level, wild.currentHp, wild.MaxHp);
        }

        /// <summary>Removes both creatures, shields and labels.</summary>
        public void Cleanup()
        {
            if (_player != null) Destroy(_player.gameObject);
            if (_wild != null) Destroy(_wild.gameObject);
            _player = _wild = null;
            DropShield(ref _playerShield, false);
            DropShield(ref _wildShield, false);
            hud.Clear();
            _playerBar = _wildBar = null;
        }

        // ---- entrances ----

        /// <summary>Grass shakes around a tile (before the creature shows); gold sparkles if it is rare.</summary>
        public IEnumerator Rustle(Vector2Int tile, bool rare, float seconds)
        {
            island.FlashTile(tile, seconds);
            Vector3 p = WorldAt(tile, 0.01f);
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
        public IEnumerator WildAppears(bool rare, bool fromWater)
        {
            var a = _wild;
            if (a == null) yield break;
            Vector3 burstAt = WorldAt(a.tile, 0.01f);
            if (fromWater)
            {
                var water = ElementStyle.Of(Element.Water);
                fx.Burst(burstAt, 24, water.burst, World(0.06f), 0.6f, World(0.004f), Vector3.up * World(0.08f));
                float h = a.View.height;
                a.grow = 1f;
                yield return Tween(0.5f, p =>
                {
                    a.lift = Mathf.Lerp(-h, 0f, p) + h * 1.6f * Mathf.Sin(p * Mathf.PI);
                    a.squash = new Vector3(1f - 0.1f * (1f - p), 1f + 0.15f * (1f - p), 1f - 0.1f * (1f - p));
                });
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
                    a.lift = a.View.height * 0.5f * Mathf.Sin(p * Mathf.PI);
                });
                a.grow = 1f;
                a.lift = 0f;
            }
            if (rare) fx.Burst(a.WorldCenter, 30, Gold, World(0.05f), 0.9f, World(0.005f), Vector3.up * World(0.03f));
            a.View.Hop();
        }

        /// <summary>Placeholder for the pod throw (step 6): a burst of light and the creature grows in.</summary>
        public IEnumerator PodBurst()
        {
            var a = _player;
            if (a == null) yield break;
            Vector3 c = WorldAt(a.tile, a.View.centerHeight);
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
        }

        // ---- turns ----

        /// <summary>Animates one turn's events in order. <paramref name="caption"/> receives the battle text lines.</summary>
        public IEnumerator Play(IReadOnlyList<BattleEvent> events, BattleSystem battle, Action<string> caption)
        {
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                var self = Actor(e.side);
                string who = battle.Get(e.side).creature.Name;
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
                        caption($"{(e.side == BattleSide.Wild ? "Wild " : "")}{who} fainted!");
                        if (self != null)
                        {
                            fx.Burst(self.WorldCenter, 20, Smoke, World(0.03f), 0.7f, World(0.005f), Vector3.up * World(0.02f));
                            self.View.Faint();
                            DropShieldOf(e.side, true);
                        }
                        yield return Wait(0.8f);
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
                    case BattleEventKind.XpGained:
                        if (_player != null) hud.PopText(_player.LocalTop + Vector3.up * 0.04f, $"+{e.amount} XP", Gold, 1f, 1.4f);
                        yield return Wait(0.5f);
                        break;
                }
            }
        }

        static bool IsOutcome(BattleEventKind k) =>
            k == BattleEventKind.Damage || k == BattleEventKind.Missed || k == BattleEventKind.Shielded ||
            k == BattleEventKind.AttackRaised || k == BattleEventKind.Healed;

        StageActor Actor(BattleSide side) => side == BattleSide.Player ? _player : _wild;

        IEnumerator PlayMove(BattleSide side, MoveData move, BattleEvent? outcome, BattleSystem battle)
        {
            var user = Actor(side);
            var target = Actor(side == BattleSide.Player ? BattleSide.Wild : BattleSide.Player);
            if (user == null || target == null) yield break;
            var style = ElementStyle.Of(move.element);
            var userStyle = ElementStyle.Of(battle.Get(side).creature.Element);
            Action impact = () => { if (outcome.HasValue) Impact(outcome.Value, user, target, userStyle); };

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

        void Impact(BattleEvent o, StageActor user, StageActor target, ElementStyle userStyle)
        {
            switch (o.kind)
            {
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
            hud.PopText(target.LocalTop + Vector3.up * 0.042f, $"-{o.amount}", Color.white);
            if (o.effectiveness == Effectiveness.Super) hud.PopText(target.LocalTop + Vector3.up * 0.075f, "Super effective!", SuperColor, 0.65f, 1.4f);
            else if (o.effectiveness == Effectiveness.NotVery) hud.PopText(target.LocalTop + Vector3.up * 0.075f, "Not very effective", WeakColor, 0.65f, 1.4f);
            if (o.shieldAbsorbed) DropShieldOf(o.side, true);
            StartCoroutine(Knockback(target, (target.tile - user.tile).normalized));
        }

        IEnumerator Knockback(StageActor target, Vector2 dir)
        {
            float seed = UnityEngine.Random.value * 10f;
            yield return Tween(0.36f, p =>
            {
                target.View.SetFlash((1f - p) * 0.85f);
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
            yield return Tween(0.26f, p => user.squash = new Vector3(1f + 0.12f * p, 1f - 0.12f * p, 1f + 0.12f * p));
            user.squash = Vector3.one;
            Vector2 dir = (target.tile - user.tile).normalized;
            Vector2 from = user.tile + dir * 0.35f, to = target.tile;
            float h0 = user.View.centerHeight, h1 = target.View.centerHeight;
            var orb = fx.Get(BattleFx.Shape.Orb);
            orb.Set(style.core, style.edge, 1f, 1f, 1f, move.element == Element.Water ? 0.12f : 0.35f);
            const float radius = 0.007f; // map metres
            yield return Tween(0.42f, p =>
            {
                Vector2 t = Vector2.Lerp(from, to, p);
                float h = Mathf.Lerp(h0, h1, p) + (move.arc ? Mathf.Sin(p * Mathf.PI) * 0.03f : 0f);
                orb.transform.localPosition = island.TileToLocal(t, Cap + h);
                orb.transform.localScale = Vector3.one * (radius * Zoom * (1f + Mathf.Sin(Time.time * 30f) * 0.08f));
                fx.Burst(island.transform.TransformPoint(orb.transform.localPosition), 2, style.trail, World(0.012f), 0.3f, World(0.003f));
            });
            fx.Release(orb);
            Vector3 hitAt = target.WorldCenter;
            fx.Burst(hitAt, 34, style.burst, World(0.08f), 0.5f, World(0.004f), Vector3.up * World(0.02f));
            StartCoroutine(Pop(hitAt, style.burst));
            impact();
            yield return Wait(0.25f);
        }

        IEnumerator Lunge(StageActor user, StageActor target, Action impact)
        {
            Vector2 d = target.tile - user.tile;
            yield return Tween(0.14f, p => user.squash = new Vector3(1f + 0.12f * p, 1f - 0.12f * p, 1f + 0.12f * p));
            yield return Tween(0.15f, p =>
            {
                float q = EaseInOut(p);
                user.nudge = d * (0.6f * q);
                user.squash = new Vector3(1.12f - 0.2f * q, 0.88f + 0.2f * q, 1.12f - 0.2f * q);
            });
            user.squash = Vector3.one;
            fx.Burst(target.WorldCenter, 16, Color.white, World(0.07f), 0.3f, World(0.0035f));
            impact();
            yield return Tween(0.26f, p => user.nudge = d * (0.6f * (1f - EaseInOut(p))));
            user.nudge = Vector2.zero;
        }

        IEnumerator Slam(StageActor user, StageActor target, ElementStyle style, Action impact)
        {
            float top = user.View.height * 0.8f;
            yield return Tween(0.26f, p => user.lift = Mathf.Sin(p * Mathf.PI * 0.5f) * top);
            yield return Tween(0.11f, p => user.lift = top * (1f - p * p));
            user.lift = 0f;
            StartCoroutine(Tween(0.22f, p =>
            {
                float k = Mathf.Sin(p * Mathf.PI) * 0.25f;
                user.squash = new Vector3(1f + k, 1f - k, 1f + k);
            }, () => user.squash = Vector3.one));
            fx.Burst(WorldAt(user.tile, 0.004f), 24, style.burst, World(0.06f), 0.45f, World(0.004f), Vector3.up * World(0.03f));
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
                    yield return Wait(0.5f);
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
            shield.transform.localScale = Vector3.one * (user.View.height * 0.72f * Zoom * size);
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
            if (pop && isActiveAndEnabled) StartCoroutine(PopShield(item));
            else fx.Release(item);
        }

        IEnumerator PopShield(BattleFx.Item item)
        {
            Vector3 s0 = item.transform.localScale;
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

        void LateUpdate()
        {
            if (_playerShield != null && _player != null) PlaceShieldIdle(_playerShield, _player);
            if (_wildShield != null && _wild != null) PlaceShieldIdle(_wildShield, _wild);
        }

        // After it has grown, the bubble just follows its creature.
        void PlaceShieldIdle(BattleFx.Item shield, StageActor user)
        {
            shield.transform.localPosition = user.LocalCenter;
            float target = user.View.height * 0.72f * Zoom;
            if (shield.transform.localScale.x > target * 0.98f) shield.transform.localScale = Vector3.one * target;
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

        static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime) yield return null;
        }

        static IEnumerator Tween(float seconds, Action<float> step, Action done = null)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
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
