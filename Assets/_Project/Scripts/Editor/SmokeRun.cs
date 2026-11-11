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
                // Panel review (Nov 11): the word "shots" in the arg is not a phase: it turns on a seat shot at the end of
                // every beat and on every new line (Logs/Panel/NNN_*.png + index.md with the line, draw calls, triangles).
                if (only != null && only.Any(o => o.Equals("shots", StringComparison.OrdinalIgnoreCase)))
                {
                    _panelShots = true;
                    only = only.Where(o => !o.Equals("shots", StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (only.Length == 0) only = null;
                    Directory.CreateDirectory(PanelDir);
                    foreach (var f in Directory.GetFiles(PanelDir)) File.Delete(f);
                }
                _only = only;
            }

            const string PanelDir = "Logs/Panel";
            readonly bool _panelShots;
            int _panelCount;
            string _panelLine;
            double _panelLineAt = -1;
            readonly StringBuilder _panelIndex = new StringBuilder("# Panel shots\n\n| # | Beat | When | Line | Draws | Tris |\n|---|---|---|---|---|---|\n");

            void PanelShot(string when)
            {
                if (!_panelShots) return;
                try
                {
                    var cam = Camera.main;
                    if (cam == null) return;
                    string beat = Current != null ? Current.phase + " - " + Current.name : "setup";
                    string safe = Regex.Replace(beat.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
                    if (safe.Length > 40) safe = safe.Substring(0, 40);
                    string file = $"{_panelCount:000}_{safe}_{when}.png";
                    BattleDebug.Render($"{PanelDir}/{file}", cam.transform.position, cam.transform.rotation);
                    var d = Dialogue_;
                    string line = d != null && d.bubble != null && Dialogue.IsTalking ? d.bubble.Text : "";
                    line = (line ?? "").Replace("\n", " ").Replace("|", "/");
                    _panelIndex.Append($"| {_panelCount:000} | {beat} | {when} | {line} | {UnityEditor.UnityStats.batches} | {UnityEditor.UnityStats.triangles} |\n");
                    _panelCount++;
                    File.WriteAllText($"{PanelDir}/index.md", _panelIndex.ToString());
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SmokeRun] panel shot failed: {e.Message}");
                }
            }

            /// <summary>A shot 0.9 s after each new line starts (typed far enough to read), once per line.</summary>
            void PanelLines()
            {
                if (!_panelShots) return;
                var d = Dialogue_;
                string text = d != null && d.bubble != null && Dialogue.IsTalking ? d.bubble.Text : null;
                if (text == null) { _panelLine = null; _panelLineAt = -1; return; }
                string key = text.Length > 24 ? text.Substring(0, 24) : text;
                if (key != _panelLine) { _panelLine = key; _panelLineAt = Now + 0.9; return; }
                if (_panelLineAt > 0 && Now >= _panelLineAt) { _panelLineAt = -1; PanelShot("line"); }
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
                if (Directory.Exists($"{OutDir}/layout")) Directory.Delete($"{OutDir}/layout", true);
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
                _beatLayout.Clear();
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
                PanelLines();
                SkipLongLines();
                WatchProgress();
                WatchRules();
                WatchLayout();
                bool done = false;
                try { done = b.done == null ? Elapsed >= b.timeout : b.done(); }
                catch (Exception e) { Note("exception", $"beat check threw {e.GetType().Name}: {e.Message}"); }
                if (done && _beatLayout.Count > 0)
                {
                    // Nov 12: a text overlapped something during the beat: the beat fails (the run goes on).
                    _failed++;
                    _results.Add($"| {b.phase} | {b.name} | **FAIL** | layout: {string.Join("; ", _beatLayout).Replace("|", "/")} |");
                    Debug.LogWarning($"[SmokeRun] FAIL: {BeatName}: layout: {string.Join("; ", _beatLayout)}");
                    PanelShot("done");
                    Next();
                    return;
                }
                if (done)
                {
                    _passed++;
                    _results.Add($"| {b.phase} | {b.name} | ok | {Elapsed:0} s{Info(b)} |");
                    PanelShot("done");
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

            // Step 21: Mugesh's notes as rules (Assets/Docs/Regressions.md), watched the whole run; a broken one is noted
            // "rule" (once per beat), so a later step can't quietly undo an earlier fix.
            double _shardMusicSince = -1, _menuSince = -1;

            // ---- Nov 12: no text overlaps another, a creature, the explorer or a prop (B7) ----
            readonly List<string> _beatLayout = new List<string>();
            readonly HashSet<string> _layoutShot = new HashSet<string>();
            double _layoutNext;
            int _layoutShots;

            void WatchLayout()
            {
                if (Now < _layoutNext) return;
                _layoutNext = Now + 0.5;
                List<string> hits;
                try { hits = TextLayoutCheck.Check(Camera.main); }
                catch (Exception e)
                {
                    Note("exception", $"layout check threw {e.GetType().Name}: {e.Message}");
                    return;
                }
                foreach (var h in hits)
                {
                    // One note per pair per beat (the share in brackets changes frame to frame).
                    string pair = Regex.Replace(h, @" \([^)]*%[^)]*\)", "");
                    if (_beatLayout.Any(x => Regex.Replace(x, @" \([^)]*%[^)]*\)", "") == pair)) continue;
                    _beatLayout.Add(h);
                    Note("layout", pair);
                    if (!_layoutShot.Add(pair) || _layoutShots >= 60) continue;
                    try
                    {
                        var cam = Camera.main;
                        string safe = Regex.Replace(h.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
                        if (safe.Length > 50) safe = safe.Substring(0, 50);
                        Directory.CreateDirectory($"{OutDir}/layout");
                        BattleDebug.Render($"{OutDir}/layout/{_layoutShots:000}_{safe}.png", cam.transform.position, cam.transform.rotation);
                        _layoutShots++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[SmokeRun] layout shot failed: {e.Message}");
                    }
                }
            }

            void WatchRules()
            {
                // R1 (Nov 6, broken by step 20): no music while only the shard floats.
                bool shardMusic = Opening.ShardOnly && Diorama.Audio.AudioService.CurrentMusic != null;
                if (!shardMusic) _shardMusicSince = -1;
                else if (_shardMusicSince < 0) _shardMusicSince = Now;
                else if (Now - _shardMusicSince > 3) Note("rule", $"R1 music {Diorama.Audio.AudioService.CurrentMusic} while only the shard floats");
                // R2, R3 (Nov 8): the menu pauses every sound; in a battle the battle row steps aside.
                if (!GameMenu.IsOpen) _menuSince = -1;
                else if (_menuSince < 0) _menuSince = Now;
                else if (Now - _menuSince > 1)
                {
                    if (!AudioListener.pause) Note("rule", "R2 the menu is open but sound isn't paused");
                    var runes = UnityEngine.Object.FindFirstObjectByType<BattleRunes>();
                    var row = runes != null ? runes.transform.Find("Rune Row") : null;
                    if (InBattle && row != null && row.gameObject.activeInHierarchy) Note("rule", "R3 the menu is open over the battle row");
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
                // The cheats are statics: left on, they'd leak into the next Play and the EditMode tests.
                DevCheats.Invincible = false;
                DevCheats.OneHitKo = false;
                DevCheats.WildOff = false;
                Dialogue.CleanHints = false;
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
                if (_only != null && _only.Any(o => o.StartsWith("Step27", StringComparison.OrdinalIgnoreCase))) Step27Shots();
                if (_only != null && _only.Any(o => o.StartsWith("MenuOverBattle", StringComparison.OrdinalIgnoreCase))) MenuOverBattle();
                if (_only != null && _only.Any(o => o.StartsWith("BattleLayout", StringComparison.OrdinalIgnoreCase))) BattleLayout();
                if (_only != null && _only.Any(o => o.StartsWith("Step29", StringComparison.OrdinalIgnoreCase))) Step29();
            }

            // ---- step 29: HP rings, the pod card's line, a healing bloom, the tired warning (bridge: RunOnly with arg "Step29") ----

            Vector3 _bloomWorld;
            double _usedAt = 999;
            bool _puffShot, _waveShot;
            readonly HashSet<string> _hallShots = new HashSet<string>();
            bool _chose29;
            double _choseAt29;

            void Step29()
            {
                const string P = "Step29";
                static void Hurt()
                {
                    var party = Party_;
                    if (party == null) return;
                    for (int i = 0; i < party.members.Count; i++)
                    {
                        var c = party.members[i];
                        if (c == null) continue;
                        c.currentHp = i == 0 ? Mathf.Max(1, c.MaxHp / 5) : i == 1 ? 0 : Mathf.Max(1, c.MaxHp * 3 / 5);
                    }
                    party.NotifyChanged();
                }
                static bool AllFull() => Party_ != null && Party_.members.All(c => c == null || c.currentHp >= c.MaxHp);
                Add(P, "preset (reload, Continue)", () =>
                {
                    DevCheats.WildOff = true;
                    GameState.Instance.QuickPreset(GameState.QuickStage.Gift);
                }, () => QuickJourney.Active && !TitleScreen.Showing && Elapsed > 6, 60f, () => { if (TitleScreen.Showing) RuneBoard.DebugPress("Continue"); }, 2f, true);
                Add(P, "go to the tower, hurt the team", () =>
                {
                    BattleDebug.SeatCamera();
                    DevCheats.Invincible = true;
                    DevCheats.WildOff = true;
                    Go("GW: tower");
                }, () => Elapsed > 4 && !SceneSwap.IsBusy, 20f);
                Add(P, "a ring on every pod (low blinks, fainted grey)", Hurt, () =>
                {
                    if (Elapsed < 2) return false;
                    var rack = UnityEngine.Object.FindFirstObjectByType<PodRack>();
                    int pods = Party_.members.Count(c => c != null && c.species != null);
                    Step27Debug.Shot("29_rings");
                    Nov11Debug.Rings("nov11_rings");
                    if (rack == null || rack.HpRings == null || rack.HpRings.Shown != pods)
                    {
                        Note("problem", $"Step29 rings: {(rack != null && rack.HpRings != null ? rack.HpRings.Shown : -1)} shown for {pods} pods");
                        return true;
                    }
                    return true;
                }, 10f, null, 0.5f, true);
                Add(P, "the hurt pod's card says where to heal", () =>
                {
                    var rack = UnityEngine.Object.FindFirstObjectByType<PodRack>();
                    var c = Party_.members[0];
                    rack.ShowCardFor(c, () => rack.SlotWorld(0) + Vector3.up * 0.06f, 6f);
                }, () =>
                {
                    if (Elapsed < 1) return false;
                    Step27Debug.Shot("29_card");
                    if (PodCard.HealTip(Party_.members[0]) != PodCard.TiredLine) Note("problem", "Step29 card: the hurt pod has no tired line");
                    return true;
                }, 8f);
                Add(P, "at the tower door: the Spirit says heal first", () =>
                {
                    TempleRules.TiredCheck(UnityEngine.Object.FindFirstObjectByType<TowerDoor>());
                }, () => Elapsed > 1.5, 6f);
                Add(P, "the bloom heals everyone", () =>
                {
                    var explorer = GameState.Instance.explorer;
                    if (HealBloom.TryGetTile(explorer.island.world, out var t)) explorer.Teleport(t + new Vector2Int(0, 2), new Vector2Int(0, -1));
                }, () =>
                {
                    var bloom = UnityEngine.Object.FindFirstObjectByType<HealBloom>();
                    if (bloom == null) return false;
                    if (Elapsed < 2.5) return false;
                    if (Elapsed < 3.1)
                    {
                        Step27Debug.Shot("29_bloom_open");
                        if (HealBloom.TryGetTile(GameState.Instance.explorer.island.world, out var bt))
                            _bloomWorld = GameState.Instance.explorer.island.transform.TransformPoint(
                                GameState.Instance.explorer.island.TileToLocal(bt, GameState.Instance.explorer.island.Content.capHeight));
                        Nov11Debug.Close("nov11_bloom_idle", _bloomWorld, 0.2f, 0.05f);
                        return false;
                    }
                    if (bloom.Heals == 0) { bloom.Use("smoke"); _usedAt = Elapsed; }
                    if (!_puffShot && Elapsed - _usedAt > 0.3) { _puffShot = true; Nov11Debug.Close("nov11_bloom_puff", _bloomWorld, 0.3f, 0.06f); }
                    if (!_waveShot && Elapsed - _usedAt > 0.95) { _waveShot = true; Nov11Debug.Close("nov11_bloom_wave", GameState.Instance.explorer.WorldPosition, 0.32f, 0.08f); }
                    if (Elapsed < 4.5) return false;
                    Step27Debug.Shot("29_bloom_healed");
                    return bloom.Heals > 0 && AllFull() && !bloom.IsOpenHere;
                }, 15f, null, 0.5f, true, () =>
                {
                    var b = UnityEngine.Object.FindFirstObjectByType<HealBloom>();
                    return $"heals {(b != null ? b.Heals : -1)}, full {AllFull()}";
                });

                // Seat shots for the report: a junction stop (chevrons), a fishing bank (ripples), the battle's two turns.
                Add(P, "shot: a junction stop on the Still Isle", () => Go("Hub: wind gate"), () =>
                {
                    if (Elapsed < 4 || SceneSwap.IsBusy) return false;
                    var e = GameState.Instance.explorer;
                    if (Elapsed < 4.6)
                    {
                        e.Teleport(new Vector2Int(15, 5), new Vector2Int(0, 1));
                        UnityEngine.Object.FindFirstObjectByType<MicrogestureInput>().DebugRaise(ThumbGesture.SwipeBack, GestureSource.Keyboard);
                        return false;
                    }
                    if (!e.IsStill || Elapsed < 6) return false;
                    Step27Debug.Shot("29_junction");
                    if (e.PlanEnd != SmartStep.Junction) Note("problem", $"Step29: the walk ended at {e.PlanEnd}, c{e.Tile.x} r{e.Tile.y}");
                    return true;
                }, 25f, null, 0.1f, false, () => $"explorer c{GameState.Instance.explorer.Tile.x} r{GameState.Instance.explorer.Tile.y}, end {GameState.Instance.explorer.PlanEnd}");
                Add(P, "back to Galewick", () => Go("GW: tower"), () => Elapsed > 4 && !SceneSwap.IsBusy, 20f);
                Add(P, "shot: a Galewick fishing bank", () =>
                {
                    var e = GameState.Instance.explorer;
                    var banks = Fishing.Spots(e.island.world).ToList();
                    if (banks.Count > 1) e.Teleport(banks[1], new Vector2Int(0, -1));
                }, () =>
                {
                    if (Elapsed < 2.5) return false;
                    Step27Debug.Shot("29_fishbank");
                    return true;
                }, 8f);
                Add(P, "a battle: the moves sink on the wild turn", () =>
                {
                    DevCheats.Invincible = true;
                    BattleDebug.Grass();
                }, () =>
                {
                    var d = Director;
                    if (d != null && d.AwaitingSendOut && Party_ != null) d.DebugPick(Mathf.Max(0, Party_.NextFighter(-1)));
                    var runes = UnityEngine.Object.FindFirstObjectByType<BattleRunes>();
                    if (d == null || !d.InBattle || !d.PlayerOut || runes == null || Elapsed < 6) return false;
                    if (runes.Up > 0.95f && !_chose29)
                    {
                        Step27Debug.Shot("29_yourturn");
                        _chose29 = true;
                        runes.Choose(0);
                        _choseAt29 = Elapsed;
                        return false;
                    }
                    if (_chose29 && Elapsed - _choseAt29 > 1.1 && runes.Up < 0.05f)
                    {
                        runes.Choose(1); // off-turn: only a shake
                        Step27Debug.Shot("29_offturn");
                        if (runes.OffTurnPresses == 0) Note("problem", "Step29: an off-turn press did not shake");
                        return true;
                    }
                    return false;
                }, 60f, null, 0.2f, false);
            }

            // ---- Nov 10: moves, battle line and name labels never overlap, from several heads (bridge: RunOnly with arg "BattleLayout") ----

            void BattleLayout()
            {
                const string P = "BattleLayout";
                Add(P, "preset (reload, Continue)", () => GameState.Instance.QuickPreset(GameState.QuickStage.Gift),
                    () => QuickJourney.Active && !TitleScreen.Showing && Elapsed > 6, 60f, () => { if (TitleScreen.Showing) RuneBoard.DebugPress("Continue"); }, 2f, true);
                Add(P, "a battle, the moves up", () =>
                {
                    BattleDebug.SeatOffset = new Vector3(0f, 0.55f, 0.45f);
                    BattleDebug.SeatCamera();
                    DevCheats.Invincible = true;
                    BattleDebug.Grass();
                }, () =>
                {
                    var d = Director;
                    return d != null && d.InBattle && d.PlayerOut && UnityEngine.Object.FindFirstObjectByType<BattleRunes>().TryGetRowBounds(out _) && Elapsed > 8;
                }, 60f, () =>
                {
                    var d = Director;
                    if (d != null && d.AwaitingSendOut && Party_ != null) d.DebugPick(Mathf.Max(0, Party_.NextFighter(-1)));
                }, 1f, true);
                foreach (var (name, seat) in new[]
                         {
                             ("seat", new Vector3(0f, 0.55f, 0.45f)), ("steep", new Vector3(0f, 0.72f, 0.2f)), ("low", new Vector3(0f, 0.36f, 0.58f)),
                             ("left", new Vector3(0.22f, 0.5f, 0.42f)), ("right", new Vector3(-0.22f, 0.5f, 0.42f)), ("close", new Vector3(0f, 0.4f, 0.28f)),
                         })
                    Add(P, $"nothing overlaps from {name}", () => BattleDebug.SeatOffset = seat, () =>
                    {
                        if (Elapsed < 2.5) return false;
                        var hits = Step24Debug.LayoutOverlaps();
                        Step27Debug.Shot($"layout_{name}");
                        if (hits.Count > 0) Note("problem", $"BattleLayout {name}: {string.Join("; ", hits)}");
                        return true;
                    }, 10f, null, 0.8f, true);
                Add(P, "seat camera back", () => BattleDebug.SeatOffset = new Vector3(0f, 0.55f, 0.45f), () => true, 3f);
            }

            // ---- Nov 10: the battle's on-top labels hide behind the menu (bridge: RunOnly with arg "MenuOverBattle") ----

            void MenuOverBattle()
            {
                const string P = "MenuOverBattle";
                static bool Labels(bool shown)
                {
                    var hud = UnityEngine.Object.FindFirstObjectByType<BattleHud>();
                    if (hud == null) return false;
                    var bars = hud.transform.Cast<Transform>().Where(t => t.name.StartsWith("HP ")).ToList();
                    return bars.Count > 0 && bars.All(t => t.gameObject.activeSelf == shown);
                }
                Add(P, "preset (reload, Continue)", () => GameState.Instance.QuickPreset(GameState.QuickStage.Gift),
                    () => QuickJourney.Active && !TitleScreen.Showing && Elapsed > 6, 60f, () => { if (TitleScreen.Showing) RuneBoard.DebugPress("Continue"); }, 2f, true);
                Add(P, "a battle, its labels up", () =>
                {
                    BattleDebug.SeatCamera();
                    DevCheats.Invincible = true;
                    BattleDebug.Grass();
                }, () => InBattle && Elapsed > 6 && Labels(true), 40f, null, 0.8f, true);
                Add(P, "the menu hides them", () => Menu?.TryOpen(), () =>
                {
                    if (!GameMenu.IsOpen || Elapsed < 1.5 || !Labels(false)) return false;
                    Step27Debug.Shot("menu_over_battle");
                    return true;
                }, 12f, null, 0.8f, true);
                Add(P, "closing brings them back", () => Menu?.Close("smoke"), () => !GameMenu.IsOpen && Elapsed > 1 && Labels(true), 12f, null, 0.8f, true);
            }

            // ---- step 27: seat shots of every door (bridge: RunOnly with arg "Step27 shots,<tag>") ----

            void Step27Shots()
            {
                const string P = "Step27 shots";
                string tag = _only.Length > 1 ? _only[1] : "shot";
                Add(P, "preset (reload, Continue)", () =>
                {
                    DevCheats.WildOff = true;
                    GameState.Instance.QuickPreset(GameState.QuickStage.Gift);
                }, () => QuickJourney.Active && !TitleScreen.Showing && Elapsed > 6, 60f, () => { if (TitleScreen.Showing) RuneBoard.DebugPress("Continue"); }, 2f, true);
                Add(P, "seat camera, every door open", () =>
                {
                    BattleDebug.SeatCamera();
                    DevCheats.Invincible = true;
                    DevCheats.WildOff = true;
                    PuzzleState.Solve(TowerDoor.TowerId, 1);
                    PuzzleState.Solve(GameState.BellowsId, 9);
                    PuzzleState.Solve(LilymereLagoon.RisenId, 2);
                    PuzzleState.Restore(PuzzleState.ToSaved());
                }, () => true, 3f);
                foreach (var (label, spot) in new[]
                         {
                             ("tower", "GW: tower"), ("forge", "CW: forge door"), ("temple", "LM: temple"),
                             ("gate_wind", "Hub: wind gate"), ("gate_fire", "Hub: fire gate"), ("gate_water", "Hub: water gate"),
                         })
                {
                    string l = label, s = spot;
                    Add(P, $"shot {l}", () => Go(s), () =>
                    {
                        if (Elapsed < 4 || SceneSwap.IsBusy) return false;
                        Step27Debug.Shot($"27_{tag}_{l}");
                        return true;
                    }, 20f);
                }
                void PickIn(string name, string spot, Vector2Int from, Action pick) =>
                    Add(P, $"look + pinch walks into {name}", () => Go(spot), () =>
                    {
                        if (Places.Now == "hall" && !SceneSwap.IsBusy && Elapsed > 3) return true;
                        if (Elapsed > 2 && Elapsed < 2.6 && !SceneSwap.IsBusy && Places.Now != "hall")
                        {
                            GameState.Instance.explorer.Teleport(from, new Vector2Int(0, -1));
                            pick();
                        }
                        return false;
                    }, 25f, null, 0.8f, false, () => $"explorer c{GameState.Instance.explorer.Tile.x} r{GameState.Instance.explorer.Tile.y}, place {Places.Now}");
                PickIn("the tower", "GW: tower", new Vector2Int(18, 9), () => UnityEngine.Object.FindFirstObjectByType<TowerDoor>().DebugPick());
                PickIn("the forge", "CW: forge door", new Vector2Int(21, 8), () => UnityEngine.Object.FindFirstObjectByType<ForgeBellows>().DebugPick());
                PickIn("the lake temple", "LM: temple", new Vector2Int(21, 12), () => UnityEngine.Object.FindFirstObjectByType<TempleDoor>().DebugPick());
                Add(P, "shot tower from the side", () =>
                {
                    Go("GW: tower");
                }, () =>
                {
                    if (Elapsed < 2 || SceneSwap.IsBusy) return false;
                    GameState.Instance.explorer.Teleport(new Vector2Int(13, 9), new Vector2Int(0, -1));
                    if (Elapsed < 4) return false;
                    Step27Debug.Shot($"27_{tag}_tower_side");
                    return true;
                }, 20f);
                Add(P, "gazed door", () =>
                {
                    Go("GW: tower");
                    Step27Debug.HoldGaze(true);
                }, () =>
                {
                    if (Elapsed < 4 || SceneSwap.IsBusy) return false;
                    Step27Debug.Shot($"27_{tag}_gaze_door");
                    Step27Debug.HoldGaze(false);
                    return true;
                }, 20f);
            }

            // ---- a new adventure ----

            void BuildNewGame()
            {
                const string P = "New game";
                Add(P, "opening: the shard, then the title: Full adventure", null, () => _adventurePicked && !TitleScreen.Showing, 120f, PumpLessons, 1f, true);
                Add(P, "opening: shard, pinch, move, two hands, starter", null,
                    () => !Opening.InProgress && Lesson is "walk" or "far" or "look" or "grass" || Onboarding.Finished && !Opening.InProgress, 300f, PumpLessons, 1f, true,
                    () => $"lesson '{Lesson}'");
                // Step 28: from the start tile each swipe walks with no tap for 20 s; it never stops and nothing reacts.
                foreach (var (name, swipe) in new[] { ("forward", ThumbGesture.SwipeForward), ("left", ThumbGesture.SwipeLeft), ("right", ThumbGesture.SwipeRight), ("back", ThumbGesture.SwipeBack) })
                {
                    string bad = null;
                    int moved = 0;
                    var last = Vector2Int.zero;
                    Add(P, $"lesson walk {name}: 20 s, no tap", () =>
                    {
                        var e = GameState.Instance.explorer;
                        e.Teleport(e.SpawnTile, new Vector2Int(0, -1));
                        last = e.Tile;
                        UnityEngine.Object.FindFirstObjectByType<MicrogestureInput>().DebugRaise(swipe, GestureSource.Keyboard);
                    }, () =>
                    {
                        if (bad != null || Elapsed < 20) return false;
                        return moved >= 30;
                    }, 24f, () =>
                    {
                        var e = GameState.Instance.explorer;
                        if (e.Tile != last) moved++;
                        last = e.Tile;
                        if (e.Heading == Vector2Int.zero) bad = "it stopped";
                        else if (InBattle) bad = "a battle started";
                        else if (SceneSwap.IsBusy) bad = "a door or gate reacted";
                        else if (!Onboarding.LessonWalking) bad = $"not lesson walking (lesson '{Lesson}')";
                    }, 0.25f, true, () => bad ?? $"{moved} tiles, c{GameState.Instance.explorer.Tile.x} r{GameState.Instance.explorer.Tile.y}");
                }
                Add(P, "a thumb tap stops the lesson walk", () => UnityEngine.Object.FindFirstObjectByType<MicrogestureInput>().DebugRaise(ThumbGesture.Tap, GestureSource.Keyboard),
                    () => Elapsed > 1 && GameState.Instance.explorer.Heading == Vector2Int.zero, 6f, null, 0.5f, true);
                // Nov 10: the lesson's card greys out behind the menu, off the tablets.
                Add(P, "menu over a lesson: the card greys", () => Menu?.TryOpen(), () =>
                {
                    var b = UnityEngine.Object.FindFirstObjectByType<DialogueBubble>();
                    if (!GameMenu.IsOpen || Elapsed < 2 || b == null || !b.IsParked) return false;
                    Step27Debug.Shot("menu_over_lesson");
                    return true;
                }, 15f, null, 0.8f, true);
                Add(P, "menu closed: the lesson's line is back", () => Menu?.Close("smoke"), () =>
                {
                    var b = UnityEngine.Object.FindFirstObjectByType<DialogueBubble>();
                    return !GameMenu.IsOpen && Elapsed > 2.5 && b != null && !b.IsParked;
                }, 15f, null, 0.8f, true);
                Add(P, "walk lessons: swipe, far, look", null, () => Lesson == "grass" || Onboarding.Finished, 180f, PumpLessons, 1f, true, () => $"lesson '{Lesson}'");
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
                    case Onboarding.PracticeLesson: ob.DebugPracticeStop(); break;
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
                // Nov 11 panel (R48): the end card is never interrupted by the evolve objective (a due starter is normal here).
                Add(P, "end card owns the stage", () =>
                {
                    if (Dialogue.CurrentObjective == GrowthDirector.ReadyLine) Note("rule", "R48 evolve.ready pinned over the end card");
                    var g = UnityEngine.Object.FindFirstObjectByType<GrowthDirector>();
                    if (g != null && g.Due >= 0) Note("rule", $"R48 pod {g.Due + 1} glows under the end card");
                }, () => Elapsed > 2, 5f);
                // Step 18: since Nov 7 the dreamed fire and water shards wait for the player (45 s each, then by themselves):
                // place each one as it's offered.
                Add(P, "See the ending", () => RuneBoard.DebugPress("See the ending"), () => FinaleDirector.Running, 60f, () =>
                {
                    foreach (var s in UnityEngine.Object.FindObjectsByType<StatueSocket>(FindObjectsSortMode.None))
                        if (s.DreamOffering && !s.IsSolved) s.DebugPlace();
                }, 1.5f, true);
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
                    // Nov 11: seat and close shots of each hall (the pillars and walls' fit and finish).
                    if (!_hallShots.Contains(keeperId) && Elapsed > 4 && Places.Now == "hall")
                    {
                        _hallShots.Add(keeperId);
                        Step27Debug.Shot($"nov11_hall_{keeperId}");
                        var isl = GameState.Instance.explorer.island;
                        Nov11Debug.Close($"nov11_hall_{keeperId}_close", isl.transform.TransformPoint(isl.LocalBounds.center), 0.4f, 0.05f);
                    }
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
