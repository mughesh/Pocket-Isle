using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 8b checks through the Claude bridge (exec:Diorama.EditorTools.Step8bDebug.Method), Play mode: thumb gestures
    /// as if from the right hand, the current lesson, look + pinch on the gold tile.
    /// </summary>
    static class Step8bDebug
    {
        static void Gesture(ThumbGesture g)
        {
            var input = Object.FindFirstObjectByType<MicrogestureInput>();
            if (input != null) input.DebugRaise(g, GestureSource.RightHand);
        }

        public static void SwipeForward() => Gesture(ThumbGesture.SwipeForward);
        public static void SwipeBack() => Gesture(ThumbGesture.SwipeBack);
        public static void SwipeLeft() => Gesture(ThumbGesture.SwipeLeft);
        public static void SwipeRight() => Gesture(ThumbGesture.SwipeRight);
        public static void Tap() => Gesture(ThumbGesture.Tap);

        /// <summary>The opening's N key: everything at once (Kilnpot), lessons from the walk lesson.</summary>
        public static void SkipOpening()
        {
            var o = Object.FindFirstObjectByType<Opening>();
            if (o != null && Opening.InProgress) typeof(Opening).GetMethod("Skip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(o, null);
        }

        // ---- ghost hands (Play) ----

        static void Ghost(string clip, GhostPlace place, string target, float scale)
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (g == null) return;
            g.Show(clip, place, target, scale, false, false);
            Debug.Log($"[Step8bDebug] Ghost {clip} {place} {target}.");
        }

        public static void GhostSwipe() => Ghost("thumb_swipe_forward", GhostPlace.AboveIsland, null, 0.6f);
        public static void GhostTap() => Ghost("thumb_tap", GhostPlace.AboveIsland, null, 0.6f);
        public static void GhostPinchTile() => Ghost("pinch", GhostPlace.AboveIsland, null, 0.6f);
        public static void GhostPoke() => Ghost("poke", GhostPlace.AboveIsland, null, 1f);
        public static void GhostRim() => Ghost("grab_rim_slide", GhostPlace.AboveIsland, null, 1f);
        public static void GhostDrop() => Ghost("grab_drop", GhostPlace.AboveIsland, null, 1f);
        public static void GhostToss() => Ghost("grab_toss", GhostPlace.AboveIsland, null, 1f);
        public static void GhostFlick() => Ghost("grab_flick", GhostPlace.AboveIsland, null, 1f);
        public static void GhostHover() => Ghost("palm_hover", GhostPlace.AboveIsland, null, 1f);

        public static void GhostInfo()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (g == null) return;
            foreach (float u in new[] { 0f, 0.5f })
            {
                g.debugFreeze = u;
                g.DebugPose();
                Debug.Log($"[Step8bDebug] Ghost at {u:0.0}: {g.DebugInfo()}");
            }
            g.debugFreeze = -1f;
        }

        public static void GhostHandsUp()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (g != null) g.Show("hands_up", GhostPlace.AboveIsland, null, 0.8f, false, true);
        }

        public static void GhostLeft()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (g != null) g.Show("thumb_swipe_forward", GhostPlace.AboveIsland, null, 0.6f, true, false);
        }

        // Skinning updates once per frame, so freeze first (one bridge command), then shoot (the next).
        static void Freeze(float u)
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (g != null) g.debugFreeze = u;
        }

        public static void GhostAt0() => Freeze(0f);
        public static void GhostAt30() => Freeze(0.3f);
        public static void GhostAt50() => Freeze(0.5f);
        public static void GhostAt75() => Freeze(0.75f);
        public static void GhostPlay() => Freeze(-1f);

        /// <summary>Seated view and a close-up from the player's side of the showing ghost (hold it with GhostAtNN first).</summary>
        public static void GhostShot()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (g == null || island == null || g.ClipName == null) return;
            var hidden = HideRigHands();
            var t = island.transform;
            var key = g.KeyPoint ?? t.position;
            string tag = $"{g.ClipName}_{(g.debugFreeze >= 0f ? g.debugFreeze.ToString("0.00") : "live")}";
            var seat = t.position + t.rotation * new Vector3(0f, 0.32f, 0.5f);
            BattleDebug.Render($"Logs/BattleShots/ghost_{tag}_seat.png", seat, Quaternion.LookRotation(key - seat, Vector3.up));
            var near = key + t.rotation * new Vector3(0.05f, 0.08f, 0.22f);
            BattleDebug.Render($"Logs/BattleShots/ghost_{tag}_near.png", near, Quaternion.LookRotation(key - near, Vector3.up));
            var side = key + t.rotation * new Vector3(0.25f, 0.04f, 0f);
            BattleDebug.Render($"Logs/BattleShots/ghost_{tag}_side.png", side, Quaternion.LookRotation(key - side, Vector3.up));
            foreach (var r in hidden)
                if (r != null) r.forceRenderingOff = false;
            Debug.Log($"[Step8bDebug] Ghost shots ghost_{tag}_*.png");
        }

        static System.Collections.Generic.List<Renderer> HideRigHands()
        {
            var hidden = new System.Collections.Generic.List<Renderer>();
            var rig = Object.FindFirstObjectByType<OVRCameraRig>();
            if (rig == null) return hidden;
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            foreach (var r in rig.GetComponentsInChildren<Renderer>())
            {
                // The island lives under the rig's TrackingSpace in Play: keep it.
                if (!r.enabled || r.forceRenderingOff || island != null && r.transform.IsChildOf(island.transform)) continue;
                r.forceRenderingOff = true;
                hidden.Add(r);
            }
            return hidden;
        }

        /// <summary>Lists the rig's ISDK hand visuals (the ghost hands clone one per side).</summary>
        public static void ListHandVisuals()
        {
            foreach (var hv in Object.FindObjectsByType<Oculus.Interaction.HandVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var path = hv.name;
                for (var t = hv.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
                Debug.Log($"[Step8bDebug] HandVisual {path}: active {hv.gameObject.activeInHierarchy}, joints {hv.Joints.Count}, root {(hv.Root != null ? hv.Root.name : "-")}, " +
                          $"joint0 {(hv.Joints.Count > 0 && hv.Joints[0] != null ? hv.Joints[0].name : "-")}");
                foreach (var smr in hv.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    Debug.Log($"[Step8bDebug]   SMR {smr.name}: active {smr.gameObject.activeInHierarchy}, mesh {(smr.sharedMesh != null ? smr.sharedMesh.name + " " + smr.sharedMesh.vertexCount + "v" : "none")}, " +
                              $"bones {smr.bones.Length}, rootBone {(smr.rootBone != null ? smr.rootBone.name : "-")}, materials {string.Join(",", System.Array.ConvertAll(smr.sharedMaterials, m => m != null ? m.name + "(" + m.shader.name + ")" : "null"))}");
            }
        }

        /// <summary>Logs the lesson, its phase and the explorer's tile.</summary>
        public static void Lesson()
        {
            var o = Object.FindFirstObjectByType<Onboarding>();
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            if (o == null || o.Runner == null) return;
            var r = o.Runner;
            Debug.Log($"[Step8bDebug] Lesson '{r.Current?.id}' {r.State} {r.Teaching:0.0} s, encounters {(Onboarding.EncountersEnabled ? "on" : "off")}, " +
                      $"talking {Dialogue.IsTalking}, explorer c{explorer.Tile.x} r{explorer.Tile.y}{(explorer.CanMove ? "" : " (can't move)")}, gold {o.GoldTile}.");
        }

        /// <summary>The current lesson counts as done (J).</summary>
        public static void Complete()
        {
            var o = Object.FindFirstObjectByType<Onboarding>();
            if (o != null) o.DebugComplete();
        }

        /// <summary>Look + pinch on the gold tile (walks there).</summary>
        public static void PinchGold()
        {
            var o = Object.FindFirstObjectByType<Onboarding>();
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            if (o == null || !o.GoldTile.HasValue) return;
            bool ok = explorer.GoTo(o.GoldTile.Value);
            Debug.Log($"[Step8bDebug] Look + pinch on the gold tile: {ok}.");
        }

        /// <summary>Walks onto the tutorial grass (the scripted first encounter).</summary>
        public static void GoGrass()
        {
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            if (explorer == null || explorer.island.world == null || !explorer.island.world.TryGetLandmark("firstGrass", out var grass)) return;
            Debug.Log($"[Step8bDebug] GoTo the tutorial grass c{grass.x} r{grass.y}: {explorer.GoTo(grass)}");
        }

        /// <summary>A tall grass tile away from the tutorial patch (it should only rustle during the tutorial).</summary>
        public static void GoOtherGrass()
        {
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            var world = explorer != null ? explorer.island.world : null;
            if (world == null || !world.TryGetLandmark("firstGrass", out var grass)) return;
            for (int r = 0; r < world.Rows; r++)
                for (int c = 0; c < world.Columns; c++)
                {
                    var t = new Vector2Int(c, r);
                    var d = t - grass;
                    if (world.At(t) != IslandMap.TallGrass || Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)) < 3 || !explorer.CanReach(t)) continue;
                    Debug.Log($"[Step8bDebug] GoTo other tall grass c{c} r{r}: {explorer.GoTo(t)}");
                    return;
                }
        }
    }
}
