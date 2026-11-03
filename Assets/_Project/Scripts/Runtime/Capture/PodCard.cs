using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Small stats card in the dialogue bubble style (name, level, element, HP bar) shown above a pod for a few seconds.
    /// Fixed real-world size, faces the head, follows its pod (an anchor function). Built at runtime by <see cref="PodRack"/>.
    /// Rows, top to bottom: name (auto-sized to fit), "Lv 5 · Fire", HP bar, the gold XP bar with the next level at its
    /// end ("→ 6", step 9f), "HP 38/38". A name too long even at the
    /// smallest size widens the panel (up to <see cref="MaxWiden"/>) instead of clipping.
    /// </summary>
    public class PodCard
    {
        static readonly int BackColorId = Shader.PropertyToID("_BackColor");
        static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int FillColorId = Shader.PropertyToID("_FillColor");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int BorderId = Shader.PropertyToID("_Border");

        static readonly Color Back = new Color(1f, 0.965f, 0.89f, 0.97f);
        static readonly Color Ink = new Color(0.23f, 0.16f, 0.12f, 1f);
        static readonly Color Green = new Color(0.44f, 0.82f, 0.42f), Yellow = new Color(0.95f, 0.79f, 0.3f), Red = new Color(0.94f, 0.36f, 0.29f);
        static readonly Color Gold = new Color(1f, 0.8f, 0.3f);

        // Step 14f: card units; the card is drawn K times bigger so every line meets the readable size (TextRule): the
        // name, "Lv 8 · Water" and the HP line all at NameSize; the XP bar's "-> next level" label was dropped.
        const float Width = 0.085f, Height = 0.072f, BarWidth = 0.068f, BarHeight = 0.0075f;
        const float XpWidth = BarWidth, XpHeight = 0.004f, XpY = -0.0275f;
        const float Pad = 0.006f, NameSize = 0.12f, MinNameShare = 1f, MaxWiden = 3f, SmallSize = NameSize;
        static float K => Mathf.Max(1f, TextRule.WorldFont / NameSize);

        readonly Transform _root;
        readonly MeshRenderer _panel, _bar, _xp;
        readonly TextMeshPro _name, _info, _hp, _next;
        readonly ProgressionConfig _progression;
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        float _until, _appear;
        System.Func<Vector3?> _anchor;

        /// <summary>Half the panel height (m): the anchor is the panel's middle.</summary>
        public static float HalfHeight => Height * 0.5f * K;

        public IslingInstance Showing { get; private set; }
        public bool Visible => _root.gameObject.activeSelf;

        public PodCard(Material hud, Mesh quad, ProgressionConfig progression)
        {
            _progression = progression;
            _root = new GameObject("Pod Card").transform;
            _panel = Quad("Panel", hud, quad, new Vector3(Width, Height, 1f), Vector3.zero, 0);
            SetPanelWidth(Width);
            _bar = Quad("HP Bar", hud, quad, new Vector3(BarWidth, BarHeight, 1f), new Vector3(0f, -0.0055f, -0.0005f), 1);
            _xp = Quad("XP Bar", hud, quad, new Vector3(XpWidth, XpHeight, 1f), new Vector3(0f, XpY, -0.0005f), 1);
            _name = Text("Name", NameSize, new Vector3(0f, 0.024f, -0.001f), 0.014f);
            _info = Text("Info", SmallSize, new Vector3(0f, 0.0095f, -0.001f), 0.012f);
            _hp = Text("HP", SmallSize, new Vector3(0f, -0.0175f, -0.001f), 0.012f);
            _next = Text("Next", SmallSize * 0.85f, new Vector3(XpWidth * 0.5f + 0.002f, XpY, -0.001f), 0.008f);
            _next.alignment = TextAlignmentOptions.Center;
            _next.rectTransform.sizeDelta = new Vector2(BarWidth - XpWidth + 0.002f, 0.008f);
            _root.gameObject.SetActive(false);
        }

        void SetPanelWidth(float width)
        {
            _panel.transform.localScale = new Vector3(width, Height, 1f);
            var block = new MaterialPropertyBlock();
            block.SetColor(BackColorId, Back);
            block.SetColor(BorderColorId, Ink);
            block.SetFloat(FillId, 0f);
            block.SetFloat(AspectId, width / Height);
            block.SetFloat(RadiusId, 0.25f);
            block.SetFloat(BorderId, 0.045f);
            _panel.SetPropertyBlock(block);
        }

        /// <summary>
        /// Sets the name at the largest size that fits the text box (down to <see cref="MinNameShare"/> of full size) and
        /// returns the panel width: the standard width, or wider (capped at <see cref="MaxWiden"/>) when even the smallest
        /// size doesn't fit. Sized by measuring, not TMP auto-size (that dropped straight to the minimum).
        /// </summary>
        float FitName(string name)
        {
            string text = $"<b>{name}</b>";
            _name.enableAutoSizing = false;
            _name.fontSize = NameSize;
            float full = _name.GetPreferredValues(text).x;
            float box = Width - 2f * Pad;
            float share = full > 1e-5f ? Mathf.Clamp(box / full * 0.98f, MinNameShare, 1f) : 1f;
            float width = Mathf.Clamp(full * share + 2f * Pad, Width, Width * MaxWiden);
            _name.fontSize = NameSize * share;
            _name.rectTransform.sizeDelta = new Vector2(width - 2f * Pad, _name.rectTransform.sizeDelta.y);
            _name.text = text;
            _name.ForceMeshUpdate();
            return width;
        }

        /// <summary>Panel width and name font size for the current card (Editor check of long names).</summary>
        public string Describe() => $"panel {_panel.transform.localScale.x * 100f:0.0} cm, name size {_name.fontSize / NameSize:P0}, " +
                                    $"box {_name.rectTransform.rect.width * 100f:0.0}x{_name.rectTransform.rect.height * 100f:0.0} cm, text {_name.textBounds.size.x * 100f:0.0}x{_name.textBounds.size.y * 100f:0.0} cm";

        MeshRenderer Quad(string name, Material hud, Mesh quad, Vector3 scale, Vector3 position, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.localScale = scale;
            go.transform.localPosition = position;
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = hud;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = order;
            return r;
        }

        TextMeshPro Text(string name, float size, Vector3 position, float boxHeight)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.localPosition = position;
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.margin = Vector4.zero;
            t.color = Ink;
            t.rectTransform.sizeDelta = new Vector2(Width - 2f * Pad, boxHeight);
            t.sortingOrder = 2;
            return t;
        }

        /// <summary>Shows the card for this creature at <paramref name="anchor"/> (followed; null = hide) for <paramref name="seconds"/>.</summary>
        public void Show(IslingInstance c, System.Func<Vector3?> anchor, float seconds)
        {
            if (c == null || anchor == null) return;
            Showing = c;
            _anchor = anchor;
            _until = Time.time + seconds;
            if (!Visible) _appear = 0f;
            _root.gameObject.SetActive(true);
            string state = c.IsFainted ? " · <color=#B04030>Fainted</color>" : "";
            _info.text = $"Lv {c.level} · {c.Element}{state}";
            _hp.text = $"HP {c.currentHp}/{c.MaxHp}";
            // Step 14f: as wide as the widest line (the name never shrinks below the readable size).
            float width = FitName(c.Name);
            width = Mathf.Max(width, Mathf.Max(_info.GetPreferredValues(_info.text).x, _hp.GetPreferredValues(_hp.text).x) + 2f * Pad);
            _info.rectTransform.sizeDelta = _hp.rectTransform.sizeDelta = new Vector2(width - 2f * Pad, 0.012f);
            SetPanelWidth(width);
            float f = c.MaxHp > 0 ? Mathf.Clamp01(c.currentHp / (float)c.MaxHp) : 0f;
            _block.Clear();
            _block.SetColor(BackColorId, new Color(0.16f, 0.13f, 0.11f, 0.9f));
            _block.SetColor(BorderColorId, Ink);
            _block.SetFloat(FillId, f);
            _block.SetColor(FillColorId, f > 0.5f ? Green : f > 0.25f ? Yellow : Red);
            _block.SetFloat(AspectId, BarWidth / BarHeight);
            _bar.SetPropertyBlock(_block);
            bool xp = _progression != null;
            _xp.gameObject.SetActive(xp);
            _next.gameObject.SetActive(false);
            if (xp)
            {
                bool cap = c.level >= _progression.levelCap;
                _block.SetFloat(FillId, _progression.XpShare(c));
                _block.SetColor(FillColorId, Gold);
                _block.SetFloat(AspectId, XpWidth / XpHeight);
                _xp.SetPropertyBlock(_block);
                _next.text = cap ? "max" : $"→ {c.level + 1}";
            }
            Place();
        }

        public void Hide()
        {
            Showing = null;
            _root.gameObject.SetActive(false);
        }

        public void Tick()
        {
            if (!Visible) return;
            if (Time.time > _until)
            {
                Hide();
                return;
            }
            _appear = Mathf.Min(1f, _appear + Time.deltaTime / 0.15f);
            Place();
        }

        void Place()
        {
            var at = _anchor != null ? _anchor() : null;
            if (!at.HasValue)
            {
                Hide();
                return;
            }
            _root.position = at.Value;
            _root.localScale = Vector3.one * (K * Mathf.SmoothStep(0.6f, 1f, _appear));
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 away = _root.position - cam.transform.position;
            if (away.sqrMagnitude > 1e-6f) _root.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
        }
    }
}
