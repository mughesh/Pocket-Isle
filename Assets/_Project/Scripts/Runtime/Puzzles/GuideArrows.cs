using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 11d (Note 15): a small gold arrow lying on the island at the window's edge, pointing toward an objective
    /// that isn't inside the island window. Diorama/Beacon in arrow mode, one quad each, island-local. Call
    /// <see cref="Draw"/> for each arrow every frame (hidden ones too) and <see cref="Destroy"/> when done. Plain C#,
    /// owned by a piece (<see cref="HandPuzzle"/>).
    /// </summary>
    public class GuideArrows
    {
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int ModeId = Shader.PropertyToID("_Mode");

        readonly IslandBuilder _island;
        readonly List<Transform> _arrows = new List<Transform>();
        readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        Mesh _quad;

        public GuideArrows(string name, IslandBuilder island, Material material, int count)
        {
            _island = island;
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
                _arrows.Add(go.transform);
                _renderers.Add(r);
            }
        }

        public int Count => _arrows.Count;

        /// <summary>
        /// Arrow <paramref name="i"/> on the window's edge toward the map tile <paramref name="target"/> (column, row),
        /// <paramref name="tiles"/> long. Hidden when off.
        /// </summary>
        public void Draw(int i, bool on, Vector2 target, float tiles, float intensity)
        {
            if (i < 0 || i >= _arrows.Count) return;
            Vector3 local = default, dir = default;
            bool show = on && EdgeToward(target, tiles, out local, out dir);
            if (_arrows[i].gameObject.activeSelf != show) _arrows[i].gameObject.SetActive(show);
            if (!show) return;
            _arrows[i].localPosition = local;
            _arrows[i].localRotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            _arrows[i].localScale = Vector3.one * (_island.map.tileSize * _island.Zoom * tiles);
            _block.Clear();
            _block.SetFloat(ModeId, 5f);
            _block.SetFloat(IntensityId, intensity);
            _renderers[i].SetPropertyBlock(_block);
        }

        /// <summary>The island-local spot just inside the window's edge on the way from its middle to the target.</summary>
        bool EdgeToward(Vector2 target, float tiles, out Vector3 local, out Vector3 dir)
        {
            local = default;
            dir = default;
            var content = _island.Content;
            if (content == null || _island.map == null) return false;
            float cap = content.capHeight + 0.004f;
            Vector3 centre = new Vector3(0f, cap, 0f);
            Vector3 to = _island.TileToLocal(target, cap) - centre;
            to.y = 0f;
            if (to.sqrMagnitude < 1e-8f) return false;
            dir = to.normalized;
            float step = _island.map.tileSize * _island.Zoom * 0.25f;
            float r = 0f;
            for (int k = 0; k < 200 && _island.FootprintContains(centre + dir * (r + step)); k++) r += step;
            r -= _island.map.tileSize * _island.Zoom * (0.6f + 0.5f * tiles);
            if (r <= 0f) return false;
            local = centre + dir * r;
            return true;
        }

        public void HideAll()
        {
            foreach (var a in _arrows)
                if (a != null && a.gameObject.activeSelf) a.gameObject.SetActive(false);
        }

        public void Destroy()
        {
            foreach (var a in _arrows)
                if (a != null) Object.Destroy(a.gameObject);
            _arrows.Clear();
            _renderers.Clear();
            if (_quad != null) Object.Destroy(_quad);
        }
    }

    /// <summary>
    /// Step 11d (Note 8): an open door that reads as "you can go in", the same for every enterable door (the forge,
    /// the wind tower, the lake temple later): a warm soft portal swirl standing in the doorway (Diorama/Beacon portal
    /// mode) and a gold ring with a glow on the threshold tile. Island-local, positioned in map tiles; hidden outside
    /// the window. Plain C#, owned by the door's scene script.
    /// </summary>
    public class DoorGlow
    {
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int ModeId = Shader.PropertyToID("_Mode");

        readonly IslandBuilder _island;
        readonly Transform _portal;
        readonly MeshRenderer _renderer;
        readonly GuideRings _ring;
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        readonly Mesh _quad;
        float _on;

        public DoorGlow(string name, IslandBuilder island, Material material, BattleFx fx)
        {
            _island = island;
            if (island == null || material == null) return;
            _quad = BattleHud.VerticalQuad();
            var go = new GameObject($"{name} Portal");
            go.layer = island.gameObject.layer;
            go.transform.SetParent(island.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            go.SetActive(false);
            _portal = go.transform;
            _ring = new GuideRings($"{name} Threshold", island, material, 1, fx);
        }

        /// <summary>
        /// The doorway's middle (column, height in tiles of its centre, row), its size in tiles (width, height), the
        /// threshold tile where the ring goes, and whether the door is open (it fades in over half a second).
        /// </summary>
        public void Draw(bool open, Vector3 doorway, Vector2 size, Vector2 threshold)
        {
            if (_portal == null) return;
            _on = Mathf.MoveTowards(_on, open ? 1f : 0f, Time.deltaTime * 2f);
            var content = _island.Content;
            float t = _island.map != null ? _island.map.tileSize : 0.05f;
            Vector3 local = content != null ? _island.TileToLocal(new Vector2(doorway.x, doorway.z), content.capHeight + doorway.y * t) : default;
            bool show = _on > 0.01f && content != null && _island.FootprintContains(_island.TileToLocal(new Vector2(doorway.x, doorway.z), content.capHeight));
            if (_portal.gameObject.activeSelf != show) _portal.gameObject.SetActive(show);
            if (show)
            {
                _portal.localPosition = local;
                _portal.localRotation = Quaternion.identity;
                _portal.localScale = new Vector3(size.x, size.y, 1f) * (t * _island.Zoom);
                _block.Clear();
                _block.SetFloat(ModeId, 4f);
                _block.SetFloat(IntensityId, 1.5f * _on);
                _renderer.SetPropertyBlock(_block);
            }
            float pulse = 0.85f + 0.2f * Mathf.Sin(Time.time * 3f);
            _ring.Draw(0, _on > 0.5f, new Vector3(threshold.x, 0.05f, threshold.y), 1.5f, 1.3f * pulse, 0.35f * pulse);
        }

        public bool Showing => _on > 0.01f;

        public void Destroy()
        {
            if (_portal != null) Object.Destroy(_portal.gameObject);
            if (_quad != null) Object.Destroy(_quad);
            _ring?.Destroy();
        }
    }
}
