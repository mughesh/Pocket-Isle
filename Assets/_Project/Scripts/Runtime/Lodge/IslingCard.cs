using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The full card for an Isling on the lodge pedestal: name, level, element, HP bar, XP bar, its three moves in their
    /// element colours, and ATK / DEF / SPD. A panel facing the head; sizes in island-local metres. Plain object owned by
    /// <see cref="LodgeRoom"/>.
    /// </summary>
    public class IslingCard
    {
        const float Width = 0.15f, Height = 0.1f, Pad = 0.008f;

        static readonly int BackColorId = Shader.PropertyToID("_BackColor");
        static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int FillColorId = Shader.PropertyToID("_FillColor");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int BorderId = Shader.PropertyToID("_Border");
        static readonly Color Green = new Color(0.44f, 0.82f, 0.42f), Yellow = new Color(0.95f, 0.79f, 0.3f), Red = new Color(0.94f, 0.36f, 0.29f);
        static readonly Color Gold = new Color(1f, 0.8f, 0.3f);

        readonly Transform _root;
        readonly MeshRenderer _panel, _hp, _xp;
        readonly TextMeshPro _title, _hpText, _moves, _stats;
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        float _appear;
        bool _shown;

        public bool Visible => _shown;
        public IslingInstance Showing { get; private set; }

        public IslingCard(Material hud, Mesh quad, Transform parent)
        {
            _root = new GameObject("Isling Card").transform;
            _root.SetParent(parent, false);
            _panel = Quad("Panel", hud, quad, new Vector2(Width, Height), Vector3.forward * 0.001f);
            _hp = Quad("HP", hud, quad, new Vector2(Width - 2f * Pad - 0.03f, 0.008f), new Vector3(-0.015f, 0.015f, 0f));
            _xp = Quad("XP", hud, quad, new Vector2(Width - 2f * Pad, 0.004f), new Vector3(0f, 0.004f, 0f));
            _title = Text("Title", 0.1f, new Vector3(0f, Height * 0.5f - 0.016f, 0f));
            _hpText = Text("HP Text", 0.065f, new Vector3(Width * 0.5f - Pad - 0.014f, 0.015f, 0f));
            _hpText.alignment = TextAlignmentOptions.Right;
            _hpText.rectTransform.sizeDelta = new Vector2(0.03f, 0.012f);
            _moves = Text("Moves", 0.07f, new Vector3(0f, -0.014f, 0f));
            _stats = Text("Stats", 0.07f, new Vector3(0f, -Height * 0.5f + 0.016f, 0f));
            _root.gameObject.SetActive(false);
        }

        MeshRenderer Quad(string name, Material hud, Mesh quad, Vector2 size, Vector3 at)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.localPosition = at;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = hud;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return r;
        }

        TextMeshPro Text(string name, float size, Vector3 at)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.localPosition = at + Vector3.back * 0.001f;
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.color = Color.white;
            t.outlineWidth = 0.2f;
            t.outlineColor = new Color32(20, 30, 25, 255);
            t.rectTransform.sizeDelta = new Vector2(Width - 2f * Pad, 0.016f);
            return t;
        }

        /// <summary>Shows the card for <paramref name="c"/> at an island-local point (its bottom centre).</summary>
        public void Show(IslingInstance c, ProgressionConfig progression, Vector3 local)
        {
            if (c == null || c.species == null) return;
            Showing = c;
            if (!_shown) _appear = 0f;
            _shown = true;
            _root.gameObject.SetActive(true);
            _root.localPosition = local + Vector3.up * (Height * 0.5f);

            var style = ElementStyle.Of(c.Element);
            _title.text = $"<b>{c.Name}</b>  <size=70%>Lv {c.level} · <color=#{ColorUtility.ToHtmlStringRGB(style.main)}>{c.Element}</color></size>";
            _hpText.text = $"{c.currentHp}/{c.MaxHp}";
            var moves = new System.Text.StringBuilder();
            foreach (var m in c.species.moves)
            {
                if (m == null) continue;
                if (moves.Length > 0) moves.Append("  ");
                var col = m.kind == MoveKind.Support ? new Color(0.85f, 0.85f, 0.8f) : ElementStyle.Of(m.element).main;
                moves.Append($"<color=#{ColorUtility.ToHtmlStringRGB(col)}>{m.Name}</color>");
            }
            _moves.text = moves.ToString();
            _stats.text = $"<size=85%>ATK</size> <b>{c.Attack}</b>   <size=85%>DEF</size> <b>{c.Defense}</b>   <size=85%>SPD</size> <b>{c.Speed}</b>";

            Paint(_panel, new Color(0.09f, 0.08f, 0.07f, 0.88f), new Color(0.79f, 0.6f, 0.26f, 1f), Width / Height, 0f, Color.clear, 0.12f, 0.03f);
            float hp = c.currentHp / (float)Mathf.Max(1, c.MaxHp);
            Paint(_hp, new Color(0.15f, 0.15f, 0.14f, 1f), new Color(0f, 0f, 0f, 0.6f), _hp.transform.localScale.x / _hp.transform.localScale.y, hp,
                hp > 0.5f ? Green : hp > 0.25f ? Yellow : Red, 0.5f, 0.12f);
            float xp = progression != null ? progression.XpShare(c) : 0f;
            Paint(_xp, new Color(0.15f, 0.15f, 0.14f, 1f), new Color(0f, 0f, 0f, 0.5f), _xp.transform.localScale.x / _xp.transform.localScale.y, xp, Gold, 0.5f, 0.12f);
        }

        void Paint(MeshRenderer r, Color back, Color border, float aspect, float fill, Color fillColor, float radius, float borderWidth)
        {
            _block.Clear();
            _block.SetColor(BackColorId, back);
            _block.SetColor(BorderColorId, border);
            _block.SetFloat(FillId, fill);
            _block.SetColor(FillColorId, fillColor);
            _block.SetFloat(AspectId, aspect);
            _block.SetFloat(RadiusId, radius);
            _block.SetFloat(BorderId, borderWidth);
            r.SetPropertyBlock(_block);
        }

        public void Hide()
        {
            _shown = false;
            Showing = null;
        }

        /// <summary>Grows in or out and turns to the head (yaw and a little pitch).</summary>
        public void Tick()
        {
            _appear = Mathf.MoveTowards(_appear, _shown ? 1f : 0f, Time.deltaTime / 0.2f);
            bool on = _appear > 0.001f;
            if (_root.gameObject.activeSelf != on) _root.gameObject.SetActive(on);
            if (!on) return;
            _root.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, _appear);
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 away = _root.position - cam.transform.position;
            if (away.sqrMagnitude > 1e-8f) _root.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
        }
    }
}
