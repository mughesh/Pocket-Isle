using System.Collections.Generic;
using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14h: every new Journal entry (a first catch, a new evolved form) pops a small stone tag over the island for
    /// <see cref="Seconds"/>: "New Isling! Toastling · 13 / 46" with its portrait beside it and a soft chime. The very first
    /// one ever: the Spirit's <c>journal.first</c>. Made at runtime by GameSave (nothing to wire).
    /// </summary>
    public class JournalToast : MonoBehaviour
    {
        public const float Seconds = 2.5f;

        IslandBuilder _island;
        StoneTablet _tag;
        MeshRenderer _pic;
        Mesh _quad;
        readonly Queue<IslingSpecies> _queue = new Queue<IslingSpecies>();
        float _start = -10f;

        public static JournalToast Instance { get; private set; }

        /// <summary>The last toast's text (tests, the bridge).</summary>
        public string LastText { get; private set; }

        /// <summary>A toast is up (popping, holding or fading).</summary>
        public bool Up => Time.time - _start < Seconds;

        /// <summary>Nov 11 panel (R50): a toast is up right now; the Spirit's catch line waits for it.</summary>
        public static bool Showing => Instance != null && Instance.Up;

        public static void Ensure(IslandBuilder island)
        {
            if (Instance != null || island == null) return;
            var go = new GameObject("Journal Toast");
            go.transform.SetParent(island.transform, false);
            go.layer = island.gameObject.layer;
            var t = go.AddComponent<JournalToast>();
            t._island = island;
        }

        void Awake()
        {
            Instance = this;
            Journal.NewEntry += OnNew;
        }

        void OnDestroy()
        {
            Journal.NewEntry -= OnNew;
            if (Instance == this) Instance = null;
            if (_tag != null) _tag.Destroy();
            if (_quad != null) Destroy(_quad);
        }

        void OnNew(IslingSpecies s)
        {
            _queue.Enqueue(s);
            if (Time.time - _start >= Seconds && !Busy) Next();
        }

        void Next()
        {
            if (_queue.Count == 0) return;
            var s = _queue.Dequeue();
            var data = JournalData.Load();
            if (data == null) return;
            var hud = HudMaterial();
            if (_tag == null && hud != null) _tag = new StoneTablet("New Isling tag", transform, hud, gameObject.layer);
            int found = Journal.Found(data.entries, Journal.CaughtIds()), total = Journal.Total(data.entries);
            LastText = Journal.ToastLine(s.displayName, found, total);
            if (_tag != null) _tag.Set(LastText.Replace("New Isling!", "<b>New Isling!</b>"), 0.15f, 0.8f, new Vector2(0.018f, 0.012f));
            Portrait(data, s);
            _start = Time.time;
            GameAudio.Ui(AudioIds.SparkChime);
            Dialogue.Say(Journal.FirstLine);
            Debug.Log($"[Journal] Toast: {LastText}");
        }

        /// <summary>An evolution show is playing (its gold name and card): the toast waits for it.</summary>
        static bool Busy => GrowthDirector.Instance != null && GrowthDirector.Instance.Showing;

        static Material HudMaterial()
        {
            var hud = FindFirstObjectByType<BattleHud>();
            return hud != null ? hud.hudMaterial : null;
        }

        void Portrait(JournalData data, IslingSpecies s)
        {
            int i = data.IndexOf(s);
            if (i < 0 || data.portraitMaterial == null || _tag == null) return;
            if (_pic == null)
            {
                var go = new GameObject("Portrait");
                go.layer = gameObject.layer;
                go.transform.SetParent(_tag.transform, false);
                _quad = new Mesh { name = "Toast Portrait" };
                go.AddComponent<MeshFilter>().sharedMesh = _quad;
                _pic = go.AddComponent<MeshRenderer>();
                _pic.sharedMaterial = data.portraitMaterial;
                _pic.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var r = data.CellRect(data.entries[i].cell);
            float h = 0.5f;
            _quad.Clear();
            _quad.SetVertices(new[] { new Vector3(-h, -h, 0f), new Vector3(-h, h, 0f), new Vector3(h, h, 0f), new Vector3(h, -h, 0f) });
            _quad.SetUVs(0, new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin) });
            _quad.SetColors(new[] { Color.white, Color.white, Color.white, Color.white });
            _quad.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            _quad.RecalculateBounds();
            // Beside the tag, on its left, a little taller than the tag.
            var size = _tag.Size;
            float side = size.y * 1.9f;
            _pic.transform.localPosition = new Vector3(-size.x * 0.5f - side * 0.45f, 0f, -0.002f);
            _pic.transform.localScale = new Vector3(side, side, 1f);
        }

        void LateUpdate()
        {
            float u = Time.time - _start;
            if (u >= Seconds)
            {
                if (_tag != null) _tag.Hide();
                if (_queue.Count > 0 && !Busy) Next();
                return;
            }
            if (_tag == null || _island == null) return;
            // Pops up (0.25 s), holds, fades (0.4 s).
            float pop = Mathf.Clamp01(u / 0.25f), fade = Mathf.Clamp01((Seconds - u) / 0.4f);
            float scale = _island.transform.lossyScale.x * (0.6f + 0.4f * EaseOutBack(pop));
            var t = _island.transform;
            Vector3 at = t.position + t.up * (0.16f * t.lossyScale.x / 0.6f) + Vector3.ProjectOnPlane(t.forward, Vector3.up).normalized * (0.08f * t.lossyScale.x / 0.6f);
            at = OverTheExplorer(at, scale);
            _tag.Place(at, scale, fade);
            if (_pic != null) _pic.enabled = fade > 0.05f;
        }

        GestureTestPiece _explorer;

        /// <summary>
        /// Nov 12 (the layout check: "New Isling!" sat on the explorer): rises along the view's up until the tag clears the
        /// explorer on screen (a few steps at most).
        /// </summary>
        Vector3 OverTheExplorer(Vector3 at, float scale)
        {
            var cam = Camera.main;
            if (_explorer == null) _explorer = GameState.Instance != null ? GameState.Instance.explorer : null;
            var body = _explorer != null ? _explorer.Body : null;
            if (cam == null || body == null || !body.gameObject.activeInHierarchy) return at;
            bool any = false;
            var b = new Bounds();
            foreach (var r in body.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled) continue;
                if (!any) b = r.bounds;
                else b.Encapsulate(r.bounds);
                any = true;
            }
            if (!any) return at;
            var size = _tag.Size * scale;
            Vector3 up = cam.transform.up, right = cam.transform.right;
            Rect Screen(Vector3 c, float hw, float hh)
            {
                var a = cam.WorldToViewportPoint(c - right * hw - up * hh);
                var d = cam.WorldToViewportPoint(c + right * hw + up * hh);
                return Rect.MinMaxRect(Mathf.Min(a.x, d.x), Mathf.Min(a.y, d.y), Mathf.Max(a.x, d.x), Mathf.Max(a.y, d.y));
            }
            float x0 = 1f, y0 = 1f, x1 = 0f, y1 = 0f;
            for (int i = 0; i < 8; i++)
            {
                var v = cam.WorldToViewportPoint(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                x0 = Mathf.Min(x0, v.x);
                y0 = Mathf.Min(y0, v.y);
                x1 = Mathf.Max(x1, v.x);
                y1 = Mathf.Max(y1, v.y);
            }
            var me = Rect.MinMaxRect(x0, y0, x1, y1);
            for (int i = 0; i < 12 && Screen(at, size.x * 0.5f, size.y * 0.5f).Overlaps(me); i++) at += up * (size.y * 0.5f);
            return at;
        }

        static float EaseOutBack(float p)
        {
            const float c = 1.7f;
            float q = p - 1f;
            return 1f + (c + 1f) * q * q * q + c * q * q;
        }
    }
}
