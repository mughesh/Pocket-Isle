using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Dev-only measurements in the headset (step 8d): a few lines of text low-left in the view and wire shapes in the
    /// world, set every frame by whoever measures something (the shard contact, palm over the flame). Only in the
    /// Editor and development builds; a release build destroys it at once. Shown from the start; F1 or three quick
    /// right-hand thumb taps toggle it. Callers check <see cref="Active"/> before building strings.
    /// </summary>
    public class DevReadout : MonoBehaviour
    {
        [Tooltip("Diorama/Dev Lines material (wire shapes).")]
        public Material lineMaterial;
        [Tooltip("Optional: three right-hand thumb taps toggle the readout.")]
        public MicrogestureInput input;
        public bool showOnStart = true;
        [Tooltip("Text panel in front of the head (m): x right, y up, z ahead.")]
        public Vector3 panelOffset = new Vector3(-0.1f, -0.11f, 0.45f);
        public float fontSize = 0.09f;

        static DevReadout _instance;
        bool _shown;
        TextMeshPro _text;
        Mesh _mesh;
        readonly SortedDictionary<string, (string text, float time)> _lines = new SortedDictionary<string, (string, float)>();
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Color> _colors = new List<Color>();
        readonly List<int> _indices = new List<int>();
        readonly StringBuilder _sb = new StringBuilder();

        /// <summary>The readout is up: measure and report.</summary>
        public static bool Active => _instance != null && _instance._shown;

        void Awake()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                Destroy(this);
                return;
            }
            _instance = this;
            _shown = showOnStart;
            _mesh = new Mesh { name = "Dev Readout Lines" };
            _mesh.MarkDynamic();
            if (lineMaterial == null) Debug.LogWarning("[DevReadout] No line material: text only.", this);
        }

        void OnEnable()
        {
            if (input != null) input.TapBurst += OnTapBurst;
        }

        void OnDisable()
        {
            if (input != null) input.TapBurst -= OnTapBurst;
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (_mesh != null) Destroy(_mesh);
            if (_text != null) Destroy(_text.gameObject);
        }

        void OnTapBurst(int count, GestureSource source)
        {
            if (count == 3 && source == GestureSource.RightHand) Toggle();
        }

        void Toggle()
        {
            _shown = !_shown;
            Debug.Log($"[DevReadout] {(_shown ? "on" : "off")}.");
        }

        /// <summary>A line of text under <paramref name="key"/> (sorted by key), kept while it is set every frame.</summary>
        public static void Text(string key, string text)
        {
            if (!Active) return;
            _instance._lines[key] = (text, Time.unscaledTime);
        }

        public static void Line(Vector3 a, Vector3 b, Color color)
        {
            if (!Active) return;
            var s = _instance;
            s._indices.Add(s._verts.Count);
            s._verts.Add(a);
            s._colors.Add(color);
            s._indices.Add(s._verts.Count);
            s._verts.Add(b);
            s._colors.Add(color);
        }

        public static void Circle(Vector3 center, Vector3 normal, float radius, Color color, int segments = 24)
        {
            if (!Active || radius <= 0f) return;
            var rot = Quaternion.FromToRotation(Vector3.up, normal.sqrMagnitude > 1e-8f ? normal : Vector3.up);
            Vector3 prev = center + rot * new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector3 p = center + rot * new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                Line(prev, p, color);
                prev = p;
            }
        }

        public static void Sphere(Vector3 center, float radius, Color color)
        {
            Circle(center, Vector3.up, radius, color);
            Circle(center, Vector3.right, radius, color);
            Circle(center, Vector3.forward, radius, color);
        }

        /// <summary>A small cross at a point.</summary>
        public static void Point(Vector3 at, float size, Color color)
        {
            Line(at - Vector3.right * size, at + Vector3.right * size, color);
            Line(at - Vector3.up * size, at + Vector3.up * size, color);
            Line(at - Vector3.forward * size, at + Vector3.forward * size, color);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) Toggle();
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            UpdateText(cam);
            if (_shown && _verts.Count > 0 && lineMaterial != null)
            {
                _mesh.Clear();
                _mesh.SetVertices(_verts);
                _mesh.SetColors(_colors);
                _mesh.SetIndices(_indices, MeshTopology.Lines, 0);
                Graphics.RenderMesh(new RenderParams(lineMaterial) { layer = gameObject.layer }, _mesh, 0, Matrix4x4.identity);
            }
            _verts.Clear();
            _colors.Clear();
            _indices.Clear();
        }

        void UpdateText(Camera cam)
        {
            // Lines not set for a moment go away (their owner stopped measuring).
            _sb.Clear();
            float now = Time.unscaledTime;
            List<string> stale = null;
            foreach (var kv in _lines)
            {
                if (now - kv.Value.time > 0.25f)
                {
                    (stale ??= new List<string>()).Add(kv.Key);
                    continue;
                }
                _sb.AppendLine(kv.Value.text);
            }
            if (stale != null)
                foreach (var k in stale) _lines.Remove(k);
            bool show = _shown && _sb.Length > 0 && cam != null;
            if (show && _text == null) _text = MakeText();
            if (_text == null) return;
            _text.gameObject.SetActive(show);
            if (!show) return;
            if (_text.transform.parent != cam.transform)
            {
                _text.transform.SetParent(cam.transform, false);
                _text.transform.localRotation = Quaternion.identity;
            }
            _text.transform.localPosition = panelOffset;
            _text.fontSize = fontSize;
            _text.text = _sb.ToString();
        }

        TextMeshPro MakeText()
        {
            var go = new GameObject("Dev Readout Text");
            var t = go.AddComponent<TextMeshPro>();
            t.alignment = TextAlignmentOptions.BottomLeft;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.color = new Color(0.75f, 1f, 0.8f);
            t.outlineWidth = 0.25f;
            t.outlineColor = new Color32(0, 0, 0, 255);
            t.rectTransform.pivot = new Vector2(0f, 0f);
            t.rectTransform.sizeDelta = new Vector2(0.3f, 0.12f);
            return t;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _instance = null;
    }
}
