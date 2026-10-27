using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Scripted perf capture for device builds (step 7c perf gate), so nobody has to play while the numbers are taken.
    /// Does nothing unless <see cref="TriggerFile"/> exists in the persistent data folder at start (push it with adb);
    /// the file is deleted at once, so the next launch plays normally. Then it runs, about <see cref="seconds"/> each:
    /// outdoor walk (unrestored), the same restored, a battle (send out, moves, a pod shatter, a catch), lodge swaps
    /// in and out, and the lodge with a full room. Each scene logs "[PerfTour] scene: ..." with average and 1% low fps,
    /// the worst frame, draw calls, triangles and memory (render counters read 0 where the player doesn't record them).
    /// Wild encounters from walking are off during the tour. The tour changes the save (it fills storage): back it up.
    /// Every launch also logs "[Startup] first frame" (cold start).
    /// </summary>
    public class PerfTour : MonoBehaviour
    {
        public const string TriggerFile = "perftour.txt";
        /// <summary>Step 8 tour: the opening (start it on a new game: no save), the hub with the statue, gate travel.</summary>
        public const string HubTriggerFile = "perftour8.txt";
        /// <summary>Step 9a tour: Cinderwood's whole trail, then the village (most props) and the springs (steam).</summary>
        public const string CinderwoodTriggerFile = "perftour9.txt";

        public GestureTestPiece explorer;
        public EncounterDirector encounters;
        public BattleRunes runes;
        public SceneSwap swap;
        public Party party;
        [Tooltip("Step 8 tour.")]
        public Opening opening;
        public ShardGates gates;
        public float seconds = 30f;
        [Tooltip("Step 9a tour: Cinderwood and its trail stops in order (column, row), written by Set Up MR_Main (9a Cinderwood).")]
        public WorldMap cinderwood;
        public List<Vector2Int> cinderwoodTrail = new List<Vector2Int>();
        [Tooltip("Where the village and springs captures stand (column, row).")]
        public Vector2Int villageSpot, springsSpot;

        ProfilerRecorder _draws, _tris, _memory;
        readonly List<float> _frames = new List<float>(4096);
        bool _recording;
        double _drawSum, _triSum, _memMax;
        int _samples;

        void Start()
        {
            Debug.Log($"[Startup] first frame at {Time.realtimeSinceStartup:0.00} s after the player started.");
            if (Consume(CinderwoodTriggerFile))
            {
                if (explorer == null || encounters == null || swap == null || cinderwood == null || cinderwoodTrail.Count < 2)
                {
                    Debug.LogError("[PerfTour] The Cinderwood tour needs the explorer, encounters, scene swap and the trail: Set Up MR_Main (9a Cinderwood).", this);
                    return;
                }
                StartCoroutine(CinderwoodTour());
                return;
            }
            bool hub = Consume(HubTriggerFile);
            if (!hub && !Consume(TriggerFile)) return;
            if (explorer == null || encounters == null || runes == null || swap == null || party == null || hub && (opening == null || gates == null))
            {
                Debug.LogError("[PerfTour] Needs the explorer, encounters, runes, scene swap, party (and for step 8 the opening and gates): Set Up MR_Main (8 hub).", this);
                return;
            }
            StartCoroutine(hub ? HubTour() : Tour());
        }

        static bool Consume(string file)
        {
            string path = Path.Combine(Application.persistentDataPath, file);
            if (!File.Exists(path)) return false;
            try { File.Delete(path); }
            catch (IOException e) { Debug.LogWarning($"[PerfTour] Couldn't delete the trigger file: {e.Message}"); }
            return true;
        }

        void OnDisable() => StopRecorders();

        void StopRecorders()
        {
            if (_draws.Valid) _draws.Dispose();
            if (_tris.Valid) _tris.Dispose();
            if (_memory.Valid) _memory.Dispose();
        }

        void Update()
        {
            if (!_recording) return;
            _frames.Add(Time.unscaledDeltaTime);
            if (_draws.Valid) _drawSum += _draws.LastValue;
            if (_tris.Valid) _triSum += _tris.LastValue;
            if (_memory.Valid) _memMax = System.Math.Max(_memMax, _memory.LastValue);
            _samples++;
        }

        void Begin(string scene)
        {
            _frames.Clear();
            _drawSum = _triSum = _memMax = 0;
            _samples = 0;
            _draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _memory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            _recording = true;
            Debug.Log($"[PerfTour] BEGIN {scene}");
        }

        void End(string scene)
        {
            _recording = false;
            StopRecorders();
            if (_frames.Count < 2)
            {
                Debug.Log($"[PerfTour] {scene}: no frames.");
                return;
            }
            var sorted = new List<float>(_frames);
            sorted.Sort();
            float total = 0f;
            foreach (var f in _frames) total += f;
            float avgFps = _frames.Count / Mathf.Max(1e-4f, total);
            // 1% low: the average fps of the slowest 1% of frames.
            int n = Mathf.Max(1, sorted.Count / 100);
            float slow = 0f;
            for (int i = sorted.Count - n; i < sorted.Count; i++) slow += sorted[i];
            float lowFps = n / Mathf.Max(1e-4f, slow);
            int under60 = 0;
            foreach (var f in _frames) if (f > 1f / 58f) under60++;
            Debug.Log($"[PerfTour] {scene}: {_frames.Count} frames, avg {avgFps:0.0} fps, 1% low {lowFps:0.0} fps, worst {sorted[sorted.Count - 1] * 1000f:0.0} ms, " +
                      $"{under60} frame(s) over 17.2 ms, draws {(_samples > 0 ? _drawSum / _samples : 0):0}, tris {(_samples > 0 ? _triSum / _samples / 1000.0 : 0):0}k, " +
                      $"memory {_memMax / (1024 * 1024):0} MB");
        }

        IEnumerator Tour()
        {
            Debug.Log("[PerfTour] Starting the perf tour.");
            yield return new WaitForSeconds(4f);
            float chance = encounters.grassChance;
            encounters.grassChance = 0f;
            if (SceneSwap.IsInside)
            {
                swap.Return();
                yield return WaitSwap();
            }

            RealmState.Restored = 0f;
            Begin("outdoor (unrestored)");
            yield return Walk(seconds);
            End("outdoor (unrestored)");

            RealmState.Restored = 1f;
            Begin("outdoor (restored)");
            yield return Walk(seconds);
            End("outdoor (restored)");

            Begin("battle");
            yield return Battle(seconds);
            End("battle");
            while (encounters.InBattle) yield return null;
            yield return new WaitForSeconds(1.5f);

            // A full room for the last two scenes.
            var rnd = new SeededRandom(11);
            while (party.storage.Count < 14 && encounters.GrassTable != null && encounters.GrassTable.Roll(rnd, out var c, out _))
            {
                c.level = 4;
                c.Heal();
                party.storage.Add(c);
            }
            party.NotifyChanged();

            Begin("swap in and out");
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                var lodge = swap.FindInterior("Lodge");
                if (lodge == null) break;
                swap.Enter(lodge, lodge.startTile);
                yield return WaitSwap();
                yield return new WaitForSeconds(2f);
                swap.Return();
                yield return WaitSwap();
                yield return new WaitForSeconds(2f);
            }
            End("swap in and out");

            var room = swap.FindInterior("Lodge");
            if (room != null)
            {
                swap.Enter(room, room.startTile);
                yield return WaitSwap();
                yield return new WaitForSeconds(2f);
                Begin("lodge (full room)");
                yield return new WaitForSeconds(seconds);
                End("lodge (full room)");
                swap.Return();
                yield return WaitSwap();
            }

            encounters.grassChance = chance;
            Debug.Log("[PerfTour] Done.");
        }

        /// <summary>
        /// Step 8: the opening as it plays (shard, unfold, Spirit; auto-touch), then the first starter is picked, the hub
        /// with the statue in view, then fire gate to Cinderwood and back, again and again.
        /// </summary>
        IEnumerator HubTour()
        {
            yield return null; // the Opening starts after this script (execution order 110)
            Debug.Log($"[PerfTour] Starting the step 8 tour ({(Opening.InProgress ? "new game" : "returning player: no opening to measure")}).");
            float chance = encounters.grassChance;
            encounters.grassChance = 0f;
            encounters.FirstEncounterDone = true;
            if (Opening.InProgress)
            {
                Begin("opening (shard, unfold, spirit)");
                yield return new WaitForSeconds(20f);
                End("opening (shard, unfold, spirit)");
                for (float t = 0f; Opening.InProgress && t < 60f; t += 0.5f)
                {
                    opening.DebugChoose(0);
                    yield return new WaitForSeconds(0.5f);
                }
            }
            if (SceneSwap.IsInside)
            {
                swap.Return();
                yield return WaitSwap();
            }
            var hub = swap.FindWorld("WorldMap_StillIsle");
            if (hub != null && explorer.island.world != hub)
            {
                swap.TravelNow(hub, hub.startTile, new Vector2Int(0, -1));
                yield return null;
            }
            if (hub != null) explorer.Teleport(hub.startTile + Vector2Int.down, new Vector2Int(0, -1));
            yield return new WaitForSeconds(2f);
            Begin("hub (statue in view)");
            yield return new WaitForSeconds(seconds * 0.5f);
            End("hub (statue in view)");

            Begin("gate travel");
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                string id = explorer.island.world == hub ? "fire" : "home";
                int i = gates.IndexOf(id);
                if (i < 0) break;
                foreach (var g in explorer.island.world.gates)
                    if (g.id == id) explorer.Teleport(g.Front(0), new Vector2Int(0, -1));
                gates.Pick(i, "PerfTour");
                yield return new WaitForSeconds(0.6f);
                yield return WaitSwap();
                yield return new WaitForSeconds(1.5f);
            }
            End("gate travel");
            encounters.grassChance = chance;
            Debug.Log("[PerfTour] Done.");
        }

        /// <summary>Editor: starts the Cinderwood tour on the next Play (writes the trigger file).</summary>
        public static void ArmCinderwoodTour() =>
            File.WriteAllText(Path.Combine(Application.persistentDataPath, CinderwoodTriggerFile), "9a");

        /// <summary>
        /// Step 9a: past the opening (picks the first starter on a new game), travel to Cinderwood's arrival, walk the
        /// whole trail stop by stop (one capture), then stand in the village unrestored and restored, then at the springs
        /// restored (steam). Encounters are off; landmark lines play as they would.
        /// </summary>
        IEnumerator CinderwoodTour()
        {
            yield return null;
            Debug.Log($"[PerfTour] Starting the Cinderwood tour ({cinderwoodTrail.Count} trail stops).");
            float chance = encounters.grassChance;
            encounters.grassChance = 0f;
            encounters.FirstEncounterDone = true;
            for (float t = 0f; Opening.InProgress && t < 60f; t += 0.5f)
            {
                if (opening != null) opening.DebugChoose(0);
                yield return new WaitForSeconds(0.5f);
            }
            if (SceneSwap.IsInside)
            {
                swap.Return();
                yield return WaitSwap();
            }
            swap.TravelNow(cinderwood, cinderwoodTrail[0], new Vector2Int(0, -1));
            yield return new WaitForSeconds(2f);

            RealmState.Restored = 0f;
            Begin("cinderwood trail walk");
            for (int i = 1; i < cinderwoodTrail.Count; i++)
            {
                var stop = cinderwoodTrail[i];
                if (!explorer.GoTo(stop))
                {
                    // Step 10f: a puzzle gate (the smoky hollow, the log) in the way: jump past it.
                    Debug.LogWarning($"[PerfTour] Can't path to trail stop {i} c{stop.x} r{stop.y} from c{explorer.Tile.x} r{explorer.Tile.y}: teleported.");
                    explorer.Teleport(stop, new Vector2Int(0, -1));
                    continue;
                }
                for (float t = 0f; explorer.Destination.HasValue && t < 30f; t += Time.deltaTime) yield return null;
            }
            End("cinderwood trail walk");

            explorer.Teleport(villageSpot, new Vector2Int(0, -1));
            yield return new WaitForSeconds(2f);
            Begin("village (unrestored)");
            yield return new WaitForSeconds(seconds * 0.5f);
            End("village (unrestored)");
            RealmState.Restored = 1f;
            yield return new WaitForSeconds(1f);
            Begin("village (restored)");
            yield return new WaitForSeconds(seconds * 0.5f);
            End("village (restored)");

            explorer.Teleport(springsSpot, new Vector2Int(0, -1));
            yield return new WaitForSeconds(2f);
            Begin("springs (restored, steam)");
            yield return new WaitForSeconds(seconds * 0.5f);
            End("springs (restored, steam)");
            RealmState.Restored = 0f;
            encounters.grassChance = chance;
            Debug.Log("[PerfTour] Done.");
        }

        static IEnumerator WaitSwap()
        {
            yield return null;
            while (SceneSwap.IsBusy) yield return null;
        }

        /// <summary>Walks to random reachable tiles (long paths run), so the window keeps scrolling.</summary>
        IEnumerator Walk(float duration)
        {
            float until = Time.time + duration;
            var map = explorer.island != null ? explorer.island.world : null;
            while (Time.time < until)
            {
                if (!explorer.Destination.HasValue && map != null)
                {
                    for (int tries = 0; tries < 30; tries++)
                    {
                        var t = explorer.Tile + new Vector2Int(Random.Range(-10, 11), Random.Range(-10, 11));
                        if (explorer.CanReach(t) && explorer.GoTo(t)) break;
                    }
                }
                yield return null;
            }
        }

        /// <summary>A grass encounter: send out, three moves, a throw above red (the pod shatters), then a catch try.</summary>
        IEnumerator Battle(float duration)
        {
            float until = Time.time + duration;
            encounters.StartEncounter(Habitat.Grass);
            yield return new WaitForSeconds(8f); // zoom, appear, auto send-out
            for (int m = 0; m < 3 && encounters.InBattle && Time.time < until; m++)
            {
                runes.Choose(m);
                yield return new WaitForSeconds(3.5f);
            }
            if (encounters.InBattle)
            {
                encounters.DebugPick(PodRack.CradleSlot); // above red: the pod shatters
                yield return new WaitForSeconds(4f);
            }
            var b = encounters.CurrentBattle;
            if (encounters.InBattle && b != null)
            {
                b.wild.isling.currentHp = Mathf.Max(1, Mathf.FloorToInt(b.wild.MaxHp * 0.1f));
                yield return new WaitForSeconds(4f); // the cradle refills
                encounters.DebugPick(PodRack.CradleSlot);
            }
            while (Time.time < until) yield return null;
            if (encounters.InBattle) runes.Choose(BattleRunes.RunIndex);
        }
    }
}
