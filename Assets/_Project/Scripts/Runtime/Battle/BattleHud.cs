using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Battle labels on the island: a small HP bar with name and level above each creature, and pop-up text
    /// (damage numbers, "Super effective!", "+XP"). Sizes are island-local metres; everything faces the head.
    /// </summary>
    public class BattleHud : MonoBehaviour
    {
        public Material hudMaterial;
        [Tooltip("HP bar size, island-local metres.")]
        public Vector2 barSize = new Vector2(0.075f, 0.011f);
        [Tooltip("Gap between the creature's head and its bar, island-local metres.")]
        public float barGap = 0.022f;
        public float labelFontSize = 0.11f;
        public float popFontSize = 0.2f;

        public class Bar
        {
            public Transform root;
            public MeshRenderer quad;
            public TextMeshPro label;
            public StageActor actor;
            public float shown, target;
            public int max;
        }

        class Pop
        {
            public TextMeshPro text;
            public Vector3 from;
            public float start, seconds, rise;
            public Color color;
        }

        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int FillColorId = Shader.PropertyToID("_FillColor");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly Color Green = new Color(0.44f, 0.82f, 0.42f), Yellow = new Color(0.95f, 0.79f, 0.3f), Red = new Color(0.94f, 0.36f, 0.29f);

        readonly List<Bar> _bars = new List<Bar>();
        readonly List<Pop> _pops = new List<Pop>();
        readonly List<TextMeshPro> _freeText = new List<TextMeshPro>();
        MaterialPropertyBlock _block;
        Mesh _quad;

        void Awake()
        {
            if (hudMaterial == null) Debug.LogError("[BattleHud] No HUD material assigned.", this);
            _quad = VerticalQuad();
        }

        void OnDestroy() => Destroy(_quad);

        public Bar AddBar(StageActor actor, string name, int level, int hp, int max)
        {
            var root = new GameObject($"HP {name}").transform;
            root.SetParent(transform, false);

            var quadGo = new GameObject("Bar");
            quadGo.transform.SetParent(root, false);
            quadGo.transform.localScale = new Vector3(barSize.x, barSize.y, 1f);
            quadGo.AddComponent<MeshFilter>().sharedMesh = _quad;
            var quad = quadGo.AddComponent<MeshRenderer>();
            quad.sharedMaterial = hudMaterial;
            quad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var label = MakeText(root, labelFontSize);
            label.text = $"<b>{name}</b>  <size=80%>Lv {level}</size>";
            label.transform.localPosition = new Vector3(0f, barSize.y * 0.5f + labelFontSize * 0.055f, 0f);

            var bar = new Bar { root = root, quad = quad, label = label, actor = actor, max = Mathf.Max(1, max), shown = hp, target = hp };
            _bars.Add(bar);
            Paint(bar);
            return bar;
        }

        /// <summary>The bar slides to the new value.</summary>
        public void SetHp(Bar bar, int hp)
        {
            if (bar != null) bar.target = Mathf.Clamp(hp, 0, bar.max);
        }

        public void Clear()
        {
            foreach (var b in _bars)
                if (b.root != null) Destroy(b.root.gameObject);
            _bars.Clear();
            foreach (var p in _pops) Free(p.text);
            _pops.Clear();
        }

        /// <summary>Text that rises and fades at an island-local point.</summary>
        public void PopText(Vector3 local, string text, Color color, float scale = 1f, float seconds = 0.9f)
        {
            var t = _freeText.Count > 0 ? _freeText[_freeText.Count - 1] : MakeText(transform, popFontSize);
            if (_freeText.Count > 0) _freeText.RemoveAt(_freeText.Count - 1);
            t.gameObject.SetActive(true);
            t.fontSize = popFontSize * scale;
            t.text = text;
            t.color = color;
            t.transform.localPosition = local;
            _pops.Add(new Pop { text = t, from = local, start = Time.time, seconds = seconds, rise = 0.025f, color = color });
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            foreach (var b in _bars)
            {
                if (b.root == null || b.actor == null) continue;
                b.root.localPosition = b.actor.LocalTop + Vector3.up * barGap;
                Face(b.root, cam);
                if (!Mathf.Approximately(b.shown, b.target))
                {
                    b.shown = Mathf.MoveTowards(b.shown, b.target, Time.deltaTime * b.max * 1.4f);
                    Paint(b);
                }
            }
            for (int i = _pops.Count - 1; i >= 0; i--)
            {
                var p = _pops[i];
                float u = (Time.time - p.start) / p.seconds;
                if (u >= 1f)
                {
                    Free(p.text);
                    _pops.RemoveAt(i);
                    continue;
                }
                p.text.transform.localPosition = p.from + Vector3.up * (p.rise * Mathf.Sin(u * Mathf.PI * 0.5f));
                var c = p.color;
                c.a = 1f - u * u;
                p.text.color = c;
                Face(p.text.transform, cam);
            }
        }

        void Paint(Bar b)
        {
            _block ??= new MaterialPropertyBlock();
            float f = b.shown / b.max;
            b.quad.GetPropertyBlock(_block);
            _block.SetFloat(FillId, f);
            _block.SetFloat(AspectId, barSize.x / barSize.y);
            _block.SetColor(FillColorId, f > 0.5f ? Green : f > 0.25f ? Yellow : Red);
            b.quad.SetPropertyBlock(_block);
        }

        /// <summary>Turns a label so its front faces the camera.</summary>
        static void Face(Transform t, Camera cam)
        {
            if (cam == null) return;
            Vector3 away = t.position - cam.transform.position;
            if (away.sqrMagnitude > 1e-8f) t.rotation = Quaternion.LookRotation(away, cam.transform.up);
        }

        void Free(TextMeshPro t)
        {
            if (t == null) return;
            t.gameObject.SetActive(false);
            _freeText.Add(t);
        }

        static TextMeshPro MakeText(Transform parent, float size)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.color = Color.white;
            t.outlineWidth = 0.25f;
            t.outlineColor = new Color32(20, 30, 25, 255);
            t.rectTransform.sizeDelta = new Vector2(0.2f, 0.03f);
            return t;
        }

        /// <summary>Unit quad in XY (u runs along +X), centred.</summary>
        internal static Mesh VerticalQuad()
        {
            var mesh = new Mesh { name = "Hud Quad" };
            mesh.SetVertices(new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) });
            mesh.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
            mesh.SetTriangles(new[] { 0, 2, 1, 1, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
