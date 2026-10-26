using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Tools/Pocket Isle/Audio Lab: edit, audition and bake synth patches in Edit mode (no Play mode needed),
    /// review every sound (Approve / Redo + note), and cut seamless loops from long clips (Lyria music).
    /// </summary>
    public class AudioLabWindow : EditorWindow
    {
        enum Tab { Patches, Review, LoopTool }

        static readonly string[] TabNames = { "Patches", "Review", "Loop tool" };
        const float ListWidth = 250f;

        Tab _tab;
        string _search = "";
        Vector2 _listScroll, _fieldScroll, _reviewScroll;
        readonly HashSet<string> _closedGroups = new HashSet<string>();
        List<SynthPatch> _patches = new List<SynthPatch>();

        SynthPatch _selected;
        SerializedObject _so;
        float[] _render;
        int _renderVariant;
        bool _stale = true;
        AudioClip _previewClip;
        Texture2D _wave, _spec;
        ClipStats _stats;

        /// <summary>The patch open in the Lab (AudioScope's Analyse Group uses it).</summary>
        public static SynthPatch Selected { get; private set; }
        float _peakDb, _rmsDb, _highShare, _seam;

        bool _autoPlay = true;
        int _presetIndex, _presetLayer;
        bool _pendingPlay;
        double _lastChange;

        // review filter
        enum ReviewFilter { All, Unreviewed, Redo, Approved, Gaps }
        ReviewFilter _filter;

        // loop tool
        AudioClip _loopSource;
        float _loopStart, _loopEnd, _loopFade = 2f;
        float[] _loopData;
        int _loopChannels, _loopRate;

        [MenuItem("Tools/Pocket Isle/Audio Lab", priority = 90)]
        public static void Open() => GetWindow<AudioLabWindow>("Audio Lab").Show();

        void OnEnable()
        {
            _autoPlay = EditorPrefs.GetBool("PocketIsle.AudioLab.AutoPlay", true);
            Reload();
        }

        void OnDisable()
        {
            AudioPreview.Stop();
            if (_previewClip != null) DestroyImmediate(_previewClip);
            if (_wave != null) DestroyImmediate(_wave);
            if (_spec != null) DestroyImmediate(_spec);
        }

        void OnProjectChange() => Reload();

        void Reload()
        {
            _patches = SynthBaker.AllPatches();
            if (_selected == null && _patches.Count > 0) Select(_patches[0]);
            Repaint();
        }

        void Select(SynthPatch p)
        {
            _selected = p;
            Selected = p;
            _so = p != null ? new SerializedObject(p) : null;
            _stale = true;
            _renderVariant = 0;
            GUI.FocusControl(null);
        }

        void Update()
        {
            // Auto-play once the slider is let go (no hot control) and the value has settled.
            if (_pendingPlay && GUIUtility.hotControl == 0 && EditorApplication.timeSinceStartup - _lastChange > 0.25)
            {
                _pendingPlay = false;
                PlayCurrent(0);
                Repaint();
            }
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _tab = (Tab)GUILayout.Toolbar((int)_tab, TabNames, EditorStyles.toolbarButton, GUILayout.Width(260));
                GUILayout.FlexibleSpace();
                bool auto = GUILayout.Toggle(_autoPlay, "Auto-play on slider release", EditorStyles.toolbarButton);
                if (auto != _autoPlay)
                {
                    _autoPlay = auto;
                    EditorPrefs.SetBool("PocketIsle.AudioLab.AutoPlay", auto);
                }
                if (GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(40))) AudioPreview.Stop();
                if (GUILayout.Button(new GUIContent("Create missing", "Make patch assets for recipes that have none yet."), EditorStyles.toolbarButton))
                {
                    int made = SynthBaker.CreateMissing();
                    Debug.Log($"[AudioLab] Created {made} patches.");
                    Reload();
                }
                if (GUILayout.Button("Bake all", EditorStyles.toolbarButton)) SynthBaker.BakeAll(_patches);
                if (GUILayout.Button(new GUIContent("Sync cues", "Create missing cues, refresh their clips, rebuild the table, validate, write AudioIds.cs."), EditorStyles.toolbarButton))
                    AudioCueTools.SyncAll();
                if (GUILayout.Button(new GUIContent("Import gen", "Import new clips from the ElevenLabs gen lane's manifest.json (Tools > Pocket Isle > Audio > Import Gen Batch)."), EditorStyles.toolbarButton))
                    GenImporter.ImportMenu();
                if (GUILayout.Button(new GUIContent("Analyse", "AudioScope: contact sheets + report.md in Temp/audio-analysis/ (all groups, or the open patch's group with Shift)."), EditorStyles.toolbarButton))
                {
                    if (Event.current.shift && _selected != null) AudioScope.AnalyseGroup(SynthBaker.Group(_selected));
                    else AudioScope.AnalyseAll();
                    EditorUtility.RevealInFinder(AudioScope.OutDir + "report.md");
                }
            }

            switch (_tab)
            {
                case Tab.Patches: DrawPatches(); break;
                case Tab.Review: DrawReview(); break;
                case Tab.LoopTool: DrawLoopTool(); break;
            }
        }

        // ---- patches tab ----

        void DrawPatches()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(ListWidth)))
                {
                    _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                    _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                    string group = null;
                    foreach (var p in _patches)
                    {
                        if (p == null) continue;
                        if (_search.Length > 0 && p.id.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                        var g = SynthBaker.Group(p);
                        if (g != group)
                        {
                            group = g;
                            bool open = !_closedGroups.Contains(g);
                            bool now = EditorGUILayout.Foldout(open, g.Length > 0 ? g : "(root)", true);
                            if (now != open)
                            {
                                if (now) _closedGroups.Remove(g);
                                else _closedGroups.Add(g);
                            }
                        }
                        if (_closedGroups.Contains(g)) continue;
                        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(18));
                        if (p == _selected) EditorGUI.DrawRect(rect, new Color(0.24f, 0.48f, 0.9f, 0.35f));
                        Dot(new Rect(rect.x + 12, rect.y + 5, 8, 8), p.review.status);
                        var label = (p.gap ? "★ " : "") + p.id;
                        if (GUI.Button(new Rect(rect.x + 24, rect.y, rect.width - 24, rect.height), label, EditorStyles.label)) Select(p);
                    }
                    EditorGUILayout.EndScrollView();
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    if (_selected == null || _so == null)
                    {
                        EditorGUILayout.HelpBox("No patches yet. Press 'Create missing' to make them from the recipes.", MessageType.Info);
                        return;
                    }
                    DrawEditor();
                }
            }
        }

        void DrawEditor()
        {
            var p = _selected;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(p.id, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Play", GUILayout.Width(60))) PlayCurrent(0);
                using (new EditorGUI.DisabledScope(p.variants < 2))
                    if (GUILayout.Button("Play random variant", GUILayout.Width(130))) PlayCurrent(Random.Range(0, p.variants));
                if (GUILayout.Button("Bake", GUILayout.Width(60))) SynthBaker.Bake(p);
                var recipe = SynthRecipes.Find(p.id);
                using (new EditorGUI.DisabledScope(recipe == null))
                    if (GUILayout.Button(new GUIContent("Revert to recipe", recipe == null ? "No recipe in code for this id." : "Reset every field to the code recipe (keeps the review)."), GUILayout.Width(110)))
                    {
                        Undo.RecordObject(p, "Revert patch");
                        SynthRecipes.Apply(p, recipe);
                        EditorUtility.SetDirty(p);
                        _so.Update();
                        _stale = true;
                    }
                if (GUILayout.Button("Ping", GUILayout.Width(40))) EditorGUIUtility.PingObject(p);
            }

            EnsureRender();
            var waveRect = GUILayoutUtility.GetRect(10, 90, GUILayout.ExpandWidth(true));
            DrawWave(waveRect);
            float seconds = _render != null ? _render.Length / (float)SynthPatch.SampleRate : 0f;
            var specRect = GUILayoutUtility.GetRect(10, 120, GUILayout.ExpandWidth(true));
            if (_spec != null) GUI.DrawTexture(specRect, _spec, ScaleMode.StretchToFill);
            var facts = $"{seconds:0.00} s   peak {_peakDb:0.0} dBFS   RMS {_rmsDb:0.0} dBFS   above 9 kHz {_highShare * 100f:0.0}%";
            float tierDb = Loudness.TierDb(p.tier) + p.tierTrimDb;
            var scope = $"mid {_stats.midDb:0.0} dB" + (p.levelMode == LevelMode.Tier ? $" (tier {p.tier} {tierDb:0.0})" : " (peak levelled)") +
                        $"   low {_stats.lowShare:0.00}   tail {_stats.tailShare:0.00}   silence {_stats.silenceShare:0.00}";
            if (p.mode == SynthMode.Loop) facts += $"   loop seam {_seam:0.0}x (above 4 = tick)";
            if (p.variants > 1) facts += $"   showing variant {_renderVariant + 1}/{p.variants}";
            EditorGUILayout.LabelField(facts, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(scope, EditorStyles.miniLabel);
            if (_stats.lowShare > AudioScope.MaxLowShare) EditorGUILayout.HelpBox($"Mostly below 250 Hz (low share {_stats.lowShare:0.00}): small speakers won't play it. Move the body up to 300-1500 Hz.", MessageType.Warning);
            if (_highShare > 0.02f) EditorGUILayout.HelpBox("More than 2% of the energy is above 9 kHz: may sound harsh. Lower the filter or master low-pass.", MessageType.Warning);
            if (p.mode == SynthMode.Sequence)
                foreach (var n in p.notes)
                    if (SynthNotes.TryParse(n.note, out int m) && !SynthNotes.InScale(m))
                        EditorGUILayout.HelpBox($"{n.note} is outside D major pentatonic (D E F# A B).", MessageType.Warning);

            DrawModelPresetRow(p);

            _fieldScroll = EditorGUILayout.BeginScrollView(_fieldScroll);
            _so.Update();
            EditorGUI.BeginChangeCheck();
            var it = _so.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.name == "m_Script") continue;
                EditorGUILayout.PropertyField(it, true);
            }
            if (EditorGUI.EndChangeCheck())
            {
                _so.ApplyModifiedProperties();
                _stale = true;
                _lastChange = EditorApplication.timeSinceStartup;
                if (_autoPlay) _pendingPlay = true;
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>Model preset -> layer N (Apply), or as a new layer (Add layer).</summary>
        void DrawModelPresetRow(SynthPatch p)
        {
            var names = ModelPresets.Names;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Model preset", GUILayout.Width(80));
                _presetIndex = Mathf.Clamp(EditorGUILayout.Popup(_presetIndex, names), 0, names.Length - 1);
                GUILayout.Label("layer", GUILayout.Width(34));
                _presetLayer = Mathf.Max(0, EditorGUILayout.IntField(_presetLayer, GUILayout.Width(28)));
                using (new EditorGUI.DisabledScope(_presetLayer >= p.layers.Count))
                    if (GUILayout.Button(new GUIContent("Apply", "Set this layer's model and settings to the preset (keeps note, offset, gain, group)."), GUILayout.Width(50)))
                    {
                        Undo.RecordObject(p, "Apply model preset");
                        ModelPresets.Apply(names[_presetIndex], p.layers[_presetLayer]);
                        AfterScriptedChange(p);
                    }
                if (GUILayout.Button(new GUIContent("Add layer", "Append a new layer made from the preset."), GUILayout.Width(70)))
                {
                    Undo.RecordObject(p, "Add model layer");
                    p.layers.Add(ModelPresets.Layer(names[_presetIndex]));
                    _presetLayer = p.layers.Count - 1;
                    AfterScriptedChange(p);
                }
            }
        }

        void AfterScriptedChange(SynthPatch p)
        {
            EditorUtility.SetDirty(p);
            _so.Update();
            _stale = true;
            if (_autoPlay) _pendingPlay = true;
        }

        void EnsureRender(int variant = -1)
        {
            if (_selected == null) return;
            if (variant >= 0 && variant != _renderVariant)
            {
                _renderVariant = variant;
                _stale = true;
            }
            if (!_stale && _render != null) return;
            _stale = false;
            _renderVariant = Mathf.Clamp(_renderVariant, 0, Mathf.Max(0, _selected.variants - 1));
            _render = SynthRenderer.Render(_selected, _renderVariant);
            SynthRenderer.Analyze(_render, out _peakDb, out _rmsDb, out _highShare);
            _seam = SynthRenderer.LoopSeam(_render);
            if (_previewClip != null) DestroyImmediate(_previewClip);
            _previewClip = AudioPreview.MakeClip(_selected.id, _render);
            BuildWave(_render);
            _stats = Loudness.Analyse(_render, SynthPatch.SampleRate);
            _spec = AudioScope.ToTexture(AudioScope.Spectrogram(_render, SynthPatch.SampleRate, 600, 120), 600, 120, _spec);
        }

        void PlayCurrent(int variant)
        {
            EnsureRender(variant);
            AudioPreview.Play(_previewClip, _selected != null && _selected.mode == SynthMode.Loop);
        }

        void BuildWave(float[] data)
        {
            const int w = 600, h = 90;
            if (_wave == null)
            {
                _wave = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            }
            var px = new Color32[w * h];
            var bg = new Color32(30, 30, 34, 255);
            var fg = new Color32(120, 200, 255, 255);
            var mid = new Color32(70, 70, 80, 255);
            for (int i = 0; i < px.Length; i++) px[i] = bg;
            for (int x = 0; x < w; x++) px[(h / 2) * w + x] = mid;
            if (data != null && data.Length > 0)
            {
                for (int x = 0; x < w; x++)
                {
                    int a = (int)((long)x * data.Length / w), b = Mathf.Max(a + 1, (int)((long)(x + 1) * data.Length / w));
                    float lo = 0, hi = 0;
                    for (int i = a; i < b && i < data.Length; i++)
                    {
                        lo = Mathf.Min(lo, data[i]);
                        hi = Mathf.Max(hi, data[i]);
                    }
                    int y0 = Mathf.Clamp((int)((lo * 0.5f + 0.5f) * (h - 1)), 0, h - 1);
                    int y1 = Mathf.Clamp((int)((hi * 0.5f + 0.5f) * (h - 1)), 0, h - 1);
                    for (int y = y0; y <= y1; y++) px[y * w + x] = fg;
                }
            }
            _wave.SetPixels32(px);
            _wave.Apply();
        }

        void DrawWave(Rect r)
        {
            if (_wave != null) GUI.DrawTexture(r, _wave, ScaleMode.StretchToFill);
        }

        static void Dot(Rect r, ReviewStatus s)
        {
            var c = s == ReviewStatus.Approved ? new Color(0.3f, 0.85f, 0.4f) : s == ReviewStatus.Redo ? new Color(0.95f, 0.35f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
            EditorGUI.DrawRect(r, c);
        }

        // ---- review tab ----

        /// <summary>One line in the Review tab: a patch, or (later) a cue with clips from elsewhere.</summary>
        public class ReviewRow
        {
            public string id, group, info, tag = "";
            public bool gap;
            public AudioReview review;
            public Object owner;
            public System.Action play;
        }

        /// <summary>Extra rows (cues without a patch) from other tools.</summary>
        public static System.Func<IEnumerable<ReviewRow>> ExtraRows;

        List<ReviewRow> Rows()
        {
            var rows = new List<ReviewRow>();
            var ids = new HashSet<string>();
            foreach (var p in _patches)
            {
                if (p == null) continue;
                var patch = p;
                ids.Add(p.id);
                rows.Add(new ReviewRow
                {
                    id = p.id, group = SynthBaker.Group(p), gap = p.gap, review = p.review, owner = p,
                    tag = p.confidence == SynthConfidence.None ? "" : "synth-" + p.confidence.ToString().ToLowerInvariant(),
                    play = () => PlayPatchVariant(patch),
                });
            }
            if (ExtraRows != null)
                foreach (var r in ExtraRows())
                    if (!ids.Contains(r.id)) rows.Add(r);
            return rows;
        }

        void PlayPatchVariant(SynthPatch p)
        {
            // The cue when there is one: it may mix in other clips (Kenney variants).
            var cue = AudioCueTools.AllCues().Find(c => c.id == p.id);
            var pick = cue != null ? cue.Pick() : null;
            if (pick != null)
            {
                AudioPreview.Play(pick, cue.loop);
                return;
            }
            var clips = SynthBaker.Clips(p);
            if (clips.Count > 0) AudioPreview.Play(clips[Random.Range(0, clips.Count)], p.mode == SynthMode.Loop);
            else
            {
                Select(p);
                PlayCurrent(0);
            }
        }

        void DrawReview()
        {
            var rows = Rows();
            int approved = 0, redo = 0;
            foreach (var r in rows)
            {
                if (r.review.status == ReviewStatus.Approved) approved++;
                if (r.review.status == ReviewStatus.Redo) redo++;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _filter = (ReviewFilter)EditorGUILayout.EnumPopup(_filter, GUILayout.Width(110));
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(200));
                GUILayout.Label($"{rows.Count} sounds   {approved} approved   {redo} redo   {rows.Count - approved - redo} to go", EditorStyles.miniLabel);
            }
            EditorGUILayout.HelpBox("Play, then Approve or Redo. Write what's wrong in the note (\"too harsh\", \"longer tail\"); the next session reads the Redo notes.", MessageType.None);

            _reviewScroll = EditorGUILayout.BeginScrollView(_reviewScroll);
            string group = null;
            foreach (var r in rows)
            {
                if (_search.Length > 0 && r.id.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (_filter == ReviewFilter.Unreviewed && r.review.status != ReviewStatus.None) continue;
                if (_filter == ReviewFilter.Redo && r.review.status != ReviewStatus.Redo) continue;
                if (_filter == ReviewFilter.Approved && r.review.status != ReviewStatus.Approved) continue;
                if (_filter == ReviewFilter.Gaps && !r.gap) continue;
                if (r.group != group)
                {
                    group = r.group;
                    EditorGUILayout.LabelField(group, EditorStyles.boldLabel);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    var dot = GUILayoutUtility.GetRect(10, 18, GUILayout.Width(10));
                    Dot(new Rect(dot.x, dot.y + 5, 8, 8), r.review.status);
                    if (GUILayout.Button("▶", GUILayout.Width(26))) r.play?.Invoke();
                    GUILayout.Label(new GUIContent((r.gap ? "★ " : "") + r.id, r.info), GUILayout.Width(190));
                    GUILayout.Label(r.tag, EditorStyles.miniLabel, GUILayout.Width(74));
                    if (GUILayout.Toggle(r.review.status == ReviewStatus.Approved, "Approve", EditorStyles.miniButtonLeft, GUILayout.Width(60)) != (r.review.status == ReviewStatus.Approved))
                        SetStatus(r, r.review.status == ReviewStatus.Approved ? ReviewStatus.None : ReviewStatus.Approved);
                    if (GUILayout.Toggle(r.review.status == ReviewStatus.Redo, "Redo", EditorStyles.miniButtonRight, GUILayout.Width(50)) != (r.review.status == ReviewStatus.Redo))
                        SetStatus(r, r.review.status == ReviewStatus.Redo ? ReviewStatus.None : ReviewStatus.Redo);
                    var note = EditorGUILayout.DelayedTextField(r.review.note ?? "");
                    if (note != r.review.note)
                    {
                        Undo.RecordObject(r.owner, "Review note");
                        r.review.note = note;
                        Save(r.owner);
                    }
                    if (r.owner is SynthPatch sp && GUILayout.Button("Edit", GUILayout.Width(36)))
                    {
                        Select(sp);
                        _tab = Tab.Patches;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        static void SetStatus(ReviewRow r, ReviewStatus s)
        {
            Undo.RecordObject(r.owner, "Review");
            r.review.status = s;
            Save(r.owner);
        }

        static void Save(Object o)
        {
            EditorUtility.SetDirty(o);
            AssetDatabase.SaveAssetIfDirty(o);
        }

        // ---- loop tool ----

        void DrawLoopTool()
        {
            EditorGUILayout.HelpBox("Cut a seamless loop from a long clip (Lyria music): set start and end, a crossfade, and bake a new looping .wav next to the source.", MessageType.None);
            var src = (AudioClip)EditorGUILayout.ObjectField("Source clip", _loopSource, typeof(AudioClip), false);
            if (src != _loopSource)
            {
                _loopSource = src;
                _loopData = null;
                if (src != null)
                {
                    _loopStart = 0f;
                    _loopEnd = src.length;
                }
            }
            if (_loopSource == null) return;

            float len = _loopSource.length;
            EditorGUILayout.LabelField($"{len:0.00} s, {_loopSource.channels} ch, {_loopSource.frequency} Hz");
            _loopStart = EditorGUILayout.Slider("Loop start (s)", _loopStart, 0f, len);
            _loopEnd = EditorGUILayout.Slider("Loop end (s)", _loopEnd, 0f, len);
            _loopFade = EditorGUILayout.Slider("Crossfade (s)", _loopFade, 0.05f, 8f);
            if (_loopEnd - _loopStart < _loopFade * 2f)
                EditorGUILayout.HelpBox("The loop must be at least twice the crossfade.", MessageType.Warning);
            if (_loopEnd + _loopFade > len)
                EditorGUILayout.HelpBox("No audio after the loop end for the crossfade: the tail before the end is used instead (the loop gets shorter by the crossfade).", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview loop"))
                {
                    var data = MakeLoop();
                    if (data != null)
                    {
                        if (_previewClip != null) DestroyImmediate(_previewClip);
                        _previewClip = AudioPreview.MakeClip(_loopSource.name + "_loop", data, _loopChannels, _loopRate);
                        AudioPreview.Play(_previewClip, true);
                    }
                }
                if (GUILayout.Button("Preview the seam (last 3 s + first 3 s)"))
                {
                    var data = MakeLoop();
                    if (data != null)
                    {
                        int n = Mathf.Min(data.Length / 2, 3 * _loopRate * _loopChannels);
                        n -= n % _loopChannels;
                        var seam = new float[n * 2];
                        System.Array.Copy(data, data.Length - n, seam, 0, n);
                        System.Array.Copy(data, 0, seam, n, n);
                        if (_previewClip != null) DestroyImmediate(_previewClip);
                        _previewClip = AudioPreview.MakeClip("seam", seam, _loopChannels, _loopRate);
                        AudioPreview.Play(_previewClip);
                    }
                }
                if (GUILayout.Button("Bake loop"))
                {
                    var data = MakeLoop();
                    if (data != null)
                    {
                        var srcPath = AssetDatabase.GetAssetPath(_loopSource);
                        var dir = srcPath.StartsWith(AudioImportRules.Root) ? Path.GetDirectoryName(srcPath).Replace('\\', '/') : AudioImportRules.MusicRoot.TrimEnd('/');
                        var path = $"{dir}/{_loopSource.name}_loop.wav";
                        WavIO.Write(path, data, _loopChannels, _loopRate);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        Debug.Log($"[AudioLab] Baked loop {path} ({data.Length / _loopChannels / (float)_loopRate:0.00} s). Set the cue's loop flag.");
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
                    }
                }
            }
        }

        float[] MakeLoop()
        {
            if (!ReadSource()) return null;
            int ch = _loopChannels, rate = _loopRate, frames = _loopData.Length / ch;
            int s = Mathf.Clamp(Mathf.RoundToInt(_loopStart * rate), 0, frames - 1);
            int e = Mathf.Clamp(Mathf.RoundToInt(_loopEnd * rate), s + 1, frames);
            int x = Mathf.Clamp(Mathf.RoundToInt(_loopFade * rate), 1, (e - s) / 2);
            int tail = e;
            if (e + x > frames)
            {
                // Not enough after the end: crossfade the last x frames of the region into the head instead.
                tail = e - x;
                e = tail;
            }
            int n = e - s;
            var outBuf = new float[n * ch];
            for (int i = 0; i < n; i++)
                for (int c = 0; c < ch; c++)
                {
                    float head = _loopData[(s + i) * ch + c];
                    if (i < x)
                    {
                        float u = i / (float)x;
                        float t = _loopData[(tail + i) * ch + c];
                        outBuf[i * ch + c] = head * Mathf.Sqrt(u) + t * Mathf.Sqrt(1f - u);
                    }
                    else outBuf[i * ch + c] = head;
                }
            return outBuf;
        }

        bool ReadSource()
        {
            if (_loopData != null) return true;
            var clip = _loopSource;
            var path = AssetDatabase.GetAssetPath(clip);
            bool temp = clip.loadType != AudioClipLoadType.DecompressOnLoad;
            if (temp)
            {
                // Streaming / compressed clips can't hand out samples: reimport as PCM for a moment.
                AudioImportRules.ReadablePcm.Add(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            try
            {
                clip.LoadAudioData();
                var data = new float[clip.samples * clip.channels];
                if (!clip.GetData(data, 0))
                {
                    Debug.LogError($"[AudioLab] Couldn't read samples from {path}.");
                    return false;
                }
                _loopData = data;
                _loopChannels = clip.channels;
                _loopRate = clip.frequency;
                return true;
            }
            finally
            {
                if (temp)
                {
                    AudioImportRules.ReadablePcm.Remove(path);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
}
