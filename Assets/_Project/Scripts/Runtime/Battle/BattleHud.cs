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
    /// Step 14k (Mugesh: too high and too small, Hearthstag's floated far above): every label sits at the bottom, just in
    /// front of its actor's pad at pad level (its plate's bottom on the pad), facing the player, <see cref="labelScale"/>
    /// bigger, still on top on the dark plate. The near Isling's label in front of its pad (toward the runes); the wild /
    /// Keeper's in front of its pad, between the pads, sliding sideways only as far as needed to clear the near Isling on
    /// screen (checked every <see cref="clearCheckSeconds"/>).
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
        [Header("Step 14k: labels at the feet")]
        [Tooltip("Labels this much bigger than the sizes above (name, bars, plate).")]
        public float labelScale = 1.35f;
        [Tooltip("A label stands this far (island-local m) in front of its pad's edge...")]
        public float footGap = 0.006f;
        [Tooltip("...its plate's bottom this far over the pad's top (island-local m).")]
        public float footLift = 0.002f;
        [Tooltip("A label's bottom edge stays over this much of the view's height (0..1).")]
        [Range(0f, 0.4f)] public float minViewportY = 0.06f;
        [Tooltip("How often the wild label re-checks that it clears the near Isling on screen (s).")]
        public float clearCheckSeconds = 0.25f;

        public class Bar
        {
            public Transform root;
            public MeshRenderer quad;
            public TextMeshPro label;
            public StageActor actor;
            public float shown, target;
            /// <summary>Step 16: the lagging "ghost" segment (HP), held a moment after a hit, then drains to shown.</summary>
            public float ghost = -1f, ghostHoldUntil;
            public int max;
            public MeshRenderer xpQuad;
            public float xp = -1f; // 0..1, -1 = no XP bar
            /// <summary>Step 14j: the backing plate; beside the pad (near actor) with this pad radius (island-local).</summary>
            public MeshRenderer plate;
            /// <summary>Step 14k: the near Isling's label (the wild / Keeper's keeps clear of that actor).</summary>
            public bool near;
            public float padRadius, top;
            public bool topSet;
            /// <summary>Step 14k: the pad's radius now (island-local; the battle zoom changes it); null = <see cref="padRadius"/>.</summary>
            public System.Func<float> padRadiusNow;
            /// <summary>Step 14k: sideways slide (island-local x) that keeps the wild label off the near Isling.</summary>
            public float shift, shiftTarget, checkedAt = -1f;
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
        static readonly int GhostId = Shader.PropertyToID("_Ghost"), GhostColorId = Shader.PropertyToID("_GhostColor");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly Color Green = new Color(0.44f, 0.82f, 0.42f), Yellow = new Color(0.95f, 0.79f, 0.3f), Red = new Color(0.94f, 0.36f, 0.29f);
        static readonly Color XpGold = new Color(1f, 0.8f, 0.3f);

        readonly List<Bar> _bars = new List<Bar>();
        readonly List<Pop> _pops = new List<Pop>();
        readonly List<TextMeshPro> _freeText = new List<TextMeshPro>();
        MaterialPropertyBlock _block;
        Mesh _quad;
        Material _plateMat, _barMat, _textMat;

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
        /// An HP bar with name and level for <paramref name="actor"/>, in front of its pad (<paramref name="padRadius"/>,
        /// island-local, or <paramref name="padRadiusNow"/> live). <paramref name="near"/> = the party's Isling.
        /// </summary>
        public Bar AddBar(StageActor actor, string name, int level, int hp, int max, float padRadius = 0f, bool near = false, System.Func<float> padRadiusNow = null)
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
            bar.near = near;
            bar.padRadius = padRadius;
            bar.padRadiusNow = padRadiusNow;
            root.localScale = Vector3.one * labelScale;
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
            if (bar == null) return;
            int t = Mathf.Clamp(hp, 0, bar.max);
            // Step 16: a drop leaves a pale segment that lingers, then follows.
            if (t < bar.target)
            {
                bar.ghost = Mathf.Max(bar.ghost, bar.shown);
                bar.ghostHoldUntil = Time.time + ghostHold;
            }
            bar.target = t;
        }

        [Header("Step 16: HP bars")]
        [Tooltip("The lagging segment waits this long after a hit (s), then drains at ghostDrain x max per second.")]
        public float ghostHold = 0.4f;
        public float ghostDrain = 0.6f;
        [Tooltip("Below this share the fill pulses red softly.")]
        public float lowShare = 0.25f;
        public Color ghostColor = new Color(1f, 0.93f, 0.75f, 0.85f);

        /// <summary>Step 16: the ghost segment's next value (pure): holds, then drains toward shown, never below it.</summary>
        public static float NextGhost(float ghost, float shown, float now, float holdUntil, float rate, float dt) =>
            ghost < 0f ? -1f : now < holdUntil ? Mathf.Max(ghost, shown) : Mathf.Max(shown, ghost - rate * dt);

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
                bool paint = false;
                if (!Mathf.Approximately(b.shown, b.target))
                {
                    // Step 16: drains smoothly (eased: fast first, settling), not at a flat rate.
                    b.shown = Mathf.MoveTowards(b.shown, b.target, Time.deltaTime * Mathf.Max(b.max * 0.5f, Mathf.Abs(b.target - b.shown) * 4f));
                    paint = true;
                }
                if (b.ghost >= 0f)
                {
                    b.ghost = NextGhost(b.ghost, b.shown, Time.time, b.ghostHoldUntil, ghostDrain * b.max, Time.deltaTime);
                    if (b.ghost <= b.shown + 0.01f) b.ghost = -1f;
                    paint = true;
                }
                if (b.shown / Mathf.Max(1, b.max) < lowShare && b.shown > 0f) paint = true;
                if (paint) Paint(b);
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

        /// <summary>Step 14k: the plate's bottom and top (root-local, before <see cref="labelScale"/>) and half width.</summary>
        void PlateExtents(out float bottom, out float top, out float halfW)
        {
            float xpH = barSize.y * xpBarHeight + 0.0012f;
            bottom = -barSize.y * 0.5f - xpH - platePadding.y;
            top = barSize.y * 0.5f + labelFontSize * 0.11f + platePadding.y;
            halfW = barSize.x * 0.5f + platePadding.x;
        }

        /// <summary>
        /// Step 14k: where a label goes: just in front of its actor's pad, its plate's bottom at pad level, facing the head
        /// (<see cref="Face"/>); the wild / Keeper's slides sideways off the near Isling on screen. Kept inside the view.
        /// </summary>
        void Place(Bar b, Camera cam)
        {
            var a = b.actor;
            var island = a.island;
            if (island == null || island.Content == null || cam == null)
            {
                b.root.localPosition = a.LocalTop + Vector3.up * barGap;
                return;
            }
            var it = island.transform;
            float scale = Mathf.Max(1e-5f, it.lossyScale.y) * labelScale;
            PlateExtents(out float bottom, out float top, out float halfW);
            float padR = b.padRadiusNow != null ? b.padRadiusNow() : b.padRadius;
            Vector3 feet = island.TileToLocal(a.tile, island.Content.capHeight + a.baseLift);
            Vector3 up = cam.transform.up;
            Vector3 At(float shift) => it.TransformPoint(feet + new Vector3(shift, footLift, padR + footGap)) + up * (-bottom * scale);

            if (!b.near && Time.time - b.checkedAt >= clearCheckSeconds)
            {
                b.checkedAt = Time.time;
                b.shiftTarget = ClearShift(cam, At, halfW * scale, (top - bottom) * 0.5f * scale, (top + bottom) * 0.5f * scale);
            }
            b.shift = b.near ? 0f : b.checkedAt < 0f ? b.shiftTarget : Mathf.MoveTowards(b.shift, b.shiftTarget, Time.deltaTime * 0.6f);
            Vector3 world = At(b.shift);
            // Keep it in view: its top under maxViewportY, its bottom over minViewportY.
            float above = top * scale, below = -bottom * scale;
            Vector3 v = cam.WorldToViewportPoint(world + up * above);
            if (v.z > 0f && v.y > maxViewportY) world = cam.ViewportToWorldPoint(new Vector3(v.x, maxViewportY, v.z)) - up * above;
            v = cam.WorldToViewportPoint(world - up * below);
            if (v.z > 0f && v.y < minViewportY) world = cam.ViewportToWorldPoint(new Vector3(v.x, minViewportY, v.z)) + up * below;
            // The near label rises over the caption above the runes rather than sit on it.
            if (b.near) world = ClearCaption(cam, world, halfW * scale, above, below);
            b.root.position = world;
        }

        BattleRunes _runes;

        /// <summary>Step 14k: lifts a label (along the view's up) until it clears the runes' caption on screen.</summary>
        Vector3 ClearCaption(Camera cam, Vector3 world, float halfW, float above, float below)
        {
            if (_runes == null) _runes = FindFirstObjectByType<BattleRunes>();
            var cap = _runes != null ? _runes.Caption : null;
            if (cap == null || !cap.gameObject.activeInHierarchy || string.IsNullOrEmpty(cap.text)) return world;
            var r = cap.GetComponent<Renderer>();
            if (r == null || !r.enabled) return world;
            var cr = ScreenRect(cam, r.bounds);
            if (!cr.HasValue) return world;
            var clear = cr.Value;
            clear.yMax += 0.015f; // a little air over the caption
            cr = clear;
            Vector3 up = cam.transform.up, right = cam.transform.right;
            for (int k = 0; k < 40; k++)
            {
                Vector3 lo = cam.WorldToViewportPoint(world - right * halfW - up * below);
                Vector3 hi = cam.WorldToViewportPoint(world + right * halfW + up * above);
                var label = Rect.MinMaxRect(Mathf.Min(lo.x, hi.x), Mathf.Min(lo.y, hi.y), Mathf.Max(lo.x, hi.x), Mathf.Max(lo.y, hi.y));
                if (!label.Overlaps(cr.Value)) break;
                world += up * ((above + below) * 0.1f);
            }
            return world;
        }

        /// <summary>
        /// Step 14k: the smallest sideways slide (island-local x, either way, up to 0.3) that keeps the wild label's screen
        /// rectangle off the near Isling's drawn bounds; 0 when it's clear where it is.
        /// </summary>
        float ClearShift(Camera cam, System.Func<float, Vector3> at, float halfW, float halfH, float midUp)
        {
            StageActor nearActor = null;
            foreach (var o in _bars)
                if (o.near && o.actor != null) nearActor = o.actor;
            if (nearActor == null || !nearActor.TryGetVisualBounds(out var nb)) return 0f;
            var r = ScreenRect(cam, nb);
            if (!r.HasValue) return 0f;
            var rect = r.Value;
            Vector3 right = cam.transform.right, up = cam.transform.up;
            for (int k = 0; k <= 30; k++)
                for (int s = 0; s < 2; s++)
                {
                    if (k == 0 && s == 1) continue;
                    float shift = (s == 0 ? 1f : -1f) * k * 0.01f;
                    Vector3 c = at(shift) + up * midUp;
                    Vector3 lo = cam.WorldToViewportPoint(c - right * halfW - up * halfH);
                    Vector3 hi = cam.WorldToViewportPoint(c + right * halfW + up * halfH);
                    var label = Rect.MinMaxRect(Mathf.Min(lo.x, hi.x), Mathf.Min(lo.y, hi.y), Mathf.Max(lo.x, hi.x), Mathf.Max(lo.y, hi.y));
                    if (!label.Overlaps(rect)) return shift;
                }
            return 0.3f;
        }

        /// <summary>A world box's rectangle on screen (viewport units), null when behind the camera.</summary>
        static Rect? ScreenRect(Camera cam, Bounds bb)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var p = bb.center + Vector3.Scale(bb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                var v = cam.WorldToViewportPoint(p);
                if (v.z <= 0f) return null;
                x0 = Mathf.Min(x0, v.x);
                y0 = Mathf.Min(y0, v.y);
                x1 = Mathf.Max(x1, v.x);
                y1 = Mathf.Max(y1, v.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
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
            Color fillColor = f > 0.5f ? Green : f > lowShare ? Yellow : Red;
            // Step 16: low HP pulses red softly.
            if (f <= lowShare && f > 0f) fillColor = Color.Lerp(Red, new Color(1f, 0.62f, 0.55f), 0.5f + 0.5f * Mathf.Sin(Time.time * 6f));
            _block.SetColor(FillColorId, fillColor);
            _block.SetFloat(GhostId, b.ghost >= 0f ? b.ghost / b.max : 0f);
            _block.SetColor(GhostColorId, ghostColor);
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
