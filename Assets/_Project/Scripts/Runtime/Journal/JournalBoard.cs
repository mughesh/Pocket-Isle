using System;
using System.Collections.Generic;
using System.Text;
using Diorama.Audio;
using Oculus.Interaction.Input;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14h: the Isling Journal, a stone book floating over the island like the settings board (old-size rules, the
    /// island visible round it): "Isling Journal", "Found 12 / 46", a tablet per element with its count (Fire, Water,
    /// Wind), a 5 x 4 grid of portraits (caught: full colour, name, "Lv N"; seen: dimmed, name, "Seen"; unseen: a black
    /// silhouette, "???", where it lives or how it evolves) and a card line for the one poked or looked at + pinched.
    /// The portraits are one mesh (one draw); each grid row's labels are one text.
    /// Made at runtime next to the game menu's board (<see cref="Ensure"/>): nothing in the scene to wire.
    /// </summary>
    public class JournalBoard : MonoBehaviour
    {
        public RuneBoard runes;

        public static bool IsOpen { get; private set; }
        public static JournalBoard Instance { get; private set; }

        const int Columns = 5, RowsPerPage = 4;
        const float CellX = 0.066f, CellY = 0.072f, Pic = 0.046f, GridBottom = 0.1f, Width = 0.37f;
        static readonly Vector2 Tab = new Vector2(0.074f, 0.03f);
        const float TabStep = 0.086f;

        JournalData _data;
        Element _element = Element.Fire;
        int _page;
        readonly List<int> _shown = new List<int>(); // entry index per grid cell on this page
        Mesh _mesh;
        Transform _grid;
        TextMeshPro _card;
        Action _closed;
        int _hover = -1, _picked = -1;
        readonly bool[] _wasPinching = { true, true };
        readonly int[] _poked = { -1, -1 };
        float _built;

        /// <summary>The Journal board, made once beside <paramref name="template"/> (the game menu's board) with its settings.</summary>
        public static JournalBoard Ensure(RuneBoard template)
        {
            if (Instance != null) return Instance;
            if (template == null) return null;
            var go = new GameObject("Journal Board");
            go.SetActive(false);
            go.transform.SetParent(template.transform.parent, false);
            go.transform.SetLocalPositionAndRotation(template.transform.localPosition, template.transform.localRotation);
            go.layer = template.gameObject.layer;
            var board = go.AddComponent<RuneBoard>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(template), board);
            // Taller than the menu: in front of the near rim and nearly upright, so the near trees never cut into it.
            board.offset = template.offset + new Vector3(0f, 0.01f, 0.09f);
            board.maxTilt = Mathf.Min(template.maxTilt, 18f);
            var j = go.AddComponent<JournalBoard>();
            j.runes = board;
            go.SetActive(true);
            return j;
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            IsOpen = false;
            if (_mesh != null) Destroy(_mesh);
        }

        // ---- open, close ----

        /// <summary>Opens on the first element with Islings found (Fire first); <paramref name="closed"/> runs on Close.</summary>
        public void Open(Action closed)
        {
            _data = JournalData.Load();
            if (_data == null || runes == null)
            {
                closed?.Invoke();
                return;
            }
            _closed = closed;
            IsOpen = true;
            _picked = -1;
            _page = 0;
            GameAudio.Ui(AudioIds.MenuOpen);
            Build();
            var caught = Journal.CaughtIds();
            Debug.Log($"[Journal] Open: found {Journal.Found(_data.entries, caught)} / {Journal.Total(_data.entries)} " +
                      $"(Fire {Count(Element.Fire)}, Water {Count(Element.Water)}, Wind {Count(Element.Wind)}).");
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            GameAudio.Ui(AudioIds.MenuClose);
            runes.Hide();
            runes.Clear();
            _grid = null;
            _card = null;
            Debug.Log("[Journal] Closed.");
            var c = _closed;
            _closed = null;
            c?.Invoke();
        }

        string Count(Element e) => $"{Journal.Found(_data.entries, Journal.CaughtIds(), e)}/{Journal.Total(_data.entries, e)}";

        // ---- building ----

        void Build()
        {
            runes.Clear();
            var caught = Journal.CaughtIds();
            _shown.Clear();
            var all = new List<int>();
            for (int i = 0; i < _data.entries.Count; i++)
                if (_data.entries[i].species.element == _element) all.Add(i);
            int perPage = Columns * RowsPerPage, pages = Mathf.Max(1, Mathf.CeilToInt(all.Count / (float)perPage));
            _page = Mathf.Clamp(_page, 0, pages - 1);
            for (int i = _page * perPage; i < all.Count && i < (_page + 1) * perPage; i++) _shown.Add(all[i]);

            float top = GridBottom + RowsPerPage * CellY;
            // The book: one big stone slab (opaque, writes depth, so nothing on the island shows through it).
            var bookCenter = new Vector2(0f, (top + 0.05f) * 0.5f - 0.012f);
            var bookSize = new Vector2(Width, top + 0.075f);
            var book = runes.AddPart("Book", bookCenter, bookSize);
            book.localPosition = new Vector3(bookCenter.x, bookCenter.y, 0.008f);
            if (runes.tabletMaterial != null)
            {
                var slab = TabletKit.Make(book, runes.tabletMaterial, bookSize, TabletKit.GoldInlay * 0.8f);
                slab.Paint(0f, 0.35f, 0f, 0f);
            }
            else runes.AddBacking(bookCenter, bookSize);
            // A text's centre is its rect's centre: left / right aligned text sits inside the rect.
            runes.AddText("<b>Isling Journal</b>", new Vector2(-Width * 0.5f + 0.02f + 0.1f, top + 0.012f), runes.fontSize * 1.05f, TextAlignmentOptions.Left, 0.2f);
            runes.AddText($"Found <b>{Journal.Found(_data.entries, caught)} / {Journal.Total(_data.entries)}</b>",
                new Vector2(Width * 0.5f - 0.02f - 0.08f, top + 0.012f), runes.fontSize * 1.05f, TextAlignmentOptions.Right, 0.16f);

            // Element tablets with their counts, then Close; the element showing is gold.
            for (int k = 0; k < Journal.Elements.Length; k++)
            {
                var e = Journal.Elements[k];
                var r = runes.Add($"{e} {Count(e)}", new Vector2((k - 1.5f) * TabStep, 0f), Tab, () => ShowElement(e));
                r.selected = e == _element;
                r.tint = ElementStyle.Of(e).burst;
            }
            var close = runes.Add("Close", new Vector2(1.5f * TabStep, 0f), Tab, Close);
            close.tint = new Color(0.62f, 0.55f, 0.4f);
            if (pages > 1)
            {
                runes.Add($"Page {_page + 1}/{pages}", new Vector2(Width * 0.5f - 0.04f, GridBottom - 0.03f), new Vector2(0.06f, 0.024f), () =>
                {
                    _page = (_page + 1) % pages;
                    Rebuild();
                });
            }

            _card = runes.AddText(CardText(), new Vector2(0f, 0.047f), runes.fontSize * 0.72f, TextAlignmentOptions.Center, Width - 0.03f);
            _card.textWrappingMode = TextWrappingModes.Normal;
            _card.rectTransform.sizeDelta = new Vector2(Width - 0.03f, 0.04f);

            _grid = runes.AddPart("Portraits", Vector2.zero, new Vector2(Width, top));
            BuildGrid(caught);
            runes.Show(0.02f);
            _built = Time.unscaledTime;
        }

        void Rebuild()
        {
            GameAudio.Ui(AudioIds.LodgePage);
            Build();
        }

        void ShowElement(Element e)
        {
            if (_element == e) return;
            _element = e;
            _page = 0;
            _picked = -1;
            Debug.Log($"[Journal] {e}: {Count(e)}.");
            Rebuild();
        }

        static Vector2 CellCenter(int cell) =>
            new Vector2((cell % Columns - (Columns - 1) * 0.5f) * CellX, GridBottom + (RowsPerPage - 1 - cell / Columns) * CellY + CellY * 0.5f);

        static Vector2 PicCenter(int cell) => CellCenter(cell) + new Vector2(0f, 0.012f);

        /// <summary>One mesh for every portrait (state = vertex colour), one text per row for the names and status lines.</summary>
        void BuildGrid(ICollection<string> caught)
        {
            var seen = Journal.SeenIds();
            var owned = Journal.Owned();
            if (_mesh == null) _mesh = new Mesh { name = "Journal Portraits" };
            _mesh.Clear();
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cols = new List<Color>();
            var tris = new List<int>();
            for (int c = 0; c < _shown.Count; c++)
            {
                var e = _data.entries[_shown[c]];
                var state = Journal.StateOf(e.species, caught, seen);
                Color tint = state == Journal.State.Caught ? Color.white : state == Journal.State.Seen ? new Color(0.5f, 0.5f, 0.5f, 0.9f) : new Color(0.02f, 0.02f, 0.03f, 0.92f);
                var rect = _data.CellRect(e.cell);
                float s = Pic * (c == _hover || c == _picked ? 1.15f : 1f) * 0.5f;
                Vector2 p = PicCenter(c);
                int b = verts.Count;
                verts.Add(new Vector3(p.x - s, p.y - s, 0f));
                verts.Add(new Vector3(p.x - s, p.y + s, 0f));
                verts.Add(new Vector3(p.x + s, p.y + s, 0f));
                verts.Add(new Vector3(p.x + s, p.y - s, 0f));
                uvs.Add(new Vector2(rect.xMin, rect.yMin));
                uvs.Add(new Vector2(rect.xMin, rect.yMax));
                uvs.Add(new Vector2(rect.xMax, rect.yMax));
                uvs.Add(new Vector2(rect.xMax, rect.yMin));
                for (int k = 0; k < 4; k++) cols.Add(tint);
                tris.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            _mesh.SetVertices(verts);
            _mesh.SetUVs(0, uvs);
            _mesh.SetColors(cols);
            _mesh.SetTriangles(tris, 0);
            _mesh.RecalculateBounds();
            if (_grid == null) return;
            var mf = _grid.GetComponent<MeshFilter>();
            if (mf == null)
            {
                mf = _grid.gameObject.AddComponent<MeshFilter>();
                var mr = _grid.gameObject.AddComponent<MeshRenderer>();
                mr.sharedMaterial = _data.portraitMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _grid.localPosition = new Vector3(0f, 0f, -0.002f);
                // Labels: one text per row, columns placed with <pos> from each label's measured width.
                for (int row = 0; row < RowsPerPage; row++)
                {
                    var t = runes.AddText("", new Vector2(0f, CellCenter(row * Columns).y - 0.021f), runes.fontSize * 0.66f, TextAlignmentOptions.Left, Columns * CellX);
                    t.rectTransform.sizeDelta = new Vector2(Columns * CellX, 0.03f);
                    t.lineSpacing = -18f;
                    t.name = $"Row {row + 1} labels";
                    _rows[row] = t;
                }
            }
            mf.sharedMesh = _mesh;
            for (int row = 0; row < RowsPerPage; row++)
            {
                var t = _rows[row];
                if (t == null) continue;
                var names = new StringBuilder();
                var status = new StringBuilder();
                for (int col = 0; col < Columns; col++)
                {
                    int c = row * Columns + col;
                    if (c >= _shown.Count) break;
                    var e = _data.entries[_shown[c]];
                    var state = Journal.StateOf(e.species, caught, seen);
                    string n = Journal.NameLine(e, state);
                    string st = Journal.StatusLine(e, state, Journal.BestLevel(e.species, owned), caught);
                    string dim = state == Journal.State.Caught ? "" : "<color=#B9B4A6>";
                    string nm = Fit(t, $"<b>{n}</b>", 100f);
                    names.Append(At(t, col, nm)).Append(dim).Append(nm).Append(dim.Length > 0 ? "</color>" : "");
                    string sm = Fit(t, st, 80f);
                    status.Append(At(t, col, sm)).Append("<color=#D8C99A>").Append(sm).Append("</color>");
                }
                t.text = names + "\n" + status;
            }
        }

        readonly TextMeshPro[] _rows = new TextMeshPro[RowsPerPage];

        /// <summary>The label at <paramref name="percent"/> size, smaller if it would run past its column.</summary>
        static string Fit(TextMeshPro t, string rich, float percent)
        {
            float w = t.GetPreferredValues($"<size={percent:0}%>{rich}</size>").x;
            float max = CellX * 0.96f;
            if (w > max) percent *= max / w;
            return $"<size={percent:0}%>{rich}</size>";
        }

        /// <summary>A &lt;pos&gt; tag that centres <paramref name="rich"/> under column <paramref name="col"/>.</summary>
        static string At(TextMeshPro t, int col, string rich)
        {
            float w = t.GetPreferredValues(rich).x;
            float width = Columns * CellX;
            float left = (col + 0.5f) * CellX - w * 0.5f;
            return $"<pos={Mathf.Clamp(left / width * 100f, 0f, 100f):0.##}%>";
        }

        string CardText()
        {
            if (_picked < 0 || _picked >= _shown.Count) return "<color=#D8C99A>Poke a portrait, or look at one and pinch.</color>";
            var e = _data.entries[_shown[_picked]];
            var caught = Journal.CaughtIds();
            var state = Journal.StateOf(e.species, caught, Journal.SeenIds());
            var s = e.species;
            string home = e.from != null ? $"Evolves from {(Known(e.from, caught) ? e.from.displayName : "???")}" : string.IsNullOrEmpty(e.hint) ? "A starter" : $"Lives in {e.hint}";
            if (state == Journal.State.Unseen)
                return $"<b>???</b>  ·  {s.element}\n{(e.from != null ? (Known(e.from, caught) ? home : "Evolves from an Isling you haven't found yet") : e.hint == "Rare" ? "A rare Isling" : home)}";
            var moves = new List<string>();
            if (s.moves != null)
                foreach (var m in s.moves)
                    if (m != null) moves.Add(m.Name);
            string evo = s.evolvesTo != null ? $"  ·  Evolves at Lv {s.evolveLevel}" : "";
            return $"<b>{s.displayName}</b>  ·  {s.element}  ·  {home}{evo}\n<color=#D8C99A>{string.Join(", ", moves)}</color>";
        }

        static bool Known(IslingSpecies s, ICollection<string> caught) => caught != null && caught.Contains(SpeciesRegistry.IdOf(s));

        void Pick(int cell, string how)
        {
            if (cell < 0 || cell >= _shown.Count) return;
            _picked = cell == _picked ? -1 : cell;
            GameAudio.Ui(AudioIds.UiPebble);
            if (_card != null) _card.text = CardText();
            BuildGrid(Journal.CaughtIds());
            Debug.Log($"[Journal] {(_picked >= 0 ? _data.entries[_shown[cell]].species.name : "card closed")} ({how}).");
        }

        /// <summary>Bridge: picks grid cell <paramref name="cell"/> on the page showing.</summary>
        public void DebugPick(int cell) => Pick(cell, "bridge");

        /// <summary>Bridge: shows an element's page.</summary>
        public void DebugElement(Element e) => ShowElement(e);

        // ---- poke, look + pinch on a portrait ----

        void Update()
        {
            if (!IsOpen || _grid == null || runes.Root == null || Time.unscaledTime - _built < 0.3f) return;
            var root = runes.Root;
            int hover = -1;
            if (HeadGaze.TryGetRay(out var ray))
            {
                var plane = new Plane(root.forward, root.position);
                if (plane.Raycast(ray, out float d)) hover = CellAt(root.InverseTransformPoint(ray.GetPoint(d)));
            }
            if (hover != _hover)
            {
                _hover = hover;
                BuildGrid(Journal.CaughtIds());
            }
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                bool tracked = HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip);
                int poked = -1;
                if (tracked)
                {
                    var local = root.InverseTransformPoint(tip);
                    if (Mathf.Abs(local.z) < 0.015f) poked = CellAt(local);
                }
                if (poked >= 0 && poked != _poked[h]) Pick(poked, "poke");
                _poked[h] = poked;
                bool pinching = tracked && hand.GetIndexFingerIsPinching();
                if (pinching && !_wasPinching[h] && _hover >= 0) Pick(_hover, "look + pinch");
                _wasPinching[h] = pinching;
            }
        }

        /// <summary>The grid cell under a board-local point (inside the portrait square, a little generous), or -1.</summary>
        int CellAt(Vector3 local)
        {
            for (int c = 0; c < _shown.Count; c++)
            {
                Vector2 p = PicCenter(c);
                if (Mathf.Abs(local.x - p.x) < Pic * 0.62f && Mathf.Abs(local.y - p.y) < Pic * 0.62f) return c;
            }
            return -1;
        }
    }
}
