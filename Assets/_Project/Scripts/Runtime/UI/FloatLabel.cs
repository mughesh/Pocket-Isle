using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Nov 13 (Mugesh: say what the healing bloom did): a short line of text that pops up over something, rises a little,
    /// faces the player and fades. Parented to <c>parent</c> (the island) so it moves with it; sizes are in world metres.
    /// </summary>
    public class FloatLabel : MonoBehaviour
    {
        TextMeshPro _text;
        Vector3 _from, _up;
        float _rise, _seconds, _age, _height;
        Color _color;

        /// <summary>Shows <paramref name="line"/> at <paramref name="world"/>, letters <paramref name="height"/> m tall.</summary>
        public static FloatLabel Show(string line, Transform parent, Vector3 world, Vector3 up, float height, Color color, float seconds = 2.2f)
        {
            var go = new GameObject("Float Label");
            if (parent != null) go.transform.SetParent(parent, false);
            var f = go.AddComponent<FloatLabel>();
            f._text = go.AddComponent<TextMeshPro>();
            f._text.text = line;
            f._text.alignment = TextAlignmentOptions.Center;
            f._text.textWrappingMode = TextWrappingModes.NoWrap;
            f._text.fontStyle = FontStyles.Bold;
            f._text.outlineWidth = 0.28f;
            f._text.outlineColor = new Color32(14, 30, 20, 255);
            f._from = parent != null ? parent.InverseTransformPoint(world) : world;
            f._up = (parent != null ? parent.InverseTransformDirection(up) : up).normalized;
            float scale = parent != null ? Mathf.Max(1e-5f, parent.lossyScale.x) : 1f;
            f._height = height / scale;
            // TextMeshPro (3D): font size 10 is about one unit of line height.
            f._text.fontSize = f._height * 10f;
            f._text.rectTransform.sizeDelta = new Vector2(f._height * 30f, f._height * 2f);
            f._rise = f._height * 2.5f;
            f._seconds = Mathf.Max(0.5f, seconds);
            f._color = color;
            f.Place();
            return f;
        }

        void LateUpdate()
        {
            _age += Time.deltaTime;
            if (_age >= _seconds)
            {
                Destroy(gameObject);
                return;
            }
            Place();
        }

        void Place()
        {
            float t = _age / _seconds;
            // Pops in (a quick overshoot), drifts up, fades over the last third.
            float pop = _age < 0.18f ? Mathf.SmoothStep(0.6f, 1.08f, _age / 0.18f) : Mathf.Lerp(1.08f, 1f, Mathf.Clamp01((_age - 0.18f) / 0.15f));
            float fade = t < 0.66f ? 1f : 1f - (t - 0.66f) / 0.34f;
            transform.localPosition = _from + _up * (_rise * Mathf.Sqrt(t));
            transform.localScale = Vector3.one * pop;
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, transform.parent != null ? transform.parent.up : Vector3.up);
            var c = _color;
            c.a *= fade;
            _text.color = c;
        }
    }
}
