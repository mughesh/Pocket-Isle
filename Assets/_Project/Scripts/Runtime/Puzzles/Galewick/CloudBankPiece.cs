using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Galewick's cloud bank (step 10b): three big low cloud puffs sit on the narrow ridge path and block it. A gust
    /// (<see cref="GustField"/>) whose sweep passes over a puff pushes it the way it blew and thins it (about three good
    /// gusts per puff, <see cref="CloudBank"/>); a gone puff drifts off and opens its tile. All gone = the path is open
    /// (saved: stage = a bit per gone puff, solved = all). Look + pinch-hold a puff: it blows away slowly. Teaches itself
    /// once ("Wave your hand over the island!", chip "Wave!", a palm ghost over the first puff).
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class CloudBankPiece : HandPuzzle
    {
        [Header("Puffs (map props grid: column, row)")]
        public Vector2Int[] puffs = new Vector2Int[3];
        public PropRecipe recipe;
        public GustField gusts;
        [Tooltip("A gust hits a puff within this far of its sweep line (island-local metres).")]
        public float corridor = 0.2f;
        [Tooltip("Puffs drawn this much bigger than a tile (they sit on the path like a wall).")]
        public float puffScale = 1.35f;

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
                    if (!_bank.Gone(i)) return PuffWorld(i, 0.5f);
                return null;
            }
        }

        public override float FocusRadius => 1f * TileWorld;

        protected override void Start()
        {
            _bank.perGust = perGust;
            _bank.holdRate = holdRate;
            _views = new PropView[puffs.Length];
            _drift = new Vector2[puffs.Length];
            _gone = new float[puffs.Length];
            if (island != null)
                for (int i = 0; i < puffs.Length; i++) _views[i] = new PropView($"Cloud Bank {i}", island, recipe);
            if (gusts != null) gusts.Gusted += OnGust;
            base.Start();
        }

        protected override void OnDestroy()
        {
            if (gusts != null) gusts.Gusted -= OnGust;
            if (_views != null)
                foreach (var v in _views) v?.Destroy();
            if (map != null)
                foreach (var p in puffs)
                {
                    map.SetPropHidden(p, false);
                    map.SetPropOpen(p, false);
                }
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
                {
                    map.SetPropHidden(puffs[i], true);
                    map.SetPropOpen(puffs[i], _bank.Gone(i));
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
                var local = island.transform.InverseTransformPoint(PuffWorld(i, 0.4f));
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
                    fx.Burst(PuffWorld(i, 0.5f), 10, new Color(1f, 1f, 1f, 0.7f), tile * 0.6f, 0.9f, tile * 0.12f, worldDir * tile * 3f);
                }
                LessonComplete();
                if (gone) Gone(i);
            }
        }

        void Gone(int i)
        {
            _gone[i] = 0f;
            map.SetPropOpen(puffs[i], true);
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
            FocusHighlight.Hover(PuffWorld(_gazed, 0.5f), FocusRadius);
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
                if (!_bank.Gone(i)) pick.Consider(i, PuffWorld(i, 0.5f), Mathf.Max(HeadGaze.MinRadius * island.transform.lossyScale.x, 0.8f * TileWorld));
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
            float size = puffScale * (0.45f + 0.55f * Mathf.Max(thick, 0.2f)) * fade;
            var tile = (Vector2)puffs[i] + new Vector2(-_drift[i].x, _drift[i].y) * 2f;
            v.Place(tile, 0.15f * (1f - fade), Quaternion.Euler(0f, 37f * i, 0f), Here && size > 0.02f, size);
        }

        Vector3 PuffWorld(int i, float height) => MapToWorld(new Vector3(puffs[i].x - _drift[i].x * 2f, height, puffs[i].y + _drift[i].y * 2f));

        /// <summary>Debug (bridge): a gust across the bank toward east (+column).</summary>
        public void DebugGust(float strength = 0.9f)
        {
            if (gusts == null || !Here) return;
            var local = island.transform.InverseTransformPoint(PuffWorld(0, 0.4f));
            // Blow along the path (map east = island-local -X), starting just west of the first puff.
            gusts.Blow(new Vector2(local.x + 0.06f, local.z), new Vector2(-1f, 0f), strength, "bridge");
        }

        public string Describe() => $"puffs {string.Join(" ", System.Array.ConvertAll(_bank.thickness, t => t.ToString("0.00")))}, mask {_bank.Mask}";
    }
}
