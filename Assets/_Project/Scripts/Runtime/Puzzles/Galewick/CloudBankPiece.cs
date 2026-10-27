using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Galewick's cloud bank (step 10b): three big low cloud puffs sit on the narrow ridge path and block it. A gust
    /// (<see cref="GustField"/>) whose sweep passes over a puff pushes it the way it blew and thins it (about three good
    /// gusts per puff, <see cref="CloudBank"/>); a gone puff drifts off and opens its tile. All gone = the path is open
    /// (saved: stage = a bit per gone puff, solved = all). Look + pinch-hold a puff: it blows away slowly. Teaches itself
    /// once ("Sweep your open hand fast over the island!", chip "Sweep!", a palm-sweep ghost over the first puff).
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class CloudBankPiece : HandPuzzle
    {
        [Header("Puffs (map props grid: column, row)")]
        public Vector2Int[] puffs = new Vector2Int[3];
        [Tooltip("Step 12b: the 2-wide ridge path: each puff also blocks this tile (the row beside it); empty = 1-wide.")]
        public Vector2Int[] puffTwins = new Vector2Int[0];
        public PropRecipe recipe;
        public GustField gusts;
        [Tooltip("A gust hits a puff within this far of its sweep line (island-local metres).")]
        public float corridor = 0.2f;
        [Tooltip("Puffs drawn this much bigger than a tile (they sit on the path like a wall). Step 11d: 2.7 (was 1.35, hidden by the plants).")]
        public float puffScale = 2.7f;
        [Tooltip("Step 11d: the puffs float this many tiles over the path, clear of the plants.")]
        public float puffHeight = 0.6f;
        [Header("Step 12b: clouds in the air")]
        [Tooltip("Gentle bob up and down (tiles), side-to-side drift (tiles), slow turn (degrees a second), billow (share of size).")]
        public float bob = 0.1f;
        public float sway = 0.3f;
        public float spin = 9f;
        public float billow = 0.07f;
        [Tooltip("Diorama/Glyph (Materials/Glyph) for the soft round shadow under each puff; empty = none.")]
        public Material shadowMaterial;
        [Tooltip("Shadow size (share of the puff size) and darkness.")]
        public float shadowSize = 0.85f;
        [Range(0f, 1f)] public float shadowAlpha = 0.32f;

        [Header("Thinning (CloudBank)")]
        public float perGust = 0.36f;
        public float holdRate = 0.4f;

        [Header("Lines")]
        public string doneLine = "gw.gust.done";

        readonly CloudBank _bank = new CloudBank(3);
        readonly HandState[] _hands = new HandState[2];
        PropView[] _views;
        Vector2[] _drift;
        float[] _gone; // seconds since it went (fading away), -1 = still there
        int _gazed = -1;

        public CloudBank Bank => _bank;
        public override bool Busy => false;
        protected override Vector2 HomeTile => puffs.Length > 0 ? (Vector2)puffs[puffs.Length / 2] : Vector2.zero;

        public override Vector3? FocusPoint
        {
            get
            {
                if (!Here) return null;
                for (int i = 0; i < puffs.Length; i++)
                    if (!_bank.Gone(i)) return PuffWorld(i, puffHeight + 0.5f);
                return null;
            }
        }

        public override float FocusRadius => 1f * TileWorld;

        // ---- step 11d: the objective (gold over every puff still there, "Clouds n/3") ----

        public override bool CountObjective(out int done, out int total)
        {
            total = puffs.Length;
            done = ObjectiveRules.Count(_bank.Mask);
            return true;
        }

        protected override int MarkCount => puffs.Length;

        protected override bool Mark(int i, out Vector3 world, out float tiles)
        {
            tiles = 1.2f * puffScale;
            world = default;
            if (_bank.Gone(i)) return false;
            world = PuffWorld(i, puffHeight + 0.55f * puffScale);
            return true;
        }

        protected override void Start()
        {
            _bank.perGust = perGust;
            _bank.holdRate = holdRate;
            _views = new PropView[puffs.Length];
            _drift = new Vector2[puffs.Length];
            _gone = new float[puffs.Length];
            if (island != null)
                for (int i = 0; i < puffs.Length; i++) _views[i] = new PropView($"Cloud Bank {i}", island, recipe);
            MakeShadows();
            if (gusts != null) gusts.Gusted += OnGust;
            base.Start();
        }

        protected override void OnDestroy()
        {
            if (gusts != null) gusts.Gusted -= OnGust;
            if (_views != null)
                foreach (var v in _views) v?.Destroy();
            if (map != null)
                for (int i = 0; i < puffs.Length; i++)
                    foreach (var p in Blocks(i))
                    {
                        map.SetPropHidden(p, false);
                        map.SetPropOpen(p, false);
                    }
            if (_shadows != null)
                foreach (var s in _shadows)
                    if (s != null) Destroy(s.gameObject);
            base.OnDestroy();
        }

        protected override void ApplyState()
        {
            int mask = IsSolved ? 7 : PuzzleState.Stage(id);
            _bank.Restore(mask);
            for (int i = 0; i < puffs.Length; i++)
            {
                _drift[i] = Vector2.zero;
                _gone[i] = _bank.Gone(i) ? 99f : -1f;
                if (map != null)
                    foreach (var p in Blocks(i))
                    {
                        map.SetPropHidden(p, true);
                        map.SetPropOpen(p, _bank.Gone(i));
                    }
            }
            SetProgressQuiet(KiteRules.Count(mask) / 3f);
        }

        protected override void Update()
        {
            ReadHands(_hands);
            if (Usable && !IsSolved) LookHold();
            else _gazed = -1;
            for (int i = 0; i < puffs.Length; i++) Draw(i);
            base.Update();
        }

        void OnGust(Vector2 origin, Vector2 dir, float strength)
        {
            if (!Here || IsSolved) return;
            for (int i = 0; i < puffs.Length; i++)
            {
                if (_bank.Gone(i)) continue;
                var local = island.transform.InverseTransformPoint(PuffWorld(i, puffHeight + 0.4f));
                var p = new Vector2(local.x, local.z) - origin;
                float lateral = Mathf.Abs(p.x * dir.y - p.y * dir.x);
                if (lateral > corridor) continue;
                MarkInteracted();
                bool gone = _bank.Gust(i, strength);
                _drift[i] += dir * 0.35f * strength;
                Debug.Log($"[CloudBankPiece] Puff {i} hit (strength {strength:0.00}): thickness {_bank.thickness[i]:0.00}{(gone ? ", gone" : "")}.");
                if (fx != null)
                {
                    float tile = TileWorld;
                    var worldDir = island.transform.TransformDirection(new Vector3(dir.x, 0f, dir.y));
                    fx.Burst(PuffWorld(i, puffHeight + 0.5f), 10, new Color(1f, 1f, 1f, 0.7f), tile * 0.6f, 0.9f, tile * 0.12f, worldDir * tile * 3f);
                }
                LessonComplete();
                if (gone) Gone(i);
            }
        }

        void Gone(int i)
        {
            _gone[i] = 0f;
            foreach (var p in Blocks(i)) map.SetPropOpen(p, true);
            int mask = _bank.Mask;
            Debug.Log($"[CloudBankPiece] Puff {i} blown away ({KiteRules.Count(mask)}/3).");
            if (_bank.AllGone)
            {
                if (!string.IsNullOrEmpty(doneLine)) Dialogue.Say(doneLine);
                if (explorer != null) explorer.Cheer();
                MarkSolved(7);
            }
            else
            {
                PuzzleState.SetStage(id, mask);
                SetProgress(KiteRules.Count(mask) / 3f);
            }
        }

        void LookHold()
        {
            _gazed = Gazed();
            if (_gazed < 0) return;
            FocusHighlight.Hover(PuffWorld(_gazed, puffHeight + 0.5f), FocusRadius);
            bool held = false;
            foreach (var h in _hands)
                if (h.pinch && !h.busy) held = true;
            if (!held) return;
            MarkInteracted();
            LessonComplete();
            _drift[_gazed] += new Vector2(0f, -0.25f) * Time.deltaTime;
            if (_bank.Hold(_gazed, Time.deltaTime)) Gone(_gazed);
        }

        int Gazed()
        {
            if (!HeadGaze.TryGetRay(out var ray)) return -1;
            var pick = new HeadGaze.ConePick(ray, _gazed);
            for (int i = 0; i < puffs.Length; i++)
                if (!_bank.Gone(i)) pick.Consider(i, PuffWorld(i, puffHeight + 0.5f), Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.4f * puffScale * TileWorld));
            return pick.Best;
        }

        void Draw(int i)
        {
            var v = _views[i];
            if (v == null) return;
            float thick = _bank.thickness[i];
            float fade = 1f;
            if (_gone[i] >= 0f)
            {
                _gone[i] += Time.deltaTime;
                fade = Mathf.Clamp01(1f - _gone[i] / 1.2f);
                _drift[i] += _drift[i].sqrMagnitude > 1e-6f ? _drift[i].normalized * Time.deltaTime * 1.5f : Vector2.zero;
            }
            // Step 12b: a cloud in the air: it bobs, sways side to side, turns slowly and billows.
            float t = Time.time + i * 1.7f;
            float size = puffScale * (0.45f + 0.55f * Mathf.Max(thick, 0.2f)) * fade * (1f + billow * Mathf.Sin(t * 1.3f));
            var tile = Centre(i) + new Vector2(-_drift[i].x, _drift[i].y) * 2f + new Vector2(sway * Mathf.Sin(t * 0.45f), 0f);
            float h = puffHeight + 0.15f * (1f - fade) + bob * Mathf.Sin(t * 0.9f);
            // Oct 28: not in a battle (the battle's zoomed stage blew the puffs up across it).
            bool show = Here && size > 0.02f && (gusts == null || gusts.encounters == null || !gusts.encounters.InBattle);
            v.Place(tile, h, Quaternion.Euler(0f, 37f * i + spin * Time.time, 0f), show, size);
            Shadow(i, show, tile, size, fade);
        }

        /// <summary>A puff's middle: between its tile and its twin on the 2-wide path.</summary>
        Vector2 Centre(int i) => i < puffTwins.Length ? ((Vector2)puffs[i] + puffTwins[i]) * 0.5f : puffs[i];

        System.Collections.Generic.IEnumerable<Vector2Int> Blocks(int i)
        {
            yield return puffs[i];
            if (i < puffTwins.Length) yield return puffTwins[i];
        }

        Vector3 PuffWorld(int i, float height)
        {
            var c = Centre(i);
            return MapToWorld(new Vector3(c.x - _drift[i].x * 2f, height, c.y + _drift[i].y * 2f));
        }

        // ---- step 12b: soft round shadows on the path ----

        MeshRenderer[] _shadows;
        static Texture2D _shadowTex;
        MaterialPropertyBlock _shadowBlock;

        void MakeShadows()
        {
            if (shadowMaterial == null || island == null) return;
            if (_shadowTex == null)
            {
                const int n = 64;
                _shadowTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Cloud Shadow", wrapMode = TextureWrapMode.Clamp };
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float r = new Vector2((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f).magnitude;
                        float a = Mathf.Clamp01(1f - r);
                        px[y * n + x] = new Color(0.08f, 0.1f, 0.14f, a * a * (3f - 2f * a));
                    }
                _shadowTex.SetPixels(px);
                _shadowTex.Apply(false, true);
            }
            var quad = BattleHud.VerticalQuad();
            _shadows = new MeshRenderer[puffs.Length];
            for (int i = 0; i < puffs.Length; i++)
            {
                var go = new GameObject($"Cloud Shadow {i}");
                go.layer = island.gameObject.layer;
                go.transform.SetParent(island.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = quad;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = shadowMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.SetActive(false);
                _shadows[i] = r;
            }
        }

        void Shadow(int i, bool show, Vector2 tile, float size, float fade)
        {
            if (_shadows == null || _shadows[i] == null) return;
            var local = island.TileToLocal(tile, island.Content.capHeight + 0.02f * island.map.tileSize);
            show = show && island.FootprintContains(local);
            var go = _shadows[i].gameObject;
            if (go.activeSelf != show) go.SetActive(show);
            if (!show) return;
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = Vector3.one * (island.map.tileSize * island.Zoom * size * shadowSize);
            if (_shadowBlock == null) _shadowBlock = new MaterialPropertyBlock();
            _shadowBlock.Clear();
            _shadowBlock.SetTexture("_MainTex", _shadowTex);
            _shadowBlock.SetColor("_Tint", new Color(1f, 1f, 1f, shadowAlpha * fade));
            _shadows[i].SetPropertyBlock(_shadowBlock);
        }

        /// <summary>Debug (bridge): a gust across the bank toward east (+column).</summary>
        public void DebugGust(float strength = 0.9f)
        {
            if (gusts == null || !Here) return;
            var local = island.transform.InverseTransformPoint(PuffWorld(0, puffHeight + 0.4f));
            // Blow along the path (map east = island-local -X), starting just west of the first puff.
            gusts.Blow(new Vector2(local.x + 0.06f, local.z), new Vector2(-1f, 0f), strength, "bridge");
        }

        public string Describe() => $"puffs {string.Join(" ", System.Array.ConvertAll(_bank.thickness, t => t.ToString("0.00")))}, mask {_bank.Mask}";
    }
}
