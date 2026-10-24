using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Small stats card in the dialogue bubble style (name, level, element, HP bar) shown above a pod for a few seconds.
    /// Fixed real-world size, faces the head. Built at runtime by <see cref="PodRack"/>; not parented to the island.
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

        readonly Transform _root;
        readonly MeshRenderer _bar;
        readonly TextMeshPro _title, _hp;
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        float _until, _appear;
        Vector3 _anchor;

        public CreatureInstance Showing { get; private set; }
        public bool Visible => _root.gameObject.activeSelf;

        public PodCard(Material hud, Mesh quad)
        {
            _root = new GameObject("Pod Card").transform;
            var panel = Quad("Panel", hud, quad, new Vector3(Width, Height, 1f), Vector3.zero, 0);
            _block.SetColor(BackColorId, Back);
            _block.SetColor(BorderColorId, Ink);
            _block.SetFloat(FillId, 0f);
            _block.SetFloat(AspectId, Width / Height);
            _block.SetFloat(RadiusId, 0.25f);
            _block.SetFloat(BorderId, 0.045f);
            panel.SetPropertyBlock(_block);
            _bar = Quad("HP Bar", hud, quad, new Vector3(BarWidth, BarHeight, 1f), new Vector3(0f, -0.008f, -0.0005f), 1);
            _title = Text("Title", 0.12f, new Vector3(0f, 0.01f, -0.001f));
            _hp = Text("HP", 0.08f, new Vector3(0f, -0.0175f, -0.001f));
            _root.gameObject.SetActive(false);
        }

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

        TextMeshPro Text(string name, float size, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.localPosition = position;
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.color = Ink;
            t.rectTransform.sizeDelta = new Vector2(Width, 0.03f);
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
            string state = c.IsFainted ? "  <color=#B04030>fainted</color>" : "";
            _title.text = $"<b>{c.Name}</b>  <size=80%>Lv {c.level}</size>\n<size=70%>{c.Element}{state}</size>";
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
