using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 27 F: the "you can pinch this" mark floating just above whatever the gaze is on (<see cref="FocusHighlight.Hover"/>):
    /// two fingertips touching at a gold dot. One flat mesh, faces the head, in the HUD material. Plain C#, owned by
    /// <see cref="FocusHighlight"/>. <see cref="Spot"/> keeps it off the target and the explorer.
    /// </summary>
    public class PinchIcon
    {
        static readonly int BackColorId = Shader.PropertyToID("_BackColor");
        static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int BorderId = Shader.PropertyToID("_Border");

        static readonly Color Finger = new Color(1f, 0.95f, 0.84f, 1f);
        static readonly Color Dot = new Color(1f, 0.74f, 0.16f, 1f);
        static readonly Color Shadow = new Color(0.2f, 0.14f, 0.08f, 1f);

        readonly Transform _root;
        readonly MeshRenderer _fingers, _dot, _shadow;
        readonly Mesh _fingerMesh, _dotMesh;
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        int _pick;

        public PinchIcon(Transform parent, Material hud)
        {
            _root = new GameObject("Pinch Icon").transform;
            _root.SetParent(parent, false);
            _fingerMesh = FingersMesh();
            _dotMesh = DiscMesh(new Vector3(0.12f, 0f, 0f), 0.11f, 16); // step 33: where the fingertips meet
            _shadow = Part("Shadow", _fingerMesh, hud);
            _shadow.transform.localScale = Vector3.one * 1.18f;
            _shadow.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            _fingers = Part("Fingers", _fingerMesh, hud);
            _dot = Part("Dot", _dotMesh, hud);
            _dot.sortingOrder = 3;
            _root.gameObject.SetActive(false);
        }

        MeshRenderer Part(string name, Mesh mesh, Material hud)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = hud;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = 2;
            return r;
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
            if (_fingerMesh != null) Object.Destroy(_fingerMesh);
            if (_dotMesh != null) Object.Destroy(_dotMesh);
        }

        /// <summary>Shows it at <paramref name="world"/>, <paramref name="size"/> metres across, at <paramref name="alpha"/>; hidden at 0.</summary>
        public void Draw(Vector3 world, float size, float alpha, Vector3 head)
        {
            bool show = alpha > 0.01f;
            if (_root.gameObject.activeSelf != show) _root.gameObject.SetActive(show);
            if (!show) return;
            _root.position = world;
            float parent = _root.parent != null ? _root.parent.lossyScale.x : 1f;
            _root.localScale = Vector3.one * (size / Mathf.Max(1e-5f, parent));
            Vector3 away = world - head;
            if (away.sqrMagnitude > 1e-6f) _root.rotation = Quaternion.LookRotation(away, Vector3.up);
            Paint(_shadow, Shadow, alpha);
            Paint(_fingers, Finger, alpha);
            Paint(_dot, Dot, alpha);
        }

        public void Hide()
        {
            if (_root.gameObject.activeSelf) _root.gameObject.SetActive(false);
        }

        void Paint(MeshRenderer r, Color c, float alpha)
        {
            c.a = alpha;
            _block.Clear();
            _block.SetColor(BackColorId, c);
            _block.SetColor(BorderColorId, c);
            _block.SetFloat(FillId, 0f);
            _block.SetFloat(AspectId, 1f);
            _block.SetFloat(RadiusId, 0.1f);
            _block.SetFloat(BorderId, 0.02f);
            r.SetPropertyBlock(_block);
        }

        /// <summary>The icon's spot this frame, from <see cref="Spot"/>, sticking to the last clear pick.</summary>
        public Vector3 Place(Vector3 target, float radius, float gap, float iconRadius, Vector3 head, Vector3? explorer, float explorerRadius) =>
            Spot(target, radius, gap, iconRadius, head, explorer, explorerRadius, ref _pick);

        /// <summary>Is <paramref name="world"/>'s sphere in the line from the head to the icon (pure)?</summary>
        public static bool Covers(Vector3 head, Vector3 icon, float iconRadius, Vector3 world, float radius)
        {
            Vector3 line = icon - head;
            float along = Vector3.Dot(world - head, line.normalized);
            if (along <= 0f || along >= line.magnitude + radius) return false;
            return (head + line.normalized * along - world).magnitude < radius + iconRadius;
        }

        /// <summary>
        /// Just above the target, or to its side, then higher, whichever covers neither the target's middle nor the explorer
        /// from the head. Keeps <paramref name="pick"/> while it stays clear, so the icon doesn't flip between frames (pure).
        /// </summary>
        public static Vector3 Spot(Vector3 target, float radius, float gap, float iconRadius, Vector3 head, Vector3? explorer, float explorerRadius, ref int pick)
        {
            Vector3 toHead = head - target;
            toHead.y = 0f;
            Vector3 right = toHead.sqrMagnitude > 1e-6f ? Vector3.Cross(Vector3.up, toHead.normalized) : Vector3.right;
            float up = radius * 0.7f + gap;
            var spots = new[]
            {
                target + Vector3.up * up,
                target + Vector3.up * up + right * (radius + gap),
                target + Vector3.up * up - right * (radius + gap),
                target + Vector3.up * (up + radius),
            };
            bool Clear(int i) => !Covers(head, spots[i], iconRadius, target, radius * 0.5f) &&
                                 (!explorer.HasValue || !Covers(head, spots[i], iconRadius, explorer.Value, explorerRadius));
            if (pick >= 0 && pick < spots.Length && Clear(pick)) return spots[pick];
            for (int i = 0; i < spots.Length; i++)
                if (Clear(i))
                {
                    pick = i;
                    return spots[i];
                }
            pick = 3;
            return spots[3];
        }

        // ---- the meshes: every uv at the middle = the HUD material's back colour ----

        static Mesh Build(string name, Vector3[] v, int[] t)
        {
            var uv = new Vector2[v.Length];
            for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(0.5f, 0.5f);
            var m = new Mesh { name = name, vertices = v, uv = uv, triangles = t };
            m.RecalculateBounds();
            return m;
        }

        static Mesh DiscMesh(Vector3 centre, float r, int n)
        {
            var v = new Vector3[n + 1];
            var t = new int[n * 6];
            v[0] = centre;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                v[i + 1] = centre + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r;
                int b = i + 1, c = (i + 1) % n + 1;
                t[i * 6] = 0; t[i * 6 + 1] = b; t[i * 6 + 2] = c;
                t[i * 6 + 3] = 0; t[i * 6 + 4] = c; t[i * 6 + 5] = b;
            }
            return Build("Pinch Dot", v, t);
        }

        /// <summary>
        /// Step 33 (friends read the old two-finger mark as a "V"): a pinch seen from the side: a small palm on the left, the
        /// index finger coming down from its top and the thumb up from its bottom, their tips meeting at the gold dot.
        /// </summary>
        static Mesh FingersMesh()
        {
            var v = new System.Collections.Generic.List<Vector3>();
            var t = new System.Collections.Generic.List<int>();
            void Tri(int i0, int i1, int i2)
            {
                t.Add(i0); t.Add(i1); t.Add(i2);
                t.Add(i0); t.Add(i2); t.Add(i1); // both windings
            }
            void Finger(Vector3 from, Vector3 to, float w0, float w1)
            {
                Vector3 dir = (to - from).normalized, side = new Vector3(-dir.y, dir.x, 0f);
                int o = v.Count;
                v.Add(from + side * w0); v.Add(from - side * w0); v.Add(to + side * w1); v.Add(to - side * w1);
                Tri(o, o + 2, o + 1);
                Tri(o + 1, o + 2, o + 3);
                // a round fingertip
                int c = v.Count;
                v.Add(to);
                const int n = 8;
                for (int i = 0; i <= n; i++)
                {
                    float ang = Mathf.Atan2(side.y, side.x) - Mathf.PI * i / n;
                    v.Add(to + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * w1);
                    if (i > 0) Tri(c, c + i, c + i + 1);
                }
            }
            // The palm: a rounded blob the fingers grow from.
            var palm = new Vector3(-0.42f, 0f, 0f);
            int p0 = v.Count;
            v.Add(palm);
            const int pn = 18;
            for (int i = 0; i < pn; i++)
            {
                float ang = i * Mathf.PI * 2f / pn;
                v.Add(palm + new Vector3(Mathf.Cos(ang) * 0.2f, Mathf.Sin(ang) * 0.24f, 0f));
                Tri(p0, p0 + 1 + i, p0 + 1 + (i + 1) % pn);
            }
            Finger(new Vector3(-0.38f, 0.2f, 0f), new Vector3(0.08f, 0.07f, 0f), 0.085f, 0.06f); // index, from above
            Finger(new Vector3(-0.34f, -0.18f, 0f), new Vector3(0.08f, -0.07f, 0f), 0.09f, 0.065f); // thumb, from below
            return Build("Pinch Fingers", v.ToArray(), t.ToArray());
        }
    }
}
