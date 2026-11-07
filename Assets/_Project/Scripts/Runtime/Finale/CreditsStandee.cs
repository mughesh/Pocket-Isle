using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 17 J: the maker's cut-out standee beside the credits banner: the picture as a paper/acrylic cut-out (its own
    /// alpha), a thin dark backing rim (the same silhouette a little bigger, just behind), a soft shadow lying on the cap
    /// and a gentle bob. Only when <see cref="FinaleDirector.standee"/> is set (an Editor hook assigns
    /// Assets/_Project/Art/Credits/CreditsPortrait.png, the round portrait, when that file exists); otherwise nothing shows, no placeholder.
    /// </summary>
    public class CreditsStandee
    {
        readonly Transform _root, _figure;
        readonly SpriteRenderer _front, _back, _shadow;
        readonly Sprite _sprite;
        readonly float _aspect;

        public CreditsStandee(Transform parent, Texture2D picture, int layer)
        {
            _aspect = picture.width / (float)Mathf.Max(1, picture.height);
            // Pivot at the feet (bottom middle), 1 unit tall.
            _sprite = Sprite.Create(picture, new Rect(0, 0, picture.width, picture.height), new Vector2(0.5f, 0f), picture.height);
            _root = new GameObject("Credits standee").transform;
            _root.gameObject.layer = layer;
            _root.SetParent(parent, false);
            _figure = new GameObject("Figure").transform;
            _figure.gameObject.layer = layer;
            _figure.SetParent(_root, false);
            _front = Make("Cut-out", _figure, Color.white, 2);
            // The backing rim: the same silhouette, a little bigger and darker, just behind (+Z is away from the reader).
            _back = Make("Backing rim", _figure, new Color(0.12f, 0.1f, 0.09f, 1f), 1);
            _back.transform.localScale = new Vector3(1.04f, 1.025f, 1f);
            _back.transform.localPosition = new Vector3(0f, -0.012f, 0.004f);
            // A soft shadow lying on the cap (the silhouette squashed flat, faint).
            _shadow = Make("Shadow", _root, new Color(0f, 0f, 0f, 0.22f), 0);
            _shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _shadow.transform.localScale = new Vector3(0.9f, 0.35f, 1f);
            _shadow.transform.localPosition = new Vector3(0.02f, 0.002f, 0.04f);
            _root.gameObject.SetActive(false);
            Debug.Log($"[CreditsStandee] Standee from {picture.name} ({picture.width}x{picture.height}).");
        }

        SpriteRenderer Make(string name, Transform parent, Color color, int order)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = _sprite;
            r.color = color;
            r.sortingOrder = order;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        /// <summary>Stands it with its feet at <paramref name="feet"/>, <paramref name="height"/> world m tall, facing the reader.</summary>
        public void Place(Vector3 feet, Quaternion facing, float height, float alpha)
        {
            bool on = alpha > 0.01f;
            if (_root.gameObject.activeSelf != on) _root.gameObject.SetActive(on);
            if (!on) return;
            float parentScale = _root.parent != null ? _root.parent.lossyScale.x : 1f;
            _root.SetPositionAndRotation(feet, facing);
            _root.localScale = Vector3.one * (height / Mathf.Max(1e-4f, parentScale));
            // A gentle bob (the figure only; the shadow stays on the cap).
            _figure.localPosition = new Vector3(0f, 0.015f + 0.012f * Mathf.Sin(Time.time * 1.6f), 0f);
            _figure.localRotation = Quaternion.Euler(0f, 0f, 1.5f * Mathf.Sin(Time.time * 0.9f));
            _front.color = new Color(1f, 1f, 1f, alpha);
            _back.color = new Color(0.12f, 0.1f, 0.09f, alpha);
            _shadow.color = new Color(0f, 0f, 0f, 0.22f * alpha);
        }

        public void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
            if (_sprite != null) Object.Destroy(_sprite);
        }

        /// <summary>Width over height of the picture.</summary>
        public float Aspect => _aspect;
    }
}
