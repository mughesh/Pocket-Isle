using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 12a: a pool of gold marks lying on the island (<c>Diorama/GoldMark</c>: glowing tiles, footprints, rings), raised
    /// a little above the cap with a dark outline so they read on every ground colour. Call <see cref="Begin"/>, then
    /// <see cref="Draw"/> per mark, then <see cref="End"/> each frame. Plain C#, owned by a component.
    /// </summary>
    public class GoldMarks
    {
        public const int Tile = 0;
        public const int Footprints = 1;
        public const int Ring = 2;

        static readonly int ModeId = Shader.PropertyToID("_Mode");
        static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        static readonly int PhaseId = Shader.PropertyToID("_Phase");
        static readonly int PulseId = Shader.PropertyToID("_Pulse");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        readonly string _name;
        readonly IslandBuilder _island;
        readonly Material _material;
        readonly List<MeshRenderer> _marks = new List<MeshRenderer>();
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        Mesh _quad;
        int _used;

        /// <summary>Glow strength (1.4 = the default gold).</summary>
        public float intensity = 1.4f;

        /// <summary>Height above the cap, in tiles.</summary>
        public float lift = 0.12f;

        public int Shown => _used;

        public GoldMarks(string name, IslandBuilder island, Material material)
        {
            _name = name;
            _island = island;
            _material = material;
            if (material == null) Debug.LogError($"[GoldMarks] {name}: no GoldMark material (run Tools > Pocket Isle > Setup > Walking (12a)); no gold marks.");
        }

        public void Begin() => _used = 0;

        /// <summary>
        /// A mark on a map point (column, row), <paramref name="tiles"/> across, turned to <paramref name="yaw"/> degrees
        /// (0 = toward +rows), at <paramref name="alpha"/>; <paramref name="phase"/> 0..1 along a trail with a travelling pulse.
        /// Hidden outside the window.
        /// </summary>
        public void Draw(Vector2 tile, int mode, float yaw, float tiles, float alpha, float phase = 0f, bool pulse = false)
        {
            if (_island == null || _material == null || _island.Content == null || alpha <= 0.01f) return;
            var map = _island.Content;
            var local = _island.TileToLocal(tile, map.capHeight + lift * map.tileSize);
            if (!_island.FootprintContains(local)) return;
            var r = Get(_used++);
            r.transform.localPosition = local;
            r.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(90f, 0f, 0f);
            r.transform.localScale = Vector3.one * (map.tileSize * _island.Zoom * tiles);
            _block.Clear();
            _block.SetFloat(ModeId, mode);
            _block.SetFloat(AlphaId, Mathf.Clamp01(alpha));
            _block.SetFloat(PhaseId, phase);
            _block.SetFloat(PulseId, pulse ? 1f : 0f);
            _block.SetFloat(IntensityId, intensity);
            r.SetPropertyBlock(_block);
            if (!r.gameObject.activeSelf) r.gameObject.SetActive(true);
        }

        public void End()
        {
            for (int i = _used; i < _marks.Count; i++)
                if (_marks[i] != null && _marks[i].gameObject.activeSelf) _marks[i].gameObject.SetActive(false);
        }

        public void HideAll()
        {
            Begin();
            End();
        }

        MeshRenderer Get(int i)
        {
            while (_marks.Count <= i)
            {
                if (_quad == null) _quad = BattleHud.VerticalQuad();
                var go = new GameObject($"{_name} {_marks.Count}");
                go.layer = _island.gameObject.layer;
                go.transform.SetParent(_island.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = _quad;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = _material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.SetActive(false);
                _marks.Add(r);
            }
            return _marks[i];
        }

        public void Destroy()
        {
            foreach (var r in _marks)
                if (r != null) Object.Destroy(r.gameObject);
            _marks.Clear();
        }

        /// <summary>Yaw (island-local degrees) for a map step (column, row): columns run toward -X, rows toward +Z.</summary>
        public static float Yaw(Vector2Int step) => Mathf.Atan2(-step.x, step.y) * Mathf.Rad2Deg;
    }
}
