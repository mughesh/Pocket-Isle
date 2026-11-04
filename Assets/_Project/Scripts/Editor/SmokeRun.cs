using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Overnight Nov 5: a flat smoke run of the whole game in Editor Play (no headset). Tools > Pocket Isle > QA > Smoke Run
    /// (or the bridge: exec:Diorama.EditorTools.SmokeRun.Run). It backs up both saves, turns Standalone "Initialize XR on
    /// Startup" off for the run, starts Play and drives the game with the dev tools and synthetic input through: a new
    /// adventure (title, opening, every lesson, the first battle + catch), the shrine and the lodge (poke, swap), the pause
    /// menu, Settings and the Journal, an evolution in a synthetic palm, save / quit / continue, every Cinderwood and
    /// Lilymere moment with its Keeper and shard, then the Quick Journey end to end (send-off, Galewick's moments, Galeowl,
    /// the wind shard, the end card, See the ending and the finale). Every beat has a goal and a time limit.
    /// It logs every exception and error (missing cue ids are errors), warning spam, no progress for 20 s, a line shown
    /// out of its context, overlapping tablets and a battle label inside a creature. Report: Logs/SmokeRun/report.md
    /// (+ run.log). Saves and the XR setting are put back when Play stops.
    /// </summary>
    [InitializeOnLoad]
    public static class SmokeRun
    {
        const string PendingKey = "PocketIsle.SmokeRun.Pending", RestoreKey = "PocketIsle.SmokeRun.Restore", XrKey = "PocketIsle.SmokeRun.XrWas",
            SlotKey = "PocketIsle.SmokeRun.LastSlot", OnlyKey = "PocketIsle.SmokeRun.Only";
        const string OutDir = "Logs/SmokeRun", BackupDir = OutDir + "/savebak";
        const float StuckSeconds = 20f, LineSkipSeconds = 2.2f;

        static SmokeRun()
        {
            EditorApplication.playModeStateChanged += OnMode;
        }

        // ---- entry ----

        [MenuItem("Tools/Pocket Isle/QA/Smoke Run")]
        public static void Run() => Launch(null);

        /// <summary>Bridge: only the phases whose names start with the words in Temp/ClaudeBridge/arg.txt (comma separated).</summary>
        public static void RunOnly()
        {
            const string arg = "Temp/ClaudeBridge/arg.txt";
            Launch(File.Exists(arg) ? File.ReadAllText(arg).Trim() : null);
        }

        [MenuItem("Tools/Pocket Isle/QA/Smoke Run: Stop")]
        public static void Stop()
        {
            if (_run != null) _run.Finish("stopped by hand");
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        static void Launch(string only)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[SmokeRun] Stop Play first: the run starts from a fresh Play with both saves set aside.");
                return;
            }
            Directory.CreateDirectory(OutDir);
            BackUpSaves();
            SessionState.SetInt(XrKey, SetXrInit(false) ? 1 : 0);
            SessionState.SetString(OnlyKey, only ?? "");
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(RestoreKey, true);
            Debug.Log("[SmokeRun] Saves backed up, XR init off for the run: entering Play.");
            EditorApplication.isPlaying = true;
        }

        static void OnMode(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            {
                SessionState.SetBool(PendingKey, false);
                var only = SessionState.GetString(OnlyKey, "");
                _run = new Runner(string.IsNullOrEmpty(only) ? null : only.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray());
                _run.Start();
            }
            else if (s == PlayModeStateChange.ExitingPlayMode && _run != null)
            {
                _run.Finish("Play stopped");
            }
            else if (s == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(RestoreKey, false))
            {
                SessionState.SetBool(RestoreKey, false);
                RestoreSaves();
                SetXrInit(SessionState.GetInt(XrKey, 1) == 1);
                if (Time.timeScale != 1f) Time.timeScale = 1f;
                Debug.Log("[SmokeRun] Saves restored, XR init back as it was.");
            }
        }

        static Runner _run;

        // ---- saves and the XR setting ----

        static readonly SaveSlot[] Slots = { SaveSlot.Adventure, SaveSlot.Quick };

        static void BackUpSaves()
        {
            Directory.CreateDirectory(BackupDir);
            foreach (var f in Directory.GetFiles(BackupDir)) File.Delete(f);
            foreach (var slot in Slots)
            {
                var path = SaveSlots.PathOf(slot);
                if (File.Exists(path)) File.Copy(path, Path.Combine(BackupDir, Path.GetFileName(path)), true);
                else File.WriteAllText(Path.Combine(BackupDir, Path.GetFileName(path) + ".none"), "");
            }
            SessionState.SetInt(SlotKey, PlayerPrefs.GetInt("pocketisle.lastSlot", -1));
            foreach (var slot in Slots) SaveSlots.Delete(slot);
        }

        static void RestoreSaves()
        {
            foreach (var slot in Slots)
            {
                var path = SaveSlots.PathOf(slot);
                var kept = Path.Combine(BackupDir, Path.GetFileName(path));
                if (File.Exists(kept)) File.Copy(kept, path, true);
                else if (File.Exists(kept + ".none") && File.Exists(path)) File.Delete(path);
            }
            int last = SessionState.GetInt(SlotKey, -1);
            if (last < 0) PlayerPrefs.DeleteKey("pocketisle.lastSlot");
            else PlayerPrefs.SetInt("pocketisle.lastSlot", last);
            PlayerPrefs.Save();
        }

        /// <summary>Standalone "Initialize XR on Startup" (by reflection, as Step12aDebug.XrInit); returns the old value.</summary>
        static bool SetXrInit(bool on)
        {
            var per = Type.GetType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget, Unity.XR.Management.Editor");
            var get = per != null ? per.GetMethod("XRGeneralSettingsForBuildTarget") : null;
            var s = get != null ? get.Invoke(null, new object[] { BuildTargetGroup.Standalone }) as UnityEngine.Object : null;
            var prop = s != null ? s.GetType().GetProperty("InitManagerOnStart") : null;
            if (prop == null) return true;
            bool was = (bool)prop.GetValue(s);
            if (was == on) return was;
            prop.SetValue(s, on);
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
            return was;
        }

        // ---- the run ----

        sealed class Beat
        {
            public string phase, name;
            public Action start;
            public Func<bool> done;
            public Action pump;
            public float timeout = 60f, every = 0.8f;
            public bool critical;
            public Func<string> info;
        }

        sealed class Note
        {
            public string kind, beat, text;
            public int count = 1;
        }

        sealed class Runner
        {
            readonly List<Beat> _beats = new List<Beat>();
            readonly string[] _only;
            readonly List<string> _results = new List<string>();
            readonly Dictionary<string, Note> _notes = new Dictionary<string, Note>();
            readonly StringBuilder _log = new StringBuilder();
            readonly Dictionary<string, int> _warnings = new Dictionary<string, int>();
            int _index = -1, _passed, _failed, _pumps;
            double _beatStart, _nextPump, _runStart, _sigSince;
            string _sig, _skipPhase, _lineText;
            double _lineSince;
            bool _finished, _stuckNoted;

            public Runner(string[] only)
            {
                _only = only;
            }

            Beat Current => _index >= 0 && _index < _beats.Count ? _beats[_index] : null;
            string BeatName => Current != null ? $"{Current.phase} / {Current.name}" : "setup";
            double Now => EditorApplication.timeSinceStartup;
            double Elapsed => Now - _beatStart;

            public void Start()
            {
                Build();
                if (_only != null) _beats.RemoveAll(b => b.phase != "Setup" && !_only.Any(o => b.phase.StartsWith(o, StringComparison.OrdinalIgnoreCase)));
                Application.logMessageReceived += OnLog;
                _runStart = Now;
                Debug.Log($"[SmokeRun] Start: {_beats.Count} beats{(_only != null ? " (only " + string.Join(", ", _only) + ")" : "")}.");
                EditorApplication.update += Tick;
                Next();
            }

            void Next()
            {
                _index++;
                while (Current != null && _skipPhase != null && Current.phase == _skipPhase)
                {
                    _results.Add($"| {Current.phase} | {Current.name} | SKIP | an earlier beat of this phase failed |");
                    _index++;
                }
                _skipPhase = null;
                if (Current == null)
                {
                    Finish("all beats run");
                    return;
                }
                _beatStart = Now;
                _nextPump = Now + 0.5;
                _stuckNoted = false;
                _sigSince = Now;
                Debug.Log($"[SmokeRun] Beat {_index + 1}/{_beats.Count}: {BeatName}.");
                Safe(Current.start, "start");
            }

            void Tick()
            {
                if (_finished) return;
                if (!EditorApplication.isPlaying)
                {
                    Finish("Play stopped");
                    return;
                }
                var b = Current;
                if (b == null) return;
                if (EditorApplication.isPaused) EditorApplication.isPaused = false; // Error Pause must not freeze the run
                SkipLongLines();
                WatchProgress();
                bool done = false;
                try { done = b.done == null ? Elapsed >= b.timeout : b.done(); }
                catch (Exception e) { Note("exception", $"beat check threw {e.GetType().Name}: {e.Message}"); }
                if (done)
                {
                    _passed++;
                    _results.Add($"| {b.phase} | {b.name} | ok | {Elapsed:0} s{Info(b)} |");
                    Debug.Log($"[SmokeRun] ok: {BeatName} ({Elapsed:0} s){Info(b)}.");
                    Next();
                    return;
                }
                if (Elapsed > b.timeout)
                {
                    _failed++;
                    string state = State();
                    _results.Add($"| {b.phase} | {b.name} | **FAIL** | not done in {b.timeout:0} s{Info(b)}; {state} |");
                    Note("stuck", $"not done in {b.timeout:0} s{Info(b)}: {state}");
                    Debug.LogWarning($"[SmokeRun] FAIL: {BeatName}: not done in {b.timeout:0} s; {state}");
                    if (b.critical) _skipPhase = b.phase;
                    Next();
                    return;
                }
                if (Now >= _nextPump)
                {
                    _nextPump = Now + b.every;
                    _pumps++;
                    AutoPump();
                    Safe(b.pump, "pump");
                }
            }

            static string Info(Beat b)
            {
                if (b.info == null) return "";
                try { return "; " + b.info(); }
                catch { return ""; }
            }

            void Safe(Action a, string what)
            {
                if (a == null) return;
                try { a(); }
                catch (Exception e)
                {
                    Note("exception", $"smoke {what} threw {e.GetType().Name}: {e.Message} ({FirstFrame(e.StackTrace)})");
                }
            }

            static string FirstFrame(string stack)
            {
                if (string.IsNullOrEmpty(stack)) return "";
                var line = stack.Split('\n').FirstOrDefault(l => l.Contains(".cs:")) ?? stack.Split('\n')[0];
                return line.Trim();
            }

            /// <summary>Lines are read in ~2 s (the run skips the rest): the flow waits for talk to end in many places.</summary>
            void SkipLongLines()
            {
                var d = Dialogue_;
                string text = d != null && d.bubble != null && Dialogue.IsTalking ? d.bubble.Text : null;
                if (text != _lineText)
                {
                    _lineText = text;
                    _lineSince = Now;
                    return;
                }
                if (text != null && Now - _lineSince > LineSkipSeconds)
                {
                    _lineSince = Now;
                    d.DebugFinishLine();
                }
            }

            /// <summary>No change in where the explorer is, the lesson, the battle, the line or any puzzle for 20 s: noted once per beat.</summary>
            void WatchProgress()
            {
                var sig = Signature();
                if (sig != _sig)
                {
                    _sig = sig;
                    _sigSince = Now;
                    return;
                }
                if (!_stuckNoted && Now - _sigSince > StuckSeconds && Current != null && Current.done != null)
                {
                    _stuckNoted = true;
                    Note("no progress", $"nothing changed for {StuckSeconds:0} s: {State()}");
                }
            }

            static string Signature()
            {
                var g = GameState.Instance;
                var e = g != null ? g.explorer : null;
                var ob = UnityEngine.Object.FindFirstObjectByType<Onboarding>();
                var d = Director;
                return $"{(g != null ? g.Where : "-")}|{(e != null ? e.Tile.ToString() : "-")}|{(ob != null ? ob.CurrentId : "-")}|{(d != null && d.InBattle)}" +
                       $"|{(d != null && d.CurrentBattle != null ? d.CurrentBattle.wild.Hp + "/" + d.CurrentBattle.player.Hp : "-")}|{Dialogue.IsTalking}" +
                       $"|{PuzzleState.Describe()}|{TitleScreen.Showing}|{Opening.InProgress}|{FinaleDirector.Running}|{SceneSwap.IsBusy}";
            }

            static string State()
            {
                var g = GameState.Instance;
                var e = g != null ? g.explorer : null;
                var ob = UnityEngine.Object.FindFirstObjectByType<Onboarding>();
                var d = Director;
                var runes = RuneBoard.DebugList().Replace("\n", " ").Trim();
                return $"where {(g != null ? g.Where : "-")}, place {Places.Now}, tile {(e != null ? $"c{e.Tile.x} r{e.Tile.y}" : "-")}, locked {(e != null && e.Locked)}, " +
                       $"title {TitleScreen.Showing}, opening {Opening.InProgress}, lesson '{(ob != null ? ob.CurrentId : "-")}', battle {(d != null && d.InBattle)}" +
                       $"{(d != null && d.AwaitingSendOut ? " (awaiting send-out)" : "")}, talking {Dialogue.IsTalking}, objective '{Dialogue.CurrentObjective}', " +
                       $"menu {GameMenu.IsOpen}, choice {ChoiceBoard.IsOpen}, finale {FinaleDirector.Running}, runes [{runes}]";
            }

            public void Finish(string why)
            {
                if (_finished) return;
                _finished = true;
                EditorApplication.update -= Tick;
                Application.logMessageReceived -= OnLog;
                if (Current != null && _index < _beats.Count)
                    for (int i = _index; i < _beats.Count; i++)
                        _results.Add($"| {_beats[i].phase} | {_beats[i].name} | not run | {why} |");
                foreach (var kv in _warnings)
                    if (kv.Value >= 15) Note("warning spam", $"{kv.Value} x {kv.Key}");
                WriteReport(why);
                if (_run == this) _run = null;
                if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            }

            void WriteReport(string why)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"# Smoke run {DateTime.Now:yyyy-MM-dd HH:mm}");
                sb.AppendLine();
                sb.AppendLine($"Ended: {why}. {(Now - _runStart) / 60.0:0.0} min. Beats: {_passed} ok, {_failed} failed, {_beats.Count - _passed - _failed} skipped / not run.");
                sb.AppendLine();
                sb.AppendLine("| Phase | Beat | Result | Notes |");
                sb.AppendLine("|---|---|---|---|");
                foreach (var r in _results) sb.AppendLine(r.Replace("\n", " "));
                sb.AppendLine();
                sb.AppendLine("## Problems");
                sb.AppendLine();
                if (_notes.Count == 0) sb.AppendLine("None.");
                foreach (var n in _notes.Values.OrderBy(n => n.kind))
                    sb.AppendLine($"- **{n.kind}** ({n.beat}){(n.count > 1 ? $" x{n.count}" : "")}: {n.text.Replace("\n", " ")}");
                File.WriteAllText(Path.Combine(OutDir, "report.md"), sb.ToString());
                File.WriteAllText(Path.Combine(OutDir, "run.log"), _log.ToString());
                Debug.Log($"[SmokeRun] Done ({why}): {_passed} ok, {_failed} failed, {_notes.Count} problem(s). Report {OutDir}/report.md.");
            }

            void Note(string kind, string text)
            {
                string key = kind + "|" + text;
                if (_notes.TryGetValue(key, out var n)) n.count++;
                else _notes[key] = new Note { kind = kind, beat = BeatName, text = text };
            }

            static readonly Regex ShowLine = new Regex(@"\[Dialogue\] Show (\S+) \((\w+), in ([^)]*)\) at (\S+)");

            void OnLog(string message, string stack, LogType type)
            {
                if (_finished) return;
                double t = Now - _runStart;
                string first = message.Split('\n')[0];
                _log.Append($"{t,7:0.0} [{type}] {first}\n");
                if (message.StartsWith("[SmokeRun]")) return;
                switch (type)
                {
                    case LogType.Exception:
                    case LogType.Error:
                    case LogType.Assert:
                        Note(type == LogType.Exception ? "exception" : "error", $"{Trim(first)} ({FirstFrame(stack)})");
                        break;
                    case LogType.Warning:
                        string key = Regex.Replace(Trim(first), @"-?\d+([.,]\d+)?", "#");
                        _warnings[key] = _warnings.TryGetValue(key, out int c) ? c + 1 : 1;
                        break;
                }
                var m = ShowLine.Match(first);
                if (m.Success)
                {
                    string context = m.Groups[3].Value.Trim();
                    if (context.Length > 0 && context != "any" && !Places.Holds(context))
                        Note("line out of context", $"{m.Groups[1].Value} ({m.Groups[2].Value}, in {context}) shown at {m.Groups[4].Value}");
                }
                if (first.StartsWith("[Step14jDebug] Tablet layouts"))
                {
                    var bad = Regex.Match(message, @"(\d+) problem\(s\)");
                    if (bad.Success && bad.Groups[1].Value != "0") Note("tablet overlap", message.Replace("\n", " ; "));
                }
            }

            static string Trim(string s) => s.Length > 220 ? s.Substring(0, 220) + "..." : s;

            // ---- generic pumping: things any beat may run into ----

            bool _adventurePicked;

            void AutoPump()
            {
                if (ChoiceBoard.IsOpen) ChoiceBoard.DebugPick(true);
                // The title: a new player's comes up after the shard is touched; a returning player's asks to Continue.
                if (TitleScreen.Showing)
                {
                    var list = RuneBoard.DebugList();
                    if (list.Contains("[Full adventure]")) _adventurePicked |= RuneBoard.DebugPress("Full adventure");
                    else if (list.Contains("[Continue")) RuneBoard.DebugPress("Continue");
                }
            }

            // ---- the beats ----

            void Add(string phase, string name, Action start, Func<bool> done, float timeout, Action pump = null, float every = 0.8f,
                bool critical = false, Func<string> info = null) =>
                _beats.Add(new Beat { phase = phase, name = name, start = start, done = done, timeout = timeout, pump = pump, every = every, critical = critical, info = info });

            /// <summary>Waits <paramref name="seconds"/> (things settle; errors are still caught).</summary>
            void Wait(string phase, string name, float seconds, Action start = null, Action pump = null) =>
                _beats.Add(new Beat { phase = phase, name = name, start = start, done = null, timeout = seconds, pump = pump });

            void Build()
            {
                Add("Setup", "seat camera, cheats (invincible), wild battles off", () =>
                {
                    BattleDebug.SeatCamera();
                    DevCheats.Invincible = true;
                    DevCheats.WildOff = true;
                }, () => true, 5f);

                BuildNewGame();
                BuildLodge();
                BuildMenus();
                BuildEvolution();
                BuildSaveContinue();
                BuildCinderwood();
                BuildLilymere();
                BuildQuick();
            }

            // ---- a new adventure ----

            void BuildNewGame()
            {
                const string P = "New game";
                Add(P, "opening: the shard, then the title: Full adventure", null, () => _adventurePicked && !TitleScreen.Showing, 120f, PumpLessons, 1f, true);
                Add(P, "opening: shard, pinch, move, two hands, starter", null,
                    () => !Opening.InProgress && Lesson is "walk" or "far" or "look" or "grass" || Onboarding.Finished && !Opening.InProgress, 300f, PumpLessons, 1f, true,
                    () => $"lesson '{Lesson}'");
                Add(P, "walk lessons: swipe, far, look", null, () => Lesson == "grass" || Onboarding.Finished, 120f, PumpLessons, 1f, true, () => $"lesson '{Lesson}'");
                Add(P, "first battle: send, rune, catch", () => DevCheats.WildOff = false,
                    () => !InBattle && Caught > 0 && !Onboarding.Running, 240f, () =>
                    {
                        if (!InBattle && Lesson == "grass") WalkIntoTutorialGrass();
                        PumpLessons();
                        PumpBattle(true);
                    }, 1.2f, true, () => $"caught {Caught}, lesson '{Lesson}'");
                Add(P, "tutorial over", () => DevCheats.WildOff = true, () => Onboarding.Finished && !InBattle && !Onboarding.Running, 60f, PumpLessons);
            }

            int _grassTries;
            double _grassAt;

            void WalkIntoTutorialGrass()
            {
                var g = GameState.Instance;
                if (g == null || g.explorer == null || g.explorer.island == null || Now < _grassAt) return;
                _grassAt = Now + 8;
                var world = g.explorer.island.world;
                if (_grassTries++ >= 3)
                {
                    BattleDebug.Grass();
                    Note("info", "walking into the tutorial grass didn't start the battle in 3 tries: started one on the spot");
                    return;
                }
                if (world != null && world.TryGetLandmark(g.onboarding != null ? g.onboarding.patchLandmark : "firstGrass", out var patch))
                {
                    bool ok = g.explorer.GoTo(patch);
                    Debug.Log($"[SmokeRun] Walk into the tutorial grass at c{patch.x} r{patch.y}: {ok}.");
                }
            }

            static string Lesson
            {
                get
                {
                    var ob = UnityEngine.Object.FindFirstObjectByType<Onboarding>();
                    return ob != null ? ob.CurrentId : null;
                }
            }

            static void PumpLessons()
            {
                var o = UnityEngine.Object.FindFirstObjectByType<Opening>();
                if (o != null && Opening.InProgress)
                {
                    o.DebugPoke();
                    o.DebugChoose(0);
                }
                var ob = UnityEngine.Object.FindFirstObjectByType<Onboarding>();
                if (ob == null || ob.CurrentId == null) return;
                switch (ob.CurrentId)
                {
                    case "pinch": ob.DebugPinch(); break;
                    case "starter": if (o != null) o.DebugChoose(0); break;
                    case "grass":
                    case "send":
                    case "rune":
                    case "catch":
                    case "free":
                        break; // the battle itself
                    default: ob.DebugComplete(); break;
                }
            }

            static EncounterDirector Director => UnityEngine.Object.FindFirstObjectByType<EncounterDirector>();
            static Dialogue Dialogue_ => UnityEngine.Object.FindFirstObjectByType<Dialogue>();
            static Party Party_ => UnityEngine.Object.FindFirstObjectByType<Party>();
            static bool InBattle => Director != null && Director.InBattle;
            static int Caught => Party_ != null ? Party_.members.Count + Party_.storage.Count - 1 : 0;

            int _moves;
            bool _labelChecked;

            /// <summary>Sends out, uses move 1; for a catch, the wild one to the red after a move, then the cradle pod.</summary>
            void PumpBattle(bool catchIt)
            {
                var d = Director;
                if (d == null || !d.InBattle)
                {
                    _moves = 0;
                    _labelChecked = false;
                    return;
                }
                var party = Party_;
                if (d.AwaitingSendOut && party != null)
                {
                    d.DebugPick(Mathf.Max(0, party.NextFighter(-1)));
                    return;
                }
                var b = d.CurrentBattle;
                if (b == null) return;
                if (!_labelChecked && d.PlayerOut && _moves > 0)
                {
                    _labelChecked = true;
                    CheckLabels();
                }
                if (catchIt && b.CatchUnlocked && _moves > 0)
                {
                    BattleDebug.ThrowCatchPod();
                    return;
                }
                if (catchIt && _moves > 0 && b.wild.Hp > b.wild.MaxHp * 0.25f) BattleDebug.WildToRed();
                var runes = UnityEngine.Object.FindFirstObjectByType<BattleRunes>();
                if (runes != null && d.PlayerOut)
                {
                    runes.Choose(0);
                    _moves++;
                }
            }

            /// <summary>Each battle label's middle against each creature's drawn bounds: a label inside a creature is a problem.</summary>
            void CheckLabels()
            {
                var hud = UnityEngine.Object.FindFirstObjectByType<BattleHud>();
                if (hud == null) return;
                var actors = UnityEngine.Object.FindObjectsByType<StageActor>(FindObjectsSortMode.None);
                foreach (Transform c in hud.transform)
                {
                    if (!c.name.StartsWith("HP ") || !c.gameObject.activeInHierarchy) continue;
                    var rs = c.GetComponentsInChildren<Renderer>();
                    if (rs.Length == 0) continue;
                    var lb = rs[0].bounds;
                    foreach (var r in rs) lb.Encapsulate(r.bounds);
                    // Labels draw on top (step 14j); inside means in the body's core (its drawn box shrunk to half), not
                    // just in its axis-aligned box (a Keeper's covers half the stage).
                    foreach (var a in actors)
                        if (a.TryGetVisualBounds(out var vb) && new Bounds(vb.center, vb.size * 0.5f).Contains(lb.center))
                            Note("label inside a mesh", $"{c.name} (centre {lb.center:F3}) inside {a.name} ({vb.center:F3} size {vb.size:F3})");
                }
                Step14jDebug.Layouts();
            }

            // ---- the shrine and the lodge ----

            void BuildLodge()
            {
                const string P = "Lodge";
                Add(P, "to the hub shrine, into the lodge", () => Go("Hub: shrine"), () => Places.Now == "lodge" && !SceneSwap.IsBusy, 45f, () =>
                {
                    if (Elapsed > 3 && !SceneSwap.IsBusy && !SceneSwap.IsInside) BattleDebug.PickShrine();
                }, 3f, true);
                Wait(P, "settle (the lodge welcome)", 6f);
                Add(P, "poke an Isling: hop + card", BattleDebug.LodgeInspect, () => Elapsed > 4, 10f);
                Add(P, "swap: rim pod 2 into the room", BattleDebug.LodgeDropPod2, () => Elapsed > 6, 12f);
                Add(P, "the Journal tablet on the rim", () => Step14hDebug_Journal("lodge"), () => JournalBoard.IsOpen, 10f);
                Add(P, "close it", () => { if (JournalBoard.Instance != null) JournalBoard.Instance.Close(); }, () => !JournalBoard.IsOpen, 10f);
                Add(P, "leave the lodge", BattleDebug.LeaveLodge, () => !SceneSwap.IsInside && !SceneSwap.IsBusy, 30f, null, 1f, true);
            }

            static void Step14hDebug_Journal(string what)
            {
                if (what == "lodge")
                {
                    var book = UnityEngine.Object.FindFirstObjectByType<LodgeJournalBook>();
                    if (book != null) book.Open("smoke");
                    else Debug.LogWarning("[SmokeRun] No lodge Journal book.");
                }
            }

            // ---- menus ----

            void BuildMenus()
            {
                const string P = "Menus";
                Add(P, "pause menu opens", () => Menu?.TryOpen(), () => GameMenu.IsOpen, 10f, () => { if (!GameMenu.IsOpen) Menu?.TryOpen(); }, 2f, true);
                Add(P, "menu layout", Step14jDebug.Layouts, () => Elapsed > 1.5, 5f);
                Add(P, "Settings", () => RuneBoard.DebugPress("Settings"), () => Elapsed > 3, 6f, null, 1f, false, () => RuneBoard.DebugList().Replace("\n", " "));
                Add(P, "Settings layout, back", () =>
                {
                    Step14jDebug.Layouts();
                    if (!RuneBoard.DebugPress("Back")) RuneBoard.DebugPress("Done");
                }, () => Elapsed > 2, 5f);
                Add(P, "close the menu", () => Menu?.Close("smoke"), () => !GameMenu.IsOpen, 8f, () => Menu?.Close("smoke"), 2f);
                Add(P, "Journal from the menu", () => Menu?.DebugOpenJournal(), () => JournalBoard.IsOpen, 10f, null, 1f, true);
                Add(P, "Journal: water page, wind page, a card", () =>
                {
                    var j = JournalBoard.Instance;
                    j.DebugElement(Element.Water);
                    j.DebugElement(Element.Wind);
                    j.DebugElement(Element.Fire);
                    j.DebugPick(0);
                }, () => Elapsed > 3, 6f);
                Add(P, "Journal layout, close", () =>
                {
                    Step14jDebug.Layouts();
                    JournalBoard.Instance.Close();
                }, () => !JournalBoard.IsOpen && !GameMenu.IsOpen, 10f, () =>
                {
                    if (JournalBoard.IsOpen) JournalBoard.Instance.Close();
                    if (GameMenu.IsOpen) Menu?.Close("smoke");
                }, 2f);
            }

            static GameMenu Menu => UnityEngine.Object.FindFirstObjectByType<GameMenu>();

            // ---- evolution in the palm ----

            string _leadBefore;

            void BuildEvolution()
            {
                const string P = "Evolution";
                Add(P, "make the lead ready", () =>
                {
                    var g = GameState.Instance;
                    _leadBefore = Party_ != null && Party_.Lead != null ? Party_.Lead.species.name : null;
                    Debug.Log("[SmokeRun] " + (g != null ? g.LeadReadyToEvolve() : "no GameState"));
                }, () => GrowthDirector.Instance != null && GrowthDirector.Instance.Due >= 0, 30f, null, 1f, true, () => $"lead {_leadBefore}");
                Add(P, "palm up: the show, the new form", () =>
                {
                    var g = GrowthDirector.Instance;
                    var island = g.pods.island.transform;
                    Vector3 toward = Vector3.ProjectOnPlane(island.forward, Vector3.up).normalized;
                    g.debugPalmUp = 1f;
                    g.debugPalm = g.pods.HomeWorld(g.Due) + Vector3.up * 0.1f + toward * 0.08f;
                }, () => Party_ != null && Party_.members.Any(m => m.species.name != _leadBefore) && Party_.members[0].species.name != _leadBefore &&
                         GrowthDirector.Instance != null && !GrowthDirector.Instance.Showing, 60f, null, 1f, false,
                    () => $"lead now {(Party_ != null && Party_.Lead != null ? Party_.Lead.species.name : "-")}");
                Add(P, "palm off", () => { if (GrowthDirector.Instance != null) GrowthDirector.Instance.debugPalm = null; }, () => Elapsed > 3, 5f);
            }

            // ---- save, quit, continue ----

            string _savedWhere, _savedTile;
            int _savedParty;

            void BuildSaveContinue()
            {
                const string P = "Save + continue";
                Add(P, "save now", () =>
                {
                    var g = GameState.Instance;
                    BattleDebug.SaveNow();
                    _savedWhere = g.Where;
                    _savedTile = g.explorer.Tile.ToString();
                    _savedParty = Party_.members.Count;
                }, () => Elapsed > 1.5, 5f);
                Add(P, "quit (reload) and Continue", () => GameState.Instance.Reload(),
                    () => !TitleScreen.Showing && GameState.Instance != null && GameState.Instance.explorer != null && Elapsed > 6 && !Opening.InProgress, 60f,
                    () => { if (TitleScreen.Showing) RuneBoard.DebugPress("Continue"); }, 2f, true);
                Add(P, "same place and party", () => BattleDebug.SeatCamera(), () =>
                {
                    var g = GameState.Instance;
                    return g.Where == _savedWhere && g.explorer.Tile.ToString() == _savedTile && Party_.members.Count == _savedParty;
                }, 8f, null, 1f, false, () => $"saved {_savedWhere} {_savedTile} party {_savedParty}, now {GameState.Instance.Where} {GameState.Instance.explorer.Tile} party {Party_.members.Count}");
                Add(P, "cheats back on", () =>
                {
                    DevCheats.Invincible = true;
                    DevCheats.WildOff = true;
                }, () => true, 3f);
            }

            // ---- realms ----

            void BuildCinderwood()
            {
                const string P = "Cinderwood";
                Add(P, "travel to Cinderwood", () => Go("CW: arrival"), () => Places.Holds("cinderwood") && !SceneSwap.IsBusy, 30f, null, 1f, true);
                Wait(P, "settle (realm card, arrival)", 6f);
                foreach (var id in new[] { "cw.flame", "cw.hollow", "cw.ember", GameState.LogId, GameState.BellowsId })
                    Moment(P, id);
                OtherMoments(P, "cinderwood", new[] { "cw.flame", "cw.hollow", "cw.ember", GameState.LogId, GameState.BellowsId });
                Keeper(P, GameState.KeeperId, "Forge Hall", ShardHolder.FireId);
                Shard(P, ShardHolder.FireId);
            }

            void BuildLilymere()
            {
                const string P = "Lilymere";
                Add(P, "travel to Lilymere", () => Go("LM: arrival"), () => Places.Holds("lilymere") && !SceneSwap.IsBusy, 30f, null, 1f, true);
                Wait(P, "settle (realm card, arrival)", 6f);
                OtherMoments(P, "lilymere", new string[0]);
                Keeper(P, "lm.keeper", "LM: lake temple", ShardHolder.WaterId);
                Shard(P, ShardHolder.WaterId);
            }

            void BuildQuick()
            {
                const string P = "Quick Journey";
                Add(P, "preset: at the gift (reload, Continue)", () =>
                {
                    DevCheats.WildOff = true;
                    GameState.Instance.QuickPreset(GameState.QuickStage.Gift);
                }, () => QuickJourney.Active && !TitleScreen.Showing && GameState.Instance != null && GameState.Instance.CurrentMap == GameState.Instance.hub &&
                         Elapsed > 6, 60f, () => { if (TitleScreen.Showing) RuneBoard.DebugPress("Continue"); }, 2f, true);
                Add(P, "seat camera", () =>
                {
                    BattleDebug.SeatCamera();
                    DevCheats.Invincible = true;
                    DevCheats.WildOff = true;
                }, () => true, 3f);
                Add(P, "send-off to the wind gate", null, () => PuzzleState.IsSolved(QuickJourney.GiftId), 60f, null, 1f, true);
                Add(P, "through the wind gate to Galewick", () => Go("Hub: wind gate"), () => Places.Holds("galewick") && !SceneSwap.IsBusy, 45f, () =>
                {
                    if (Elapsed < 3 || SceneSwap.IsBusy || Places.Holds("galewick")) return;
                    var gates = UnityEngine.Object.FindFirstObjectByType<ShardGates>();
                    int i = gates != null ? gates.IndexOf("wind") : -1;
                    if (i >= 0) gates.Pick(i, "smoke");
                }, 3f, true);
                Wait(P, "settle (realm card, arrival)", 6f);
                foreach (var (id, _) in QuickJourneyDirector.Route)
                    if (id != TowerDoor.TowerId)
                        Moment(P, id == "gw.mill" ? "gw.arrive" : id, id == QuickJourneyDirector.RidgeId);
                Moment(P, "gw.mill");
                Add(P, "tower door opens, into the hall", () => Go("GW: tower"), () => Places.Now == "hall" && !SceneSwap.IsBusy, 60f, () =>
                {
                    // Open: walk in (the dev spot inside); shut: try it (three kites fly).
                    if (PuzzleState.IsSolved(TowerDoor.TowerId))
                    {
                        if (!SceneSwap.IsBusy && Elapsed > 8) Go("GW: tower hall");
                        return;
                    }
                    var d = UnityEngine.Object.FindFirstObjectByType<TowerDoor>();
                    if (d != null && Elapsed > 2) d.DebugTry();
                }, 3f, true);
                Keeper(P, "gw.keeper", null, ShardHolder.WindId);
                Shard(P, ShardHolder.WindId);
                Add(P, "end card", null, () => RuneBoard.DebugList().Contains("See the ending"), 90f, null, 1f, true);
                Add(P, "See the ending", () => RuneBoard.DebugPress("See the ending"), () => FinaleDirector.Running, 60f, null, 1f, true);
                Add(P, "the finale to its end", () => FinaleDirector.Moment += OnFinaleMoment, () => _finaleDone, 420f, PumpFinale, 1.5f, false,
                    () => FinaleDirector.Instance != null ? FinaleDirector.Instance.Describe() : "no finale");
            }

            bool _finaleDone;

            void OnFinaleMoment(string m)
            {
                Debug.Log($"[SmokeRun] Finale moment {m}.");
                if (m == "done") _finaleDone = true;
            }

            void PumpFinale()
            {
                var f = FinaleDirector.Instance;
                if (f == null) return;
                if (!FinaleDirector.Running && Elapsed > 10)
                {
                    _finaleDone = true;
                    return;
                }
                f.DebugGazePinch();
                // A palm under the Heart's forehead socket (the goodbye); in front of it otherwise (it lands).
                var island = f.island;
                var local = island.transform.InverseTransformPoint(f.heart.SocketWorld);
                var tile = island.LocalToTile(local);
                var map = new Vector3(tile.x, (local.y - island.Content.capHeight) / island.map.tileSize, tile.y);
                f.debugPalmUp = 1f;
                f.debugPalm = f.Step == FinaleStep.Goodbye ? map + new Vector3(0f, -0.4f, 0.3f) : map + new Vector3(0f, -0.6f, 3.5f);
            }

            // ---- moments, Keepers, shards ----

            void Moment(string phase, string id, bool optional = false)
            {
                int k = 0;
                Add(phase, $"moment {id}", () =>
                {
                    var p = Piece(id);
                    if (p == null || p.IsSolved) return;
                    GoNear(p);
                }, () => PuzzleState.IsSolved(id) || Piece(id) == null && Elapsed > 1, optional ? 40f : 75f, () =>
                {
                    var p = Piece(id);
                    if (p == null || !p.Here || SceneSwap.IsBusy || InBattle) return;
                    if (Elapsed > 2 && !p.Usable && k % 6 == 5) GoNear(p);
                    Drive(p, k++);
                }, 0.7f, false, () =>
                {
                    var p = Piece(id);
                    return p == null ? "no piece in the scene" : $"{p.GetType().Name}, here {p.Here}, usable {p.Usable}, stage {PuzzleState.Stage(id)}";
                });
                // Not solved by its hook: noted, then solved so the story can go on.
                Add(phase, $"(go on past {id})", () =>
                {
                    if (PuzzleState.IsSolved(id) || Piece(id) == null) return;
                    Note(optional ? "info" : "moment not solved", $"{id}: its debug hook didn't solve it in time; solved by hand to go on");
                    PuzzleState.Solve(id, Math.Max(1, PuzzleState.Stage(id)));
                }, () => Elapsed > 1, 3f);
            }

            /// <summary>Every other piece on the map (not the statue sockets or the Keeper's strings), in scene order.</summary>
            void OtherMoments(string phase, string mapName, string[] except)
            {
                var g = GameState.Instance != null ? GameState.Instance : UnityEngine.Object.FindFirstObjectByType<GameState>();
                var all = UnityEngine.Object.FindObjectsByType<HandPuzzle>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
                var ids = all.Where(p => p.map != null && p.map.name.ToLowerInvariant().Contains(mapName) && !(p is StatueSocket) && !(p is KeeperUntangle) &&
                                         !except.Contains(p.id))
                    .Select(p => p.id).Distinct().ToList();
                ids.Sort(StoryOrder);
                foreach (var id in ids) Moment(phase, id);
            }

            static readonly string[] Order = { "lm.stream", "lm.rain", "lm.channel", "lm.pads", "lm.scoop", "lm.basin" };

            static int StoryOrder(string a, string b)
            {
                int ia = Array.FindIndex(Order, o => a.StartsWith(o)), ib = Array.FindIndex(Order, o => b.StartsWith(o));
                if (ia < 0) ia = 99;
                if (ib < 0) ib = 99;
                return ia != ib ? ia.CompareTo(ib) : string.CompareOrdinal(a, b);
            }

            /// <summary>The running piece with this id (an inactive copy in the scene never started: skipped).</summary>
            static HandPuzzle Piece(string id)
            {
                foreach (var p in UnityEngine.Object.FindObjectsByType<HandPuzzle>(FindObjectsSortMode.None))
                    if (p.id == id && p.isActiveAndEnabled) return p;
                return null;
            }

            static void GoNear(HandPuzzle p)
            {
                var g = GameState.Instance;
                if (g == null || p.map == null || SceneSwap.IsBusy) return;
                var home = Vector2Int.RoundToInt(p.Home);
                var tile = SpiritTrail.TrailEnd(p.map, p, home + new Vector2Int(0, 3));
                if (SceneSwap.IsInside) g.swap.ReturnNow();
                if (g.explorer.island.world != p.map) g.swap.TravelNow(p.map, tile, new Vector2Int(0, -1));
                else g.explorer.Teleport(tile, new Vector2Int(0, -1));
                Debug.Log($"[SmokeRun] To {p.id} ({p.GetType().Name}) at c{tile.x} r{tile.y} (home {home}).");
            }

            static void Drive(HandPuzzle p, int k)
            {
                switch (p)
                {
                    case FlameCupPiece f: if (k % 8 == 0) f.DebugCup(6f); break;
                    case LanternHollowPiece l:
                        for (int i = 0; i < l.lanterns.Length; i++) l.DebugTouch(i);
                        break;
                    case ForgeEmberPiece e:
                        if (k % 2 == 0) e.DebugLookPinchVent();
                        else e.DebugLookPinchFirebox();
                        break;
                    case VineBurnPiece v:
                        if (k % 2 == 0) v.DebugLookPinchVent();
                        else v.DebugLookPinchVine();
                        break;
                    case CarryPiece c: if (k % 4 == 0 && c.spots.Count > 0) c.DebugPlace((k / 4) % c.spots.Count); break;
                    case PumpPiece pump: pump.DebugStroke(); break;
                    case WheelPiece w:
                        for (int i = 0; i < 3; i++) w.DebugFlick(0.9f);
                        break;
                    case KitePiece kite: kite.DebugFly(k % kite.kites.Length); break;
                    case CloudBankPiece bank: bank.DebugGust(1f); break;
                    case RainGarden rain:
                        // A palm over each bud in turn (the buds' middle reaches only some of them).
                        if (k % 6 == 0 && rain.buds.Length > 0) rain.DebugPalm(rain.buds[(k / 6) % rain.buds.Length], 4f, true);
                        break;
                    case LilyPadBridge pads: pads.DebugSteer(k % pads.slots.Length); break;
                    case ScoopPour scoop: if (k % 4 == 0) scoop.DebugLookPour(); break;
                    case StatueSocket socket: socket.DebugPlace(); break;
                    case KeeperUntangle knots: knots.DebugSnap(k % knots.knots.Length); break;
                }
            }

            void Keeper(string phase, string keeperId, string spot, string shardId)
            {
                Add(phase, $"Keeper {keeperId}: meet, fight, shard", () =>
                {
                    DevCheats.OneHitKo = true;
                    if (spot != null) Go(spot);
                }, () => ShardHolder.Holds(shardId) || PuzzleState.IsSolved(shardId), 240f, () =>
                {
                    var hall = Hall(keeperId);
                    if (hall == null || SceneSwap.IsBusy) return;
                    if (hall.untangle != null && !hall.untangle.IsSolved && hall.untangle.Here) Drive(hall.untangle, _pumps);
                    PumpBattle(false);
                    if (!InBattle) hall.CollectShard("smoke run");
                }, 1.2f, true, () => $"calm {PuzzleState.IsSolved(keeperId)}, held {ShardHolder.Holds(shardId)}, place {Places.Now}");
                Add(phase, "cheats: one-hit KO off", () => DevCheats.OneHitKo = false, () => !InBattle && Elapsed > 2, 30f);
            }

            static KeeperHall Hall(string keeperId)
            {
                foreach (var h in UnityEngine.Object.FindObjectsByType<KeeperHall>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (h.keeperId == keeperId) return h;
                return null;
            }

            void Shard(string phase, string shardId)
            {
                Add(phase, $"home to the statue with {shardId}", () => Go("Hub: statue"), () => Places.Holds("hub") && !SceneSwap.IsBusy, 40f, () =>
                {
                    if (!Places.Holds("hub") && !SceneSwap.IsBusy && !InBattle) Go("Hub: statue");
                }, 3f, true);
                Moment(phase, shardId);
            }

            static void Go(string label)
            {
                var s = GameState.Instance;
                if (s == null) return;
                foreach (var spot in s.Spots)
                    if (spot.label == label)
                    {
                        if (!s.GoTo(spot)) Debug.LogWarning($"[SmokeRun] Go to {label}: refused (busy or in a battle).");
                        return;
                    }
                Debug.LogWarning($"[SmokeRun] No spot '{label}'.");
            }
        }
    }
}
