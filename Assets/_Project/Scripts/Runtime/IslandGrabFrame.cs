using System.Collections.Generic;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The island's grab frame (step 8d), like the grab frame on Meta's panels: a thin, soft white rounded rectangle
    /// tracing the rocky base just under the cap's edge, a little outside it. It fades in when a hand's pinch point or
    /// palm comes within <see cref="showDistance"/> of the grab shell, brightest on the side nearest the hand. Hover
    /// (in the shell, ready to grab) = brighter and a little thicker; grabbed = solid with a touch of gold; two hands
    /// light both their sides. Step 8e: only the parts of the frame over the grab zone light up (with the pods active,
    /// never the strip in front of them). It fades out over <see cref="fadeSeconds"/> once the hand leaves, stays
    /// hidden during scene swaps, and pulses while a lesson hints at the rim (<see cref="IslandHandle.HintGlow"/>).
    /// Step 14a: it shows the handles only: a bracket over each of the four corners (always faintly there, like a notched
    /// corner piece; white when a hand is within <see cref="showDistance"/>, gold while held) and, while exploring, a line
    /// along the front lip under the pods. The rest of the frame is gone.
    /// Step 14i: hold to grab: a gold ring fills around the handle while a hand arms the grab (<see cref="IslandHandle.TryGetArm"/>);
    /// lesson ghosts show the same ring through <see cref="Ring"/>.
    /// </summary>
    [DefaultExecutionOrder(200)] // after the island has moved this frame
    public class IslandGrabFrame : MonoBehaviour
    {
        public IslandBuilder island;
        public IslandHandle handle;
        [Tooltip("Diorama/Grab Frame material.")]
        public Material material;

        [Header("Shape (island-local m)")]
        [Tooltip("Height of the frame (0 = the cap's base; the cap top is about +0.006).")]
        public float height = 0f;
        [Tooltip("How far outside the base's edge.")]
        public float outset = 0.008f;
        public float cornerRadius = 0.03f;
        [Range(2, 16)] public int cornerSegments = 8;
        [Tooltip("Extra points along each straight side (the glow follows the hand along it).")]
        [Range(1, 24)] public int sideSegments = 10;

        [Header("Look")]
        [Tooltip("Ribbon width (world m): near, hover, held.")]
        public Vector3 width = new Vector3(0.003f, 0.0045f, 0.005f);
        [Tooltip("A pinch point or palm this close to a handle (world m) lights it.")]
        public float showDistance = 0.06f;
        [Tooltip("Step 14a: the corner brackets' level with no hand near (subtle, always there).")]
        [Range(0f, 1f)] public float idleLevel = 0.18f;
        [Tooltip("Corner bracket length along each edge (island-local m; the handle's corner size).")]
        public float bracketLength = 0.1f;
        [Tooltip("The lip line sits this far under the pods' line (world m), on the base's front face.")]
        public float lipLineBelowPods = 0.03f;
        [Tooltip("Frame points this far from the grab zone (world m) stay dark (the strip in front of active pods).")]
        public float zoneFeather = 0.012f;
        [Tooltip("Along the frame, the glow falls off this far from the point nearest the hand (world m).")]
        public float nearSpread = 0.12f;
        [Tooltip("The rest of the frame while a hand is near (x the nearest side's brightness).")]
        [Range(0f, 1f)] public float farLevel = 0.25f;
        [Range(0f, 1f)] public float nearLevel = 0.55f;
        [Range(0f, 2f)] public float hoverLevel = 1f;
        [Range(0f, 2f)] public float grabbedLevel = 1.1f;
        public Color color = new Color(0.95f, 0.97f, 1f);
        [Tooltip("Held: the frame tints toward this.")]
        public Color grabbedColor = new Color(1f, 0.78f, 0.3f);
        [Range(0f, 1f)] public float grabbedTint = 0.85f;
        public float fadeInSeconds = 0.12f;
        public float fadeSeconds = 0.3f;

        [Header("Hold ring (step 14i)")]
        [Tooltip("The arming ring's radius and ribbon width (world m).")]
        public float ringRadius = 0.018f;
        public float ringWidth = 0.004f;
        [Range(8, 64)] public int ringSegments = 40;
        [Tooltip("The empty part of the ring (alpha).")]
        [Range(0f, 1f)] public float ringTrack = 0.15f;

        struct HandState
        {
            public float near, hover, held;
            public Vector3 closest;
        }

        /// <summary>Bridge checks without hands: a fake open right palm here (world), null = off.</summary>
        public static System.Func<Vector3?> DebugPalm;

        readonly HandState[] _hands = new HandState[2];
        readonly List<Vector3> _local = new List<Vector3>();
        readonly List<bool> _isHandle = new List<bool>();
        readonly List<Vector3> _lipWorld = new List<Vector3>();
        readonly List<Vector3> _world = new List<Vector3>();
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Color> _colors = new List<Color>();
        readonly List<Vector2> _uvs = new List<Vector2>();
        readonly List<int> _tris = new List<int>();
        Mesh _mesh, _ringMesh;
        Vector2 _builtFor;

        struct RingRequest
        {
            public Vector3 point;
            public float fill, alpha;
        }

        static readonly List<RingRequest> _rings = new List<RingRequest>();

        /// <summary>Step 14i: draws a hold ring this frame (a lesson ghost's hold); fill 0..1.</summary>
        public static void Ring(Vector3 world, float fill, float alpha = 1f) =>
            _rings.Add(new RingRequest { point = world, fill = Mathf.Clamp01(fill), alpha = Mathf.Clamp01(alpha) });
        float _hint;

        void Start()
        {
            if (island == null || handle == null || material == null)
            {
                Debug.LogError("[IslandGrabFrame] Needs the island, its handle and the Grab Frame material (Set Up MR_Main (8d)): no grab frame.", this);
                enabled = false;
                return;
            }
            _mesh = new Mesh { name = "Island Grab Frame" };
            _mesh.MarkDynamic();
            _ringMesh = new Mesh { name = "Island Hold Rings" };
            _ringMesh.MarkDynamic();
        }

        void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_ringMesh != null) Destroy(_ringMesh);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            DebugPalm = null;
            DebugArm = null;
            _rings.Clear();
        }

        /// <summary>Bridge checks without hands: a fake arming ring (world point, fill), null = off.</summary>
        public static System.Func<(Vector3, float)?> DebugArm;

        bool Hidden => SceneSwap.IsBusy || handle.ScaleFactor < 0.3f; // the opening: the island hasn't grown yet

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            bool hidden = Hidden;
            float any = 0f;
            for (int h = 0; h < 2; h++) any = Mathf.Max(any, Track(ref _hands[h], h == 0, hidden, dt));
            _hint = Mathf.MoveTowards(_hint, hidden ? 0f : handle.HintGlow, dt * 4f);
            DrawRings(hidden);
            // The corner brackets always show a little (once the island has grown).
            if (hidden || any <= 0.002f && _hint <= 0.002f && idleLevel <= 0.002f) return;
            Draw();
        }

        /// <summary>Step 14i: the arming rings (each hand's, the ghosts' and the bridge's), facing the eye.</summary>
        void DrawRings(bool hidden)
        {
            if (!hidden)
            {
                for (int h = 0; h < 2; h++)
                    if (handle.TryGetArm(h == 0, out var p, out var f)) Ring(p, f);
                var fake = DebugArm != null ? DebugArm() : null;
                if (fake.HasValue) Ring(fake.Value.Item1, fake.Value.Item2);
            }
            var cam = Camera.main;
            if (hidden || _rings.Count == 0 || cam == null)
            {
                _rings.Clear();
                return;
            }
            _verts.Clear();
            _colors.Clear();
            _uvs.Clear();
            _tris.Clear();
            Vector3 eye = cam.transform.position;
            float s = island.transform.lossyScale.x / 0.6f;
            foreach (var r in _rings)
            {
                Vector3 n = (eye - r.point).normalized;
                Vector3 up = Vector3.Cross(n, Vector3.Cross(Vector3.up, n)).normalized;
                if (up.sqrMagnitude < 1e-6f) up = Vector3.forward;
                Vector3 right = Vector3.Cross(up, n);
                Vector3 c = r.point + n * 0.004f;
                // The track (full, faint) and the gold fill clockwise from the top.
                Arc(c, up, right, ringRadius * s, ringWidth * s * 0.7f, 1f, new Color(color.r, color.g, color.b, ringTrack * r.alpha));
                if (r.fill > 0.001f) Arc(c + n * 0.0005f, up, right, ringRadius * s, ringWidth * s * 1.6f, r.fill, new Color(grabbedColor.r, grabbedColor.g * 0.9f, grabbedColor.b * 0.5f, 1.6f * r.alpha));
            }
            _rings.Clear();
            _ringMesh.Clear();
            _ringMesh.SetVertices(_verts);
            _ringMesh.SetColors(_colors);
            _ringMesh.SetUVs(0, _uvs);
            _ringMesh.SetTriangles(_tris, 0);
            _ringMesh.RecalculateBounds();
            Graphics.RenderMesh(new RenderParams(material) { layer = island.gameObject.layer, shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off },
                _ringMesh, 0, Matrix4x4.identity);
        }

        void Arc(Vector3 c, Vector3 up, Vector3 right, float radius, float w, float fill, Color col)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt(ringSegments * fill));
            int first = _verts.Count;
            for (int i = 0; i <= n; i++)
            {
                float a = fill * Mathf.PI * 2f * i / n;
                Vector3 dir = up * Mathf.Cos(a) + right * Mathf.Sin(a);
                _verts.Add(c + dir * (radius - w * 0.5f));
                _verts.Add(c + dir * (radius + w * 0.5f));
                _colors.Add(col);
                _colors.Add(col);
                _uvs.Add(new Vector2(0f, 0f));
                _uvs.Add(new Vector2(0f, 1f));
                if (i == n) continue;
                int a0 = first + i * 2, b0 = a0 + 2;
                _tris.Add(a0);
                _tris.Add(b0);
                _tris.Add(a0 + 1);
                _tris.Add(a0 + 1);
                _tris.Add(b0);
                _tris.Add(b0 + 1);
            }
        }

        /// <summary>Eases one hand's near / hover / held levels; returns how much it lights the frame.</summary>
        float Track(ref HandState s, bool left, bool hidden, float dt)
        {
            var hand = HandRegistry.Get(left ? Handedness.Left : Handedness.Right);
            float near = 0f, hover = 0f, held = 0f;
            Vector3? fake = !left && DebugPalm != null ? DebugPalm() : null;
            if (!hidden && fake.HasValue)
            {
                float d = handle.DistanceToHandles(fake.Value);
                near = d < showDistance ? 1f - Mathf.SmoothStep(0f, 1f, d / showDistance) : 0f;
                hover = d <= 0.005f ? 1f : 0f;
                s.closest = fake.Value;
            }
            else if (!hidden && HandLookup.TryGetJoint(hand, HandJointId.HandPalm, out var palm))
            {
                held = handle.IsHandGrabbing(left) ? 1f : 0f;
                // The nearer of the pinch point and the palm (pinch and palm grabs both work).
                Vector3 probe = palm;
                float d = handle.DistanceToHandles(palm);
                if (IslandHandle.TryGetPinchPoint(hand, out var pinch))
                {
                    float dp = handle.DistanceToHandles(pinch);
                    if (dp < d)
                    {
                        d = dp;
                        probe = pinch;
                    }
                }
                if (d < showDistance)
                {
                    near = 1f - Mathf.SmoothStep(0f, 1f, d / showDistance);
                    hover = handle.IsHandHovering(left) || d <= 0.005f ? 1f : 0f;
                }
                if (near > 0f || held > 0f) s.closest = probe;
            }
            s.near = Ease(s.near, Mathf.Max(near, held), dt);
            s.hover = Ease(s.hover, Mathf.Max(hover, held), dt);
            s.held = Ease(s.held, held, dt);
            return s.near;
        }

        float Ease(float from, float to, float dt) =>
            Mathf.MoveTowards(from, to, dt / Mathf.Max(0.01f, to > from ? fadeInSeconds : fadeSeconds));

        void Draw()
        {
            var cam = Camera.main;
            if (cam == null) return;
            BuildPath();
            var t = island.transform;
            _world.Clear();
            foreach (var p in _local) _world.Add(t.TransformPoint(p));
            _lipWorld.Clear();
            if (handle.LipActive)
            {
                // The lip line: along the front face under the pods, between the corner brackets.
                var half = island.FootprintHalfSize;
                var front = t.TransformPoint(new Vector3(0f, 0f, half.y));
                front.y = handle.PodLineY - lipLineBelowPods;
                float y = t.InverseTransformPoint(front).y;
                float x = half.x - bracketLength;
                for (int i = 0; i <= sideSegments; i++)
                    _lipWorld.Add(t.TransformPoint(new Vector3(Mathf.Lerp(x, -x, i / (float)sideSegments), y, half.y + outset)));
            }

            // Each hand's nearest point on the handles: its glow centres there.
            var nearest = new Vector3[2];
            for (int h = 0; h < 2; h++) nearest[h] = NearestOnFrame(_hands[h].closest);
            float pulse = _hint * (0.55f + 0.45f * Mathf.Sin(Time.time * 4f));
            float heldAll = Mathf.Max(_hands[0].held, _hands[1].held);
            float hoverAll = Mathf.Max(_hands[0].hover, _hands[1].hover);
            float w = Mathf.Lerp(Mathf.Lerp(width.x, width.y, hoverAll), width.z, heldAll);
            Color tint = Color.Lerp(color, grabbedColor, grabbedTint * heldAll);

            _verts.Clear();
            _colors.Clear();
            _uvs.Clear();
            _tris.Clear();
            Vector3 eye = cam.transform.position;
            Strip(_world, true, _isHandle, nearest, pulse, w, tint, eye, idleLevel);
            if (_lipWorld.Count > 1) Strip(_lipWorld, false, null, nearest, pulse, w, tint, eye, 0f);
            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.SetColors(_colors);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetTriangles(_tris, 0);
            _mesh.RecalculateBounds();
            Graphics.RenderMesh(new RenderParams(material) { layer = island.gameObject.layer, shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off },
                _mesh, 0, Matrix4x4.identity);
        }

        /// <summary>Adds one ribbon (a closed loop masked to the handle points, or an open line) to the mesh.</summary>
        void Strip(List<Vector3> pts, bool closed, List<bool> mask, Vector3[] nearest, float pulse, float w, Color tint, Vector3 eye, float idle)
        {
            int n = pts.Count, first = _verts.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = pts[i];
                Vector3 tangent = pts[closed ? (i + 1) % n : Mathf.Min(i + 1, n - 1)] - pts[closed ? (i - 1 + n) % n : Mathf.Max(i - 1, 0)];
                Vector3 side = Vector3.Cross(tangent, eye - p);
                side = side.sqrMagnitude > 1e-10f ? side.normalized * (w * 0.5f) : Vector3.up * (w * 0.5f);
                float level = Mathf.Max(idle, pulse * 0.6f);
                for (int h = 0; h < 2; h++)
                {
                    var s = _hands[h];
                    if (s.near <= 0.001f) continue;
                    float along = (p - nearest[h]).magnitude / Mathf.Max(0.001f, nearSpread);
                    float local = farLevel + (1f - farLevel) * Mathf.Exp(-along * along);
                    float peak = Mathf.Lerp(nearLevel, hoverLevel, s.hover);
                    // Held: every handle solid.
                    float handLevel = Mathf.Lerp(local * peak, grabbedLevel, s.held) * s.near;
                    level = Mathf.Max(level, handLevel);
                }
                // Only the handles light (the corner brackets; the lip line is all handle).
                if (mask != null && !mask[i]) level = 0f;
                var c = new Color(tint.r, tint.g, tint.b, level);
                _verts.Add(p - side);
                _verts.Add(p + side);
                _colors.Add(c);
                _colors.Add(c);
                _uvs.Add(new Vector2(0f, 0f));
                _uvs.Add(new Vector2(0f, 1f));
                if (!closed && i == n - 1) continue;
                int a = first + i * 2, b = first + ((i + 1) % n) * 2;
                _tris.Add(a);
                _tris.Add(b);
                _tris.Add(a + 1);
                _tris.Add(a + 1);
                _tris.Add(b);
                _tris.Add(b + 1);
            }
        }

        Vector3 NearestOnFrame(Vector3 p)
        {
            Vector3 best = p;
            float bestSq = float.MaxValue;
            int n = _world.Count;
            for (int i = 0; i < n + _lipWorld.Count - 1; i++)
            {
                bool lip = i >= n;
                if (!lip && !_isHandle[i]) continue;
                Vector3 a = lip ? _lipWorld[i - n] : _world[i], b = lip ? _lipWorld[i - n + 1] : _world[(i + 1) % n], ab = b - a;
                float k = ab.sqrMagnitude > 1e-10f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                Vector3 q = a + ab * k;
                float d = (q - p).sqrMagnitude;
                if (d < bestSq)
                {
                    bestSq = d;
                    best = q;
                }
            }
            return best;
        }

        /// <summary>The rounded rectangle around the island's footprint (island-local), rebuilt when the footprint changes.</summary>
        void BuildPath()
        {
            var half = island.FootprintHalfSize;
            if (_local.Count > 0 && half == _builtFor) return;
            _builtFor = half;
            _local.Clear();
            var b = new Bounds(Vector3.zero, new Vector3(half.x * 2f, 0f, half.y * 2f));
            float hx = half.x + outset, hz = half.y + outset;
            float r = Mathf.Min(cornerRadius, Mathf.Min(hx, hz) * 0.9f);
            // Corner centres, counter-clockwise seen from above (+X, +Z first), and the angle each corner's arc starts at.
            var centers = new[] { new Vector2(hx - r, hz - r), new Vector2(-hx + r, hz - r), new Vector2(-hx + r, -hz + r), new Vector2(hx - r, -hz + r) };
            for (int k = 0; k < 4; k++)
            {
                float start = k * 90f;
                for (int i = 0; i <= cornerSegments; i++)
                {
                    float a = (start + 90f * i / cornerSegments) * Mathf.Deg2Rad;
                    _local.Add(new Vector3(b.center.x + centers[k].x + Mathf.Cos(a) * r, height, b.center.z + centers[k].y + Mathf.Sin(a) * r));
                }
                // The straight side to the next corner's arc.
                var next = centers[(k + 1) % 4];
                float na = ((k + 1) * 90f) * Mathf.Deg2Rad;
                Vector3 from = _local[_local.Count - 1];
                var to = new Vector3(b.center.x + next.x + Mathf.Cos(na) * r, height, b.center.z + next.y + Mathf.Sin(na) * r);
                for (int i = 1; i < sideSegments; i++) _local.Add(Vector3.Lerp(from, to, i / (float)sideSegments));
            }
            // Step 14a: the corner brackets are the points within bracketLength of a corner along both edges' lines.
            _isHandle.Clear();
            foreach (var p in _local) _isHandle.Add(Mathf.Abs(p.x) > hx - bracketLength && Mathf.Abs(p.z) > hz - bracketLength);
        }
    }
}
