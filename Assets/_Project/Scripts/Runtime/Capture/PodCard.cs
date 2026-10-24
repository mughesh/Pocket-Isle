using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Small stats card in the dialogue bubble style (name, level, element, HP bar) shown above a pod for a few seconds.
    /// Fixed real-world size, faces the head. Built at runtime by <see cref="PodRack"/>; not parented to the island.
    /// Rows, top to bottom: name (auto-sized to fit), "Lv 5 · Fire", HP bar, "HP 38/38". A name too long even at the
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

        const float Width = 0.085f, Height = 0.05f, BarWidth = 0.068f, BarHeight = 0.0075f;
        const float Pad = 0.006f, NameSize = 0.12f, MinNameShare = 0.6f, MaxWiden = 1.4f, SmallSize = 0.075f;

        readonly Transform _root;
        readonly MeshRenderer _panel, _bar;
        readonly TextMeshPro _name, _info, _hp;
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        float _until, _appear;
        Vector3 _anchor;

        public CreatureInstance Showing { get; private set; }
        public bool Visible => _root.gameObject.activeSelf;

        public PodCard(Material hud, Mesh quad)
        {
            _root = new GameObject("Pod Card").transform;
            _panel = Quad("Panel", hud, quad, new Vector3(Width, Height, 1f), Vector3.zero, 0);
            SetPanelWidth(Width);
            _bar = Quad("HP Bar", hud, quad, new Vector3(BarWidth, BarHeight, 1f), new Vector3(0f, -0.0065f, -0.0005f), 1);
            _name = Text("Name", NameSize, new Vector3(0f, 0.0135f, -0.001f), 0.014f);
            _info = Text("Info", SmallSize, new Vector3(0f, 0.0028f, -0.001f), 0.008f);
            _hp = Text("HP", SmallSize, new Vector3(0f, -0.0168f, -0.001f), 0.008f);
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

        /// <summary>Shows the card for this creature above <paramref name="anchorWorld"/> for <paramref name="seconds"/>.</summary>
        public void Show(CreatureInstance c, Vector3 anchorWorld, float seconds)
        {
            if (c == null) return;
            Showing = c;
            _anchor = anchorWorld;
            _until = Time.time + seconds;
            if (!Visible) _appear = 0f;
            _root.gameObject.SetActive(true);
            SetPanelWidth(FitName(c.Name));
            string state = c.IsFainted ? " · <color=#B04030>Fainted</color>" : "";
            _info.text = $"Lv {c.level} · {c.Element}{state}";
            float f = c.MaxHp > 0 ? Mathf.Clamp01(c.currentHp / (float)c.MaxHp) : 0f;
            _block.Clear();
            _block.SetColor(BackColorId, new Color(0.16f, 0.13f, 0.11f, 0.9f));
            _block.SetColor(BorderColorId, Ink);
            _block.SetFloat(FillId, f);
            _block.SetColor(FillColorId, f > 0.5f ? Green : f > 0.25f ? Yellow : Red);
            _block.SetFloat(AspectId, BarWidth / BarHeight);
            _bar.SetPropertyBlock(_block);
            _hp.text = $"HP {c.currentHp}/{c.MaxHp}";
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
            _root.position = _anchor;
            _root.localScale = Vector3.one * Mathf.SmoothStep(0.6f, 1f, _appear);
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
