using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 13: a small carved stone tablet with text, facing the player (the Hall of Fame's cards, the credits): a rounded
    /// warm-grey panel in the HUD material with a dark edge and the text in TextMeshPro. Plain class; the finale owns it.
    /// </summary>
    public class StoneTablet
    {
        static readonly int BackColorId = Shader.PropertyToID("_BackColor");
        static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int BorderId = Shader.PropertyToID("_Border");

        public static Color Stone = new Color(0.78f, 0.76f, 0.7f, 0.97f);
        public static Color Edge = new Color(0.36f, 0.33f, 0.28f, 1f);
        public static Color Ink = new Color(0.22f, 0.17f, 0.12f, 1f);

        readonly Transform _root;
        readonly MeshRenderer _panel;
        readonly TextMeshPro _text;
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        readonly Mesh _quad;
        Vector2 _size;

        public Transform transform => _root;

        public StoneTablet(string name, Transform parent, Material hud, int layer)
        {
            _root = new GameObject(name).transform;
            _root.gameObject.layer = layer;
            _root.SetParent(parent, false);
            _quad = BattleHud.VerticalQuad();
            var panel = new GameObject("Panel");
            panel.layer = layer;
            panel.transform.SetParent(_root, false);
            panel.AddComponent<MeshFilter>().sharedMesh = _quad;
            _panel = panel.AddComponent<MeshRenderer>();
            _panel.sharedMaterial = hud;
            _panel.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _panel.receiveShadows = false;
            var textGo = new GameObject("Text");
            textGo.layer = layer;
            textGo.transform.SetParent(_root, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            _text = textGo.AddComponent<TextMeshPro>();
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.color = Ink;
            _text.sortingOrder = 2;
            _root.gameObject.SetActive(false);
        }

        /// <summary>The text, its size (TMP font size at 1 m = the bubble's scale) and the width it wraps at (m).</summary>
        public void Set(string text, float fontSize, float maxWidth, Vector2 padding)
        {
            _text.text = text;
            _text.fontSize = fontSize;
            var pref = _text.GetPreferredValues(text, maxWidth, 1f);
            float w = Mathf.Min(maxWidth, pref.x), h = pref.y;
            _text.rectTransform.sizeDelta = new Vector2(w + 0.002f, h + 0.002f);
            _size = new Vector2(w + padding.x * 2f, h + padding.y * 2f);
            _panel.transform.localScale = new Vector3(_size.x, _size.y, 1f);
        }

        /// <summary>Shows it at <paramref name="world"/> facing the camera, <paramref name="scale"/> times its size, faded by alpha.</summary>
        public void Place(Vector3 world, float scale, float alpha)
        {
            bool show = alpha > 0.01f && scale > 0.01f;
            if (_root.gameObject.activeSelf != show) _root.gameObject.SetActive(show);
            if (!show) return;
            _root.position = world;
            _root.localScale = Vector3.one * scale / Mathf.Max(1e-4f, _root.parent != null ? _root.parent.lossyScale.x : 1f);
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 away = world - cam.transform.position;
                if (away.sqrMagnitude > 1e-6f) _root.rotation = Quaternion.LookRotation(away, Vector3.up);
            }
            _block.Clear();
            _block.SetColor(BackColorId, new Color(Stone.r, Stone.g, Stone.b, Stone.a * alpha));
            _block.SetColor(BorderColorId, new Color(Edge.r, Edge.g, Edge.b, alpha));
            _block.SetFloat(FillId, 0f);
            _block.SetFloat(AspectId, _size.x / Mathf.Max(1e-4f, _size.y));
            _block.SetFloat(RadiusId, Mathf.Min(0.45f, 0.012f / Mathf.Max(1e-4f, _size.y)));
            _block.SetFloat(BorderId, Mathf.Clamp(0.0028f / Mathf.Max(1e-4f, _size.y), 0.02f, 0.3f));
            _panel.SetPropertyBlock(_block);
            _text.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
        }

        public void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
            if (_quad != null) Object.Destroy(_quad);
        }
    }
}
