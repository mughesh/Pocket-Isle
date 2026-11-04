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
    /// silhouette, "???", where it lives or how it evolves). Step 14i: poking a portrait (or look + pinch) opens a detail
    /// card beside the book on the same board: a slowly turning 3D model (the portrait if only seen, a silhouette if
    /// unseen), name, element badge, strong / weak vs, where it lives, the evolution line, its moves, your best one's level
    /// and how many you have, and a one-line description (<see cref="JournalLore"/>). Poke it again to close the card.
    /// The portraits are one mesh (one draw).
    /// Step 14k (Mugesh: text too small, gaze not needed here): 4 x 3 portraits per page (pages instead of smaller text),
    /// every label bigger, one text per cell so a hint wraps to 2 lines; input is poke and the hand ray + pinch (no look +
    /// pinch); a fingertip near a portrait or the ray on it pops it (10% bigger, lifted, white rim), like every board tablet.
    /// Made at runtime next to the game menu's board (<see cref="Ensure"/>): nothing in the scene to wire.
    /// </summary>
    public class JournalBoard : MonoBehaviour
    {
        public RuneBoard runes;

        public static bool IsOpen { get; private set; }
        public static JournalBoard Instance { get; private set; }

        public const int Columns = 4, RowsPerPage = 3;
        const float CellX = 0.0875f, CellY = 0.1f, Pic = 0.056f, GridBottom = 0.1f, Width = 0.37f;
        /// <summary>Step 14k: label sizes (x the board's font size; 14j: names 0.66, status ~0.53, help 0.72).</summary>
        public const float NameFont = 1f, StatusFont = 0.8f, HelpFont = 0.95f, HeaderFont = 1.2f;
        const float HoverLift = RuneBoard.HoverLift, HoverGrow = RuneBoard.HoverGrow;
        static readonly Vector2 Tab = new Vector2(0.074f, 0.036f); // step 14k: taller (bigger labels), was 0.03
        static readonly List<float> TabX = new List<float>();
        /// <summary>Step 14j: an unseen Isling: one flat dark slate colour (not black, no shading), the shape only.</summary>
        public static readonly Color SilhouetteColor = new Color(0.17f, 0.19f, 0.25f, 1f).linear; // vertex colours are linear
        const float CardW = 0.29f, CardGap = 0.014f, ModelHeight = 0.075f;

        JournalData _data;
        Element _element = Element.Fire;
        int _page;
        readonly List<int> _shown = new List<int>(); // entry index per grid cell on this page
        Mesh _mesh;
        Transform _grid;
        TextMeshPro _card;
        Transform _model;
        Mesh _cardMesh;
        Action _closed;
        int _hover = -1, _picked = -1;
        readonly bool[] _wasPinching = { true, true };
        readonly int[] _poked = { -1, -1 }, _rayCell = { -1, -1 };
        readonly float[] _pop = new float[Columns * RowsPerPage]; // eased hover per cell
        readonly TextMeshPro[] _labels = new TextMeshPro[Columns * RowsPerPage];
        MeshRenderer _rim;
        ICollection<string> _caught;
        MaterialPropertyBlock _rimBlock;
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
            // Taller than the menu and nearly upright. Nov 4 (Mugesh: it came in front of the menu, big in his face): back over
            // the island, 12 cm behind the near rim, and lifted 8 cm so the near trees stay under it.
            board.offset = template.offset + new Vector3(0f, 0.08f, -0.12f);
            board.maxTilt = Mathf.Min(template.maxTilt, 18f);
            // Step 14k: poke and the hand ray + pinch; no look + pinch here.
            board.gazeSelect = false;
            board.handRays = true;
            var j = go.AddComponent<JournalBoard>();
            j.runes = board;
            board.RayLit = h => j._rayCell[h] >= 0;
            go.SetActive(true);
            return j;
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            IsOpen = false;
            if (_mesh != null) Destroy(_mesh);
            if (_cardMesh != null) Destroy(_cardMesh);
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
            _model = null;
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
            _caught = caught;
            System.Array.Clear(_pop, 0, _pop.Length);
            _hover = -1;
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
            runes.AddText("<b>Isling Journal</b>", new Vector2(-Width * 0.5f + 0.02f + 0.1f, top + 0.014f), runes.fontSize * HeaderFont, TextAlignmentOptions.Left, 0.2f);
            runes.AddText($"Found <b>{Journal.Found(_data.entries, caught)} / {Journal.Total(_data.entries)}</b>",
                new Vector2(Width * 0.5f - 0.02f - 0.08f, top + 0.014f), runes.fontSize * HeaderFont, TextAlignmentOptions.Right, 0.16f);

            // Element tablets with their counts, then Close; the element showing is gold. Step 14j: one row by the layout
            // rule (fixed gaps, sized to fit the book's width).
            var tab = TabletLayout.Row(Journal.Elements.Length + 1, Width - 0.02f, Tab, RuneBoard.HoverGrow, TabX);
            for (int k = 0; k < Journal.Elements.Length; k++)
            {
                var e = Journal.Elements[k];
                var r = runes.Add($"{e} {Count(e)}", new Vector2(TabX[k], 0f), tab, () => ShowElement(e));
                r.selected = e == _element;
                r.tint = ElementStyle.Of(e).burst;
                // Nov 4 (Mugesh: only a coloured border): the tablet's face in its element colour.
                var face = ElementStyle.Of(e).main;
                face.a = 0.85f;
                r.face = face;
            }
            var close = runes.Add("Close", new Vector2(TabX[TabX.Count - 1], 0f), tab, Close);
            close.tint = new Color(0.62f, 0.55f, 0.4f);
            // The help line on the left of the strip between the grid and the tabs, the page tablet on its right.
            const float stripY = (GridBottom + 0.015f) * 0.5f + 0.004f;
            float pageW = 0.084f, helpW = Width - 0.03f - (pages > 1 ? pageW + TabletLayout.MinGap : 0f);
            if (pages > 1)
            {
                runes.Add($"Page {_page + 1}/{pages}", new Vector2(Width * 0.5f - 0.015f - pageW * 0.5f, stripY), new Vector2(pageW, 0.034f), () =>
                {
                    _page = (_page + 1) % pages;
                    _picked = -1;
                    Rebuild();
                });
            }

            _card = runes.AddText(CardText(), new Vector2(-Width * 0.5f + 0.015f + helpW * 0.5f, stripY), runes.fontSize * HelpFont, TextAlignmentOptions.Center, helpW);
            _card.textWrappingMode = TextWrappingModes.Normal;
            _card.rectTransform.sizeDelta = new Vector2(helpW, 0.05f);
            _card.enableAutoSizing = true;
            _card.fontSizeMin = runes.fontSize * 0.75f;
            _card.fontSizeMax = runes.fontSize * HelpFont;

            _grid = runes.AddPart("Portraits", Vector2.zero, new Vector2(Width, top));
            BuildGrid(caught);
            PaintRim();
            _model = null;
            if (_picked >= 0 && _picked < _shown.Count) BuildCard(_data.entries[_shown[_picked]], caught, bookCenter, bookSize);
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

        /// <summary>The portrait sits in the cell's top part; the labels (name, status / hint) fill the rest.</summary>
        static Vector2 PicCenter(int cell) => CellCenter(cell) + new Vector2(0f, CellY * 0.5f - 0.004f - Pic * 0.5f);

        /// <summary>The labels' box under the portrait (cell-relative top, height).</summary>
        static Vector2 LabelCenter(int cell) => CellCenter(cell) + new Vector2(0f, -0.0475f + LabelHeight * 0.5f - 0.003f);

        const float LabelHeight = CellY - Pic - 0.009f;

        /// <summary>One mesh for every portrait (state = vertex colour), one text per cell for its name and status line.</summary>
        void BuildGrid(ICollection<string> caught)
        {
            BuildPictures(caught);
            if (_grid == null) return;
            var seen = Journal.SeenIds();
            var owned = Journal.Owned();
            for (int c = 0; c < _labels.Length; c++)
            {
                var t = _labels[c];
                if (t == null) continue;
                if (c >= _shown.Count)
                {
                    t.text = "";
                    continue;
                }
                var e = _data.entries[_shown[c]];
                var state = Journal.StateOf(e.species, caught, seen);
                string n = Journal.NameLine(e, state);
                string st = Journal.StatusLine(e, state, Journal.BestLevel(e.species, owned), caught);
                string dim = state == Journal.State.Caught ? "" : "<color=#B9B4A6>";
                // The name on one line (shrunk only if it would run past the cell), the status / hint on up to 2 lines.
                t.text = $"<size={NamePercent(t, n):0}%>{dim}<b>{n}</b>{(dim.Length > 0 ? "</color>" : "")}</size>\n" +
                         $"<size={StatusFont / NameFont * 100f:0}%><color=#D8C99A>{st}</color></size>";
            }
        }

        /// <summary>Step 14k: the portraits mesh (with each cell's hover pop: bigger, lifted toward the player).</summary>
        void BuildPictures(ICollection<string> caught)
        {
            var seen = Journal.SeenIds();
            if (_mesh == null) _mesh = new Mesh { name = "Journal Portraits" };
            _mesh.Clear();
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cols = new List<Color>();
            var tris = new List<int>();
            var flags = new List<Vector2>();
            for (int c = 0; c < _shown.Count; c++)
            {
                var e = _data.entries[_shown[c]];
                var state = Journal.StateOf(e.species, caught, seen);
                Color tint = state == Journal.State.Caught ? Color.white : state == Journal.State.Seen ? new Color(0.5f, 0.5f, 0.5f, 0.9f) : SilhouetteColor;
                float flat = state == Journal.State.Unseen ? 1f : 0f;
                var rect = _data.CellRect(e.cell);
                float pop = Mathf.Max(_pop[c], c == _picked ? 1f : 0f);
                float s = Pic * (1f + HoverGrow * pop) * 0.5f, z = -HoverLift * _pop[c];
                Vector2 p = PicCenter(c);
                int b = verts.Count;
                verts.Add(new Vector3(p.x - s, p.y - s, z));
                verts.Add(new Vector3(p.x - s, p.y + s, z));
                verts.Add(new Vector3(p.x + s, p.y + s, z));
                verts.Add(new Vector3(p.x + s, p.y - s, z));
                uvs.Add(new Vector2(rect.xMin, rect.yMin));
                uvs.Add(new Vector2(rect.xMin, rect.yMax));
                uvs.Add(new Vector2(rect.xMax, rect.yMax));
                uvs.Add(new Vector2(rect.xMax, rect.yMin));
                for (int k = 0; k < 4; k++)
                {
                    cols.Add(tint);
                    flags.Add(new Vector2(flat, 0f));
                }
                tris.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            _mesh.SetVertices(verts);
            _mesh.SetUVs(0, uvs);
            _mesh.SetUVs(1, flags);
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
                for (int c = 0; c < _labels.Length; c++)
                {
                    var t = runes.AddText("", LabelCenter(c), runes.fontSize * NameFont, TextAlignmentOptions.Top, CellX * 0.96f);
                    t.rectTransform.sizeDelta = new Vector2(CellX * 0.96f, LabelHeight);
                    t.textWrappingMode = TextWrappingModes.Normal;
                    t.overflowMode = TextOverflowModes.Ellipsis;
                    t.lineSpacing = -14f;
                    t.name = $"Cell {c + 1} label";
                    _labels[c] = t;
                }
                // The hover rim: one white rounded frame behind the popped portrait (the boards' focus look).
                var rimGo = new GameObject("Hover rim");
                rimGo.transform.SetParent(_grid, false);
                rimGo.layer = runes.gameObject.layer;
                rimGo.AddComponent<MeshFilter>().sharedMesh = RimQuad;
                _rim = rimGo.AddComponent<MeshRenderer>();
                _rim.sharedMaterial = runes.hudMaterial;
                _rim.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _rim.enabled = false;
            }
            mf.sharedMesh = _mesh;
        }

        static Mesh _rimQuad;
        static Mesh RimQuad => _rimQuad != null ? _rimQuad : _rimQuad = BattleHud.VerticalQuad();

        /// <summary>The white rim behind the most-popped portrait (fades with its pop).</summary>
        void PaintRim()
        {
            if (_rim == null || runes.hudMaterial == null) return;
            int best = -1;
            for (int c = 0; c < _shown.Count; c++)
                if (_pop[c] > 0.02f && (best < 0 || _pop[c] > _pop[best])) best = c;
            _rim.enabled = best >= 0;
            if (best < 0) return;
            float k = _pop[best], side = Pic * (1f + HoverGrow * k) + 0.005f;
            Vector2 p = PicCenter(best);
            _rim.transform.localPosition = new Vector3(p.x, p.y, 0.001f - HoverLift * k);
            _rim.transform.localScale = new Vector3(side, side, 1f);
            if (_rimBlock == null) _rimBlock = new MaterialPropertyBlock();
            _rimBlock.SetColor("_BackColor", new Color(1f, 1f, 1f, 0.06f * k));
            _rimBlock.SetColor("_BorderColor", new Color(1f, 1f, 1f, 0.95f * k));
            _rimBlock.SetFloat("_Fill", 0f);
            _rimBlock.SetFloat("_Inset", 0f);
            _rimBlock.SetFloat("_Aspect", 1f);
            _rimBlock.SetFloat("_Radius", 0.14f);
            _rimBlock.SetFloat("_Border", 0.06f);
            _rim.SetPropertyBlock(_rimBlock);
        }

        /// <summary>The name's size (percent of the label font): 100, smaller only if it would run past the cell.</summary>
        static float NamePercent(TextMeshPro t, string name)
        {
            float w = t.GetPreferredValues($"<b>{name}</b>").x;
            float max = CellX * 0.94f;
            return w > max ? 100f * max / w : 100f;
        }

        string CardText() => _picked < 0 || _picked >= _shown.Count
            ? "<color=#D8C99A>Poke a portrait, or point and pinch.</color>"
            : "<color=#D8C99A>Poke it again to close its card.</color>";

        static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        static string ElementWord(Element e) => $"<color={Hex(Color.Lerp(ElementStyle.Of(e).main, Color.white, 0.35f))}><b>{e}</b></color>";

        /// <summary>Step 14i: the detail card beside the book (same board, same facing).</summary>
        void BuildCard(JournalData.Entry e, ICollection<string> caught, Vector2 bookCenter, Vector2 bookSize)
        {
            var s = e.species;
            var seen = Journal.SeenIds();
            var state = Journal.StateOf(s, caught, seen);
            float cx = Width * 0.5f + CardGap + CardW * 0.5f;
            var size = new Vector2(CardW, bookSize.y);
            var slabPart = runes.AddPart("Card", new Vector2(cx, bookCenter.y), size);
            slabPart.localPosition = new Vector3(cx, bookCenter.y, 0.008f);
            if (runes.tabletMaterial != null) TabletKit.Make(slabPart, runes.tabletMaterial, size, TabletKit.GoldInlay * 0.8f).Paint(0f, 0.35f, 0f, 0f);
            else runes.AddBacking(new Vector2(cx, bookCenter.y), size);
            float top = bookCenter.y + size.y * 0.5f, left = cx - CardW * 0.5f + 0.014f, inner = CardW - 0.028f;

            // The picture: a turning model when caught, the portrait when seen, a silhouette when not.
            float picY = top - 0.012f - ModelHeight * 0.5f;
            if (state == Journal.State.Caught && s.Prefab != null) MakeModel(s, new Vector2(cx, picY - ModelHeight * 0.45f));
            else CardPortrait(e, state, new Vector2(cx, picY));

            string name = state == Journal.State.Unseen ? "???" : s.displayName;
            var title = runes.AddText($"<b>{name}</b>", new Vector2(cx, picY - ModelHeight * 0.5f - 0.016f), runes.fontSize * 1.15f, TextAlignmentOptions.Center, inner);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            // Element badge: a small tablet with its face in the element's colour (not a button).
            var badgeAt = new Vector2(cx, picY - ModelHeight * 0.5f - 0.042f);
            var badgeSize = new Vector2(0.06f, 0.022f);
            var badgePart = runes.AddPart("Element badge", badgeAt, badgeSize);
            if (runes.tabletMaterial != null)
            {
                var badge = TabletKit.Make(badgePart, runes.tabletMaterial, badgeSize, ElementStyle.Of(s.element).burst);
                var face = ElementStyle.Of(s.element).main;
                face.a = 0.85f;
                badge.face = face;
                badge.Paint(0f, 0f, 0f, 0f);
            }
            var badgeText = runes.AddText($"<b>{s.element}</b>", badgeAt, runes.fontSize * 0.8f, TextAlignmentOptions.Center, badgeSize.x);
            TabletKit.StyleLabel(badgeText);
            // In front of the badge tablet (which sits at the part's depth), as a rune's label sits on its tablet.
            badgeText.transform.localPosition = new Vector3(badgeAt.x, badgeAt.y, badgePart.localPosition.z + TabletKit.LabelZ);

            var sb = new StringBuilder();
            string where = e.from != null ? null : string.IsNullOrEmpty(e.hint) ? "A starter" : e.hint == "Rare" ? "tall grass (rare)" : e.hint;
            if (state == Journal.State.Unseen)
            {
                // Unseen: the hint only.
                if (e.from != null) sb.Append(Known(e.from, caught) ? $"Evolves from {e.from.displayName}" : "Evolves from an Isling you haven't found yet");
                else sb.Append(where == "A starter" ? "A starter" : $"Lives in {where}");
            }
            else
            {
                sb.Append($"Strong vs {ElementWord(JournalLore.StrongAgainst(s.element))}   Weak vs {ElementWord(JournalLore.WeakAgainst(s.element))}\n");
                if (where != null) sb.Append($"<color=#D8C99A>Lives in</color> {where}\n");
                else sb.Append($"<color=#D8C99A>Evolves from</color> {(Known(e.from, caught) ? e.from.displayName : "???")}\n");
                var chain = JournalLore.Chain(s, x => _data.IndexOf(x) >= 0 ? _data.entries[_data.IndexOf(x)].from : null);
                sb.Append($"<color=#D8C99A>Evolves</color> {JournalLore.ChainLine(chain, x => Journal.StateOf(x, caught, seen) != Journal.State.Unseen)}\n");
                if (state == Journal.State.Caught)
                {
                    sb.Append("<color=#D8C99A>Moves</color> ");
                    var moves = new List<string>();
                    if (s.moves != null)
                        foreach (var m in s.moves)
                            if (m != null) moves.Add($"{m.Name} <size=80%>{ElementWord(m.element)}</size>");
                    sb.Append(string.Join(", ", moves)).Append('\n');
                    int best = Journal.BestLevel(s, Journal.Owned()), have = 0;
                    foreach (var c in Journal.Owned())
                        if (c != null && c.species == s) have++;
                    sb.Append(best > 0 ? $"Your best: <b>Lv {best}</b>   Caught ×{have}\n" : "Caught (none with you now)\n");
                }
                else sb.Append("<color=#D8C99A>Catch one to learn its moves.</color>\n");
                sb.Append($"<i>{JournalLore.Line(s)}</i>");
            }
            float bodyTop = picY - ModelHeight * 0.5f - 0.058f, bodyBottom = bookCenter.y - size.y * 0.5f + 0.012f;
            var body = runes.AddText(sb.ToString(), new Vector2(left + inner * 0.5f, (bodyTop + bodyBottom) * 0.5f), runes.fontSize * 1.0f, TextAlignmentOptions.TopLeft, inner);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.rectTransform.sizeDelta = new Vector2(inner, bodyTop - bodyBottom);
            body.enableAutoSizing = true;
            body.fontSizeMin = runes.fontSize * 0.75f;
            body.fontSizeMax = runes.fontSize * 1.0f;
            body.paragraphSpacing = 6f;
        }

        /// <summary>A portrait (seen) or silhouette (unseen) in place of the model.</summary>
        void CardPortrait(JournalData.Entry e, Journal.State state, Vector2 at)
        {
            var part = runes.AddPart("Card portrait", at, new Vector2(ModelHeight, ModelHeight));
            if (_cardMesh == null) _cardMesh = new Mesh { name = "Journal Card Portrait" };
            _cardMesh.Clear();
            var r = _data.CellRect(e.cell);
            float h = ModelHeight * 0.5f;
            _cardMesh.SetVertices(new List<Vector3> { new Vector3(-h, -h, 0f), new Vector3(-h, h, 0f), new Vector3(h, h, 0f), new Vector3(h, -h, 0f) });
            _cardMesh.SetUVs(0, new List<Vector2> { new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin) });
            Color tint = state == Journal.State.Seen ? new Color(0.8f, 0.8f, 0.8f, 1f) : SilhouetteColor;
            _cardMesh.SetColors(new List<Color> { tint, tint, tint, tint });
            var flat = new Vector2(state == Journal.State.Unseen ? 1f : 0f, 0f);
            _cardMesh.SetUVs(1, new List<Vector2> { flat, flat, flat, flat });
            _cardMesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            _cardMesh.RecalculateBounds();
            part.gameObject.AddComponent<MeshFilter>().sharedMesh = _cardMesh;
            var mr = part.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _data.portraitMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>The Isling's baked model, fitted to the card and turning slowly (Update).</summary>
        void MakeModel(IslingSpecies s, Vector2 feet)
        {
            var part = runes.AddPart("Card model", feet, new Vector2(ModelHeight, ModelHeight));
            part.localPosition = new Vector3(feet.x, feet.y, -0.03f);
            var go = Instantiate(s.Prefab, part, false);
            go.name = $"Journal {s.displayName}";
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = runes.gameObject.layer;
            // Fit by the drawn bounds (in the part's space): height ModelHeight, feet on the spot.
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            bool any = false;
            Bounds b = default;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                if (!any) b = r.bounds;
                else b.Encapsulate(r.bounds);
                any = true;
            }
            if (!any) return;
            float worldH = Mathf.Max(1e-4f, b.size.y), wantWorld = ModelHeight * part.lossyScale.y;
            float k = wantWorld / worldH;
            go.transform.localScale = Vector3.one * k;
            // Re-centre: bounds were measured at scale 1 from the part's origin.
            Vector3 offsetLocal = part.InverseTransformVector(new Vector3(b.center.x - part.position.x, b.min.y - part.position.y, b.center.z - part.position.z)) * k;
            go.transform.localPosition = -offsetLocal;
            _model = part;
        }

        static bool Known(IslingSpecies s, ICollection<string> caught) => caught != null && caught.Contains(SpeciesRegistry.IdOf(s));

        void Pick(int cell, string how)
        {
            if (cell < 0 || cell >= _shown.Count) return;
            _picked = cell == _picked ? -1 : cell;
            GameAudio.Ui(AudioIds.UiPebble);
            // Step 14i: the card is part of the board: rebuild it (the board keeps its facing).
            Build();
            Debug.Log($"[Journal] {(_picked >= 0 ? _data.entries[_shown[cell]].species.name : "card closed")} ({how}).");
        }

        /// <summary>Bridge: picks grid cell <paramref name="cell"/> on the page showing.</summary>
        public void DebugPick(int cell) => Pick(cell, "bridge");

        /// <summary>Bridge: shows an element's page.</summary>
        public void DebugElement(Element e) => ShowElement(e);

        // ---- step 14k: poke, hand ray + pinch, hover pop (no look + pinch) ----

        /// <summary>Bridge: hovers a cell as a finger would (-1 = off), to see the pop without hands.</summary>
        public static int DebugHover = -1;

        void Update()
        {
            if (!IsOpen || _grid == null || runes.Root == null) return;
            // Step 14i: the card's model turns slowly (about one turn in 12 s).
            if (_model != null) _model.localRotation = Quaternion.Euler(0f, Time.unscaledTime * 30f, 0f);
            if (Time.unscaledTime - _built < 0.3f) return;
            var root = runes.Root;
            int hover = DebugHover;
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                bool tracked = HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip);
                int poked = -1;
                if (tracked)
                {
                    var local = root.InverseTransformPoint(tip);
                    int under = CellAt(local);
                    // A fingertip up to 4 cm in front of a portrait pops it; touching it picks it.
                    if (under >= 0 && local.z > -0.04f && local.z < 0.015f) hover = under;
                    if (Mathf.Abs(local.z) < 0.015f) poked = under;
                }
                if (poked >= 0 && poked != _poked[h]) Pick(poked, "poke");
                _poked[h] = poked;
                // The hand ray (drawn by the board): a portrait under it pops, a pinch picks it.
                _rayCell[h] = runes.RayPoint(h, out var rp) ? CellAt(new Vector3(rp.x, rp.y, 0f)) : -1;
                if (hover < 0 && _rayCell[h] >= 0) hover = _rayCell[h];
                bool pinching = tracked && hand.GetIndexFingerIsPinching();
                bool busy = runes.grabGuard != null && runes.grabGuard.IsHandBusy(h == 0);
                if (pinching && !_wasPinching[h] && !busy && _rayCell[h] >= 0) Pick(_rayCell[h], $"ray + pinch ({(h == 0 ? "left" : "right")})");
                _wasPinching[h] = pinching;
            }
            if (hover != _hover && hover >= 0) GameAudio.At(AudioIds.RuneHover, root.TransformPoint(PicCenter(hover)));
            _hover = hover;
            // Ease each portrait's pop (in ~0.12 s, out ~0.2 s); rebuild the portraits mesh only while something moves.
            bool moving = false;
            float dt = Time.unscaledDeltaTime;
            for (int c = 0; c < _pop.Length; c++)
            {
                float to = c == _hover ? 1f : 0f, was = _pop[c];
                _pop[c] = Mathf.MoveTowards(was, to, dt / (to > was ? 0.12f : 0.2f));
                moving |= _pop[c] != was;
            }
            if (moving)
            {
                BuildPictures(_caught);
                PaintRim();
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
