using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 10f, Cinderwood's kiln village: the Hearth Shrine's flame is nearly out (a tiny sputtering blue-orange wisp,
    /// the plaza cold and dark) and the shrine can't be rested at (<see cref="HearthShrines.SetDormant"/>). Cup it: both
    /// palms within <see cref="radius"/> of the flame and turned toward it for 2 s, or one hand curled round it for 3 s,
    /// or look + pinch-hold it for 3 s (<see cref="CupMeter"/>). While held it grows and brightens and warm sparks rise
    /// between the fingers; let go and it shrinks back. Rekindled: it flares, a warm pulse rolls out from the shrine
    /// over the plaza (a <see cref="WarmSpots"/> circle: every kiln glows and smokes, hut windows and lantern posts catch
    /// one after another), the plaza Islings hop, and the shrine works from then on (saved). A realm already restored
    /// (an old save, the dev toggle) counts it as rekindled.
    /// While the flame is out this draws the shrine (its cold body and the wisp) instead of the island's copy.
    /// </summary>
    [DefaultExecutionOrder(-20)] // before the gaze walk and tile touch read the held hands
    public class FlameCupPiece : HandPuzzle
    {
        [Header("Shrine (props grid: column, row of its 2 x 2 anchor)")]
        public Vector2Int shrine;
        [Tooltip("The flame's middle, tiles above the cap.")]
        public float flameHeight = 1.58f;
        [Tooltip("Where the wisp's base sits (tiles above the cap).")]
        public float wispBase = 1.36f;
        public PropRecipe coldShrine, wisp;
        [Tooltip("Wisp size while dying, and once fully warm (x a tile).")]
        public Vector2 wispSize = new Vector2(0.75f, 1.4f);

        [Header("Cup (metres at the placed island size)")]
        [Tooltip("Oct 27 headset: 10 cm / 60 deg was too tight for real cupped hands.")]
        public float radius = 0.14f;
        [Range(10f, 90f)] public float facingAngle = 75f;
        [Tooltip("A hand already cupping stays in until it's this much farther / more turned away (tracking wobble).")]
        public float keepRadius = 1.3f, keepAngle = 15f;
        public float twoHandSeconds = 2f, oneHandSeconds = 3f, lookSeconds = 3f;

        [Header("The village answers")]
        [Tooltip("WarmSpots slot this flame owns.")]
        public int warmSlot;
        [Tooltip("Where the warm pulse ends up (map column, row): the plaza's middle.")]
        public Vector2 villageCentre;
        [Tooltip("Tiles the warmth reaches round the plaza, and its soft edge.")]
        public float villageRadius = 10f, villageEdge = 1.5f;
        public float pulseSeconds = 3f;
        [Tooltip("Props that spark as the pulse reaches them (recipe names).")]
        public string[] catchers = { "Kiln", "LanternPost", "ClayHut" };

        [Header("Lines")]
        public string doneLine = "cw.flame.done";
        [Range(0f, 1f)] public float volume = 0.7f;

        readonly CupMeter _meter = new CupMeter();
        readonly HandState[] _hands = new HandState[2];
        readonly bool[] _cupping = new bool[2];
        PropView _body, _wisp;
        AudioSource _audio, _crackle;
        bool _gazed, _lookHold, _pulsing, _quietSolve;
        float _shown, _sparkAt, _debugUntil;

        public CupMeter Meter => _meter;
        public override bool Busy => _meter.Held;
        protected override Vector2 HomeTile => Centre;
        Vector2 Centre => shrine + new Vector2(0.5f, 0.5f);
        public Vector3 FlameWorld => MapToWorld(new Vector3(Centre.x, flameHeight, Centre.y));
        public override Vector3? FocusPoint => Here && !IsSolved ? FlameWorld : (Vector3?)null;
        public override float FocusRadius => 0.7f * TileWorld;

        // The lesson ghost: both hands cupping, mirrored, either side of the flame.
        protected override bool GhostBothHands => true;
        protected override float GhostSpread => 0.11f * Scale;

        float Scale => grabGuard != null ? grabGuard.ScaleFactor : 1f;

        protected override void Start()
        {
            _meter.twoHands = twoHandSeconds;
            _meter.oneHand = oneHandSeconds;
            _meter.look = lookSeconds;
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            // A soft crackle that swells while the flame is held (one looping source; one-shots restarted on every
            // tracking flicker and droned like a motor on the headset).
            _crackle = gameObject.AddComponent<AudioSource>();
            _crackle.playOnAwake = false;
            _crackle.spatialBlend = 0f;
            _crackle.loop = true;
            _crackle.clip = PuzzleSounds.Crackle;
            _crackle.volume = 0f;
            if (island != null)
            {
                _body = new PropView("Village Shrine (cold)", island, coldShrine);
                _wisp = new PropView("Village Shrine Wisp", island, wisp);
            }
            base.Start();
        }

        protected override void OnDestroy()
        {
            _body?.Destroy();
            _wisp?.Destroy();
            if (map != null)
            {
                map.SetPropHidden(shrine, false);
                HearthShrines.SetDormant(map.name, shrine, false);
            }
            WarmSpots.Clear(warmSlot);
            base.OnDestroy();
        }

        protected override void ApplyState()
        {
            StopAllCoroutines();
            _pulsing = false;
            bool lit = IsSolved;
            _meter.Restore(lit);
            _shown = lit ? 1f : 0f;
            map.SetPropHidden(shrine, !lit);
            HearthShrines.SetDormant(map.name, shrine, !lit);
            SetProgressQuiet(lit ? 1f : 0f);
            Debug.Log($"[FlameCupPiece] {id}: the village flame is {(lit ? "burning" : "nearly out")}.");
        }

        protected override void Update()
        {
            ReadHands(_hands);
            if (!IsSolved && Here && RealmState.Restored >= 0.99f) QuietSolve();
            if (Usable && !IsSolved) Cup();
            else
            {
                _gazed = false;
                _cupping[0] = _cupping[1] = false;
                if (!IsSolved) _meter.Feed(Time.deltaTime, 0, false, false);
            }
            Draw();
            Warm();
            Sound();
            // The shrine's ring fills while it's held, so it's clear how far it got.
            if (map != null) HearthShrines.SetDormantFill(map.name, shrine, IsSolved ? 0f : _meter.Progress);
            base.Update();
        }

        // ---- cupping ----

        void Cup()
        {
            Vector3 flame = FlameWorld;
            float r = radius * Scale;
            int facing = 0;
            bool wrapped = false;
            for (int h = 0; h < 2; h++)
            {
                bool was = _cupping[h];
                _cupping[h] = false;
                if (!_hands[h].tracked || _hands[h].busy) continue;
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (!HandLookup.TryGetJointPose(hand, HandJointId.HandPalm, out var palm)) continue;
                // The palmar side is the palm joint's -Y for both hands (ISDK v207, as PalmHover).
                Vector3 normal = palm.rotation * Vector3.down;
                // Sticky: a hand already in stays in a little farther and more turned (tracking wobble).
                float rr = was ? r * keepRadius : r, aa = was ? facingAngle + keepAngle : facingAngle;
                if (DevReadout.Active)
                {
                    var to = flame - palm.position;
                    DevReadout.Text($"cup{h}", $"cup {(h == 0 ? "L" : "R")}: {to.magnitude * 100f:0.0} cm (<{rr * 100f:0}), angle {Vector3.Angle(normal, to):0} (<{aa:0}), warmth {_meter.Progress:0.00}");
                }
                if (!CupRules.Facing(palm.position, normal, flame, rr, aa)) continue;
                _cupping[h] = true;
                facing++;
                if (HandLookup.TryGetJoint(hand, HandJointId.HandMiddleTip, out var middle) && CupRules.Wraps(palm.position, normal, middle, flame, r, facingAngle))
                    wrapped = true;
                // Hands round the flame never walk the explorer or swipe.
                IslandHandle.MarkHandHeld(h == 0);
                TileTouch.Block(h == 0, 0.4f);
            }
            if (Time.time < _debugUntil) facing = 2; // bridge: as if both hands cupped it
            _lookHold = false;
            if (facing == 0) LookHold();
            else _gazed = false;
            bool wasHeld = _meter.Held;
            bool done = _meter.Feed(Time.deltaTime, facing, wrapped, _lookHold);
            if (_meter.Held && !wasHeld)
            {
                MarkInteracted();
                Debug.Log($"[FlameCupPiece] {id}: cupped ({(facing >= 2 ? "two hands" : wrapped ? "one hand round it" : _lookHold ? "look + pinch-hold" : "?")}).");
            }
            if (!_meter.Held && wasHeld) Debug.Log($"[FlameCupPiece] {id}: let go at warmth {_meter.Progress:0.00}.");
            SetProgress(_meter.Progress);
            if (_meter.Held && Time.time >= _sparkAt && fx != null)
            {
                // Warm sparks rise between the fingers while it's held.
                _sparkAt = Time.time + 0.09f;
                float tile = TileWorld;
                fx.Burst(flame + island.transform.up * tile * 0.1f, 2, new Color(1f, 0.66f, 0.28f), tile * 0.25f, 0.8f, tile * 0.05f, island.transform.up * tile * 1.1f);
            }
            if (done) StartCoroutine(Rekindle());
        }

        void LookHold()
        {
            if (!HeadGaze.TryGetRay(out var ray))
            {
                _gazed = false;
                return;
            }
            var pick = new HeadGaze.ConePick(ray, _gazed ? 0 : -1);
            pick.Consider(0, FlameWorld, Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.9f * TileWorld));
            _gazed = pick.Best == 0;
            if (!_gazed) return;
            FocusHighlight.Hover(FlameWorld, FocusRadius);
            foreach (var h in _hands)
                if (h.pinch && !h.busy) _lookHold = true;
        }

        // ---- rekindled ----

        IEnumerator Rekindle()
        {
            _pulsing = true;
            Debug.Log($"[FlameCupPiece] {id}: rekindled, the warmth rolls over the plaza.");
            LessonComplete();
            MarkSolved(1);
            HearthShrines.SetDormant(map.name, shrine, false);
            GameAudio.Procedural(_audio, PuzzleSounds.Whoosh, volume);
            Flare();
            if (explorer != null) explorer.Cheer();
            if (!string.IsNullOrEmpty(doneLine)) Dialogue.Say(doneLine);

            var catchers = Catchers();
            var caught = new bool[catchers.Count];
            float tileSize = island.map.tileSize;
            for (float t = 0f; t < pulseSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / pulseSeconds);
                Vector2 c = Vector2.Lerp(Centre, villageCentre, k);
                float r = Mathf.Lerp(1.5f, villageRadius, k);
                if (Here) WarmSpots.Set(warmSlot, c, r, villageEdge, tileSize);
                for (int i = 0; i < catchers.Count; i++)
                {
                    if (caught[i] || (catchers[i].tile - c).magnitude > r - villageEdge * 0.5f) continue;
                    caught[i] = true;
                    if (Here) Catch(catchers[i]);
                }
                if (t > 0.6f && t - Time.deltaTime <= 0.6f || t > 1.6f && t - Time.deltaTime <= 1.6f) MapCreature.HopNear(map, villageCentre, villageRadius);
                yield return null;
            }
            MapCreature.HopNear(map, villageCentre, villageRadius);
            _pulsing = false;
        }

        /// <summary>The flame flares: a bright orb swells and fades, embers fly up (as a shrine used to rest).</summary>
        void Flare()
        {
            if (fx == null) return;
            float tile = TileWorld;
            var warm = new Color(1f, 0.7f, 0.3f);
            fx.Burst(FlameWorld, 40, warm, tile * 0.8f, 1f, tile * 0.07f, island.transform.up * tile);
            StartCoroutine(Orb());
        }

        IEnumerator Orb()
        {
            var orb = fx.Get(BattleFx.Shape.Orb);
            if (orb == null) yield break;
            orb.Set(Color.white, new Color(1f, 0.7f, 0.3f), 1f, 1.5f, 1f, 0.3f);
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                float u = t / 0.8f;
                orb.transform.position = FlameWorld;
                orb.transform.localScale = Vector3.one * (TileWorld * (0.4f + 1.3f * Mathf.Sin(u * Mathf.PI)) / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
                orb.SetAlpha(Here ? 1f - u * u : 0f);
                yield return null;
            }
            fx.Release(orb);
        }

        struct Catcher
        {
            public Vector2 tile;
            public float top;
        }

        /// <summary>Kilns, lantern posts and huts round the plaza (their middles), nearest the shrine first.</summary>
        List<Catcher> Catchers()
        {
            var list = new List<Catcher>();
            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                {
                    if (!map.TryGetProp(map.PropAt(r, c), out var e) || e.recipe == null) continue;
                    if (System.Array.IndexOf(catchers, e.recipe.name) < 0) continue;
                    var f = WorldMap.FootprintOf(e);
                    var mid = new Vector2(c + (f.x - 1) * 0.5f, r + (f.y - 1) * 0.5f);
                    if ((mid - villageCentre).magnitude > villageRadius) continue;
                    list.Add(new Catcher { tile = mid, top = e.recipe.name == "ClayHut" ? 0.5f : e.recipe.name == "LanternPost" ? 0.85f : 0.3f });
                }
            return list;
        }

        void Catch(Catcher c)
        {
            if (fx == null) return;
            float tile = TileWorld;
            fx.Burst(MapToWorld(new Vector3(c.tile.x, c.top, c.tile.y)), 8, new Color(1f, 0.62f, 0.25f), tile * 0.4f, 0.7f, tile * 0.05f, island.transform.up * tile * 0.6f);
            GameAudio.Procedural(_audio, PuzzleSounds.Catch, volume * 0.35f);
        }

        /// <summary>The realm got restored without it (an old save, the dev toggle): the flame just burns, no fanfare.</summary>
        void QuietSolve()
        {
            if (_quietSolve) return;
            _quietSolve = true;
            Debug.Log($"[FlameCupPiece] {id}: the realm is restored, so the village flame burns again (no cupping).");
            MarkSolved(1);
            HearthShrines.SetDormant(map.name, shrine, false);
            map.SetPropHidden(shrine, false);
        }

        // ---- looks ----

        void Draw()
        {
            if (_body == null) return;
            bool cold = !IsSolved || _pulsing && _shown < 0.999f;
            // The wisp grows while held, shrinks back when let go; once lit it swells up to full and the island's own
            // shrine (its baked flame, warm in the spot) takes over.
            float target = IsSolved ? 1f : _meter.Progress;
            _shown = Mathf.MoveTowards(_shown, target, Time.deltaTime * (IsSolved ? 2.5f : 3f));
            if (!cold)
            {
                if (map.IsPropHidden(shrine)) map.SetPropHidden(shrine, false);
                _body.Hide();
                _wisp.Hide();
                return;
            }
            bool show = Here;
            _body.Place(Centre, 0f, Quaternion.identity, show);
            float sputter = IsSolved ? 1f : 0.85f + 0.15f * Mathf.Sin(Time.time * 17f) * Mathf.Sin(Time.time * 5.3f);
            float size = Mathf.Lerp(wispSize.x, wispSize.y, _shown) * sputter;
            // Own restoration: 0 = a cold blue sputter, 1 = a steady orange flame.
            _wisp.restoredSelf = Mathf.Lerp(0.12f, 1f, _shown);
            _wisp.glowBoostB = new Vector4(0.9f + 0.6f * _shown, 0f, 0f, 0f);
            _wisp.Place(Centre, wispBase, Quaternion.Euler(0f, Time.time * 40f, 0f), show, size);
            if (IsSolved && _shown >= 0.999f && map.IsPropHidden(shrine)) map.SetPropHidden(shrine, false);
        }

        void Sound()
        {
            if (_crackle == null) return;
            float want = !IsSolved && Here && _meter.Held ? volume * 0.3f * (0.4f + 0.6f * _meter.Progress) * GameAudio.SfxVolume : 0f;
            _crackle.volume = Mathf.MoveTowards(_crackle.volume, want, Time.deltaTime * 0.8f);
            if (_crackle.volume > 0.001f && !_crackle.isPlaying) _crackle.Play();
            else if (_crackle.volume <= 0.001f && _crackle.isPlaying) _crackle.Stop();
        }

        void Warm()
        {
            if (_pulsing) return;
            if (IsSolved && Here) WarmSpots.Set(warmSlot, villageCentre, villageRadius, villageEdge, island.map.tileSize);
            else if (!Here || !IsSolved) WarmSpots.Clear(warmSlot);
        }

        /// <summary>Debug (bridge): as if both hands cupped it for <paramref name="seconds"/> (the explorer must be near).</summary>
        public void DebugCup(float seconds)
        {
            if (!Here || IsSolved) return;
            _debugUntil = Time.time + seconds;
            Debug.Log($"[FlameCupPiece] {id}: debug cup for {seconds:0.#} s (usable {Usable}).");
        }

        public string Describe() => $"flame {(IsSolved ? "lit" : "dying")}, warmth {_meter.Progress:0.00}, spot r {WarmSpots.Radius(warmSlot) / Mathf.Max(1e-4f, island.map.tileSize):0.#} tiles";
    }
}
