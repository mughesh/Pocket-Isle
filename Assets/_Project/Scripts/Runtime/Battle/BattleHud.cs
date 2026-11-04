using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Battle labels on the island: a small HP bar with name and level above each creature (the party creature's also
    /// has a thin gold XP bar under it), and pop-up text (damage numbers, "Super effective!", "+XP").
    /// Sizes are island-local metres; everything faces the head.
    /// Step 14j (Mugesh: names and bars inside heads, the Keeper's inside its aura and shield): a label sits above the
    /// actor's full visual bounds (<see cref="StageActor.TryGetVisualTop"/>: body, parts, aura, flames, shield) by
    /// <see cref="barGap"/>, is drawn on top of everything (depth test off, late queue) on a dark backing plate, and stays
    /// under the top of the view (<see cref="maxViewportY"/>). The near actor's label (seen from behind) sits beside its
    /// pad on the near side, away from the explorer, so it never covers the wild one.
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
        [Tooltip("XP bar height relative to the HP bar.")]
        public float xpBarHeight = 0.4f;
        [Header("Step 14j: always readable")]
        [Tooltip("A label's top edge stays under this much of the view's height (0..1, from the bottom).")]
        [Range(0.5f, 1f)] public float maxViewportY = 0.86f;
        [Tooltip("The dark plate behind a label (name + bars), island-local metres of padding.")]
        public Vector2 platePadding = new Vector2(0.008f, 0.005f);
        public Color plateColor = new Color(0.04f, 0.05f, 0.05f, 0.62f);
        [Tooltip("The near actor's label: this far (island-local m) out from its pad's edge, on the side away from the explorer.")]
        public float besideGap = 0.01f;

        public class Bar
        {
            public Transform root;
            public MeshRenderer quad;
            public TextMeshPro label;
            public StageActor actor;
            public float shown, target;
            public int max;
            public MeshRenderer xpQuad;
            public float xp = -1f; // 0..1, -1 = no XP bar
            /// <summary>Step 14j: the backing plate; beside the pad (near actor) with this pad radius (island-local).</summary>
            public MeshRenderer plate;
            public bool beside;
            public float padRadius, top;
            public bool topSet;
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
        static readonly Color XpGold = new Color(1f, 0.8f, 0.3f);

        readonly List<Bar> _bars = new List<Bar>();
        readonly List<Pop> _pops = new List<Pop>();
        readonly List<TextMeshPro> _freeText = new List<TextMeshPro>();
        MaterialPropertyBlock _block;
        Mesh _quad;
        Material _plateMat, _barMat, _textMat;
        Transform _explorer;

        static readonly int BackColorId = Shader.PropertyToID("_BackColor");
        static readonly int ZTestId = Shader.PropertyToID("_ZTest");

        void Awake()
        {
            if (hudMaterial == null) Debug.LogError("[BattleHud] No HUD material assigned.", this);
            _quad = VerticalQuad();
            if (hudMaterial != null)
            {
                // Step 14j: drawn on top: no depth test, after every model and effect (plate, bars, then the text).
                _plateMat = OnTop(hudMaterial, 3990);
                _barMat = OnTop(hudMaterial, 3992);
            }
        }

        static Material OnTop(Material source, int queue)
        {
            var m = new Material(source) { name = source.name + " (on top)", renderQueue = queue };
            m.SetFloat(ZTestId, (float)UnityEngine.Rendering.CompareFunction.Always);
            return m;
        }

        void OnDestroy()
        {
            Destroy(_quad);
            if (_plateMat != null) Destroy(_plateMat);
            if (_barMat != null) Destroy(_barMat);
            if (_textMat != null) Destroy(_textMat);
        }

        /// <summary>
        /// An HP bar with name and level for <paramref name="actor"/>. Step 14j: <paramref name="besidePadRadius"/> &gt; 0
        /// = the near actor: the label sits beside its pad (that radius, island-local) instead of over its head.
        /// </summary>
        public Bar AddBar(StageActor actor, string name, int level, int hp, int max, float besidePadRadius = 0f)
        {
            var root = new GameObject($"HP {name}").transform;
            root.SetParent(transform, false);

            var quadGo = new GameObject("Bar");
            quadGo.transform.SetParent(root, false);
            quadGo.transform.localScale = new Vector3(barSize.x, barSize.y, 1f);
            quadGo.AddComponent<MeshFilter>().sharedMesh = _quad;
            var quad = quadGo.AddComponent<MeshRenderer>();
            quad.sharedMaterial = _barMat != null ? _barMat : hudMaterial;
            quad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var label = MakeText(root, labelFontSize);
            label.text = $"<b>{name}</b>  <size=80%>Lv {level}</size>";
            label.transform.localPosition = new Vector3(0f, barSize.y * 0.5f + labelFontSize * 0.055f, 0f);
            label.fontSharedMaterial = TextOnTop(label);

            var bar = new Bar { root = root, quad = quad, label = label, actor = actor, max = Mathf.Max(1, max), shown = hp, target = hp };
            bar.beside = besidePadRadius > 0f;
            bar.padRadius = besidePadRadius;
            bar.plate = MakePlate(root);
            _bars.Add(bar);
            Paint(bar);
            return bar;
        }

        /// <summary>Removes one bar (a creature left the field).</summary>
        public void RemoveBar(Bar bar)
        {
            if (bar == null) return;
            if (bar.root != null) Destroy(bar.root.gameObject);
            _bars.Remove(bar);
        }

        /// <summary>Adds (or sets) the thin XP bar under an HP bar: 0..1 toward the next level.</summary>
        public void SetXp(Bar bar, float share)
        {
            if (bar == null) return;
            if (bar.xpQuad == null)
            {
                var go = new GameObject("XP");
                go.transform.SetParent(bar.root, false);
                float h = barSize.y * xpBarHeight;
                go.transform.localScale = new Vector3(barSize.x, h, 1f);
                go.transform.localPosition = new Vector3(0f, -(barSize.y + h) * 0.5f - 0.0012f, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = _quad;
                bar.xpQuad = go.AddComponent<MeshRenderer>();
                bar.xpQuad.sharedMaterial = _barMat != null ? _barMat : hudMaterial;
                bar.xpQuad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            bar.xp = Mathf.Clamp01(share);
            _block ??= new MaterialPropertyBlock();
            bar.xpQuad.GetPropertyBlock(_block);
            _block.SetFloat(FillId, bar.xp);
            _block.SetFloat(AspectId, 1f / Mathf.Max(0.05f, xpBarHeight) * barSize.x / barSize.y);
            _block.SetColor(FillColorId, XpGold);
            bar.xpQuad.SetPropertyBlock(_block);
        }

        /// <summary>
        /// Step 9f: grows the XP bar under an HP bar to <paramref name="scale"/> times its size (2 while the XP after a win
        /// fills, 1 = normal), keeping its top edge under the HP bar.
        /// </summary>
        public void SetXpScale(Bar bar, float scale)
        {
            if (bar == null) return;
            if (bar.xpQuad == null) SetXp(bar, Mathf.Max(0f, bar.xp));
            float h = barSize.y * xpBarHeight * scale;
            bar.xpQuad.transform.localScale = new Vector3(barSize.x * scale, h, 1f);
            bar.xpQuad.transform.localPosition = new Vector3(0f, -(barSize.y + h) * 0.5f - 0.0012f, 0f);
        }

        /// <summary>New level and max HP (a level-up or an evolution): the label changes, the HP bar rescales.</summary>
        public void SetLevel(Bar bar, string name, int level, int hp, int max)
        {
            if (bar == null) return;
            bar.label.text = $"<b>{name}</b>  <size=80%>Lv {level}</size>";
            float share = bar.shown / Mathf.Max(1, bar.max);
            bar.max = Mathf.Max(1, max);
            bar.shown = share * bar.max;
            bar.target = Mathf.Clamp(hp, 0, bar.max);
            Paint(bar);
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
                Place(b, cam);
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

        /// <summary>
        /// Step 14j: where a label goes: over the actor's full visual top (the near actor: beside its pad), clamped under
        /// the top of the view. Its top eases up at once and settles down slowly (smoke and flames flicker).
        /// </summary>
        void Place(Bar b, Camera cam)
        {
            var a = b.actor;
            var island = a.island;
            if (island == null || island.Content == null)
            {
                b.root.localPosition = a.LocalTop + Vector3.up * barGap;
                return;
            }
            var it = island.transform;
            float scale = Mathf.Max(1e-5f, it.lossyScale.y);
            Vector3 world;
            if (b.beside)
            {
                // Beside the pad on the near side (+Z faces the player), on the side away from the explorer.
                Vector3 feet = island.TileToLocal(a.tile, island.Content.capHeight + a.baseLift);
                float side = 1f;
                if (_explorer == null)
                {
                    var ev = FindFirstObjectByType<ExplorerView>();
                    if (ev != null) _explorer = ev.transform;
                }
                if (_explorer != null && it.InverseTransformPoint(_explorer.position).x > feet.x) side = -1f;
                float half = barSize.x * 0.5f + platePadding.x;
                // Clear the pad and the body (a wide one, e.g. Tidemother's fins, overhangs its pad).
                float body = a.TryGetVisualBounds(out var vb) ? Mathf.Max(vb.extents.x, vb.extents.z) / scale : 0f;
                float reach = Mathf.Max(b.padRadius, body);
                Vector3 local = feet + new Vector3(side * (reach + besideGap + half), labelFontSize * 0.1f, b.padRadius * 0.6f);
                world = it.TransformPoint(local);
            }
            else
            {
                float top = a.TryGetVisualTop(out float t) ? t : it.TransformPoint(a.LocalTop).y;
                if (!b.topSet || top > b.top) b.top = top;
                else b.top = Mathf.MoveTowards(b.top, top, Time.deltaTime * 0.04f * scale);
                b.topSet = true;
                world = it.TransformPoint(a.LocalTop);
                // The bar's bottom edge (the XP bar's, when there is one) clears the top by the gap.
                float below = barSize.y * 0.5f + (b.xpQuad != null ? barSize.y * xpBarHeight + 0.0012f : 0f) + platePadding.y;
                world.y = b.top + (barGap * 0.5f + below) * scale;
            }
            // Keep it in view: its top edge under maxViewportY of the view's height.
            if (cam != null)
            {
                float above = (barSize.y * 0.5f + labelFontSize * 0.11f + platePadding.y) * scale;
                Vector3 v = cam.WorldToViewportPoint(world + cam.transform.up * above);
                if (v.z > 0f && v.y > maxViewportY)
                    world = cam.ViewportToWorldPoint(new Vector3(v.x, maxViewportY, v.z)) - cam.transform.up * above;
            }
            b.root.position = world;
        }

        /// <summary>Step 14j: the dark plate behind a label's name and bars.</summary>
        MeshRenderer MakePlate(Transform root)
        {
            var go = new GameObject("Plate");
            go.transform.SetParent(root, false);
            float textH = labelFontSize * 0.11f;
            float xpH = barSize.y * xpBarHeight + 0.0012f;
            float bottom = -barSize.y * 0.5f - xpH - platePadding.y, top = barSize.y * 0.5f + textH + platePadding.y;
            go.transform.localPosition = new Vector3(0f, (top + bottom) * 0.5f, 0.0005f);
            go.transform.localScale = new Vector3(barSize.x + platePadding.x * 2f, top - bottom, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = _plateMat != null ? _plateMat : hudMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetFloat(FillId, 0f);
            block.SetColor(BackColorId, plateColor);
            block.SetFloat(AspectId, go.transform.localScale.x / Mathf.Max(1e-4f, go.transform.localScale.y));
            r.SetPropertyBlock(block);
            return r;
        }

        /// <summary>Step 14j: one shared label material drawn on top (no depth test, after the bars).</summary>
        Material TextOnTop(TextMeshPro t)
        {
            if (_textMat == null)
            {
                _textMat = new Material(t.fontSharedMaterial) { name = "Battle Label (on top)", renderQueue = 3995 };
                _textMat.SetFloat(ShaderUtilities.ShaderTag_ZTestMode, (float)UnityEngine.Rendering.CompareFunction.Always);
            }
            return _textMat;
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
        public static Mesh VerticalQuad()
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
