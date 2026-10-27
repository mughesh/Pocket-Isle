using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9e balance: a headless Keeper fight simulator and an XP curve check (bridge:
    /// exec:Diorama.EditorTools.KeeperSim.Report). Fights run the real <see cref="BattleSystem"/> with
    /// <see cref="KeeperRules"/>; the player is a simple "best move" AI: the hardest-hitting attack (a Water move while
    /// the crown shield is up), a switch to the healthiest reserve below 30% HP (once per creature), the next one in
    /// after a faint. No supports, no running.
    /// </summary>
    public static class KeeperSim
    {
        const string SpeciesDir = "Assets/_Project/Data/Creatures/Species";
        public static readonly string[] Starters = { "Kilnpot", "Pebbloop", "Pufflet" };
        public static readonly string[] FireCommons = { "Toastling", "Waxle", "Kettlewing", "Ashmole", "Coalbeetle", "Mapadillo", "Cindercap" };

        public struct Outcome
        {
            public int fights, wins;
            public int turns, survivors;
            public float WinRate => fights > 0 ? wins / (float)fights : 0f;
        }

        public static IslingSpecies Load(string name) => AssetDatabase.LoadAssetAtPath<IslingSpecies>($"{SpeciesDir}/{name}.asset");

        /// <summary>Step 9f: the Water visitor at the hot springs, in the sim's parties when it exists.</summary>
        public const string Helper = "Steambara";

        public static TypeChart Chart => AssetDatabase.LoadAssetAtPath<TypeChart>("Assets/_Project/Data/Battle/TypeChart.asset");

        /// <summary><paramref name="fights"/> fights of a Lv <paramref name="level"/> party (the starter + two random fire commons) per starter.</summary>
        /// <remarks>Step 9f: with <paramref name="helper"/>, the starter + the helper + one random fire common.</remarks>
        public static Dictionary<string, Outcome> Run(IslingSpecies keeper, int keeperLevel, int level, int fights, int seed = 1, IslingSpecies helper = null)
        {
            var chart = Chart;
            var commons = new List<IslingSpecies>();
            foreach (var n in FireCommons)
            {
                var s = Load(n);
                if (s != null) commons.Add(s);
            }
            var rnd = new SeededRandom(seed);
            var all = new Dictionary<string, Outcome>();
            foreach (var starterName in Starters)
            {
                var starter = Load(starterName);
                if (starter == null || keeper == null || commons.Count < 2) continue;
                var o = new Outcome();
                for (int i = 0; i < fights; i++)
                {
                    int a = (int)(rnd.Next() * commons.Count), b = (a + 1 + (int)(rnd.Next() * (commons.Count - 1))) % commons.Count;
                    var party = new List<IslingInstance> { new IslingInstance(starter, level), new IslingInstance(helper != null ? helper : commons[a], level), new IslingInstance(commons[b], level) };
                    var r = Fight(party, new IslingInstance(keeper, keeperLevel), chart, seed * 7919 + i, out int turns);
                    o.fights++;
                    o.turns += turns;
                    if (r == BattleResult.Won)
                    {
                        o.wins++;
                        foreach (var c in party)
                            if (!c.IsFainted) o.survivors++;
                    }
                }
                all[starterName] = o;
            }
            return all;
        }

        /// <summary>One Keeper fight to the end (at most 80 turns).</summary>
        public static BattleResult Fight(List<IslingInstance> party, IslingInstance keeper, TypeChart chart, int seed, out int turns)
        {
            var switched = new HashSet<IslingInstance>();
            bool Reserve(IslingInstance except)
            {
                foreach (var c in party)
                    if (c != except && !c.IsFainted) return true;
                return false;
            }
            // Step 9f: like a player who heard "Water beats fire", the one with the shield-breaking element goes first.
            var lead = party[0];
            var rules = new KeeperRules();
            if (keeper.species != null) rules.breaksWith = keeper.species.keeperBreaksWith; // step 10d: Galeowl breaks to Fire
            foreach (var c in party)
                if (c.Element == rules.breaksWith)
                {
                    lead = c;
                    break;
                }
            BattleSystem battle = null;
            battle = new BattleSystem(lead, keeper, chart, seed, false, () => Reserve(battle.player.isling), rules);
            for (turns = 0; turns < 80 && !battle.IsOver; turns++)
            {
                var me = battle.player.isling;
                if (battle.PlayerMustSwitch)
                {
                    battle.Switch(Healthiest(party, me));
                    turns--;
                    continue;
                }
                if (me.currentHp < me.MaxHp * 0.3f && !switched.Contains(me))
                {
                    var next = Healthiest(party, me);
                    if (next != null && next.currentHp > next.MaxHp * 0.6f)
                    {
                        switched.Add(me);
                        battle.Switch(next);
                        continue;
                    }
                }
                battle.PlayTurn(BestMove(battle, chart));
            }
            return battle.Result;
        }

        static IslingInstance Healthiest(List<IslingInstance> party, IslingInstance except)
        {
            IslingInstance best = null;
            foreach (var c in party)
                if (c != except && !c.IsFainted && (best == null || c.currentHp / (float)c.MaxHp > best.currentHp / (float)best.MaxHp)) best = c;
            return best;
        }

        /// <summary>The attack with the most expected damage; the shield-breaking element first while the shield is up.</summary>
        public static int BestMove(BattleSystem battle, TypeChart chart)
        {
            var moves = battle.player.Moves;
            var k = battle.keeper;
            int best = -1;
            float bestScore = float.MinValue;
            for (int i = 0; i < moves.Length; i++)
            {
                var m = moves[i];
                if (m == null || m.kind != MoveKind.Attack) continue;
                float score = m.power * m.accuracy / 100f * chart.Multiplier(m.element, battle.wild.isling.Element);
                if (k != null && k.Shielded && m.element == k.breaksWith) score += 1000f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best < 0 ? 0 : best;
        }

        // ---- XP curve ----

        public struct TrailEstimate
        {
            public int grassHub, grassCinder;
            public float battlesHub, battlesCinder;
            public float level, levelP10, levelP90;
        }

        /// <summary>
        /// Walks the normal trail on the real maps (hub: start, first grass, fire gate; Cinderwood: arrival, first grass,
        /// plaza, springs, log, temple; shortest tile paths between them) <paramref name="runs"/> times with the real
        /// encounter rules (2 scripted battles, then 3 grace steps and 12.5% per tall-grass step), the starter (Lv 5)
        /// fighting and winning every battle. <paramref name="wander"/> repeats every grass tile that many times (players
        /// who poke around).
        /// </summary>
        public static TrailEstimate Trail(int runs, float wander, int seed = 3, ProgressionConfig configOverride = null, int cinderLevelBonus = 0)
        {
            var hub = AssetDatabase.LoadAssetAtPath<WorldMap>("Assets/_Project/Data/WorldMap_StillIsle.asset");
            var cinder = AssetDatabase.LoadAssetAtPath<WorldMap>("Assets/_Project/Data/WorldMap_Cinderwood.asset");
            var config = configOverride != null ? configOverride : AssetDatabase.LoadAssetAtPath<ProgressionConfig>("Assets/_Project/Data/Battle/Progression.asset");
            var starter = Load("Pebbloop");
            var est = new TrailEstimate();
            if (hub == null || cinder == null || config == null || starter == null) return est;
            var fireGate = hub.gates.Find(g => g.id == "fire");
            var hubPath = Chain(hub, Mark(hub, "start"), Mark(hub, "firstGrass"), fireGate.Front(0));
            var cinderPath = Chain(cinder, Mark(cinder, "start"), Mark(cinder, "firstGrass"), Mark(cinder, "plaza"), Mark(cinder, "springs"),
                Mark(cinder, "log"), Mark(cinder, "temple"));
            est.grassHub = GrassOn(hub, hubPath);
            est.grassCinder = GrassOn(cinder, cinderPath);

            var rnd = new SeededRandom(seed);
            var levels = new List<int>();
            float bh = 0f, bc = 0f;
            var firstTable = hub.GrassTableAt(Mark(hub, "firstGrass"));
            for (int run = 0; run < runs; run++)
            {
                var me = new IslingInstance(starter, 5);
                // The tutorial battle and the first encounter (Lv 3, scripted).
                Progression.AddXp(me, config.Reward(3, false), config);
                Progression.AddXp(me, config.Reward(3, false), config);
                int since = 0;
                bh += Walk(hub, hubPath, wander, me, config, rnd, ref since, 0);
                bc += Walk(cinder, cinderPath, wander, me, config, rnd, ref since, cinderLevelBonus);
                levels.Add(me.level);
            }
            levels.Sort();
            est.battlesHub = bh / runs + 2f;
            est.battlesCinder = bc / runs;
            float sum = 0f;
            foreach (var l in levels) sum += l;
            est.level = sum / runs;
            est.levelP10 = levels[runs / 10];
            est.levelP90 = levels[runs * 9 / 10];
            return est;
        }

        static int Walk(WorldMap map, List<Vector2Int> path, float wander, IslingInstance me, ProgressionConfig config, SeededRandom rnd, ref int since, int levelBonus)
        {
            int battles = 0;
            foreach (var t in path)
            {
                if (map.At(t) != IslandMap.TallGrass) continue;
                int repeats = Mathf.FloorToInt(wander) + (rnd.Next() < wander - Mathf.Floor(wander) ? 1 : 0);
                for (int k = 0; k < repeats; k++)
                {
                    if (++since <= 3 || rnd.Next() >= 0.125f) continue;
                    since = 0;
                    var table = map.GrassTableAt(t);
                    if (table == null || !table.Roll(rnd, out var wild, out bool rare)) continue;
                    battles++;
                    Progression.AddXp(me, config.Reward(wild.level + levelBonus, rare), config);
                }
            }
            return battles;
        }

        // Step 10f: the nearest walkable tile (the springs landmark sits in a pool, the temple's on the door: the path search
        // could never reach them, so the trail estimate skipped the plaza -> springs -> log legs).
        internal static Vector2Int Mark(WorldMap map, string name) => map.TryGetLandmark(name, out var t) ? GameState.NearestWalkable(map, t) : map.startTile;

        static int GrassOn(WorldMap map, List<Vector2Int> path)
        {
            int n = 0;
            foreach (var t in path)
                if (map.At(t) == IslandMap.TallGrass) n++;
            return n;
        }

        internal static List<Vector2Int> Chain(WorldMap map, params Vector2Int[] stops)
        {
            var all = new List<Vector2Int>();
            for (int i = 1; i < stops.Length; i++) all.AddRange(PathBetween(map, stops[i - 1], stops[i]));
            return all;
        }

        /// <summary>Shortest 4-way path on walkable layout tiles (props ignored: the puzzle props open), start excluded.</summary>
        internal static List<Vector2Int> PathBetween(WorldMap map, Vector2Int from, Vector2Int to)
        {
            var prev = new Dictionary<Vector2Int, Vector2Int>();
            var q = new Queue<Vector2Int>();
            q.Enqueue(from);
            prev[from] = from;
            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (c == to) break;
                foreach (var d in dirs)
                {
                    var n = c + d;
                    if (prev.ContainsKey(n) || n.x < 0 || n.y < 0 || n.y >= map.Rows || n.x >= map.Columns) continue;
                    if (n != to && !IslandMap.IsWalkable(map.At(n))) continue;
                    prev[n] = c;
                    q.Enqueue(n);
                }
            }
            var path = new List<Vector2Int>();
            if (!prev.ContainsKey(to)) return path;
            for (var c = to; c != from; c = prev[c]) path.Add(c);
            path.Reverse();
            return path;
        }

        // ---- report ----

        public static void Report()
        {
            var keeper = Load("Hearthstag");
            var log = new StringBuilder("[KeeperSim] Hearthstag ");
            if (keeper == null)
            {
                Debug.LogError("[KeeperSim] Hearthstag species missing.");
                return;
            }
            int keeperLevel = KeeperLevel;
            var k = new IslingInstance(keeper, keeperLevel);
            log.Append($"Lv {keeperLevel}: HP {k.MaxHp} (a base-40 Lv {keeperLevel} has {BattleMath.MaxHp(40, keeperLevel)}), atk {k.Attack} def {k.Defense} spd {k.Speed}\n");
            var helper = Load(Helper);
            log.Append($"Parties of 3: the starter + {Helper} + one random fire common (+ {Helper}), or the starter + two fire commons (+ 2 fire).\n");
            log.Append("| Lv | Party | Kilnpot | Pebbloop | Pufflet | All | Avg turns |\n|---|---|---|---|---|---|---|\n");
            foreach (int lv in new[] { 6, 7, 8, 9, 10 })
                foreach (var h in helper != null ? new[] { helper, null } : new IslingSpecies[] { null })
                {
                    var row = Run(keeper, keeperLevel, lv, lv == 8 || lv == 9 ? 1000 : 300, 1, h);
                    int f = 0, w = 0, t = 0;
                    log.Append($"| {lv} | {(h != null ? "+ " + Helper : "+ 2 fire")} |");
                    foreach (var n in Starters)
                    {
                        var o = row.TryGetValue(n, out var x) ? x : default;
                        f += o.fights;
                        w += o.wins;
                        t += o.turns;
                        log.Append($" {o.WinRate:P0} |");
                    }
                    log.Append($" {w / (float)Mathf.Max(1, f):P0} | {t / (float)Mathf.Max(1, f):0.0} |\n");
                }
            log.Append("\n| Trail | Tall-grass tiles | Battles | Starter level at the temple (p10 / mean / p90) |\n|---|---|---|---|\n");
            foreach (float wander in new[] { 1f, 2f, 3f })
            {
                var t = Trail(1000, wander);
                log.Append($"| x{wander:0} grass (hub {t.grassHub}, Cinderwood {t.grassCinder}) | {(t.grassHub + t.grassCinder) * wander:0} | hub {t.battlesHub:0.0}, Cinderwood {t.battlesCinder:0.0} | " +
                           $"{t.levelP10:0} / {t.level:0.0} / {t.levelP90:0} |\n");
            }
            Debug.Log(log.ToString());
        }

        /// <summary>Grid search over the Keeper's attack / defense (HP base 88 = 2.2x a base-40 at Lv 10), Lv 10, Lv 9 party.</summary>
        public static void Tune()
        {
            var keeper = Object.Instantiate(Load("Hearthstag"));
            var log = new StringBuilder("[KeeperSim] Tune (Lv 10, HP base 88), win % of a Lv 9 party of 3 (rows atk, cols def 8..16):\n");
            keeper.baseStats.hp = 88;
            for (int atk = 6; atk <= 12; atk++)
            {
                log.Append($"atk {atk,2}:");
                for (int def = 8; def <= 16; def += 2)
                {
                    keeper.baseStats.attack = atk;
                    keeper.baseStats.defense = def;
                    int f = 0, w = 0;
                    foreach (var kv in Run(keeper, 10, 9, 300))
                    {
                        f += kv.Value.fights;
                        w += kv.Value.wins;
                    }
                    log.Append($" {w * 100 / Mathf.Max(1, f),3}");
                }
                log.Append('\n');
            }
            Object.DestroyImmediate(keeper);
            Debug.Log(log.ToString());
        }

        /// <summary>
        /// Step 9f sweep: the arrival level per XP multiplier (shortest trail and x2 grass), then a grid over the
        /// Keeper's HP / attack / defense with the lowest and highest per-starter win % of a Lv 8 and a Lv 9 party with
        /// Steambara. Targets: arrival 8-9, every starter 60-75%.
        /// </summary>
        public static void Sweep()
        {
            var log = new StringBuilder("[KeeperSim] Sweep\n| XP x | Cinderwood wild +Lv | shortest (p10 / mean / p90) | x2 grass |\n|---|---|---|---|\n");
            var config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ProgressionConfig>("Assets/_Project/Data/Battle/Progression.asset"));
            foreach (float m in new[] { 2.5f, 3f, 3.5f })
                foreach (int bonus in new[] { 0, 2 })
                {
                    config.rewardMultiplier = m;
                    var a = Trail(600, 1f, 3, config, bonus);
                    var b = Trail(600, 2f, 3, config, bonus);
                    log.Append($"| {m:0.0} | +{bonus} | {a.levelP10:0} / {a.level:0.0} / {a.levelP90:0} | {b.levelP10:0} / {b.level:0.0} / {b.levelP90:0} |\n");
                }
            Object.DestroyImmediate(config);
            var keeper = Object.Instantiate(Load("Hearthstag"));
            var helper = Load(Helper);
            log.Append("\nKeeper Lv 10 grid, + Steambara parties (water leads), win % Kilnpot / Pebbloop / Pufflet at Lv 8 | Lv 9\n");
            foreach (int hp in new[] { 105, 120, 135, 150 })
                foreach (int atk in new[] { 10, 12 })
                    foreach (int def in new[] { 9, 12, 15 })
                    {
                        keeper.baseStats.hp = hp;
                        keeper.baseStats.attack = atk;
                        keeper.baseStats.defense = def;
                        log.Append($"hp {hp,3} atk {atk,2} def {def,2}:");
                        foreach (int lv in new[] { 8, 9 })
                        {
                            var row = Run(keeper, 10, lv, 150, 1, helper);
                            foreach (var n in Starters) log.Append($" {Mathf.RoundToInt((row.TryGetValue(n, out var o) ? o.WinRate : 0f) * 100f),3}");
                            if (lv == 8) log.Append("  |");
                        }
                        log.Append('\n');
                    }
            Object.DestroyImmediate(keeper);
            Debug.Log(log.ToString());
        }

        /// <summary>The Keeper's level in MR_Main's Keeper Hall (or 10 when the scene isn't open).</summary>
        internal static int KeeperLevel
        {
            get
            {
                var hall = KeeperHall.FindById(KeeperHall.KeeperId);
                return hall != null ? hall.keeperLevel : 10;
            }
        }
    }
}
