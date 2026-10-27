using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Oct 27 (Mugesh: "the visual guidances are the ones that should be directing the user"): gold pulsing rings that
    /// mark where a hand goes, in the same look as the carried log's drop spots (<see cref="CarryPiece"/>, the beacon
    /// material in ring mode). Lying flat, island-local, one quad each, with a soft gold glow orb in the middle (pooled
    /// <see cref="BattleFx"/> orbs) so it reads from any angle. Call <see cref="Draw"/> for each ring every frame (hidden
    /// ones too) and <see cref="Destroy"/> when done. Plain C#, owned by a piece.
    /// </summary>
    public class GuideRings
    {
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int ModeId = Shader.PropertyToID("_Mode");

        readonly IslandBuilder _island;
        readonly BattleFx _fx;
        readonly BattleFx.Item[] _orbs;
        readonly List<Transform> _rings = new List<Transform>();
        readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        Mesh _quad;

        public int Count => _rings.Count;

        public GuideRings(string name, IslandBuilder island, Material material, int count, BattleFx fx = null)
        {
            _island = island;
            _fx = fx;
            _orbs = new BattleFx.Item[count];
            if (island == null || material == null) return;
            _quad = BattleHud.VerticalQuad();
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"{name} {i}");
                go.layer = island.gameObject.layer;
                go.transform.SetParent(island.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = _quad;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.SetActive(false);
                _rings.Add(go.transform);
                _renderers.Add(r);
            }
        }

        /// <summary>
        /// Ring <paramref name="i"/> at a map spot (column, height in tiles above the cap, row), <paramref name="tiles"/> across,
        /// at <paramref name="intensity"/> (1 = the drop spots' resting glow), with an orb <paramref name="orbTiles"/> across
        /// (0 = none); hidden when off or outside the window.
        /// </summary>
        public void Draw(int i, bool on, Vector3 mapSpot, float tiles, float intensity, float orbTiles = 0f)
        {
            if (i < 0 || i >= _rings.Count) return;
            Vector3 local = on ? _island.TileToLocal(new Vector2(mapSpot.x, mapSpot.z), _island.Content.capHeight + mapSpot.y * _island.map.tileSize) : default;
            bool visible = on && _island.FootprintContains(local);
            if (_rings[i].gameObject.activeSelf != visible) _rings[i].gameObject.SetActive(visible);
            Orb(i, visible && orbTiles > 0f, local, orbTiles, intensity);
            if (!visible) return;
            _rings[i].localPosition = local;
            _rings[i].localRotation = Quaternion.Euler(90f, 0f, 0f);
            _rings[i].localScale = Vector3.one * (_island.map.tileSize * _island.Zoom * tiles);
            _block.Clear();
            _block.SetFloat(ModeId, 1f);
            _block.SetFloat(FillId, 1f);
            _block.SetFloat(IntensityId, intensity);
            _renderers[i].SetPropertyBlock(_block);
        }

        void Orb(int i, bool on, Vector3 local, float tiles, float intensity)
        {
            if (_fx == null) return;
            if (!on)
            {
                if (_orbs[i] != null) _fx.Release(_orbs[i]);
                _orbs[i] = null;
                return;
            }
            if (_orbs[i] == null)
            {
                _orbs[i] = _fx.Get(BattleFx.Shape.Orb);
                if (_orbs[i] == null) return;
                _orbs[i].Set(new Color(1f, 0.9f, 0.55f), new Color(1f, 0.62f, 0.16f), 1f, 1.6f, 1f, 0.25f);
            }
            var t = _orbs[i].transform;
            t.position = _island.transform.TransformPoint(local);
            float world = _island.map.tileSize * _island.Zoom * tiles * _island.transform.lossyScale.x;
            t.localScale = Vector3.one * (world / Mathf.Max(1e-4f, _fx.transform.lossyScale.x));
            _orbs[i].SetAlpha(Mathf.Clamp01(0.3f * intensity));
        }

        public void HideAll()
        {
            foreach (var r in _rings)
                if (r != null && r.gameObject.activeSelf) r.gameObject.SetActive(false);
            for (int i = 0; i < _orbs.Length; i++) Orb(i, false, default, 0f, 0f);
        }

        public void Destroy()
        {
            if (_orbs != null) HideAllOrbs();
            foreach (var r in _rings)
                if (r != null) Object.Destroy(r.gameObject);
            _rings.Clear();
            _renderers.Clear();
            if (_quad != null) Object.Destroy(_quad);
        }

        void HideAllOrbs()
        {
            for (int i = 0; i < _orbs.Length; i++) Orb(i, false, default, 0f, 0f);
        }
    }
}
