using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Diorama
{
    /// <summary>
    /// Runs the Quick Journey's own beats in the quick save slot (step 9g; a Galewick journey since step 14c):
    /// - The gift: once the lessons are done, back in the hub and calm, the Spirit says <c>quick.gift</c>, two pods fly
    ///   from it to the rim (<see cref="QuickJourneyConfig.giftSpecies"/> at the gift level), the starter and anything
    ///   caught are raised to <see cref="QuickJourneyConfig.raiseTo"/>, then <c>quick.gate</c> sends the Spirit to the wind
    ///   gate (it glows gold). Saved as the puzzle <c>quick.gift</c>.
    /// - Galewick: the ridge cloud bank is cleared already (the sweep was taught at the arrival).
    /// - The gold path: standing still a few seconds shows gold tiles toward the next goal (wind gate, mill, kites, tower,
    ///   home gate with the shard, then the statue).
    /// - The end: the wind shard locks into the statue, the statue whispers, the Spirit wonders, <c>quick.end</c>, then
    ///   Keep exploring / See the ending / Title runes. Saved as the puzzle <c>quick.end</c>.
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
            ("gw.mill", "mill"), ("gw.kites", "kites"), (TowerDoor.TowerId, "tower"),
        };

        bool _gifting, _ending, _timing, _previewing;
        float _calm, _still, _nextGold, _savedAt;
        Vector2Int _lastTile;
        Coroutine _flash;
        readonly List<Vector2Int> _path = new List<Vector2Int>();

        void Awake()
        {
            QuickJourney.Config = config;
            QuickJourney.Owner = this;
            if (config == null) Debug.LogError("[QuickJourney] No config (Set Up MR_Main (9g title, menus, quick)): no Quick Journey dials.", this);
            // The pause hint waits until the gift has been given.
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
            TryGift();
            Presolve();
            if (!QuickJourney.Finished) GoldHint();
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

        // ---- Galewick ----

        CloudBankPiece _ridge;

        /// <summary>Step 14c: the ridge's cloud bank is already blown away in the quick slot (the arrival taught the sweep).</summary>
        void Presolve()
        {
            if (PuzzleState.IsSolved(RidgeId) || galewick == null) return;
            if (_ridge == null)
                foreach (var b in FindObjectsByType<CloudBankPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (b.id == RidgeId) _ridge = b;
            if (_ridge != null) _ridge.Presolve("the Quick Journey");
            else PuzzleState.Solve(RidgeId, 1);
        }

        // ---- the gift ----

        /// <summary>Step 12b: into a realm before the gift came: it's skipped for good (no two Islings arriving later).</summary>
        void OnTravelled(WorldMap to)
        {
            if (!QuickJourney.Active || _gifting || PuzzleState.IsSolved(QuickJourney.GiftId) || to == null || to == hub) return;
            PuzzleState.Solve(QuickJourney.GiftId, QuickJourney.GiftSkipped);
            Debug.Log($"[QuickJourney] Into {to.name} before the gift: the gift is skipped.");
        }

        void TryGift()
        {
            bool ready = !_gifting && QuickJourney.GiftAllowed(QuickJourney.Active, PuzzleState.IsSolved(QuickJourney.GiftId), false) &&
                         onboarding != null && onboarding.IsFinished &&
                         island.world == hub && (swap == null || !swap.Inside) && !SceneSwap.IsBusy && !Opening.InProgress &&
                         !TitleScreen.Showing && !GameMenu.IsOpen && (encounters == null || !encounters.InBattle) &&
                         !explorer.Locked && !Dialogue.IsTalking && party.members.Count > 0;
            _calm = ready ? _calm + Time.deltaTime : 0f;
            if (_calm >= 2f) StartCoroutine(Gift());
        }

        IEnumerator Gift()
        {
            _gifting = true;
            _calm = 0f;
            explorer.Locked = true;
            Debug.Log("[QuickJourney] The gift: two friends for the road.");
            Dialogue.Say("quick.gift");
            for (float t = 0f; t < 1.5f && !Dialogue.IsTalking; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(1f);
            var (giftLevel, raiseTo) = Levels();
            foreach (var s in config.giftSpecies)
            {
                if (s == null) continue;
                var gift = new IslingInstance(s, giftLevel);
                yield return Fly(gift);
                yield return new WaitForSeconds(0.25f);
            }
            foreach (var c in party.members)
                Raise(c, raiseTo);
            foreach (var c in party.storage)
                Raise(c, raiseTo);
            party.NotifyChanged();
            PuzzleState.Solve(QuickJourney.GiftId, 1);
            for (float t = 0f; t < 12f && Dialogue.IsTalking; t += Time.deltaTime) yield return null;
            Dialogue.Say("quick.gate");
            explorer.Locked = false;
            _gifting = false;
            Debug.Log($"[QuickJourney] Gift given: {string.Join(", ", party.members.ConvertAll(c => $"{c.Name} Lv {c.level}"))}.");
        }

        /// <summary>The gift level and raise level for this party's starter (the config's per-starter tuning).</summary>
        (int gift, int raise) Levels()
        {
            foreach (var c in party.members)
                if (c != null && config.perStarter != null)
                    foreach (var t in config.perStarter)
                        if (t.starter != null && (t.starter == c.species || t.starter.evolvesTo == c.species)) return (t.giftLevel, t.raiseTo);
            return (config.giftLevel, config.raiseTo);
        }

        static void Raise(IslingInstance c, int level)
        {
            if (c == null || c.level >= level) return;
            c.level = level;
            c.xp = 0;
            c.Heal();
        }

        /// <summary>A pod with the gift flies from the Spirit to the next free rim socket, then the Isling joins the party.</summary>
        IEnumerator Fly(IslingInstance gift)
        {
            int slot = Mathf.Min(party.members.Count, Party.Size - 1);
            PodView pod = null;
            if (pods != null && pods.podPrefab != null && spirit != null)
            {
                pod = Instantiate(pods.podPrefab, island.transform, false);
                pod.name = $"Gift Pod {gift.Name}";
                GameAudio.On(Diorama.Audio.AudioIds.GiftPods, pod.transform);
                pod.Show(gift);
                var grab = pod.GetComponent<PodGrab>();
                if (grab != null) grab.enabled = false;
                Vector3 from = spirit.transform.position, to = pods.HomeWorld(slot);
                float s = island.transform.lossyScale.x;
                for (float t = 0f; t < 0.9f; t += Time.deltaTime)
                {
                    float u = Mathf.SmoothStep(0f, 1f, t / 0.9f);
                    pod.transform.position = Vector3.Lerp(from, to, u) + Vector3.up * (0.06f * s * Mathf.Sin(u * Mathf.PI));
                    pod.BaseScale = Vector3.one * Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(t / 0.4f));
                    yield return null;
                }
                Destroy(pod.gameObject);
                if (fx != null) fx.Burst(to, 14, RuneBoard.Gold, 0.05f * s, 0.9f, 0.004f * s, Vector3.up * 0.05f);
            }
            party.Add(gift);
            party.NotifyChanged();
        }

        // ---- the gold path ----

        void GoldHint()
        {
            if (config == null || !config.goldPathHint || !PuzzleState.IsSolved(QuickJourney.GiftId) || explorer.Locked || !explorer.CanMove ||
                explorer.Destination.HasValue || SceneSwap.IsBusy || swap != null && swap.Inside || GameMenu.IsOpen || TitleScreen.Showing ||
                encounters != null && encounters.InBattle || explorer.Tile != _lastTile)
            {
                _still = 0f;
                _lastTile = explorer.Tile;
                return;
            }
            _still += Time.deltaTime;
            if (_still < config.goldPathAfter || Time.time < _nextGold || _flash != null) return;
            _nextGold = Time.time + config.goldPathEvery;
            var world = island.world;
            var goal = NextGoal(world);
            // Nothing to show right next to the goal (the puzzle there has its own hints).
            if (!goal.HasValue || !ExplorerPath.Find(world, explorer.Tile, goal.Value, _path, 400) || _path.Count <= 2) return;
            _flash = StartCoroutine(Flash(new List<Vector2Int>(_path)));
            Debug.Log($"[QuickJourney] Gold path: {Mathf.Min(_path.Count, config.goldPathTiles)} of {_path.Count} tiles toward c{goal.Value.x} r{goal.Value.y}.");
        }

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

        IEnumerator Flash(List<Vector2Int> path)
        {
            var from = explorer.Tile;
            int n = Mathf.Min(path.Count, config.goldPathTiles);
            for (int i = 0; i < n; i++)
            {
                if (explorer.Tile != from) break;
                island.FlashTile(path[i], 0.4f);
                yield return new WaitForSeconds(0.1f);
            }
            _flash = null;
        }

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

        void ShowEnd()
        {
            if (runes == null) return;
            runes.Clear();
            var size = new Vector2(0.1f, 0.04f);
            var keep = runes.Add(EndRunes[0], new Vector2(-0.11f, 0f), size, KeepExploring);
            runes.Add(EndRunes[1], new Vector2(0f, 0f), size, SeeTheEnding);
            runes.Add(EndRunes[2], new Vector2(0.11f, 0f), size, ToTitle);
            runes.DefaultFocus = keep;
            runes.Show();
            Debug.Log("[QuickJourney] End card: " + string.Join(" / ", EndRunes) + ".");
        }

        /// <summary>Every gate stays open; the dials stay in Galewick only; the Spirit's trail takes over, pointing to Cinderwood next.</summary>
        void KeepExploring()
        {
            runes.Hide();
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
            runes.Clear();
            _previewing = true;
            FinaleDirector.Moment -= OnFinaleMoment;
            FinaleDirector.Moment += OnFinaleMoment;
            Debug.Log($"[QuickJourney] See the ending: {BeginEnding(save, () => FinaleDirector.DevPlay(false, false))} (the quick save is suspended).");
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
            yield return new WaitForSeconds(4f);
            Debug.Log("[QuickJourney] The ending is over: back to the title (the quick save as it was).");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void ToTitle()
        {
            runes.Hide();
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
