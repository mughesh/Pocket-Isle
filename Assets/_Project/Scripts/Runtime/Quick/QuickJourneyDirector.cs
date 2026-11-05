using System.Collections;
using System.Collections.Generic;
using Diorama.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Diorama
{
    /// <summary>
    /// Runs the Quick Journey's own beats in the quick save slot (step 9g; a Galewick journey since step 14c):
    /// - The send-off (no gift since Nov 3): once the lessons are done, back in the hub and calm, <c>quick.gate</c> sends
    ///   the Spirit to the wind gate and its trail shows the way. Saved as the puzzle <c>quick.gift</c> (stage 3).
    ///   Galeowl is easier in the quick slot instead (<see cref="QuickJourneyConfig.keeperLevelOffset"/>).
    /// - The gold path: the Spirit's trail toward the next goal (wind gate, mill, kites, the ridge clouds, tower, home gate
    ///   with the shard, then the statue). Nov 3: the ridge clouds are back in the quick slot (14c had cleared them).
    /// - The end: the wind shard locks into the statue, the statue whispers, the Spirit wonders, <c>quick.end</c> (its last
    ///   line asks: keep exploring or see the end?), then Keep exploring / See the ending / Title runes float in over the
    ///   statue (Nov 3: they sat over the near rim, where a resting hand could press one). Saved as the puzzle <c>quick.end</c>.
    /// - See the ending (Nov 3): not a cut. The Spirit dreams ahead (<c>quick.ending.go</c>), the fire and water shards
    ///   appear with it and fly into the statue one after the other, then the finale plays from the wake.
    /// - The timer (opening start to the shard in the statue), shown in the dev readout.
    /// Also tells <see cref="QuickJourney"/> whether the explorer is in Galewick (the dials apply there).
    /// </summary>
    public class QuickJourneyDirector : MonoBehaviour
    {
        public QuickJourneyConfig config;
        public Party party;
        public PodRack pods;
        public LanternSpirit spirit;
        public IslandBuilder island;
        public GestureTestPiece explorer;
        public EncounterDirector encounters;
        public Onboarding onboarding;
        public GameSave save;
        public SceneSwap swap;
        public BattleFx fx;
        [Tooltip("The end's Keep exploring / See the ending / Title runes.")]
        public RuneBoard runes;
        [Tooltip("Step 14c: the quick realm (Galewick).")]
        public WorldMap hub, galewick;

        /// <summary>The quick realm's shard and the ridge cloud bank it skips.</summary>
        public const string ShardId = ShardHolder.WindId, RidgeId = "gw.clouds";

        /// <summary>The route through Galewick, in order: each puzzle and the landmark the gold path leads to.</summary>
        public static readonly (string puzzle, string landmark)[] Route =
        {
            ("gw.mill", "mill"), ("gw.kites", "kites"), (RidgeId, "ridge"), (TowerDoor.TowerId, "tower"),
        };

        bool _sending, _ending, _timing, _previewing;
        float _calm, _savedAt;

        void Awake()
        {
            QuickJourney.Config = config;
            QuickJourney.Owner = this;
            if (config == null) Debug.LogError("[QuickJourney] No config (Set Up MR_Main (9g title, menus, quick)): no Quick Journey dials.", this);
            // The pause hint waits until the send-off.
            GameMenu.HoldHint = () => QuickJourney.Active && !PuzzleState.IsSolved(QuickJourney.GiftId);
        }

        void OnEnable()
        {
            PuzzleState.Changed += OnPuzzle;
            if (swap != null) swap.TravelFinished += OnTravelled;
        }

        void OnDisable()
        {
            PuzzleState.Changed -= OnPuzzle;
            if (swap != null) swap.TravelFinished -= OnTravelled;
            SaveTimer();
        }

        void OnDestroy()
        {
            // A scene reload may wake the new director before this one is destroyed: only the owner clears.
            if (QuickJourney.Owner != this) return;
            QuickJourney.Owner = null;
            QuickJourney.Config = null;
            QuickJourney.InQuickRealm = false;
            FinaleDirector.Moment -= OnFinaleMoment;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveTimer();
        }

        void Update()
        {
            var outdoor = swap != null ? swap.Outdoor : island != null ? island.world : null;
            QuickJourney.InQuickRealm = outdoor != null && outdoor == galewick;
            if (!QuickJourney.Active) return;
            Timer();
            TrySendOff();
        }

        // ---- timer ----

        void Timer()
        {
            if (!_timing && Opening.InProgress)
            {
                // A new quick game: the clock starts with the opening (this scene's start).
                QuickJourney.Seconds = Time.timeSinceLevelLoad;
                QuickJourney.TimerDone = false;
                Debug.Log("[QuickJourney] Timer started with the opening.");
            }
            _timing = true;
            if (!QuickJourney.TimerDone) QuickJourney.Seconds += Time.deltaTime;
            if (Time.unscaledTime - _savedAt > 5f) SaveTimer();
            if (DevReadout.Active)
                DevReadout.Text("quick", $"Quick Journey {QuickJourney.Format(QuickJourney.Seconds)}{(QuickJourney.TimerDone ? " (done)" : "")}");
        }

        void SaveTimer()
        {
            _savedAt = Time.unscaledTime;
            PlayerPrefs.Save();
        }

        // ---- the send-off ----

        /// <summary>Into a realm before the send-off: it's skipped for good.</summary>
        void OnTravelled(WorldMap to)
        {
            if (!QuickJourney.Active || _sending || PuzzleState.IsSolved(QuickJourney.GiftId) || to == null || to == hub) return;
            PuzzleState.Solve(QuickJourney.GiftId, QuickJourney.GiftSkipped);
            Debug.Log($"[QuickJourney] Into {to.name} before the send-off: skipped.");
        }

        void TrySendOff()
        {
            bool ready = !_sending && QuickJourney.GiftAllowed(QuickJourney.Active, PuzzleState.IsSolved(QuickJourney.GiftId), false) &&
                         onboarding != null && onboarding.IsFinished &&
                         island.world == hub && (swap == null || !swap.Inside) && !SceneSwap.IsBusy && !Opening.InProgress &&
                         !TitleScreen.Showing && !GameMenu.IsOpen && (encounters == null || !encounters.InBattle) &&
                         !explorer.Locked && !Dialogue.IsTalking && party.members.Count > 0;
            _calm = ready ? _calm + Time.deltaTime : 0f;
            if (_calm >= 2f) StartCoroutine(SendOff());
        }

        /// <summary>
        /// Nov 3 (Mugesh): no gift any more (it gave Islings you had, or ones from another realm): the Spirit sends you off
        /// to the wind gate and you catch your own friends on the way; the quick slot keeps Galeowl easier instead
        /// (<see cref="QuickJourneyConfig.keeperLevelOffset"/>). Saved as the puzzle <c>quick.gift</c> (stage 3 = sent off).
        /// </summary>
        IEnumerator SendOff()
        {
            _sending = true;
            _calm = 0f;
            PuzzleState.Solve(QuickJourney.GiftId, QuickJourney.SentOff);
            Dialogue.Say("quick.gate");
            Debug.Log("[QuickJourney] Send-off: the Spirit points at the wind gate.");
            yield return new WaitForSeconds(1.5f);
            var trail = FindFirstObjectByType<SpiritTrail>();
            if (trail != null) trail.Show("quick journey: send-off");
            _sending = false;
        }

        // ---- the route (the Spirit's trail shows it in the quick slot too, since Nov 3) ----

        /// <summary>Where the route goes next from here (null = nowhere: the journey is done or not on a route map).</summary>
        public Vector2Int? NextGoal(WorldMap world)
        {
            if (world == null) return null;
            if (world == hub)
            {
                if (ShardHolder.Holds(ShardId)) return Mark(world, "statue");
                if (PuzzleState.IsSolved(ShardId)) return null;
                int wind = world.gates.FindIndex(g => g.id == "wind");
                return wind >= 0 ? GameState.NearestWalkable(world, world.gates[wind].Front(0)) : (Vector2Int?)null;
            }
            if (world == galewick)
            {
                if (ShardHolder.Holds(ShardId))
                {
                    Vector2Int? best = null;
                    foreach (var g in world.gates)
                    {
                        if (g.id != "home") continue;
                        var t = GameState.NearestWalkable(world, g.Front(0));
                        if (!best.HasValue || (t - explorer.Tile).sqrMagnitude < (best.Value - explorer.Tile).sqrMagnitude) best = t;
                    }
                    return best;
                }
                if (PuzzleState.IsSolved(ShardId)) return null;
                // The arrival's puffs teach the sweep with their own gold.
                if (!PuzzleState.IsSolved("gw.arrive")) return null;
                return Mark(world, NextLandmark(PuzzleState.IsSolved));
            }
            return null;
        }

        /// <summary>The landmark of the first unsolved beat on the Galewick route (the tower once all are done) (pure).</summary>
        public static string NextLandmark(System.Func<string, bool> solved)
        {
            foreach (var (puzzle, landmark) in Route)
                if (!solved(puzzle)) return landmark;
            return "tower";
        }

        static Vector2Int? Mark(WorldMap world, string landmark) =>
            world.TryGetLandmark(landmark, out var t) ? GameState.NearestWalkable(world, t) : (Vector2Int?)null;

        // ---- the end ----

        void OnPuzzle(string id)
        {
            if (id != ShardId || !PuzzleState.IsSolved(id) || !QuickJourney.Active || PuzzleState.IsSolved(QuickJourney.EndId) || _ending) return;
            StartCoroutine(End());
        }

        IEnumerator End()
        {
            _ending = true;
            if (!QuickJourney.TimerDone)
            {
                QuickJourney.TimerDone = true;
                SaveTimer();
                Debug.Log($"[QuickJourney] Finished: the wind shard is in the statue after {QuickJourney.Format(QuickJourney.Seconds)}.");
            }
            yield return new WaitForSeconds(1.5f);
            // The statue's own lines first: its whisper (heart.dream.wind), the Spirit's "Did the statue just... speak?".
            for (float t = 0f; t < 30f && (Dialogue.IsTalking || StatueLinesPending()); t += Time.deltaTime)
                yield return null;
            Dialogue.Say("quick.end");
            for (float t = 0f; t < 1.5f && !Dialogue.IsTalking; t += Time.deltaTime) yield return null;
            for (float t = 0f; t < 15f && Dialogue.IsTalking; t += Time.deltaTime) yield return null;
            PuzzleState.Solve(QuickJourney.EndId, 1);
            ShowEnd();
            _ending = false;
        }

        static readonly string[] StatueLines = { "heart.dream.wind", "spirit.dream.1", "hub.shard.placed", "hub.shard.more" };

        static bool StatueLinesPending()
        {
            foreach (var id in StatueLines)
                if (Dialogue.IsPending(id)) return true;
            return false;
        }

        /// <summary>The end card's runes, left to right (tests).</summary>
        public static readonly string[] EndRunes = { "Keep exploring", "See the ending", "Title" };

        [Header("The end (Nov 3)")]
        [Tooltip("The end runes float this high over the statue's head (island-local metres).")]
        public float runesOverStatue = 0.035f;

        void ShowEnd()
        {
            if (runes == null) return;
            runes.Clear();
            // Step 16 (judges: tiny runes above the statue): the title's tablet size and gap, in front of the statue.
            var size = EndRuneSize;
            float step = size.x + EndRuneGap;
            var keep = runes.Add(EndRunes[0], new Vector2(-step, 0f), size, KeepExploring);
            runes.Add(EndRunes[1], new Vector2(0f, 0f), size, SeeTheEnding);
            runes.Add(EndRunes[2], new Vector2(step, 0f), size, ToTitle);
            runes.DefaultFocus = keep;
            runes.Anchor = InFrontOfStatue;
            var at = OverStatue();
            if (at.HasValue)
            {
                GameAudio.At(AudioIds.CardRise, at.Value);
                if (fx != null) fx.Burst(at.Value, 24, new Color(1f, 0.85f, 0.45f), TileWorld * 1.2f, 0.9f, TileWorld * 0.06f, island.transform.up * TileWorld * 0.6f);
            }
            runes.Show(0.18f);
            Dialogue.SetFocal("end card", true);
            Debug.Log("[QuickJourney] End card in front of the " + (at.HasValue ? "statue" : "near rim (no statue in view)") + ": " + string.Join(" / ", EndRunes) + ".");
        }

        /// <summary>Step 16: the end card's tablets: the title screen's wide size, 2.4 cm apart (the menu's gap).</summary>
        public static readonly Vector2 EndRuneSize = new Vector2(0.1f, 0.042f);
        public const float EndRuneGap = 0.024f;

        [Tooltip("Step 16: the end card floats this far from the statue toward the near rim (share) and this high over the cap (island-local m).")]
        [Range(0f, 1f)] public float endTowardPlayer = 0.55f;
        public float endAboveCap = 0.07f;

        /// <summary>Step 16: between the statue and the near rim, low enough to read; null = the near rim (no statue in view).</summary>
        Vector3? InFrontOfStatue()
        {
            if (island == null || island.world != hub || StatueSocket.HeartVoice == null) return null;
            var t = island.transform;
            Vector3 statue = t.InverseTransformPoint(StatueSocket.HeartVoice.position);
            var b = island.LocalBounds;
            Vector3 nearRim = new Vector3(b.center.x, island.CapTopLocal, b.max.z);
            Vector3 p = Vector3.Lerp(new Vector3(statue.x, island.CapTopLocal, statue.z), nearRim, endTowardPlayer);
            p.y = island.CapTopLocal + endAboveCap;
            return t.TransformPoint(p);
        }

        float TileWorld => island != null && island.map != null ? island.map.tileSize * island.transform.lossyScale.x : 0.05f;

        /// <summary>Over the statue's head (the Heart's voice point) while the hub shows; null = the near rim.</summary>
        Vector3? OverStatue()
        {
            if (island == null || island.world != hub || StatueSocket.HeartVoice == null) return null;
            return StatueSocket.HeartVoice.position + island.transform.up * runesOverStatue * island.transform.lossyScale.x;
        }

        /// <summary>Every gate stays open; the dials stay in Galewick only; the Spirit's trail takes over, pointing to Cinderwood next.</summary>
        void KeepExploring()
        {
            runes.Hide();
            Dialogue.SetFocal("end card", false);
            runes.Clear();
            Debug.Log("[QuickJourney] Keep exploring: the full adventure's trail leads on (Cinderwood next).");
            var trail = FindFirstObjectByType<SpiritTrail>();
            if (trail != null) trail.Show("quick journey: keep exploring");
        }

        /// <summary>
        /// The finale from the wake (as the dev panel's Finale: play from wake), on a copy: the quick save is written now and
        /// then suspended, so nothing the finale does (shards placed, finale done, Hall of Fame) reaches it. Back to the title after.
        /// </summary>
        void SeeTheEnding()
        {
            runes.Hide();
            Dialogue.SetFocal("end card", false);
            runes.Clear();
            _previewing = true;
            FinaleDirector.Moment -= OnFinaleMoment;
            FinaleDirector.Moment += OnFinaleMoment;
            Debug.Log($"[QuickJourney] See the ending: {BeginEnding(save, () => { StartCoroutine(DreamAhead()); return "the Spirit dreams ahead"; })} (the quick save is suspended).");
        }

        /// <summary>The other two shards, in the order they fly into the statue.</summary>
        public static readonly string[] DreamShards = { ShardHolder.FireId, ShardHolder.WaterId };

        /// <summary>
        /// Nov 3 (Mugesh: the cut to the finale looked awkward for a judge): the Spirit dreams ahead, the fire and water
        /// shards appear with it, float out and fly into the statue one after the other (quiet: no jingle, no whispers),
        /// one shard jingle, then the finale from the wake. The save is suspended already: none of this is kept.
        /// </summary>
        IEnumerator DreamAhead()
        {
            if (explorer != null) explorer.Locked = true;
            Dialogue.Say("quick.ending.go");
            for (float t = 0f; t < 1.5f && !Dialogue.IsTalking; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(1.2f);
            foreach (var id in DreamShards)
                if (!PuzzleState.IsSolved(id)) PuzzleState.SetStage(id, 1);
            var from = spirit != null ? spirit.transform.position : island.transform.position;
            if (fx != null) fx.Burst(from, 30, new Color(1f, 0.85f, 0.5f), TileWorld * 1.5f, 1f, TileWorld * 0.06f, island.transform.up * TileWorld * 0.5f);
            GameAudio.At(AudioIds.CardRise, from);
            Debug.Log("[QuickJourney] Dreaming ahead: the fire and water shards appear with the Spirit.");
            yield return new WaitForSeconds(2.2f);
            int flown = 0;
            foreach (var id in DreamShards)
            {
                var socket = StatueSocket.FindById(id);
                if (socket == null || !socket.FlyInQuiet()) continue;
                flown++;
                yield return new WaitForSeconds(1.7f);
            }
            if (flown > 0) GameAudio.Ui(AudioIds.JingleShard);
            Debug.Log($"[QuickJourney] Dreaming ahead: {flown} shard(s) flew into the statue; the finale next.");
            for (float t = 0f; t < 8f && Dialogue.IsTalking; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(0.8f);
            Debug.Log($"[QuickJourney] The finale: {FinaleDirector.DevPlay(false, false)}.");
        }

        /// <summary>The quick save written once, then suspended before the finale starts: nothing it does reaches the save (tests).</summary>
        public static string BeginEnding(GameSave save, System.Func<string> play)
        {
            if (save != null)
            {
                save.Save("quick journey: see the ending");
                save.Suspended = true;
            }
            return play();
        }

        void OnFinaleMoment(string moment)
        {
            if (moment != "done" || !_previewing) return;
            FinaleDirector.Moment -= OnFinaleMoment;
            StartCoroutine(AfterEnding());
        }

        IEnumerator AfterEnding()
        {
            // Step 16: the thank-you card's Title goes back itself; otherwise give it time to be read.
            for (float t = 0f; t < 20f && FinaleDirector.FinalCardUp; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(1f);
            Debug.Log("[QuickJourney] The ending is over: back to the title (the quick save as it was).");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void ToTitle()
        {
            runes.Hide();
            Dialogue.SetFocal("end card", false);
            if (save != null)
            {
                save.Save("quick journey: title");
                save.Suspended = true;
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Test hook (the Claude bridge): the end card as if the shard had just locked in.</summary>
        public void DebugEnd()
        {
            if (!_ending) StartCoroutine(End());
        }
    }
}
