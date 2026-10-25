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
