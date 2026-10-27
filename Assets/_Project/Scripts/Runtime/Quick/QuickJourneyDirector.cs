using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Diorama
{
    /// <summary>
    /// Runs the Quick Journey's own beats in the quick save slot (step 9g):
    /// - The gift: once the lessons are done, back in the hub and calm, the Spirit says <c>quick.gift</c>, two pods fly
    ///   from it to the rim (<see cref="QuickJourneyConfig.giftSpecies"/> at the gift level), the starter and anything
    ///   caught are raised to <see cref="QuickJourneyConfig.raiseTo"/>, then <c>quick.gate</c> sends the Spirit to the fire
    ///   gate (it glows gold). Saved as the puzzle <c>quick.gift</c>.
    /// - The gold path: standing still a few seconds shows gold tiles toward the next goal (fire gate, log, forge door,
    ///   home gate with the shard, then the statue).
    /// - The end: when the fire shard locks into the statue, <c>quick.end</c>, then Keep exploring / Title runes. Saved as
    ///   the puzzle <c>quick.end</c>.
    /// - The timer (opening start to the shard in the statue), shown in the dev readout.
    /// Also tells <see cref="QuickJourney"/> whether the explorer is in Cinderwood (the dials apply there).
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
        [Tooltip("The end's Keep exploring / Title runes.")]
        public RuneBoard runes;
        public WorldMap hub, cinderwood;

        bool _gifting, _ending, _timing;
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

        void OnEnable() => PuzzleState.Changed += OnPuzzle;

        void OnDisable()
        {
            PuzzleState.Changed -= OnPuzzle;
            SaveTimer();
        }

        void OnDestroy()
        {
            // A scene reload may wake the new director before this one is destroyed: only the owner clears.
            if (QuickJourney.Owner != this) return;
            QuickJourney.Owner = null;
            QuickJourney.Config = null;
            QuickJourney.InCinderwood = false;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveTimer();
        }

        void Update()
        {
            var outdoor = swap != null ? swap.Outdoor : island != null ? island.world : null;
            QuickJourney.InCinderwood = outdoor != null && outdoor == cinderwood;
            if (!QuickJourney.Active) return;
            Timer();
            TryGift();
            GoldHint();
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

        // ---- the gift ----

        void TryGift()
        {
            bool ready = !_gifting && !PuzzleState.IsSolved(QuickJourney.GiftId) && onboarding != null && onboarding.IsFinished &&
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
                if (ShardHolder.HasFire) return Mark(world, "statue");
                if (PuzzleState.IsSolved(ShardHolder.FireId)) return null;
                int fire = world.gates.FindIndex(g => g.id == "fire");
                return fire >= 0 ? GameState.NearestWalkable(world, world.gates[fire].Front(0)) : (Vector2Int?)null;
            }
            if (world == cinderwood)
            {
                if (ShardHolder.HasFire)
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
                if (PuzzleState.IsSolved(ShardHolder.FireId)) return null;
                // Step 10f: the smoky hollow blocks the way to the springs and the log until its lanterns are lit.
                return Mark(world, PuzzleState.IsSolved(GameState.LogId) ? "temple" : PuzzleState.IsSolved("cw.hollow") || PastHollow(world) ? "log" : "hollow");
            }
            return null;
        }

        /// <summary>Step 10f: the log's trail is reachable from here (past the hollow already: an old save, a Temple preset).</summary>
        bool PastHollow(WorldMap world) => Mark(world, "log") is Vector2Int log && explorer.CanReach(log);

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
            if (id != ShardHolder.FireId || !PuzzleState.IsSolved(id) || !QuickJourney.Active || PuzzleState.IsSolved(QuickJourney.EndId) || _ending) return;
            StartCoroutine(End());
        }

        IEnumerator End()
        {
            _ending = true;
            if (!QuickJourney.TimerDone)
            {
                QuickJourney.TimerDone = true;
                SaveTimer();
                Debug.Log($"[QuickJourney] Finished: the fire shard is in the statue after {QuickJourney.Format(QuickJourney.Seconds)}.");
            }
            yield return new WaitForSeconds(1.5f);
            // The statue's own lines first.
            for (float t = 0f; t < 25f && (Dialogue.IsTalking || Dialogue.IsPending("hub.shard.placed") || Dialogue.IsPending("hub.shard.more")); t += Time.deltaTime)
                yield return null;
            Dialogue.Say("quick.end");
            for (float t = 0f; t < 1.5f && !Dialogue.IsTalking; t += Time.deltaTime) yield return null;
            for (float t = 0f; t < 15f && Dialogue.IsTalking; t += Time.deltaTime) yield return null;
            PuzzleState.Solve(QuickJourney.EndId, 1);
            ShowEnd();
            _ending = false;
        }

        void ShowEnd()
        {
            if (runes == null) return;
            runes.Clear();
            var size = new Vector2(0.1f, 0.04f);
            var keep = runes.Add("Keep exploring", new Vector2(-0.055f, 0f), size, () =>
            {
                runes.Hide();
                runes.Clear();
                Debug.Log("[QuickJourney] Keep exploring.");
            });
            runes.Add("Title", new Vector2(0.055f, 0f), size, ToTitle);
            runes.DefaultFocus = keep;
            runes.Show();
            Debug.Log("[QuickJourney] End card: Keep exploring / Title.");
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
