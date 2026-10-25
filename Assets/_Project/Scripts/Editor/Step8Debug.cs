using System.Collections.Generic;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 8 checks through the Claude bridge (exec:Diorama.EditorTools.Step8Debug.Method). Shots go to Logs/BattleShots
    /// and hide the rig's hands and the Spirit, so the island reads clearly (edit mode or Play).
    /// </summary>
    static class Step8Debug
    {
        // ---- gates (Play mode) ----

        /// <summary>Puts the explorer in front of a gate on the current map and walks it in (look + pinch path).</summary>
        static void UseGate(string id)
        {
            var gates = Object.FindFirstObjectByType<ShardGates>();
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            if (gates == null || explorer == null || explorer.island.world == null) return;
            foreach (var g in explorer.island.world.gates)
            {
                if (g.id != id) continue;
                explorer.Teleport(g.Front(0) + Vector2Int.up, new Vector2Int(0, -1));
                gates.Pick(gates.IndexOf(id), "Step8Debug");
                return;
            }
            Debug.LogWarning($"[Step8Debug] No '{id}' gate on {explorer.island.world.name}.");
        }

        /// <summary>Opening: pick a starter as if looked at and pinched (0 Kilnpot, 1 Pebbloop, 2 Pufflet).</summary>
        static void Choose(int i)
        {
            var o = Object.FindFirstObjectByType<Opening>();
            if (o != null) o.DebugChoose(i);
        }

        public static void ChoosePebbloop() => Choose(1);

        /// <summary>Walks the explorer to the hub's first tall grass (the scripted first encounter).</summary>
        public static void GoFirstGrass()
        {
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            if (explorer == null || explorer.island.world == null || !explorer.island.world.TryGetLandmark("firstGrass", out var grass)) return;
            Debug.Log($"[Step8Debug] GoTo first grass c{grass.x} r{grass.y}: {explorer.GoTo(grass)}");
        }

        /// <summary>A seated shot from the island's front (the opening's floating pods, the unfold), hands hidden.</summary>
        public static void SeatedShot()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null) return;
            var hidden = HideNoise();
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0f, 0.32f, 0.5f);
            var look = t.position + t.rotation * new Vector3(0f, 0.06f, 0f);
            BattleDebug.Render($"Logs/BattleShots/opening_{Time.frameCount}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
            Restore(hidden);
            Debug.Log($"[Step8Debug] Shot Logs/BattleShots/opening_{Time.frameCount}.png");
        }

        public static void FireGate() => UseGate("fire");
        public static void HomeGate() => UseGate("home");
        public static void WaterGate() => UseGate("water");

        /// <summary>Logs where the explorer is and which map shows (after travels, loads).</summary>
        public static void Where()
        {
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            var swap = Object.FindFirstObjectByType<SceneSwap>();
            var party = Object.FindFirstObjectByType<Party>();
            if (explorer == null) return;
            Debug.Log($"[Step8Debug] On {explorer.island.world.name} c{explorer.Tile.x} r{explorer.Tile.y}, inside {(swap != null && swap.Inside)}, " +
                      $"realm {RealmState.Restored:0.##}, party {(party != null ? party.members.Count : -1)}.");
        }

        /// <summary>Seated shots of the hub: at the start, the statue, each gate.</summary>
        public static void HubShots()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null || island.world == null) return;
            var views = new List<(string, Vector2)>
            {
                ("start", StillIsleTools.StartTile), ("statue", StillIsleTools.StatueAnchor + Vector2.one),
                ("fire", StillIsleTools.FireGate), ("water", StillIsleTools.WaterGate), ("wind", StillIsleTools.WindGate),
            };
            Shots(island, views);
        }

        /// <summary>Close shots of single props on the hub (statue, gates).</summary>
        public static void PropShots()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null || island.world == null) return;
            var hidden = HideNoise();
            var t = island.transform;
            foreach (var (name, tile, size) in new[]
                     {
                         ("statue", StillIsleTools.StatueAnchor + new Vector2(1f, 1f), 3.2f),
                         ("firegate", StillIsleTools.FireGate + new Vector2(0.5f, 0f), 2.2f),
                         ("watergate", StillIsleTools.WaterGate + new Vector2(0.5f, 0f), 2.2f),
                         ("windgate", StillIsleTools.WindGate + new Vector2(0.5f, 0f), 2.2f),
                     })
            {
                island.SetView(tile, tile, 1f);
                island.Prepare();
                Vector3 target = island.TileToLocal(tile, island.Content.capHeight + 0.6f * size * island.map.tileSize);
                float d = size * island.map.tileSize * t.lossyScale.x * 1.9f;
                var look = t.TransformPoint(target);
                var eye = look + t.rotation * new Vector3(0f, 0.45f, 1f).normalized * d;
                BattleDebug.Render($"Logs/BattleShots/hub_prop_{name}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
            }
            Restore(hidden);
            Debug.Log("[Step8Debug] Prop shots in Logs/BattleShots/hub_prop_*.png");
        }

        static void Shots(IslandBuilder island, List<(string name, Vector2 tile)> views)
        {
            var hidden = HideNoise();
            var t = island.transform;
            foreach (var (name, tile) in views)
            {
                island.SetView(tile, tile, 1f);
                island.Prepare();
                var eye = t.position + t.rotation * new Vector3(0f, 0.5f, 0.42f);
                var rot = Quaternion.LookRotation(t.position + t.rotation * new Vector3(0f, 0f, 0.02f) - eye, Vector3.up);
                BattleDebug.Render($"Logs/BattleShots/hub_{name}.png", eye, rot);
            }
            Restore(hidden);
            Debug.Log($"[Step8Debug] {views.Count} hub shots in Logs/BattleShots/hub_*.png");
        }

        // forceRenderingOff is not serialized, so hiding never dirties the scene (a dirty scene makes the Test Runner
        // open a modal save dialog that blocks the bridge).
        // In Play the island (and the opening's shard) live under the rig's TrackingSpace: only the hands are hidden then,
        // and the Spirit and map creatures only in edit mode.
        static List<Renderer> HideNoise()
        {
            var hidden = new List<Renderer>();
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var roots = new List<Component>();
            var rig = Object.FindFirstObjectByType<OVRCameraRig>();
            if (rig != null) roots.Add(rig);
            if (!Application.isPlaying)
            {
                var spirit = Object.FindFirstObjectByType<LanternSpirit>();
                if (spirit != null) roots.Add(spirit);
                foreach (var mc in Object.FindObjectsByType<MapCreature>(FindObjectsSortMode.None)) roots.Add(mc);
            }
            foreach (var root in roots)
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    bool keep = Application.isPlaying && (island != null && r.transform.IsChildOf(island.transform) || r.name == "Opening Shard");
                    if (keep || !r.enabled || r.forceRenderingOff) continue;
                    r.forceRenderingOff = true;
                    hidden.Add(r);
                }
            return hidden;
        }

        static void Restore(List<Renderer> hidden)
        {
            foreach (var r in hidden)
                if (r != null) r.forceRenderingOff = false;
        }
    }
}
