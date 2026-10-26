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
    /// build compiles it out and the object removes itself. Opened and closed with the left hand's menu gesture (palm up,
    /// pinch: Meta's app menu button for hand tracking, <c>OVRInput.Button.Start</c> on <c>Controller.LHand</c>; also the
    /// left controller's menu button), F2 in the Editor. A plain 2D panel floats beside the island's left side, facing
    /// the player, kept inside the central view. Buttons work with a poke (index tip through the panel) and with
    /// look + pinch; in the Editor also with a mouse click. Destructive ones (new game, reset story, load) ask for a
    /// second press. Everything goes through <see cref="GameState"/> (Quick Journey reuses it).
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
        public Vector2 size = new Vector2(0.4f, 0.32f);
        [Tooltip("Gap between the island's left edge and the panel (m), and its height above the cap (m).")]
        public float sideGap = 0.04f;
        public float height = 0.13f;
        [Tooltip("The panel stays within this angle of the head's forward (degrees).")]
        public float maxViewAngle = 28f;
        public float fontSize = 0.1f;

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
        readonly float[] _pokeZ = { -1f, -1f };
        readonly float[] _pokeCooldown = new float[2];
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
            Debug.Log("[DevPanel] Ready: left-hand menu pinch (palm up) or F2 opens it.");
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
            string[] names = { "Go to", "Party", "Encounter", "Story", "Save", "Tools" };
            float w = (size.x - 0.02f) / names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                var tab = (Tab)i;
                string n = names[i];
                _tabs.Add(MakeButton(() => n, () => ShowTab(tab), () => _tab == tab, false,
                    new Vector2(-size.x * 0.5f + 0.01f + w * (i + 0.5f), size.y * 0.5f - 0.044f), new Vector2(w - 0.004f, 0.026f)));
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
                    Add("Team of 3, Lv 9", () => state.PresetTeam(9));
                    Add("Team of 3, Lv 11", () => state.PresetTeam(11));
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
                    break;
                case Tab.Tools:
                    Add(() => $"FPS: {OnOff(_fpsOn)}", () => _fpsOn = !_fpsOn, () => _fpsOn);
                    Add(() => $"Readouts: {OnOff(DevReadout.Shown)}", () => DevReadout.Shown = !DevReadout.Shown, () => DevReadout.Shown);
                    Add(() => $"Time x{Time.timeScale:0}", () => Time.timeScale = Time.timeScale >= 4f ? 1f : Time.timeScale * 2f, () => Time.timeScale > 1f);
                    Add(() => $"Walk x2: {OnOff(_walkFast)}", ToggleWalk, () => _walkFast);
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
                    return $"1: {GameState.SlotLabel(1) ?? "empty"}   2: {GameState.SlotLabel(2) ?? "empty"}\n3: {GameState.SlotLabel(3) ?? "empty"}   auto: {GameState.SlotLabel(0) ?? "empty"}";
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
            if (!open) DevCheats.PanelGazed = false;
            else
            {
                Place(true);
                ShowTab(_tab);
            }
            Debug.Log($"[DevPanel] {(open ? "Open" : "Closed")}.");
        }

        void Place(bool snap)
        {
            var cam = Camera.main;
            if (cam == null || island == null || island.map == null) return;
            var t = island.transform;
            var half = island.FootprintHalfSize;
            // Island +X is the player's left.
            Vector3 edge = t.TransformPoint(new Vector3(half.x + 0.02f, island.CapTopLocal, 0f));
            Vector3 right = t.right;
            right.y = 0f;
            right = right.sqrMagnitude > 1e-6f ? right.normalized : Vector3.left;
            Vector3 target = edge + right * (sideGap + size.x * 0.5f) + Vector3.up * height;
            Vector3 head = cam.transform.position, fwd = cam.transform.forward, dir = target - head;
            float angle = Vector3.Angle(fwd, dir);
            if (angle > maxViewAngle && dir.sqrMagnitude > 1e-6f)
                target = head + Vector3.RotateTowards(dir, fwd * dir.magnitude, (angle - maxViewAngle) * Mathf.Deg2Rad, 0f);
            _root.position = snap ? target : Vector3.Lerp(_root.position, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 6f));
            Vector3 away = _root.position - head;
            if (away.sqrMagnitude > 1e-6f) _root.rotation = Quaternion.LookRotation(away, Vector3.up);
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
            if (HeadGaze.TryGetRay(out var ray) && Hit(ray, out var p))
            {
                DevCheats.PanelGazed = true;
                _hover = ButtonAt(p);
            }
            Pinches();
            Pokes();
            Mouse();
            Refresh();
        }

        static bool OpenPressed()
        {
            if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LHand) || OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch))
                return true;
            var kb = Keyboard.current;
            return kb != null && kb.f2Key.wasPressedThisFrame;
        }

        /// <summary>The panel-local point where a ray meets the panel (inside its rectangle).</summary>
        bool Hit(Ray ray, out Vector2 p)
        {
            p = default;
            Vector3 o = _root.InverseTransformPoint(ray.origin), d = _root.InverseTransformDirection(ray.direction);
            if (d.z <= 1e-4f) return false;
            Vector3 h = o + d * (-o.z / d.z);
            p = new Vector2(h.x, h.y);
            return Mathf.Abs(h.x) <= size.x * 0.5f && Mathf.Abs(h.y) <= size.y * 0.5f;
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
                if (pinching && !_wasPinching[h] && !busy && _hover != null) Press(_hover, $"look + pinch ({(h == 0 ? "left" : "right")})");
                _wasPinching[h] = pinching;
            }
        }

        /// <summary>A fingertip coming through the panel from the front presses the button it crosses.</summary>
        void Pokes()
        {
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (!HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip))
                {
                    _pokeZ[h] = -1f;
                    continue;
                }
                Vector3 l = _root.InverseTransformPoint(tip);
                float before = _pokeZ[h];
                _pokeZ[h] = l.z;
                if (before < -0.004f && l.z >= -0.004f && l.z < 0.03f && Time.unscaledTime >= _pokeCooldown[h])
                {
                    var b = ButtonAt(new Vector2(l.x, l.y));
                    if (b != null)
                    {
                        _pokeCooldown[h] = Time.unscaledTime + 0.35f;
                        Press(b, $"poke ({(h == 0 ? "left" : "right")})");
                    }
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
        }

        void PaintButton(Button b, bool tab)
        {
            string text = b.label();
            if (b.text.text != text) b.text.text = text;
            bool on = b.on != null && b.on();
            Color back = b == _confirming ? ConfirmColor : tab ? on ? TabOnColor : TabColor : on ? OnColor : b.action == null ? PanelColor : ButtonColor;
            Color border = b == _hover ? new Color(1f, 0.9f, 0.55f, 1f) : new Color(1f, 1f, 1f, b.action == null ? 0f : 0.18f);
            Paint(b.quad, back, border, b.size.x / b.size.y, 0.3f, b == _hover ? 0.12f : 0.06f);
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
