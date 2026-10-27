using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Untangle the Keeper (step 10e, RealmsDesign "Untangle the Keeper"): in the tower hall Galeowl sits on its perch
    /// wrapped in kite-string loops with three glowing knots on its body, each with two loose ends (gold dots). Pinch both
    /// ends of a knot (one per hand) and pull apart more than 12 cm: twang, its loops fall away, the owl shudders. One hand
    /// works too (pinch an end, pull away more than 15 cm), and look + pinch a knot loosens it by itself in 2 s
    /// (<see cref="KnotPull"/>). All three undone = solved (saved, <see cref="HandPuzzle.id"/> "gw.knots", stage = a mask of
    /// the knots undone): the <see cref="KeeperHall"/> listening starts the meeting and the battle. A lost or fled battle
    /// doesn't tangle it again. Fingertips near the owl never walk the explorer (<see cref="TileTouch.Block"/>).
    /// The lesson line is pinned until the first knot snaps; its ghost pulls apart with both hands.
    /// </summary>
    public class KeeperUntangle : HandPuzzle
    {
        public const int AllKnots = 7;

        [Header("Untangle")]
        public KeeperHall keeper;
        public SceneSwap swap;
        [Tooltip("Kite string ribbons (the 10b KiteString material).")]
        public Material stringMaterial;
        [Tooltip("Knots on the body: directions from its centre (x right, y up, z toward the player).")]
        public Vector3[] knots = { new Vector3(0f, -0.1f, 1f), new Vector3(-0.85f, 0.35f, 0.55f), new Vector3(0.85f, -0.25f, 0.6f) };
        [Tooltip("Body radius for the knots and loops, share of the Keeper's height.")]
        public float bodyRadius = 0.36f;
        [Tooltip("Each loose end sits this far from its knot (m at island scale 1).")]
        public float endSpread = 0.035f;
        [Tooltip("A hand pinching this close to an end holds it (m, scaled with the island when it's bigger).")]
        public float grabRadius = 0.035f;
        [Tooltip("Both ends pulled apart this much beyond where they were grabbed (real metres).")]
        public float twoHandPull = 0.12f;
        [Tooltip("One end pulled away from its knot this much (real metres).")]
        public float oneHandPull = 0.15f;
        [Tooltip("Look + pinch a knot: it loosens by itself after this long.")]
        public float loosenSeconds = 2f;
        [Tooltip("Hands this close to the owl (share of its height) don't walk the explorer.")]
        public float touchGuard = 0.9f;
        [Range(0f, 1f)] public float volume = 0.8f;
        public Color knotGlow = new Color(1f, 0.8f, 0.35f);
        public Color endDot = new Color(1f, 0.85f, 0.3f);

        readonly HandState[] _hands = new HandState[2];
        readonly List<KnotPull> _pulls = new List<KnotPull>();
        readonly List<float> _fall = new List<float>();
        readonly List<BattleFx.Item> _orbs = new List<BattleFx.Item>();
        int _mask;
        int _loosening = -1;
        float _loosenT;
        Mesh _strings;
        MeshRenderer _stringRenderer;
        AudioSource _audio;
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<int> _tris = new List<int>();

        public override bool Here => island != null && swap != null && map != null && swap.Inside && swap.Interior == map && !SceneSwap.IsBusy;
        public override bool Busy => _loosening >= 0 || AnyHeld;
        protected override bool Available => !IsSolved && keeper != null && keeper.Model != null && !keeper.Calmed && keeper.Idle;
        protected override Vector2 HomeTile => keeper != null ? keeper.keeperSpot : Vector2.zero;
        protected override bool GhostBothHands => true;
        protected override float GhostSpread => 2f * endSpread * Scale;
        public override float FocusRadius => endSpread * 1.4f * Scale;

        public override Vector3? FocusPoint
        {
            get
            {
                if (!Here || !Available) return null;
                for (int i = 0; i < _pulls.Count; i++)
                    if (!Done(i)) return KnotWorld(i);
                return null;
            }
        }

        /// <summary>Knots undone (0..3).</summary>
        public int Undone
        {
            get
            {
                int n = 0;
                for (int i = 0; i < knots.Length; i++)
                    if (Done(i)) n++;
                return n;
            }
        }

        bool AnyHeld
        {
            get
            {
                foreach (var p in _pulls)
                    if (p.AnyHeld) return true;
                return false;
            }
        }

        bool Done(int i) => (_mask & (1 << i)) != 0;
        float Scale => Mathf.Max(1e-4f, island.transform.lossyScale.x);

        protected override void Start()
        {
            if (keeper == null || swap == null)
            {
                Debug.LogError("[KeeperUntangle] Needs the Keeper hall and the scene swap (Set Up MR_Main (10e tower hall)).", this);
                enabled = false;
                return;
            }
            for (int i = 0; i < knots.Length; i++)
            {
                _pulls.Add(new KnotPull());
                _fall.Add(1f);
            }
            if (island != null)
            {
                var go = new GameObject("Keeper Knot Strings");
                go.layer = island.gameObject.layer;
                go.transform.SetParent(island.transform, false);
                _strings = new Mesh { name = "Keeper Knot Strings" };
                _strings.MarkDynamic();
                go.AddComponent<MeshFilter>().sharedMesh = _strings;
                _stringRenderer = go.AddComponent<MeshRenderer>();
                _stringRenderer.sharedMaterial = stringMaterial;
                _stringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _stringRenderer.receiveShadows = false;
                _stringRenderer.enabled = false;
                if (stringMaterial == null) Debug.LogError("[KeeperUntangle] No string material: the loops won't show.", this);
            }
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            base.Start();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ReleaseOrbs();
            if (_strings != null) Destroy(_strings);
            if (_stringRenderer != null) Destroy(_stringRenderer.gameObject);
        }

        protected override void ApplyState()
        {
            _mask = IsSolved ? AllKnots : PuzzleState.Stage(id) & AllKnots;
            _loosening = -1;
            for (int i = 0; i < _pulls.Count; i++)
            {
                _pulls[i].Reset();
                if (Done(i)) _pulls[i].Loosen();
                _fall[i] = Done(i) ? 1f : 0f;
            }
            SetProgressQuiet(Undone / (float)Mathf.Max(1, knots.Length));
        }

        // ---- the owl's frame ----

        Vector3 Center => keeper.Model.WorldCenter;
        float Radius => keeper.Model.View.height * bodyRadius * island.Zoom * Scale * keeper.Model.grow;

        Vector3 Dir(Vector3 d)
        {
            var t = island.transform;
            return (t.right * d.x + t.up * d.y + t.forward * d.z).normalized;
        }

        Vector3 KnotWorld(int i) => Center + Dir(knots[i]) * (Radius * 1.04f);

        void Ends(int i, out Vector3 a, out Vector3 b)
        {
            var d = Dir(knots[i]);
            var side = Vector3.Cross(island.transform.up, d);
            if (side.sqrMagnitude < 1e-4f) side = island.transform.right;
            side.Normalize();
            var k = KnotWorld(i);
            float s = endSpread * Scale;
            var out1 = d * (s * 0.45f) - island.transform.up * (s * 0.25f);
            a = k + side * s + out1;
            b = k - side * s + out1;
        }

        // ---- update ----

        protected override void Update()
        {
            ReadHands(_hands);
            bool here = Here && keeper != null && keeper.Model != null && keeper.Model.gameObject.activeInHierarchy;
            if (here) GuardTouch();
            bool working = here && Available && Usable;
            if (working) Work();
            else
            {
                foreach (var p in _pulls)
                    if (!p.Snapped && p.AnyHeld) p.Reset();
                _loosening = -1;
            }
            for (int i = 0; i < _fall.Count; i++)
                if (Done(i) && _fall[i] < 1f) _fall[i] = Mathf.Min(1f, _fall[i] + Time.deltaTime / 0.8f);
            Draw(here && !keeper.Calmed);
            base.Update();
        }

        /// <summary>Hands near the owl never walk the explorer (pulling ends, pokes, a dip toward the floor).</summary>
        void GuardTouch()
        {
            float guard = keeper.Model.View.height * touchGuard * island.Zoom * Scale + endSpread * Scale * 2f;
            var c = Center;
            for (int h = 0; h < 2; h++)
            {
                var s = _hands[h];
                if (!s.tracked) continue;
                bool near = (s.palm - c).magnitude < guard + 0.06f || s.pinch && (s.pinchPoint - c).magnitude < guard;
                if (near) TileTouch.Block(h == 0, 0.8f);
            }
        }

        void Work()
        {
            float scale = Mathf.Max(1f, Scale);
            var left = Hand(0);
            var right = Hand(1);
            for (int i = 0; i < _pulls.Count; i++)
            {
                if (Done(i)) continue;
                var p = _pulls[i];
                Ends(i, out p.endA, out p.endB);
                p.center = KnotWorld(i);
                p.grabRadius = grabRadius * scale;
                p.twoHandPull = twoHandPull;
                p.oneHandPull = oneHandPull;
                bool wasHeld = p.AnyHeld;
                bool snapped = p.Tick(left, right);
                if (!wasHeld && p.AnyHeld)
                {
                    MarkInteracted();
                    Debug.Log($"[KeeperUntangle] Knot {i + 1}: {(p.BothHeld ? "both ends" : "an end")} pinched.");
                }
                // A hand that took an end here isn't free for the other knots this frame.
                if (p.Held(0) >= 0) left.started = false;
                if (p.Held(1) >= 0) right.started = false;
                if (snapped)
                {
                    Snap(i, p.SnappedTwoHanded ? "both hands pulled apart" : "one hand pulled away");
                    return;
                }
            }
            for (int h = 0; h < 2; h++)
                foreach (var p in _pulls)
                    if (p.Held(h) >= 0) IslandHandle.MarkHandHeld(h == 0);
            Gaze(left, right);
        }

        KnotPull.Hand Hand(int h)
        {
            var s = _hands[h];
            return new KnotPull.Hand(s.pinch, s.pinchStarted, s.pinchPoint, s.busy);
        }

        void Gaze(KnotPull.Hand left, KnotPull.Hand right)
        {
            if (_loosening >= 0)
            {
                _loosenT += Time.deltaTime;
                if (_loosenT >= loosenSeconds)
                {
                    int i = _loosening;
                    _loosening = -1;
                    _pulls[i].Loosen();
                    Snap(i, "look + pinch: it loosened");
                }
                return;
            }
            if (AnyHeld || !HeadGaze.TryGetRay(out var ray)) return;
            var pick = new HeadGaze.ConePick(ray, -1);
            for (int i = 0; i < _pulls.Count; i++)
                if (!Done(i)) pick.Consider(i, KnotWorld(i), Mathf.Max(HeadGaze.MinRadius * Scale, endSpread * 1.2f * Scale));
            if (pick.Best < 0) return;
            FocusHighlight.Hover(KnotWorld(pick.Best), endSpread * Scale);
            // A pinch that didn't take an end (those cleared their started flag).
            bool pinched = left.started && !left.busy || right.started && !right.busy;
            if (!pinched) return;
            MarkInteracted();
            _loosening = pick.Best;
            _loosenT = 0f;
            Debug.Log($"[KeeperUntangle] Look + pinch on knot {pick.Best + 1}: it loosens by itself in {loosenSeconds:0.#} s.");
        }

        void Snap(int i, string how)
        {
            _mask |= 1 << i;
            _fall[i] = 0f;
            int undone = Undone;
            if (_audio != null)
            {
                _audio.pitch = Random.Range(0.92f, 1.08f) * (1f + 0.08f * undone);
                _audio.PlayOneShot(PuzzleSounds.Twang, volume);
            }
            if (fx != null)
            {
                float tile = TileWorld;
                fx.Burst(KnotWorld(i), 16, knotGlow, tile * 0.5f, 0.6f, tile * 0.04f);
                fx.Burst(KnotWorld(i), 10, new Color(0.93f, 0.9f, 0.82f), tile * 0.35f, 0.8f, tile * 0.025f, Vector3.zero, tile * 2f);
            }
            if (keeper.Model != null) keeper.Model.View.Hop(); // the owl shudders
            Debug.Log($"[KeeperUntangle] Knot {i + 1} snaps ({how}): {undone} / {knots.Length} undone.");
            LessonComplete();
            if (undone >= knots.Length) MarkSolved(AllKnots);
            else
            {
                PuzzleState.SetStage(id, _mask);
                SetProgress(undone / (float)knots.Length);
            }
        }

        // ---- drawing ----

        void Draw(bool show)
        {
            bool any = false;
            int orb = 0;
            if (show)
            {
                var cam = Camera.main;
                Vector3 camLocal = cam != null ? island.transform.InverseTransformPoint(cam.transform.position) : Vector3.up * 2f;
                _verts.Clear();
                _tris.Clear();
                var c = Center;
                float r = Radius;
                for (int i = 0; i < _pulls.Count; i++)
                {
                    float fall = _fall[i];
                    if (fall >= 1f) continue;
                    any = true;
                    var d = Dir(knots[i]);
                    // Two loops round the body through the knot; once snapped they widen, drop and shrink away.
                    float grow = 1f + 0.5f * fall, drop = r * 1.3f * fall * fall;
                    var n1 = Vector3.Cross(d, island.transform.up);
                    if (n1.sqrMagnitude < 1e-4f) n1 = island.transform.right;
                    var n2 = Quaternion.AngleAxis(55f, d) * n1.normalized;
                    foreach (var n in new[] { n1.normalized, n2 })
                        Loop(c - island.transform.up * drop, d, n, r * 1.03f * grow, camLocal, (1f - fall) * 0.0024f);
                    if (Done(i)) continue;
                    var p = _pulls[i];
                    var k = KnotWorld(i);
                    // Wiggles while look + pinch loosens it.
                    if (_loosening == i) k += island.transform.right * (Mathf.Sin(Time.time * 40f) * 0.002f * Scale);
                    for (int e = 0; e < 2; e++)
                    {
                        var end = p.EndNow(e);
                        Strand(k, end, camLocal, 0.0024f);
                        Orb(ref orb, end, 0.006f * Scale * (p.Held(0) == e || p.Held(1) == e ? 1.3f : 1f), endDot, 1.6f);
                    }
                    float pulse = 1f + 0.15f * Mathf.Sin(Time.time * 4f + i);
                    float loosen = _loosening == i ? _loosenT / loosenSeconds : 0f;
                    Orb(ref orb, k, 0.011f * Scale * pulse * (1f + 0.4f * Mathf.Max(p.Stretch, loosen)), knotGlow, 2.2f + 2f * Mathf.Max(p.Stretch, loosen));
                }
                if (_strings != null)
                {
                    _strings.Clear();
                    _strings.SetVertices(_verts);
                    _strings.SetTriangles(_tris, 0);
                    _strings.RecalculateBounds();
                }
            }
            if (_stringRenderer != null) _stringRenderer.enabled = any;
            while (_orbs.Count > orb)
            {
                if (fx != null) fx.Release(_orbs[_orbs.Count - 1]);
                _orbs.RemoveAt(_orbs.Count - 1);
            }
        }

        void Orb(ref int index, Vector3 world, float radius, Color color, float power)
        {
            if (fx == null) return;
            if (index >= _orbs.Count) _orbs.Add(fx.Get(BattleFx.Shape.Orb));
            var o = _orbs[index++];
            if (o == null) return;
            o.transform.position = world;
            o.transform.localScale = Vector3.one * (radius / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
            o.Set(Color.Lerp(color, Color.white, 0.5f), color, 0.95f, power, 1f, 0.15f);
        }

        void ReleaseOrbs()
        {
            if (fx != null)
                foreach (var o in _orbs) fx.Release(o);
            _orbs.Clear();
        }

        /// <summary>A closed ribbon round <paramref name="c"/> through direction <paramref name="d"/>, turning about <paramref name="axis"/>.</summary>
        void Loop(Vector3 c, Vector3 d, Vector3 axis, float r, Vector3 camLocal, float width)
        {
            if (width <= 1e-5f) return;
            const int segments = 28;
            var b = Vector3.Cross(axis, d).normalized;
            var pts = new Vector3[segments + 1];
            for (int s = 0; s <= segments; s++)
            {
                float a = s / (float)segments * Mathf.PI * 2f;
                // Squashed a little top to bottom like the owl's round body.
                var w = c + (d * Mathf.Cos(a) + b * Mathf.Sin(a)) * r;
                w += island.transform.up * (Vector3.Dot(w - c, island.transform.up) * -0.08f);
                pts[s] = island.transform.InverseTransformPoint(w);
            }
            Ribbon(pts, camLocal, width / Scale);
        }

        void Strand(Vector3 a, Vector3 b, Vector3 camLocal, float width)
        {
            const int segments = 8;
            var la = island.transform.InverseTransformPoint(a);
            var lb = island.transform.InverseTransformPoint(b);
            var sag = (la + lb) * 0.5f + Vector3.down * ((lb - la).magnitude * 0.25f);
            var pts = new Vector3[segments + 1];
            for (int s = 0; s <= segments; s++)
            {
                float t = s / (float)segments;
                pts[s] = (1 - t) * (1 - t) * la + 2 * (1 - t) * t * sag + t * t * lb;
            }
            Ribbon(pts, camLocal, width / Scale);
        }

        void Ribbon(Vector3[] pts, Vector3 camLocal, float width)
        {
            int start = _verts.Count;
            for (int s = 0; s < pts.Length; s++)
            {
                var p = pts[s];
                var tangent = pts[Mathf.Min(s + 1, pts.Length - 1)] - pts[Mathf.Max(s - 1, 0)];
                var side = Vector3.Cross(tangent, camLocal - p).normalized * width * 0.5f;
                _verts.Add(p - side);
                _verts.Add(p + side);
                if (s == pts.Length - 1) continue;
                int v = start + s * 2;
                _tris.Add(v); _tris.Add(v + 2); _tris.Add(v + 1);
                _tris.Add(v + 1); _tris.Add(v + 2); _tris.Add(v + 3);
            }
        }

        // ---- debug (bridge) ----

        /// <summary>Knot <paramref name="i"/> snaps as if pulled apart (two hands).</summary>
        public void DebugSnap(int i)
        {
            if (i < 0 || i >= _pulls.Count || Done(i) || !Available) return;
            MarkInteracted();
            _pulls[i].Loosen();
            Snap(i, "bridge");
        }

        /// <summary>Look + pinch on knot <paramref name="i"/> (loosens in 2 s).</summary>
        public void DebugGaze(int i)
        {
            if (i < 0 || i >= _pulls.Count || Done(i) || !Available) return;
            MarkInteracted();
            _loosening = i;
            _loosenT = 0f;
        }

        public string Describe() => $"knots undone {Undone}/{knots.Length} (mask {_mask}), loosening {_loosening}, available {Available}, usable {Usable}";
    }
}
