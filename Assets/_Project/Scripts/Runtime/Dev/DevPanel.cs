using System;
using System.Collections.Generic;
using Oculus.Interaction.Input;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// The in-headset dev / QA panel (step 9e). Only in the Editor and QA builds (scripting define POCKET_DEV); a release
    /// build compiles it out and the object removes itself. Step 9g: opened from the game menu's Dev rune (the left hand's
    /// menu gesture now opens the game menu) or F2; F2 closes it. A plain 2D panel opens behind the island (Oct 27; the rays
    /// reach it), facing the player, and follows only when the island moves (<see cref="behindIsland"/> off: a little past
    /// arm's reach in front, toward the dominant hand, coming back in front when the head turns well away). Its edge moves it like the island's rim (pinch the frame, or point at it and pinch, then drag);
    /// it keeps that spot (relative to the head's facing) until the app closes. Buttons work with a poke (a fingertip
    /// touches the panel) and with each hand's own ray (ISDK pointer pose: a thin line to the panel, pinch to press; this
    /// panel only); in the Editor also with a mouse click. No look + pinch (step 9e: it fought the game's own gaze).
    /// Destructive ones (new game, reset story, load) ask for a second press. Everything goes through
    /// <see cref="GameState"/> (Quick Journey reuses it).
    /// Tabs: Go to, Party, Encounter, Story, Save, Tools (see Assets/Docs/QA-Guide.md).
    /// </summary>
    public class DevPanel : MonoBehaviour
    {
        public GameState state;
        public IslandBuilder island;
        [Tooltip("Pinches from a hand that is grabbing the rim or a puzzle are ignored.")]
        public IslandHandle grabGuard;
        [Tooltip("Diorama/Hud material (panel and buttons).")]
        public Material hudMaterial;
        [Tooltip("Panel size (m).")]
        public Vector2 size = new Vector2(0.36f, 0.32f);
        [Tooltip("Distance from the head (m): an easy reach for pokes, not in the face.")]
        public float distance = 0.55f;
        [Tooltip("Degrees toward the dominant hand, and below the eyes.")]
        public float sideAngle = 14f;
        public float downAngle = 22f;
        [Tooltip("Comes back in front when the head turns further than this from it (degrees).")]
        public float followAngle = 55f;
        public float fontSize = 0.1f;
        [Tooltip("Oct 27: opens behind the island (the far side from the head, facing it), its lower edge this high above " +
                 "the island and this far past its edge (m). Off = the old spot beside the head.")]
        public bool behindIsland = true;
        public float behindHeight = 0.05f, behindGap = 0.1f;
        [Tooltip("Hand rays reach this far (m).")]
        public float rayLength = 2f;
        [Tooltip("The grab frame round the panel (m): visible width; a pinch from this far outside to this far inside the edge grabs it. " +
                 "A pinch (or the ray + pinch) on any empty spot of the panel, not on a button, grabs it too.")]
        public float frame = 0.018f;
        public float grabOut = 0.05f, grabIn = 0.03f;
        [Tooltip("A direct pinch counts this far in front of or behind the panel (m).")]
        public float grabDepth = 0.08f;

#if UNITY_EDITOR || POCKET_DEV
        enum Tab { GoTo, Party, Encounter, Story, Save, Tools }

        class Button
        {
            public Func<string> label;
            public Action action;
            public Func<bool> on;
            public bool confirm;
            public Vector2 center, size;
            public MeshRenderer quad;
            public TextMeshPro text;
        }

        static DevPanel _instance;
        static readonly int BackColorId = Shader.PropertyToID("_BackColor");
        static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int BorderId = Shader.PropertyToID("_Border");

        static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.11f, 0.93f);
        static readonly Color ButtonColor = new Color(0.2f, 0.24f, 0.28f, 0.97f);
        static readonly Color OnColor = new Color(0.22f, 0.5f, 0.3f, 0.97f);
        static readonly Color TabColor = new Color(0.15f, 0.18f, 0.22f, 0.97f);
        static readonly Color TabOnColor = new Color(0.55f, 0.4f, 0.16f, 0.97f);
        static readonly Color ConfirmColor = new Color(0.62f, 0.2f, 0.16f, 0.97f);

        Transform _root;
        Mesh _quad;
        MaterialPropertyBlock _block;
        MeshRenderer _back;
        TextMeshPro _title, _status, _fps;
        readonly List<Button> _tabs = new List<Button>();
        readonly List<Button> _content = new List<Button>();
        Tab _tab;
        bool _open;
        Button _hover, _confirming;
        float _confirmUntil, _statusUntil;
        string _statusText;
        readonly bool[] _wasPinching = { true, true };
        readonly bool[] _pokeArmed = { true, true };
        readonly float[] _pokeCooldown = new float[2];
        readonly Button[] _rayHover = new Button[2];
        readonly bool[] _rayOnPanel = new bool[2];
        readonly bool[] _fingerNear = new bool[2];
        readonly MeshRenderer[] _ray = new MeshRenderer[2], _rayDot = new MeshRenderer[2];
        readonly bool[] _rayOnFrame = new bool[2];
        MeshRenderer _frame;
        bool _frameNear;
        int _dragHand = -1;
        Vector3 _dragFrom, _dragRootFrom;
        bool _moved;
        Vector3 _offset, _islandAt;
        Vector3 _anchorForward;
        int _species;
        int _level = 9;
        bool _fpsOn, _walkFast;
        float _walkSeconds, _runSeconds;
        float _fpsValue, _fpsAccum;
        int _fpsFrames;

        public static bool IsOpen => _instance != null && _instance._open;

        void Awake()
        {
            _instance = this;
            _quad = BattleHud.VerticalQuad();
            _block = new MaterialPropertyBlock();
            if (hudMaterial == null) Debug.LogError("[DevPanel] No HUD material (Set Up MR_Main (9e dev panel and fury)).", this);
        }

        void Start()
        {
            if (state == null) state = GameState.Instance;
            if (state != null && state.explorer != null)
            {
                _walkSeconds = state.explorer.walkSeconds;
                _runSeconds = state.explorer.runSeconds;
            }
            Build();
            SetOpen(false);
            Debug.Log("[DevPanel] Ready: the game menu's Dev rune or F2 opens it.");
        }

        void OnEnable() => DialogueBubble.KeepClear += KeepClear;

        void OnDisable() => DialogueBubble.KeepClear -= KeepClear;

        /// <summary>The Spirit's bubble keeps off the open panel.</summary>
        void KeepClear(List<Vector4> keep)
        {
            if (_open && _root != null) keep.Add(new Vector4(_root.position.x, _root.position.y, _root.position.z, size.magnitude * 0.5f));
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
            DevCheats.PanelGazed = false;
            if (_quad != null) Destroy(_quad);
            if (_fps != null) Destroy(_fps.gameObject);
        }

        // ---- building ----

        void Build()
        {
            _root = new GameObject("Dev Panel UI").transform;
            _root.SetParent(transform, false);
            _back = Quad(_root, "Back");
            // Transparent quads sort by distance: the back always first, then buttons, then text.
            _back.sortingOrder = -1;
            _back.transform.localScale = new Vector3(size.x, size.y, 1f);
            Paint(_back, PanelColor, new Color(1f, 1f, 1f, 0.25f), size.x / size.y, 0.06f, 0.02f);
            _title = Text(_root, "Title", new Vector2(0f, size.y * 0.5f - 0.016f), new Vector2(size.x - 0.02f, 0.02f), fontSize * 0.85f);
            _title.alignment = TextAlignmentOptions.Left;
            _status = Text(_root, "Status", new Vector2(0f, -size.y * 0.5f + 0.028f), new Vector2(size.x - 0.02f, 0.045f), fontSize * 0.8f);
            _status.alignment = TextAlignmentOptions.TopLeft;
            _frame = Quad(_root, "Grab Frame");
            _frame.sortingOrder = -2;
            _frame.transform.localPosition = new Vector3(0f, 0f, 0.001f);
            _frame.transform.localScale = new Vector3(size.x + frame * 2f, size.y + frame * 2f, 1f);
            string[] names = { "Go to", "Party", "Encounter", "Story", "Save", "Tools" };
            float w = (size.x - 0.02f) / names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                var tab = (Tab)i;
                string n = names[i];
                _tabs.Add(MakeButton(() => n, () => ShowTab(tab), () => _tab == tab, false,
                    new Vector2(-size.x * 0.5f + 0.01f + w * (i + 0.5f), size.y * 0.5f - 0.044f), new Vector2(w - 0.004f, 0.026f)));
            }
            for (int h = 0; h < 2; h++)
            {
                _ray[h] = Quad(transform, h == 0 ? "Left Ray" : "Right Ray");
                _rayDot[h] = Quad(transform, h == 0 ? "Left Ray Dot" : "Right Ray Dot");
                _ray[h].sortingOrder = 2;
                _rayDot[h].sortingOrder = 3;
                _ray[h].enabled = _rayDot[h].enabled = false;
            }
            _fps = Text(transform, "FPS Overlay", Vector2.zero, new Vector2(0.12f, 0.02f), fontSize * 0.9f);
            _fps.alignment = TextAlignmentOptions.Left;
            _fps.gameObject.SetActive(false);
            ShowTab(Tab.GoTo);
        }

        MeshRenderer Quad(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = hudMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        TextMeshPro Text(Transform parent, string name, Vector2 at, Vector2 box, float font)
        {
            var go = new GameObject(name);
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshPro>();
            t.sortingOrder = 1;
            t.rectTransform.sizeDelta = box;
            t.rectTransform.localPosition = new Vector3(at.x, at.y, -0.002f);
            t.fontSize = font;
            t.color = new Color(0.95f, 0.95f, 0.92f);
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Truncate;
            t.text = string.Empty;
            return t;
        }

        Button MakeButton(Func<string> label, Action action, Func<bool> on, bool confirm, Vector2 center, Vector2 box)
        {
            var b = new Button { label = label, action = action, on = on, confirm = confirm, center = center, size = box };
            b.quad = Quad(_root, "Button");
            b.quad.transform.localPosition = new Vector3(center.x, center.y, -0.001f);
            b.quad.transform.localScale = new Vector3(box.x, box.y, 1f);
            b.text = Text(_root, "Label", center, box - new Vector2(0.006f, 0.002f), fontSize);
            return b;
        }

        void ClearContent()
        {
            foreach (var b in _content)
            {
                Destroy(b.quad.gameObject);
                Destroy(b.text.gameObject);
            }
            _content.Clear();
            _hover = null;
            _confirming = null;
        }

        // Grid layout: three columns below the tabs.
        int _cell;
        const int Columns = 3;
        const float RowHeight = 0.034f;

        void Add(string label, Action action, Func<bool> on = null, bool confirm = false) => Add(() => label, action, on, confirm);

        void Add(Func<string> label, Action action, Func<bool> on = null, bool confirm = false)
        {
            float cw = (size.x - 0.02f) / Columns;
            int col = _cell % Columns, row = _cell / Columns;
            _cell++;
            var center = new Vector2(-size.x * 0.5f + 0.01f + cw * (col + 0.5f), size.y * 0.5f - 0.082f - row * RowHeight);
            _content.Add(MakeButton(label, action, on, confirm, center, new Vector2(cw - 0.005f, RowHeight - 0.005f)));
        }

        void NextRow()
        {
            if (_cell % Columns != 0) _cell += Columns - _cell % Columns;
        }

        void ShowTab(Tab tab)
        {
            _tab = tab;
            ClearContent();
            _cell = 0;
            if (state == null)
            {
                Status("No GameState in the scene (Set Up MR_Main (9e dev panel and fury)).");
                return;
            }
            switch (tab)
            {
                case Tab.GoTo:
                    foreach (var s in state.Spots)
                    {
                        var spot = s;
                        Add(spot.label, () => Status(state.GoTo(spot) ? $"Going to {spot.label}." : "Busy (a battle or a swap): try again."));
                    }
                    break;
                case Tab.Party:
                    Add("Heal all", () => { state.HealAll(); Status("Healed."); });
                    Add("Lead Lv -1", () => state.LeadLevel(-1));
                    Add("Lead Lv +1", () => state.LeadLevel(1));
                    Add("Lead Lv +5", () => state.LeadLevel(5));
                    Add("All Lv 5", () => state.AllLevels(5));
                    Add("All Lv 10", () => state.AllLevels(10));
                    Add("All Lv 15", () => state.AllLevels(15));
                    Add("Team of 3, Lv 8", () => state.PresetTeam(8));
                    Add("Team of 3, Lv 10", () => state.PresetTeam(10));
                    Add("Clear storage", () => state.ClearStorage(), null, true);
                    Add(() => $"One-hit KO: {OnOff(DevCheats.OneHitKo)}", () => DevCheats.OneHitKo = !DevCheats.OneHitKo, () => DevCheats.OneHitKo);
                    Add(() => $"Invincible: {OnOff(DevCheats.Invincible)}", () => DevCheats.Invincible = !DevCheats.Invincible, () => DevCheats.Invincible);
                    NextRow();
                    Picker();
                    Add("Add to party", () => state.AddSpecies(Picked, _level));
                    break;
                case Tab.Encounter:
                    Picker();
                    Add("Fight now", () => Status(state.Fight(Picked, _level) ? $"A wild {Picked.displayName} Lv {_level}!" : "Can't start one here (a battle on, inside, or zoomed)."));
                    NextRow();
                    Add(() => $"Wild battles: {OnOff(!DevCheats.WildOff)}", () => DevCheats.WildOff = !DevCheats.WildOff, () => !DevCheats.WildOff);
                    Add(() => $"Rare x10: {OnOff(DevCheats.RareBoost > 1f)}", () => DevCheats.RareBoost = DevCheats.RareBoost > 1f ? 1f : 10f, () => DevCheats.RareBoost > 1f);
                    break;
                case Tab.Story:
                    Add("Skip tutorial", () => Edit(state.SkipTutorial()), () => state.TutorialDone);
                    Add("Reset tutorial", () => Edit(state.ResetTutorial()), null, true);
                    Add("Replay opening", () => state.NewGame(), null, true);
                    Add(() => $"Cinderwood restored: {OnOff(state.Restored(state.cinderwood))}", () => Edit(state.SetRestored(state.cinderwood, !state.Restored(state.cinderwood))),
                        () => state.Restored(state.cinderwood));
                    if (state.galewick != null)
                        Add(() => $"Galewick restored: {OnOff(state.Restored(state.galewick))}", () => Edit(state.SetRestored(state.galewick, !state.Restored(state.galewick))),
                            () => state.Restored(state.galewick));
                    if (state.lilymere != null)
                        Add(() => $"Lilymere restored: {OnOff(state.Restored(state.lilymere))}", () => Edit(state.SetRestored(state.lilymere, !state.Restored(state.lilymere))),
                            () => state.Restored(state.lilymere));
                    Flag("CW flame", "cw.flame", 1); // step 10f
                    Flag("CW hollow", "cw.hollow", 63);
                    Flag("CW firebox", "cw.ember", 1);
                    Add(() => $"Log: {(GameState.Solved(GameState.LogId) ? "placed" : "fallen")}",
                        () => Edit(state.SetPuzzle(GameState.LogId, !GameState.Solved(GameState.LogId), GameState.Solved(GameState.LogId) ? 0 : 2)), () => GameState.Solved(GameState.LogId));
                    Add(() => $"Forge + door: {(GameState.Solved(GameState.BellowsId) ? "lit, open" : "cold")}",
                        () => Edit(state.SetPuzzle(GameState.BellowsId, !GameState.Solved(GameState.BellowsId), GameState.Solved(GameState.BellowsId) ? 0 : 3)), () => GameState.Solved(GameState.BellowsId));
                    Add(() => $"Keeper: {(GameState.Solved(GameState.KeeperId) ? "calm" : "frightened")}",
                        () => Edit(state.SetPuzzle(GameState.KeeperId, !GameState.Solved(GameState.KeeperId), GameState.Solved(GameState.KeeperId) ? 0 : 1)), () => GameState.Solved(GameState.KeeperId));
                    Add(() => $"Fire shard: {(GameState.ShardHeld ? "held" : "none")}",
                        () => Edit(state.SetPuzzle(GameState.ShardId, false, GameState.ShardHeld ? 0 : 1)), () => GameState.ShardHeld);
                    Add(() => $"Shard in statue: {OnOff(GameState.Solved(GameState.ShardId))}",
                        () => Edit(state.SetPuzzle(GameState.ShardId, !GameState.Solved(GameState.ShardId), 1)), () => GameState.Solved(GameState.ShardId));
                    if (state.galewick != null)
                    {
                        // Step 10e: Galewick's beats, in story order.
                        Flag("GW mill", "gw.mill", 1);
                        Flag("GW kites", "gw.kites", 7);
                        Flag("GW clouds", "gw.clouds", 7);
                        Flag("GW tower", TowerDoor.TowerId, 1);
                        Flag("GW knots", "gw.knots", KeeperUntangle.AllKnots);
                        Add(() => $"Galeowl: {(GameState.Solved("gw.keeper") ? "calm" : "frightened")}",
                            () => Edit(state.SetPuzzle("gw.keeper", !GameState.Solved("gw.keeper"), GameState.Solved("gw.keeper") ? 0 : 1)), () => GameState.Solved("gw.keeper"));
                        Add(() => $"Wind shard: {(GameState.Solved(ShardHolder.WindId) ? "in statue" : ShardHolder.Holds(ShardHolder.WindId) ? "held" : "none")}",
                            () => Edit(GameState.Solved(ShardHolder.WindId) ? state.SetPuzzle(ShardHolder.WindId, false, 0)
                                : ShardHolder.Holds(ShardHolder.WindId) ? state.SetPuzzle(ShardHolder.WindId, true, 2) : state.SetPuzzle(ShardHolder.WindId, false, 1)),
                            () => PuzzleState.Stage(ShardHolder.WindId) > 0 || GameState.Solved(ShardHolder.WindId));
                    }
                    if (state.lilymere != null)
                    {
                        // Step 11b: Lilymere's beats, in story order (basin = the lagoon risen).
                        Flag("LM pads", "lm.pads", 7);
                        Flag("LM rain", "lm.rain", 31);
                        Flag("LM basin + rise", LilymereLagoon.RisenId, 2);
                        // Step 11c: Tidemother and the water shard.
                        Add(() => $"Tidemother: {(GameState.Solved("lm.keeper") ? "calm" : "frightened")}",
                            () => Edit(state.SetPuzzle("lm.keeper", !GameState.Solved("lm.keeper"), GameState.Solved("lm.keeper") ? 0 : 1)), () => GameState.Solved("lm.keeper"));
                        Add(() => $"Water shard: {(GameState.Solved(ShardHolder.WaterId) ? "in statue" : ShardHolder.Holds(ShardHolder.WaterId) ? "held" : "none")}",
                            () => Edit(GameState.Solved(ShardHolder.WaterId) ? state.SetPuzzle(ShardHolder.WaterId, false, 0)
                                : ShardHolder.Holds(ShardHolder.WaterId) ? state.SetPuzzle(ShardHolder.WaterId, true, 2) : state.SetPuzzle(ShardHolder.WaterId, false, 1)),
                            () => PuzzleState.Stage(ShardHolder.WaterId) > 0 || GameState.Solved(ShardHolder.WaterId));
                    }
                    Add("Reset all story", () => Edit(state.ResetStory()), null, true);
                    break;
                case Tab.Save:
                    for (int i = 1; i <= 3; i++)
                    {
                        int slot = i;
                        Add($"Save {slot}", () => Status(state.SaveSlot(slot) ? $"Saved to slot {slot}." : "Nothing to save yet."));
                    }
                    for (int i = 1; i <= 3; i++)
                    {
                        int slot = i;
                        Add($"Load {slot}", () => Status(state.LoadSlot(slot) ? $"Loading slot {slot}..." : $"Slot {slot} is empty."), null, true);
                    }
                    Add("Load auto", () => Status(state.LoadSlot(0) ? "Loading the auto slot..." : "The auto slot is empty."), null, true);
                    Add("New game", () => state.NewGame(), null, true);
                    Add("Show save path", () => Status(GameSave.SavePath, 12f));
                    NextRow();
                    // Step 9g: the Quick Journey (its own save slot).
                    Add("Quick: start", () => state.QuickPreset(GameState.QuickStage.Start), null, true);
                    Add("Quick: at gift", () => state.QuickPreset(GameState.QuickStage.Gift), null, true);
                    Add("Quick: at gate", () => state.QuickPreset(GameState.QuickStage.Gate), null, true);
                    Add("Quick: at temple", () => state.QuickPreset(GameState.QuickStage.Temple), null, true);
                    break;
                case Tab.Tools:
                    Add(() => $"FPS: {OnOff(_fpsOn)}", () => _fpsOn = !_fpsOn, () => _fpsOn);
                    Add(() => $"Readouts: {OnOff(DevReadout.Shown)}", () => DevReadout.Shown = !DevReadout.Shown, () => DevReadout.Shown);
                    Add(() => $"Time x{Time.timeScale:0}", () => Time.timeScale = Time.timeScale >= 4f ? 1f : Time.timeScale * 2f, () => Time.timeScale > 1f);
                    Add(() => $"Walk x2: {OnOff(_walkFast)}", ToggleWalk, () => _walkFast);
                    Add(() => $"Labels: {OnOff(DebugLabels.Visible)}", () => DebugLabels.Visible = !DebugLabels.Visible, () => DebugLabels.Visible);
                    Add("Record ghost", RecordGhost);
                    Add("Report bug", ReportBug);
                    Add("Close", () => SetOpen(false));
                    break;
            }
        }

        void Picker()
        {
            Add("< Species", () => _species--);
            Add(() => Picked != null ? $"{Picked.displayName} Lv {_level}" : "-", null);
            Add("Species >", () => _species++);
            Add("Lv -1", () => _level = Mathf.Max(1, _level - 1));
            Add("Lv +1", () => _level = Mathf.Min(30, _level + 1));
        }

        IslingSpecies Picked
        {
            get
            {
                var list = state.Species;
                if (list.Count == 0) return null;
                _species = (_species % list.Count + list.Count) % list.Count;
                return list[_species];
            }
        }

        static string OnOff(bool on) => on ? "on" : "off";

        /// <summary>A story beat toggled solved (at <paramref name="stage"/>) or not done.</summary>
        void Flag(string label, string id, int stage) =>
            Add(() => $"{label}: {(GameState.Solved(id) ? "done" : "not yet")}", () => Edit(state.SetPuzzle(id, !GameState.Solved(id), GameState.Solved(id) ? 0 : stage)),
                () => GameState.Solved(id));

        void Edit(bool ok)
        {
            if (!ok) Status("No save yet: finish the opening first.");
        }

        void ToggleWalk()
        {
            _walkFast = !_walkFast;
            if (state == null || state.explorer == null) return;
            state.explorer.walkSeconds = _walkFast ? _walkSeconds * 0.5f : _walkSeconds;
            state.explorer.runSeconds = _walkFast ? _runSeconds * 0.5f : _runSeconds;
        }

        void RecordGhost()
        {
            var recorder = FindFirstObjectByType<GhostClipRecorder>();
            string clip = recorder != null ? recorder.RecordNextWanted() : null;
            Status(recorder == null ? "No ghost clip recorder in the scene." : clip != null ? $"Recording '{clip}' after the countdown." : "Nothing to record (all clips done, or one is recording).");
        }

        void ReportBug()
        {
            string name = BugReport.Write(state, _fpsValue);
            Status(name != null ? $"Bug report saved: bugreports/{name}" : "The bug report failed (see the log).", 15f);
        }

        void Status(string text, float seconds = 5f)
        {
            _statusText = text;
            _statusUntil = Time.unscaledTime + seconds;
        }

        string TabInfo()
        {
            if (state == null) return string.Empty;
            switch (_tab)
            {
                case Tab.Save:
                    return $"1: {GameState.SlotLabel(1) ?? "empty"}   2: {GameState.SlotLabel(2) ?? "empty"}\n3: {GameState.SlotLabel(3) ?? "empty"}   auto: {GameState.SlotLabel(0) ?? "empty"}\n" +
                           $"Playing {SaveSlots.Label(SaveSlots.Active)}; quick timer {QuickJourney.Format(QuickJourney.Seconds)}{(QuickJourney.TimerDone ? " (done)" : "")}";
                case Tab.Story:
                    return "Story changes save, edit the save and reload the game.";
                case Tab.Party:
                    var lead = state.party != null ? state.party.Lead : null;
                    return lead != null ? $"Lead: {lead.Name} Lv {lead.level}, {state.party.members.Count} in pods, {state.party.storage.Count} stored" : "No party yet.";
                default:
                    return string.Empty;
            }
        }

        // ---- open, place ----

        void SetOpen(bool open)
        {
            _open = open;
            if (_root != null) _root.gameObject.SetActive(open);
            if (!open)
            {
                DevCheats.PanelGazed = false;
                _dragHand = -1;
                for (int h = 0; h < 2; h++)
                    if (_ray[h] != null) _ray[h].enabled = _rayDot[h].enabled = false;
            }
            else
            {
                Place(true);
                ShowTab(_tab);
            }
            Debug.Log($"[DevPanel] {(open ? "Open" : "Closed")}.");
        }

        /// <summary>
        /// In front of the head (yaw only), toward the dominant hand and below the eyes, at arm's reach, facing the head.
        /// World-locked: it only moves when (re)opened or when the head has turned more than <see cref="followAngle"/> away.
        /// Once dragged by its frame it goes to that spot instead (kept relative to the head's facing).
        /// </summary>
        void Place(bool snap)
        {
            if (_dragHand >= 0 || !TryHeadYaw(out var head, out var fwd)) return;
            if (!_moved && behindIsland && island != null)
            {
                // Oct 27 (Mugesh): behind the island, a ray's reach away, instead of beside the head; stays there and
                // only follows when the island itself is moved.
                Vector3 at = island.transform.position;
                if (!snap && (at - _islandAt).sqrMagnitude < 0.05f * 0.05f) return;
                _islandAt = at;
                Vector3 away = at - head;
                away.y = 0f;
                away = away.sqrMagnitude > 1e-4f ? away.normalized : fwd;
                Vector2 half = island.FootprintHalfSize * island.transform.lossyScale.x;
                float r = Mathf.Max(half.x, half.y) * 1.2f + behindGap;
                _root.position = at + away * r + Vector3.up * (behindHeight + size.y * 0.5f);
                Face(head);
                return;
            }
            if (!snap && Vector3.Angle(fwd, _anchorForward) < followAngle) return;
            _anchorForward = fwd;
            if (_moved) _root.position = head + Quaternion.LookRotation(fwd) * _offset;
            else
            {
                float side = DominantHand.Left ? -sideAngle : sideAngle;
                Vector3 dir = Quaternion.AngleAxis(side, Vector3.up) * fwd;
                dir = Quaternion.AngleAxis(downAngle, Vector3.Cross(Vector3.up, dir)) * dir;
                _root.position = head + dir * distance;
            }
            Face(head);
        }

        static bool TryHeadYaw(out Vector3 head, out Vector3 fwd)
        {
            head = fwd = default;
            var cam = Camera.main;
            if (cam == null) return false;
            head = cam.transform.position;
            fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-6f) return false;
            fwd.Normalize();
            return true;
        }

        void Face(Vector3 head)
        {
            Vector3 away = _root.position - head;
            if (away.sqrMagnitude > 1e-6f) _root.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        /// <summary>On the panel's edge band (like the island's rim): up to <paramref name="outside"/> beyond the edge, <paramref name="inside"/> within it.</summary>
        bool OnFrame(Vector2 p, float outside, float inside)
        {
            float ex = Mathf.Abs(p.x) - size.x * 0.5f, ey = Mathf.Abs(p.y) - size.y * 0.5f;
            return ex <= outside && ey <= outside && (ex >= -inside || ey >= -inside);
        }

        bool PinchOnFrame(Vector3 pinch)
        {
            Vector3 l = _root.InverseTransformPoint(pinch);
            return Mathf.Abs(l.z) < grabDepth && OnGrab(new Vector2(l.x, l.y));
        }

        /// <summary>Oct 27 (Mugesh: the frame was too thin): the wide edge band, or any empty spot of the panel.</summary>
        bool OnGrab(Vector2 p)
        {
            if (OnFrame(p, grabOut, grabIn)) return true;
            bool inside = Mathf.Abs(p.x) <= size.x * 0.5f && Mathf.Abs(p.y) <= size.y * 0.5f;
            return inside && ButtonAt(p) == null;
        }

        /// <summary>
        /// Moving the panel by its frame: a pinch on the edge band (or the ray on the frame + pinch) holds it, the panel follows the pinch
        /// point and keeps facing the head; letting go keeps the spot.
        /// </summary>
        void Drag()
        {
            _frameNear = false;
            for (int h = 0; h < 2; h++)
            {
                if (_dragHand >= 0 && _dragHand != h) continue;
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (!IslandHandle.TryGetPinchPoint(hand, out var pinch)) continue;
                if (PinchOnFrame(pinch)) _frameNear = true;
            }
            if (_dragHand < 0) return;
            var held = HandRegistry.Get(_dragHand == 0 ? Handedness.Left : Handedness.Right);
            if (held == null || !held.IsTrackedDataValid || !held.GetIndexFingerIsPinching() || !IslandHandle.TryGetPinchPoint(held, out var now))
            {
                _dragHand = -1;
                if (TryHeadYaw(out var head, out var fwd))
                {
                    _moved = true;
                    _anchorForward = fwd;
                    _offset = Quaternion.Inverse(Quaternion.LookRotation(fwd)) * (_root.position - head);
                }
                Debug.Log("[DevPanel] Moved.");
                return;
            }
            DevCheats.PanelGazed = true;
            _root.position = _dragRootFrom + (now - _dragFrom);
            var cam = Camera.main;
            if (cam != null) Face(cam.transform.position);
        }

        bool StartDrag(int h, IHand hand, bool byRay)
        {
            if (!IslandHandle.TryGetPinchPoint(hand, out var pinch)) return false;
            if (!byRay && !PinchOnFrame(pinch)) return false;
            _dragHand = h;
            _dragFrom = pinch;
            _dragRootFrom = _root.position;
            return true;
        }

        // ---- every frame ----

        void Update()
        {
            if (_root == null) return;
            if (OpenPressed()) SetOpen(!_open);
            UpdateFps();
            if (!_open)
            {
                DevCheats.PanelGazed = false;
                return;
            }
            Place(false);
            if (_confirming != null && Time.unscaledTime > _confirmUntil) _confirming = null;
            _hover = null;
            DevCheats.PanelGazed = false;
            Drag();
            Pokes();
            Rays();
            Pinches();
            Mouse();
            Refresh();
        }

        static bool OpenPressed()
        {
            // Step 9g: the menu gesture belongs to the game menu (its Dev rune opens this panel).
            var kb = Keyboard.current;
            return kb != null && kb.f2Key.wasPressedThisFrame;
        }

        /// <summary>The panel-local point where a ray meets the panel (inside its rectangle).</summary>
        bool Hit(Ray ray, out Vector2 p) => HitPlane(ray, out p) && Mathf.Abs(p.x) <= size.x * 0.5f && Mathf.Abs(p.y) <= size.y * 0.5f;

        /// <summary>The panel-local point where a ray meets the panel's plane (in front of the ray's origin).</summary>
        bool HitPlane(Ray ray, out Vector2 p)
        {
            p = default;
            Vector3 o = _root.InverseTransformPoint(ray.origin), d = _root.InverseTransformDirection(ray.direction);
            if (d.z <= 1e-4f) return false;
            Vector3 h = o + d * (-o.z / d.z);
            p = new Vector2(h.x, h.y);
            return true;
        }

        Button ButtonAt(Vector2 p)
        {
            foreach (var b in _tabs)
                if (Inside(b, p)) return b;
            foreach (var b in _content)
                if (Inside(b, p)) return b;
            return null;
        }

        static bool Inside(Button b, Vector2 p) => Mathf.Abs(p.x - b.center.x) <= b.size.x * 0.5f && Mathf.Abs(p.y - b.center.y) <= b.size.y * 0.5f;

        void Pinches()
        {
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                bool pinching = hand != null && hand.IsTrackedDataValid && hand.GetIndexFingerIsPinching();
                bool busy = grabGuard != null && grabGuard.IsHandBusy(h == 0);
                if (pinching && !_wasPinching[h] && !busy && _dragHand < 0)
                {
                    // A pinch at the edge (or the ray on the frame) moves the panel; the ray on a button presses it.
                    if (!StartDrag(h, hand, _rayOnFrame[h]) && _rayHover[h] != null) Press(_rayHover[h], $"ray + pinch ({(h == 0 ? "left" : "right")})");
                }
                _wasPinching[h] = pinching;
            }
        }

        /// <summary>
        /// Each hand's pointer ray (ISDK pointer pose): when it meets the panel a thin line and a dot show and the button
        /// under it lights up. Hidden while that hand's fingertip is near the panel (it is poking).
        /// </summary>
        void Rays()
        {
            for (int h = 0; h < 2; h++)
            {
                _rayHover[h] = null;
                _rayOnPanel[h] = _rayOnFrame[h] = false;
                _ray[h].enabled = _rayDot[h].enabled = false;
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (hand == null || !hand.IsTrackedDataValid || _fingerNear[h] || _dragHand >= 0 || !hand.GetPointerPose(out var pose)) continue;
                if (!HitPlane(new Ray(pose.position, pose.forward), out var p)) continue;
                // The ray on the frame band or an empty spot of the panel grabs; on a button it presses it.
                bool onBar = OnGrab(p);
                if (!onBar && (Mathf.Abs(p.x) > size.x * 0.5f || Mathf.Abs(p.y) > size.y * 0.5f)) continue;
                Vector3 end = _root.TransformPoint(new Vector3(p.x, p.y, -0.003f));
                Vector3 a = pose.position, along = end - a;
                float len = along.magnitude;
                if (len > rayLength) continue;
                _rayOnPanel[h] = true;
                _rayOnFrame[h] = onBar;
                DevCheats.PanelGazed = true;
                _rayHover[h] = onBar ? null : ButtonAt(p);
                bool pinching = hand.GetIndexFingerIsPinching();
                Color c = _rayHover[h] != null || onBar ?new Color(1f, 0.9f, 0.55f, pinching ? 1f : 0.85f) : new Color(1f, 1f, 1f, 0.5f);
                // The line: a long thin rounded quad from the hand to the hit, turned to face the head.
                var cam = Camera.main;
                Vector3 up = along / Mathf.Max(1e-4f, len);
                Vector3 toCam = cam != null ? cam.transform.position - (a + end) * 0.5f : -along;
                Vector3 normal = Vector3.ProjectOnPlane(toCam, up);
                if (normal.sqrMagnitude < 1e-8f) normal = Vector3.Cross(up, Vector3.right);
                var t = _ray[h].transform;
                t.SetPositionAndRotation((a + end) * 0.5f, Quaternion.LookRotation(-normal, up));
                t.localScale = new Vector3(0.0035f, len, 1f);
                Paint(_ray[h], new Color(c.r, c.g, c.b, c.a * 0.55f), Color.clear, 0.0035f / Mathf.Max(1e-4f, len), 0.5f, 0f);
                var d = _rayDot[h].transform;
                d.SetPositionAndRotation(end, _root.rotation);
                d.localScale = Vector3.one * (pinching ? 0.007f : 0.011f);
                Paint(_rayDot[h], c, Color.clear, 1f, 0.5f, 0f);
                _ray[h].enabled = _rayDot[h].enabled = true;
            }
        }

        /// <summary>
        /// A fingertip touching the panel presses the button under it (from a little in front to a little behind the
        /// surface, so a slow or shallow poke counts); it re-arms once the finger pulls back 1.5 cm. A fingertip within
        /// 4 cm lights the button it is over.
        /// </summary>
        void Pokes()
        {
            for (int h = 0; h < 2; h++)
            {
                _fingerNear[h] = false;
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (!HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip)) continue;
                Vector3 l = _root.InverseTransformPoint(tip);
                bool over = Mathf.Abs(l.x) <= size.x * 0.5f + 0.02f && Mathf.Abs(l.y) <= size.y * 0.5f + 0.02f;
                _fingerNear[h] = over && l.z > -0.06f && l.z < 0.04f;
                if (l.z < -0.015f || !over) _pokeArmed[h] = true;
                var b = over ? ButtonAt(new Vector2(l.x, l.y)) : null;
                if (b != null && l.z > -0.04f && l.z < 0.03f) _hover = b;
                if (_pokeArmed[h] && b != null && l.z >= -0.006f && l.z < 0.03f && Time.unscaledTime >= _pokeCooldown[h])
                {
                    _pokeArmed[h] = false;
                    _pokeCooldown[h] = Time.unscaledTime + 0.3f;
                    Press(b, $"poke ({(h == 0 ? "left" : "right")})");
                }
            }
        }

        void Mouse()
        {
            if (!Application.isEditor) return;
            var m = UnityEngine.InputSystem.Mouse.current;
            var cam = Camera.main;
            if (m == null || cam == null || !m.leftButton.wasPressedThisFrame) return;
            if (Hit(cam.ScreenPointToRay(m.position.ReadValue()), out var p))
            {
                var b = ButtonAt(p);
                if (b != null) Press(b, "mouse");
            }
        }

        void Press(Button b, string how)
        {
            if (b.action == null) return;
            if (b.confirm && _confirming != b)
            {
                _confirming = b;
                _confirmUntil = Time.unscaledTime + 3f;
                Status($"Press \"{b.label()}\" again to confirm.", 3f);
                return;
            }
            _confirming = null;
            Debug.Log($"[DevPanel] {b.label()} ({how}).");
            try
            {
                b.action();
            }
            catch (Exception e)
            {
                Debug.LogError($"[DevPanel] {b.label()} failed: {e}");
                Status($"{b.label()} failed: {e.Message}");
            }
        }

        void Refresh()
        {
            _title.text = $"<b>Dev panel</b>  {(state != null ? state.Where : "")}";
            _status.text = Time.unscaledTime < _statusUntil && !string.IsNullOrEmpty(_statusText) ? _statusText : TabInfo();
            foreach (var b in _tabs) PaintButton(b, true);
            foreach (var b in _content) PaintButton(b, false);
            bool lit = _dragHand >= 0 || _frameNear || _rayOnFrame[0] || _rayOnFrame[1];
            float fh = size.y + frame * 2f;
            Paint(_frame, Color.clear, lit ? new Color(1f, 0.9f, 0.55f, 0.95f) : new Color(0.85f, 0.85f, 0.8f, 0.35f), (size.x + frame * 2f) / fh, 0.08f, frame * 1.4f / fh);
        }

        void PaintButton(Button b, bool tab)
        {
            string text = b.label();
            if (b.text.text != text) b.text.text = text;
            bool on = b.on != null && b.on();
            Color back = b == _confirming ? ConfirmColor : tab ? on ? TabOnColor : TabColor : on ? OnColor : b.action == null ? PanelColor : ButtonColor;
            bool lit = b == _hover || b == _rayHover[0] || b == _rayHover[1];
            Color border = lit ? new Color(1f, 0.9f, 0.55f, 1f) : new Color(1f, 1f, 1f, b.action == null ? 0f : 0.18f);
            Paint(b.quad, back, border, b.size.x / b.size.y, 0.3f, lit ? 0.12f : 0.06f);
        }

        void Paint(MeshRenderer r, Color back, Color border, float aspect, float radius, float borderWidth)
        {
            _block.Clear();
            _block.SetColor(BackColorId, back);
            _block.SetColor(BorderColorId, border);
            _block.SetFloat(FillId, 0f);
            _block.SetFloat(AspectId, aspect);
            _block.SetFloat(RadiusId, radius);
            _block.SetFloat(BorderId, borderWidth);
            r.SetPropertyBlock(_block);
        }

        void UpdateFps()
        {
            _fpsAccum += Time.unscaledDeltaTime;
            _fpsFrames++;
            if (_fpsAccum >= 0.5f)
            {
                _fpsValue = _fpsFrames / _fpsAccum;
                _fpsAccum = 0f;
                _fpsFrames = 0;
            }
            _fps.gameObject.SetActive(_fpsOn);
            if (!_fpsOn) return;
            var cam = Camera.main;
            if (cam == null) return;
            var c = cam.transform;
            _fps.transform.SetPositionAndRotation(c.position + c.forward * 0.5f - c.up * 0.12f - c.right * 0.1f, c.rotation);
            _fps.text = $"{_fpsValue:0} fps  {1000f / Mathf.Max(1f, _fpsValue):0.0} ms";
        }

        // ---- bridge (Editor checks) ----

        /// <summary>Opens or closes the panel.</summary>
        public static void DebugOpen(bool open)
        {
            if (_instance != null) _instance.SetOpen(open);
        }

        /// <summary>Presses the first button (tabs first) whose label starts with <paramref name="label"/>; confirms by itself.</summary>
        public static bool DebugPress(string label)
        {
            if (_instance == null) return false;
            foreach (var list in new[] { _instance._tabs, _instance._content })
                foreach (var b in list)
                    if (b.action != null && b.label().StartsWith(label, StringComparison.OrdinalIgnoreCase))
                    {
                        _instance._confirming = b;
                        _instance.Press(b, "bridge");
                        return true;
                    }
            Debug.Log($"[DevPanel] No button '{label}' on this tab.");
            return false;
        }
#else
        void Awake() => Destroy(gameObject);
#endif
    }
}
