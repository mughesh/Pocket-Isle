using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Galewick's kites (step 10b, "kites into your room"). Three kites lie in the kite field's grass. Pinch one (or close a
    /// fist on it) and it lifts on its string, following the hand with a little lag; let go higher than
    /// <see cref="liftThreshold"/> above the island top (or toss it up faster than <see cref="tossSpeed"/>) and it flies:
    /// out over the island in the room, in a lazy figure-eight on its own slot (fanned, each a bit higher), tail
    /// fluttering, its string a soft curve down to the explorer's backpack. Let go lower and it drifts back down. Look +
    /// pinch a kite: it rises and flies by itself. Flying kites follow the explorer (and lift higher and back during
    /// battles), hide indoors and off Galewick, and are saved (stage = a bit per flown kite; solved = all three). The wind
    /// tower (<see cref="TowerDoor"/>) takes them: <see cref="Tie"/> sends them up to its crown, where they stay.
    /// Flight is scripted motion, not physics (prompt 10b, 5b). Draws: 2 per kite + 1 for all strings.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class KitePiece : HandPuzzle
    {
        [Header("Kites (map props grid: column, row)")]
        public Vector2Int[] kites = new Vector2Int[3];
        [Tooltip("The kites' looks (lying flat; flying ones are stood up). Same order as the tiles.")]
        public PropRecipe[] recipes = new PropRecipe[3];
        [Tooltip("Thin unlit material for the strings.")]
        public Material stringMaterial;

        [Header("Lift and release (island-local metres = scaled with the island)")]
        [Tooltip("Let go this high above the island top and it flies (prompt: ~15 cm; 8 cm if raising feels unreliable).")]
        public float liftThreshold = 0.15f;
        [Tooltip("Or tossed up at least this fast (m/s, real world).")]
        public float tossSpeed = 0.6f;
        [Tooltip("A hand's grip point this close to a lying kite grabs it (tiles, plus world slack).")]
        public float grabRadius = 1.1f;
        public float grabSlack = 0.02f;
        [Tooltip("How fast a held kite catches up with the hand (per second).")]
        public float holdFollow = 14f;

        [Header("Flight (island-local metres)")]
        [Tooltip("Lowest slot above the island top (prompt: 30-50 cm, scaled).")]
        public float flyHeight = 0.3f;
        [Tooltip("Each next kite flies this much higher.")]
        public float flyStep = 0.08f;
        [Tooltip("Kites fan out this far apart sideways.")]
        public float flySpread = 0.15f;
        [Tooltip("Figure-eight size (x, y) and speed (cycles a second).")]
        public Vector2 eightSize = new Vector2(0.07f, 0.035f);
        public float eightSpeed = 0.18f;
        [Tooltip("During battles: this much higher and farther back.")]
        public Vector2 battleOffset = new Vector2(0.14f, 0.16f);
        [Tooltip("Flying kite size (a tile x this).")]
        public float flyingScale = 2.4f;
        [Tooltip("How fast slots follow the explorer (per second).")]
        public float follow = 2.5f;
        public float riseSeconds = 1.2f, fallSeconds = 0.9f;

        [Header("Tied to the tower")]
        [Tooltip("The tower's crown (map column, height in tiles, row): where Tie sends them.")]
        public Vector3 tiedAt;

        [Header("Lines")]
        public string[] countLines = { "gw.kites.one", "gw.kites.two", "gw.kites.three" };
        public EncounterDirector encounters;

        /// <summary>A kite started flying (kite index, kites flying now).</summary>
        public event Action<KitePiece, int, int> Flew;

        enum K { Lying, Held, Rising, Flying, Falling, Tied }

        class Kite
        {
            public K state;
            public Vector3 pos;       // island-local
            public Vector3 from;      // animation start
            public float t;
            public int hand = -1;
            public int order = -1;    // flying slot
            public PropView view;
            public float yaw;
        }

        readonly List<Kite> _kites = new List<Kite>();
        readonly HandState[] _hands = new HandState[2];
        readonly Vector3[] _lastGrip = new Vector3[2];
        readonly Vector3[] _gripVelocity = new Vector3[2];
        Mesh _strings;
        MeshRenderer _stringRenderer;
        Transform _backpack;
        int _gazed = -1;
        bool _tied;

        public int FlyingMask
        {
            get
            {
                int m = 0;
                for (int i = 0; i < _kites.Count; i++)
                    if (_kites[i].state == K.Flying || _kites[i].state == K.Rising) m |= KiteRules.Bit(i);
                return m;
            }
        }

        public int FlyingCount => KiteRules.Count(FlyingMask);
        public bool AreTied => _tied;

        public override bool Busy
        {
            get
            {
                foreach (var k in _kites)
                    if (k.state == K.Held) return true;
                return false;
            }
        }

        protected override Vector2 HomeTile
        {
            get
            {
                Vector2 sum = Vector2.zero;
                foreach (var k in kites) sum += k;
                return kites.Length > 0 ? sum / kites.Length : Vector2.zero;
            }
        }

        public override Vector3? FocusPoint
        {
            get
            {
                if (!Here) return null;
                // The nearest lying kite to the explorer.
                int best = -1;
                float bestD = float.MaxValue;
                for (int i = 0; i < _kites.Count; i++)
                {
                    if (_kites[i].state != K.Lying) continue;
                    float d = (kites[i] - explorer.Tile).sqrMagnitude;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = i;
                    }
                }
                return best >= 0 ? LyingWorld(best) : (Vector3?)null;
            }
        }

        public override float FocusRadius => 0.8f * TileWorld;

        protected override void Start()
        {
            for (int i = 0; i < kites.Length; i++)
            {
                var k = new Kite { state = K.Lying };
                if (island != null) k.view = new PropView($"Kite {i}", island, i < recipes.Length ? recipes[i] : null);
                k.yaw = 20f + 37f * i;
                _kites.Add(k);
            }
            if (island != null)
            {
                var go = new GameObject("Kite Strings");
                go.layer = island.gameObject.layer;
                go.transform.SetParent(island.transform, false);
                _strings = new Mesh { name = "Kite Strings" };
                _strings.MarkDynamic();
                go.AddComponent<MeshFilter>().sharedMesh = _strings;
                _stringRenderer = go.AddComponent<MeshRenderer>();
                _stringRenderer.sharedMaterial = stringMaterial;
                _stringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _stringRenderer.receiveShadows = false;
                if (stringMaterial == null) Debug.LogError("[KitePiece] No string material: the strings won't show.", this);
            }
            base.Start();
        }

        protected override void OnDestroy()
        {
            foreach (var k in _kites) k.view?.Destroy();
            if (_strings != null) Destroy(_strings);
            if (_stringRenderer != null) Destroy(_stringRenderer.gameObject);
            if (map != null)
                foreach (var a in kites) map.SetPropHidden(a, false);
            base.OnDestroy();
        }

        protected override void ApplyState()
        {
            int mask = IsSolved ? 7 : PuzzleState.Stage(id);
            _tied = PuzzleState.IsSolved(TowerDoor.TowerId);
            int order = 0;
            for (int i = 0; i < _kites.Count; i++)
            {
                var k = _kites[i];
                k.hand = -1;
                bool flown = (mask & KiteRules.Bit(i)) != 0;
                k.state = _tied ? K.Tied : flown ? K.Flying : K.Lying;
                k.order = flown ? order++ : -1;
                k.pos = k.state == K.Lying ? LyingLocal(i) : island != null && explorer != null && explorer.Body != null ? SlotLocal(k) : Vector3.zero;
                if (k.state == K.Tied) k.pos = TiedLocal(i);
            }
            if (map != null)
                foreach (var a in kites) map.SetPropHidden(a, true);
            SetProgressQuiet(KiteRules.Count(mask) / 3f);
        }

        protected override void Update()
        {
            ReadHands(_hands);
            TrackGrips();
            bool usable = Usable && !_tied;
            if (usable) Grab();
            if (usable) LookPinch();
            else _gazed = -1;
            for (int i = 0; i < _kites.Count; i++) Move(i);
            Draw();
            base.Update();
        }

        // ---- hands ----

        void TrackGrips()
        {
            for (int h = 0; h < 2; h++)
            {
                var s = _hands[h];
                if (!s.Holding)
                {
                    _gripVelocity[h] = Vector3.zero;
                    _lastGrip[h] = s.palm;
                    continue;
                }
                var p = s.GripPoint;
                if (Time.deltaTime > 0f && _lastGrip[h] != Vector3.zero)
                    _gripVelocity[h] = Vector3.Lerp(_gripVelocity[h], (p - _lastGrip[h]) / Time.deltaTime, 0.5f);
                _lastGrip[h] = p;
            }
        }

        void Grab()
        {
            float tile = TileWorld;
            for (int h = 0; h < 2; h++)
            {
                var s = _hands[h];
                if (s.busy || !(s.pinchStarted || s.fistStarted)) continue;
                if (HoldingKite(h)) continue;
                int best = -1;
                float bestD = grabRadius * tile + grabSlack;
                for (int i = 0; i < _kites.Count; i++)
                {
                    var k = _kites[i];
                    if (k.state != K.Lying && k.state != K.Falling) continue;
                    float d = (Local2World(k.pos) - s.GripPoint).magnitude;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = i;
                    }
                }
                if (best < 0) continue;
                var kite = _kites[best];
                kite.state = K.Held;
                kite.hand = h;
                MarkInteracted();
                Debug.Log($"[KitePiece] Kite {best} grabbed by the {(h == 0 ? "left" : "right")} {(s.pinch ? "pinch" : "fist")}.");
            }
        }

        bool HoldingKite(int h)
        {
            foreach (var k in _kites)
                if (k.state == K.Held && k.hand == h) return true;
            return false;
        }

        void LookPinch()
        {
            _gazed = Gazed();
            if (_gazed < 0) return;
            FocusHighlight.Hover(Local2World(_kites[_gazed].pos), FocusRadius);
            if (!GazePinch(_hands)) return;
            MarkInteracted();
            Debug.Log($"[KitePiece] Kite {_gazed}: look + pinch, it rises by itself.");
            Launch(_gazed);
        }

        int Gazed()
        {
            if (!HeadGaze.TryGetRay(out var ray)) return -1;
            var pick = new HeadGaze.ConePick(ray, _gazed);
            for (int i = 0; i < _kites.Count; i++)
                if (_kites[i].state == K.Lying)
                    pick.Consider(i, Local2World(_kites[i].pos), Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.8f * TileWorld));
            return pick.Best;
        }

        // ---- motion ----

        void Move(int i)
        {
            var k = _kites[i];
            float dt = Time.deltaTime;
            switch (k.state)
            {
                case K.Lying:
                    k.pos = LyingLocal(i);
                    break;
                case K.Held:
                {
                    var s = _hands[k.hand];
                    if (!s.tracked || !s.Holding)
                    {
                        Release(i);
                        break;
                    }
                    if (island != null) IslandHandle.MarkHandHeld(k.hand == 0);
                    var target = island.transform.InverseTransformPoint(s.GripPoint);
                    k.pos = Vector3.Lerp(k.pos, target, 1f - Mathf.Exp(-holdFollow * dt));
                    break;
                }
                case K.Rising:
                {
                    k.t += dt / Mathf.Max(0.1f, riseSeconds);
                    var slot = SlotLocal(k);
                    k.pos = Vector3.Lerp(k.from, slot, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k.t)));
                    if (k.t >= 1f) k.state = K.Flying;
                    break;
                }
                case K.Flying:
                    k.pos = Vector3.Lerp(k.pos, SlotLocal(k), 1f - Mathf.Exp(-follow * dt));
                    break;
                case K.Falling:
                    k.t += dt / Mathf.Max(0.1f, fallSeconds);
                    k.pos = Vector3.Lerp(k.from, LyingLocal(i), Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k.t)));
                    if (k.t >= 1f) k.state = K.Lying;
                    break;
                case K.Tied:
                    k.pos = Vector3.Lerp(k.pos, TiedLocal(i), 1f - Mathf.Exp(-1.5f * dt));
                    break;
            }
        }

        void Release(int i)
        {
            var k = _kites[i];
            int h = k.hand;
            k.hand = -1;
            float height = k.pos.y - island.CapTopLocal; // island-local: scaled with the island
            float up = h >= 0 ? _gripVelocity[h].y : 0f; // real-world m/s
            if (KiteRules.Flies(height, up, liftThreshold, tossSpeed))
            {
                Debug.Log($"[KitePiece] Kite {i} let go {height * 100f:0} cm up (island-scaled), {up:0.00} m/s up: it flies.");
                Launch(i);
            }
            else
            {
                Debug.Log($"[KitePiece] Kite {i} let go {height * 100f:0} cm up, {up:0.00} m/s: too low, it drifts back down (needs {liftThreshold * 100f:0} cm).");
                k.state = K.Falling;
                k.from = k.pos;
                k.t = 0f;
            }
        }

        void Launch(int i)
        {
            var k = _kites[i];
            int order = 0;
            foreach (var o in _kites)
                if (o != k && o.order >= 0) order++;
            k.order = order;
            k.state = K.Rising;
            k.from = k.pos;
            k.t = 0f;
            int mask = FlyingMask;
            int n = KiteRules.Count(mask);
            if (fx != null)
            {
                float tile = TileWorld;
                fx.Burst(Local2World(k.pos), 14, new Color(1f, 0.92f, 0.6f), tile * 0.7f, 0.7f, tile * 0.05f, Vector3.up * tile * 0.6f);
            }
            if (n - 1 < countLines.Length && !string.IsNullOrEmpty(countLines[n - 1])) Dialogue.Say(countLines[n - 1]);
            LessonComplete();
            Flew?.Invoke(this, i, n);
            if (n >= 3)
            {
                if (explorer != null) explorer.Cheer();
                MarkSolved(7);
            }
            else
            {
                PuzzleState.SetStage(id, mask);
                SetProgress(n / 3f);
            }
        }

        /// <summary>The wind tower takes the kites: they fly up to its crown and stay there (TowerDoor saves it).</summary>
        public void Tie()
        {
            _tied = true;
            foreach (var k in _kites)
            {
                k.state = K.Tied;
                k.hand = -1;
            }
            Debug.Log("[KitePiece] The kites fly up to the tower's crown.");
        }

        // ---- places (island-local) ----

        Vector3 LyingLocal(int i) => island.TileToLocal(kites[i], island.Content.capHeight + 0.05f * island.map.tileSize);

        Vector3 LyingWorld(int i) => Local2World(LyingLocal(i));

        Vector3 Local2World(Vector3 local) => island.transform.TransformPoint(local);

        Vector3 ExplorerLocal
        {
            get
            {
                if (explorer == null || explorer.Body == null) return Vector3.zero;
                return island.transform.InverseTransformPoint(explorer.Body.position);
            }
        }

        Vector3 SlotLocal(Kite k)
        {
            var slot = KiteRules.Slot(Mathf.Max(0, k.order), flyHeight, flyStep, flySpread);
            float phase = Time.time * eightSpeed * Mathf.PI * 2f + k.order * 2.1f;
            slot.x += Mathf.Sin(phase) * eightSize.x;
            slot.y += Mathf.Sin(phase * 2f) * eightSize.y;
            bool battle = encounters != null && encounters.InBattle;
            if (battle)
            {
                slot.y += battleOffset.y;
                slot.z -= battleOffset.x;
            }
            var e = ExplorerLocal;
            return new Vector3(e.x + slot.x, island.CapTopLocal + slot.y, e.z - 0.05f + slot.z);
        }

        Vector3 TiedLocal(int i)
        {
            float a = Time.time * 0.6f + i * 2.1f;
            var spot = tiedAt + new Vector3((i - 1) * 0.9f + Mathf.Sin(a) * 0.15f, 0.9f + i * 0.35f + Mathf.Sin(a * 1.7f) * 0.1f, 0f);
            return island.TileToLocal(new Vector2(spot.x, spot.z), island.Content.capHeight + spot.y * island.map.tileSize);
        }

        Vector3 BackpackLocal
        {
            get
            {
                if (_backpack == null && explorer != null && explorer.Body != null)
                    foreach (var t in explorer.Body.GetComponentsInChildren<Transform>())
                        if (t.name.Contains("Backpack"))
                        {
                            _backpack = t;
                            break;
                        }
                if (_backpack != null) return island.transform.InverseTransformPoint(_backpack.position);
                return ExplorerLocal + Vector3.up * 0.02f;
            }
        }

        // ---- drawing ----

        void Draw()
        {
            bool show = Here || _tied && island != null && island.world == map && !SceneSwap.IsInside;
            float tileM = island.map.tileSize * island.Zoom;
            var strings = new List<(Vector3 a, Vector3 b)>();
            for (int i = 0; i < _kites.Count; i++)
            {
                var k = _kites[i];
                if (k.view == null) continue;
                if (k.state == K.Lying)
                {
                    k.view.windowClip = true;
                    k.view.Place(kites[i], 0f, Quaternion.Euler(0f, k.yaw, 0f), Here);
                    continue;
                }
                // Up off the grass it stands up (nose up, face to the player) and grows to its flying size.
                float grow = k.state == K.Held ? 1.4f : k.state == K.Falling ? Mathf.Lerp(flyingScale, 1f, k.t) : flyingScale;
                float sway = Mathf.Sin(Time.time * 2.3f + i) * 12f;
                var rot = Quaternion.Euler(0f, 0f, sway) * Quaternion.Euler(90f - 15f, 0f, 0f);
                k.view.PlaceLocal(k.pos, rot, tileM * grow, show);
                if (show && k.state != K.Falling)
                    strings.Add((k.state == K.Tied ? TiedAnchor : BackpackLocal, k.pos + rot * new Vector3(0f, 0f, 0.25f) * tileM * grow));
            }
            DrawStrings(strings, show);
        }

        Vector3 TiedAnchor => island.TileToLocal(new Vector2(tiedAt.x, tiedAt.z), island.Content.capHeight + tiedAt.y * island.map.tileSize);

        /// <summary>One mesh: a camera-facing ribbon per string, a sagging quadratic Bezier from the anchor to the kite.</summary>
        void DrawStrings(List<(Vector3 a, Vector3 b)> list, bool show)
        {
            if (_strings == null) return;
            _stringRenderer.enabled = show && list.Count > 0;
            if (!_stringRenderer.enabled) return;
            const int segments = 14;
            var cam = Camera.main;
            Vector3 camLocal = cam != null ? island.transform.InverseTransformPoint(cam.transform.position) : Vector3.up * 2f;
            float width = 0.0016f / Mathf.Max(1e-4f, island.transform.lossyScale.x);
            var verts = new List<Vector3>(list.Count * (segments + 1) * 2);
            var tris = new List<int>(list.Count * segments * 6);
            foreach (var (a, b) in list)
            {
                float len = (b - a).magnitude;
                var sag = (a + b) * 0.5f + Vector3.down * len * 0.18f;
                int start = verts.Count;
                for (int s = 0; s <= segments; s++)
                {
                    float t = s / (float)segments;
                    var p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * sag + t * t * b;
                    var tangent = 2 * (1 - t) * (sag - a) + 2 * t * (b - sag);
                    var side = Vector3.Cross(tangent, camLocal - p).normalized * width * 0.5f;
                    verts.Add(p - side);
                    verts.Add(p + side);
                    if (s == segments) continue;
                    int v = start + s * 2;
                    tris.Add(v); tris.Add(v + 2); tris.Add(v + 1);
                    tris.Add(v + 1); tris.Add(v + 2); tris.Add(v + 3);
                }
            }
            _strings.Clear();
            _strings.SetVertices(verts);
            _strings.SetTriangles(tris, 0);
            _strings.RecalculateBounds();
        }

        // ---- debug (bridge) ----

        /// <summary>Kite <paramref name="i"/> rises and flies as if by look + pinch.</summary>
        public void DebugFly(int i)
        {
            if (i < 0 || i >= _kites.Count || _kites[i].state != K.Lying) return;
            MarkInteracted();
            Launch(i);
        }

        /// <summary>Kite <paramref name="i"/> held at <paramref name="height"/> (island-local metres above the top) then let go.</summary>
        public void DebugLiftAndRelease(int i, float height)
        {
            if (i < 0 || i >= _kites.Count) return;
            var k = _kites[i];
            k.pos = LyingLocal(i) + Vector3.up * height;
            k.hand = -1;
            k.state = K.Held;
            Release(i);
        }

        public string Describe()
        {
            var parts = new List<string>();
            for (int i = 0; i < _kites.Count; i++) parts.Add($"{i}:{_kites[i].state}");
            return $"kites {string.Join(" ", parts)}, mask {FlyingMask}, tied {_tied}";
        }
    }
}
