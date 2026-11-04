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
            _tag.Place(at, scale, fade);
            if (_pic != null) _pic.enabled = fade > 0.05f;
        }

        static float EaseOutBack(float p)
        {
            const float c = 1.7f;
            float q = p - 1f;
            return 1f + (c + 1f) * q * q * q + c * q * q;
        }
    }
}
