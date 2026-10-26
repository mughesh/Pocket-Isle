using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 9h bridge helpers (exec:Diorama.EditorTools.Step9hDebug.X). Shots go to Logs/BattleShots/9h_*.png.</summary>
    public static class Step9hDebug
    {
        /// <summary>Logo v2 alone, far from everything: Logs/BattleShots/9h_logo_front.png and 9h_logo_34.png.</summary>
        public static void LogoShot()
        {
            var letters = PropKit.Load(PropKit.LogoName);
            var chunk = PropKit.Load(PropKit.LogoBaseName);
            var material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Props/Logo.mat");
            if (letters == null || letters.bakedMesh == null || chunk == null || chunk.bakedMesh == null || material == null)
            {
                Debug.LogError("[Step9hDebug] Logo not baked (Step9hSetup.LogoOnly).");
                return;
            }
            var at = new Vector3(200f, 200f, 200f);
            var made = new System.Collections.Generic.List<GameObject>();
            foreach (var r in new[] { letters, chunk })
            {
                var go = new GameObject("Logo Shot " + r.name) { hideFlags = HideFlags.DontSave };
                go.AddComponent<MeshFilter>().sharedMesh = r.bakedMesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = material;
                var block = new MaterialPropertyBlock();
                r.ApplyTo(block);
                block.SetFloat("_WindowClip", 0f);
                block.SetFloat("_RestoredSelf", 1f);
                mr.SetPropertyBlock(block);
                go.transform.position = at;
                made.Add(go);
            }
            var b = letters.bakedMesh.bounds;
            b.Encapsulate(chunk.bakedMesh.bounds);
            var c = at + b.center;
            float d = b.size.x * 1.2f;
            var front = new Vector3(0f, 0.2f, 1f).normalized;
            BattleDebug.Render("Logs/BattleShots/9h_logo_front.png", c + front * d, Quaternion.LookRotation(-front, Vector3.up));
            var side = new Vector3(0.75f, 0.35f, 1f).normalized;
            BattleDebug.Render("Logs/BattleShots/9h_logo_34.png", c + side * d, Quaternion.LookRotation(-side, Vector3.up));
            foreach (var go in made) Object.DestroyImmediate(go);
            Debug.Log($"[Step9hDebug] Logo shots: {letters.bakedMesh.triangles.Length / 3} + {chunk.bakedMesh.triangles.Length / 3} triangles.");
        }

        /// <summary>The two-hands lesson's ghost (both near rim corners), without starting a new game.</summary>
        public static void GhostTwoHands()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (g != null) g.Show("grab_rim_slide", GhostPlace.RimCorners, null, 1f, DominantHand.Left, true);
        }

        /// <summary>The explorer next to the Cinderwood shrine (the palm ghost shows until the lodge was visited).</summary>
        public static void GoShrine() => GoSpot("CW: shrine");

        /// <summary>The explorer at the fallen log (its lesson ghost carries it to the ledge).</summary>
        public static void GoLog() => GoSpot("CW: log");

        static void GoSpot(string label)
        {
            var s = Object.FindFirstObjectByType<GameState>();
            if (s == null) return;
            foreach (var spot in s.Spots)
                if (spot.label == label)
                {
                    s.GoTo(spot);
                    return;
                }
        }

        /// <summary>Puts the Editor's "last played" slot back to Full adventure (after a Quick preset test).</summary>
        public static void LastAdventure()
        {
            PlayerPrefs.SetInt("pocketisle.lastSlot", (int)SaveSlot.Adventure);
            PlayerPrefs.Save();
            Debug.Log("[Step9hDebug] Last played slot: Full adventure.");
        }

        /// <summary>A pinch for the pinch lesson (no hands in flat Play).</summary>
        public static void Pinch()
        {
            var o = Object.FindFirstObjectByType<Onboarding>();
            if (o != null) o.DebugPinch();
        }

        /// <summary>Holds the showing ghost at its grab (then its release) frame; shoot in the next command.</summary>
        public static void GhostGrab() => FreezeAt(0);
        public static void GhostRelease() => FreezeAt(1);

        static void FreezeAt(int which)
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (g != null) g.DebugFreezeAt(which);
        }

        public static void Chip() => SetChip(true);
        public static void Full() => SetChip(false);

        static void SetChip(bool chip)
        {
            var b = Object.FindFirstObjectByType<DialogueBubble>();
            if (b != null) b.DebugChip(chip);
        }

        /// <summary>Lesson, bubble and Spirit state in one line.</summary>
        public static void State()
        {
            var o = Object.FindFirstObjectByType<Onboarding>();
            var b = Object.FindFirstObjectByType<DialogueBubble>();
            var g = Object.FindFirstObjectByType<GhostHands>();
            var d = Object.FindFirstObjectByType<Dialogue>();
            Debug.Log($"[Step9hDebug] State: lesson {(o != null ? o.CurrentId ?? "-" : "none")} ({(o != null && o.Runner != null ? o.Runner.State.ToString() : "-")}), " +
                      $"opening {Opening.InProgress}, line {(d != null ? d.CurrentTrigger ?? "-" : "-")}, pinned {Dialogue.IsPinned}, " +
                      $"bubble '{(b != null ? b.Text : "-")}' chip {(b != null && b.IsChip)}, ghost {(g != null ? g.ClipName ?? "-" : "-")} at {(g != null && g.KeyPoint.HasValue ? g.KeyPoint.Value.ToString("F3") : "-")}.");
        }

        /// <summary>From the seat (name in Temp/ClaudeBridge/shot.txt): Logs/BattleShots/9h_{name}.png.</summary>
        public static void Shot()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null) return;
            string name = System.IO.File.Exists("Temp/ClaudeBridge/shot.txt") ? System.IO.File.ReadAllText("Temp/ClaudeBridge/shot.txt").Trim() : "seat";
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0f, 0.42f, 0.5f);
            var rot = Quaternion.LookRotation(t.position + t.rotation * new Vector3(0f, 0.09f, 0f) - eye, Vector3.up);
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (name.StartsWith("ghost_") && g != null && g.KeyPoint.HasValue)
            {
                // Close on the ghost's key point, from the player's side, with the island behind it.
                // From the player's head side (Camera.main), looking at the key point.
                var key = g.KeyPoint.Value;
                var head = Camera.main != null ? Camera.main.transform.position : eye;
                var from = key + (head - key).normalized * 0.32f + Vector3.up * 0.06f;
                eye = from;
                rot = Quaternion.LookRotation(key - from, Vector3.up);
            }
            BattleDebug.Render($"Logs/BattleShots/9h_{name}.png", eye, rot);
            Debug.Log($"[Step9hDebug] Shot 9h_{name}.png");
        }
    }
}
