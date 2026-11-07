using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 17 J: the credits as a wide slate banner standing behind the island (back edge, above the cap, facing the
    /// player), the lines rolling slowly upward through it: a few lines visible at once, faded in at the bottom edge and
    /// out at the top (TextMeshPro in 3D has no mask, so the window is an alpha fade per line). Plain class; the finale
    /// owns it. Optional standee beside it: <see cref="CreditsStandee"/>.
    /// </summary>
    public class CreditsBanner
    {
        readonly StoneTablet _panel;
        readonly TextMeshPro[] _lines;
        readonly float _lineHeight, _windowHeight;
        readonly int _visible;

        public Transform transform => _panel.transform;

        /// <summary>How far the lines travel (m before scale): from the first line entering to the last one leaving.</summary>
        public float Distance => (_lines.Length + _visible) * _lineHeight;

        public CreditsBanner(Transform parent, Material hud, int layer, string[] lines, float fontSize, float width, int visible)
        {
            _visible = Mathf.Max(1, visible);
            _panel = new StoneTablet("Credits banner", parent, hud, layer);
            _panel.Slate();
            _lines = new TextMeshPro[lines.Length];
            // One real line's height at this font (blank lines measure as nothing), with a little leading.
            float measured = 0f;
            for (int i = 0; i < lines.Length; i++)
            {
                var go = new GameObject($"Credits line {i}");
                go.layer = layer;
                go.transform.SetParent(_panel.transform, false);
                var t = go.AddComponent<TextMeshPro>();
                if (t.font == null) t.font = TMP_Settings.defaultFontAsset; // under an inactive root TMP never woke up
                t.text = lines[i];
                t.fontSize = fontSize;
                t.alignment = TextAlignmentOptions.Center;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Overflow;
                // A long line shrinks to fit the slate's width instead of running past it.
                t.enableAutoSizing = true;
                t.fontSizeMax = fontSize;
                t.fontSizeMin = fontSize * 0.55f;
                t.color = new Color(1f, 0.95f, 0.84f, 0f);
                t.sortingOrder = 3;
                measured = Mathf.Max(measured, t.GetPreferredValues("Ag", width, 1f).y);
                go.SetActive(false);
                _lines[i] = t;
            }
            _lineHeight = Mathf.Max(0.005f, measured * 1.15f);
            _windowHeight = _lineHeight * _visible;
            _panel.SetBox(new Vector2(width, _windowHeight + 0.03f));
            foreach (var t in _lines) t.rectTransform.sizeDelta = new Vector2(width - 0.02f, _lineHeight);
        }

        /// <summary>
        /// Shows it at <paramref name="world"/> with <paramref name="facing"/> (+Z away from the reader), the roll at
        /// <paramref name="u"/> (0 = every line still below the window, 1 = the last line gone out of the top).
        /// </summary>
        public void Place(Vector3 world, Quaternion facing, float scale, float alpha, float u)
        {
            _panel.Place(world, scale, alpha, facing);
            float top = _windowHeight * 0.5f, bottom = -top;
            float scroll = Mathf.Clamp01(u) * Distance;
            float fade = _lineHeight * 0.5f;
            for (int i = 0; i < _lines.Length; i++)
            {
                var t = _lines[i];
                // Line i starts just under the window and moves up with the scroll.
                float y = bottom - _lineHeight * 0.5f - i * _lineHeight + scroll;
                // Faded in only once the whole line is inside the window, out before it reaches the top edge (nothing shows past the slate).
                float a = Mathf.Clamp01((y - bottom - _lineHeight * 0.5f) / fade) * Mathf.Clamp01((top - y - _lineHeight * 0.5f) / fade);
                a = Mathf.Clamp01(a) * alpha;
                // Faded out entirely: hidden.
                bool on = a > 0.01f;
                if (t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
                if (!on) continue;
                t.transform.localPosition = new Vector3(0f, y, -0.002f);
                t.color = new Color(1f, 0.95f, 0.84f, a);
            }
        }

        public void Hide() => _panel.Hide();

        public void Destroy() => _panel.Destroy();
    }
}
