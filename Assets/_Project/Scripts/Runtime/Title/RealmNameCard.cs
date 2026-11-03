using System;
using System.Collections;
using Diorama.Audio;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14c: a carved title card rises over the island's far half when the explorer enters a realm (or comes home),
    /// holds, then sinks: the realm's name in the logo's chunky strokes and its emblem on a board in the realm's style
    /// (a baked prop per realm, PropKit.RealmCards), one line under it (TextMeshPro on the board's lower band). The first
    /// visit shows name + line (<see cref="firstSeconds"/>); later visits the name only (<see cref="againSeconds"/>); the
    /// first visit after the realm is restored shows its restored line once. It waits until the arrival line has been
    /// said (never over the path view while the Spirit sets an objective), and never during a battle, the menu, the
    /// title or the finale. Seen flags are saved as the puzzle stage <c>card.&lt;realm&gt;</c> (1 = seen, 2 = restored line seen).
    /// </summary>
    public class RealmNameCard : MonoBehaviour
    {
        [Serializable]
        public struct Card
        {
            public WorldMap map;
            [Tooltip("The baked card (Data/Props/RealmCard_*).")]
            public PropRecipe recipe;
            [Tooltip("The line under the name on the first visit (empty = none).")]
            public string line;
            [Tooltip("The line on the first visit after the realm is restored (empty = none).")]
            public string restoredLine;
            [Tooltip("The realm's shard (restored once it is taken); empty for the hub.")]
            public string shard;
        }

        public IslandBuilder island;
        public SceneSwap swap;
        public EncounterDirector encounters;
        [Tooltip("Diorama/Prop, window clip off (Materials/Props/Logo.mat, as the title logo).")]
        public Material material;
        public BattleFx fx;
        public Card[] cards = new Card[0];

        [Header("Placement (island-local metres at island scale 1)")]
        public float width = 0.26f;
        [Tooltip("Card centre this high over the island's top surface. Nov 3 (Mugesh): centred and higher (it sat low at the back, behind Cinderwood's trees).")]
        public float height = 0.25f;
        [Tooltip("Toward the far rim: 0 = the island's middle, 1 = the far edge.")]
        [Range(0f, 1.2f)] public float far = 0.1f;
        [Tooltip("It rises from this far below its spot (and sinks back there).")]
        public float rise = 0.08f;

        [Header("Timing (s)")]
        public float firstSeconds = 3f, againSeconds = 1.5f;
        [Tooltip("Longest wait for the arrival line before it shows anyway.")]
        public float waitMax = 10f;
        public float riseSeconds = 0.6f, sinkSeconds = 0.6f;

        [Header("Line")]
        [Tooltip("Unused since step 14f: the line is at the readable size (TextRule) under the card, wrapping in two lines at most.")]
        public float lineSize = 0.1f;
        public Color lineColor = new Color(1f, 0.96f, 0.86f, 1f);

        static readonly int WindowClipId = Shader.PropertyToID("_WindowClip");
        static readonly int RestoredSelfId = Shader.PropertyToID("_RestoredSelf");
        static readonly int ClearSinkId = Shader.PropertyToID("_ClearSink");

        /// <summary>A card is up (tests, the bridge).</summary>
        public static bool Showing { get; private set; }
        public static string ShowingRealm { get; private set; }

        Transform _pivot, _meshT;
        MeshRenderer _renderer;
        MeshFilter _filter;
        MaterialPropertyBlock _block;
        TextMeshPro _text;
        Coroutine _run;
        float _yaw, _show, _lift, _scale = 1f;

        void Start()
        {
            if (island == null || swap == null || material == null)
            {
                Debug.LogError("[RealmNameCard] Needs the island, the scene swap and the logo material (Set Up MR_Main (14c Galewick first)).", this);
                enabled = false;
                return;
            }
            _pivot = new GameObject("Realm Card Pivot").transform;
            _pivot.SetParent(island.transform, false);
            _meshT = new GameObject("Realm Card").transform;
            _meshT.SetParent(_pivot, false);
            _filter = _meshT.gameObject.AddComponent<MeshFilter>();
            _renderer = _meshT.gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _block = new MaterialPropertyBlock();
            var go = new GameObject("Realm Card Line");
            go.transform.SetParent(_pivot, false);
            _text = go.AddComponent<TextMeshPro>();
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.NoWrap;
            _text.color = lineColor;
            _text.fontStyle = FontStyles.Bold;
            _text.outlineWidth = 0.28f;
            _text.outlineColor = new Color32(40, 26, 16, 255);
            _pivot.gameObject.SetActive(false);
            swap.TravelFinished += OnTravelled;
        }

        void OnDestroy()
        {
            if (swap != null) swap.TravelFinished -= OnTravelled;
            Showing = false;
        }

        void OnTravelled(WorldMap to)
        {
            int i = Array.FindIndex(cards, c => c.map != null && c.map == to);
            if (i < 0 || cards[i].recipe == null || cards[i].recipe.bakedMesh == null) return;
            if (_run != null) StopCoroutine(_run);
            _run = StartCoroutine(Run(cards[i]));
        }

        /// <summary>
        /// What this visit shows, from the saved <paramref name="flags"/> (1 = seen, 2 = restored line seen) (pure).
        /// Returns the new flags; <paramref name="withLine"/> = name + line (long), else the name only (short).
        /// </summary>
        public static int Plan(int flags, bool restored, out bool withLine, out bool restoredLine)
        {
            withLine = restoredLine = false;
            if ((flags & 1) == 0)
            {
                withLine = true;
                restoredLine = restored;
                return 1 | (restored ? 2 : 0);
            }
            if (restored && (flags & 2) == 0)
            {
                withLine = restoredLine = true;
                return flags | 2;
            }
            return flags;
        }

        public static string FlagId(WorldMap map) => "card." + (map != null ? map.realm : "?");

        bool Calm => !SceneSwap.IsBusy && !SceneSwap.IsInside && !TitleScreen.Showing && !Opening.InProgress && !GameMenu.IsOpen &&
                     !FinaleDirector.Running && (encounters == null || !encounters.InBattle);

        IEnumerator Run(Card card)
        {
            // The arrival line first: the card never covers the path while the Spirit sets the objective.
            string arrival = card.map.arrivalLine;
            float waited = 0f;
            yield return new WaitForSeconds(0.4f);
            // Only the arrival line itself: an objective pinned right after it stays up the whole moment (Nov 3: the card
            // waited for it and came 10 s late in Cinderwood and Lilymere).
            while (waited < waitMax && (!Calm || !string.IsNullOrEmpty(arrival) && Dialogue.IsPending(arrival)))
            {
                waited += Time.deltaTime;
                yield return null;
            }
            if (!Calm || island.world != card.map)
            {
                Debug.Log($"[RealmNameCard] {card.map.realm}: skipped ({(island.world != card.map ? "left the map" : "not calm: a battle, the menu, a swap or the finale")}).");
                _run = null;
                yield break;
            }
            bool restored = !string.IsNullOrEmpty(card.shard) && (PuzzleState.IsSolved(card.shard) || PuzzleState.Stage(card.shard) >= 1);
            string id = FlagId(card.map);
            int flags = Plan(PuzzleState.Stage(id), restored, out bool withLine, out bool restoredLine);
            if (flags != PuzzleState.Stage(id)) PuzzleState.SetStage(id, flags);
            string line = withLine ? restoredLine && !string.IsNullOrEmpty(card.restoredLine) ? card.restoredLine : card.line : "";
            float hold = withLine && !string.IsNullOrEmpty(line) ? firstSeconds : againSeconds;
            Debug.Log($"[RealmNameCard] {card.map.realm}: '{(string.IsNullOrEmpty(line) ? "(name only)" : line)}' for {hold:0.#} s (waited {waited:0.0} s for the arrival line).");
            Setup(card, line);
            Sound(AudioIds.CardRise, AudioIds.LogoRise);
            Showing = true;
            ShowingRealm = card.map.realm;
            for (float t = 0f; t < riseSeconds; t += Time.deltaTime)
            {
                float u = t / riseSeconds;
                _show = EaseOutBack(u);
                _lift = -rise * (1f - Mathf.SmoothStep(0f, 1f, u));
                yield return null;
            }
            _show = 1f;
            _lift = 0f;
            Sparkle(18);
            for (float t = 0f; t < hold && Calm && island.world == card.map; t += Time.deltaTime) yield return null;
            Sound(AudioIds.CardSink, null);
            for (float t = 0f; t < sinkSeconds; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / sinkSeconds);
                _lift = -rise * u;
                _show = 1f - u * u;
                yield return null;
            }
            _pivot.gameObject.SetActive(false);
            Showing = false;
            ShowingRealm = null;
            _run = null;
        }

        void Setup(Card card, string line)
        {
            var mesh = card.recipe.bakedMesh;
            _filter.sharedMesh = mesh;
            var b = mesh.bounds;
            // Step 14f: the name's capitals at least the readable size (shape unit = a capital's height).
            _scale = Mathf.Max(width / Mathf.Max(1e-4f, b.size.x), TextRule.IslandCap);
            _meshT.localScale = Vector3.one * _scale;
            _meshT.localPosition = -b.center * _scale;
            _block.Clear();
            card.recipe.ApplyTo(_block);
            _block.SetFloat(WindowClipId, 0f);
            _block.SetFloat(RestoredSelfId, 1f);
            _block.SetFloat(ClearSinkId, 0f);
            _renderer.SetPropertyBlock(_block);
            // The line on the board's lower band (shape units: CardLineY), just in front of the board.
            _text.text = line;
            _text.gameObject.SetActive(!string.IsNullOrEmpty(line));
            // Step 14f: the board's lower band was too small for a readable line: it sits under the card now, at the
            // readable size (TextRule), wrapping at the card's width or 0.5 m (island scale 1), dark-outlined over the scene.
            _text.enableAutoSizing = false;
            // Nov 3 late (Mugesh: the line under the name was too big): secondary to the name.
            _text.fontSize = TextRule.IslandFont * TextRule.Secondary;
            _text.textWrappingMode = TextWrappingModes.Normal;
            float wrap = Mathf.Max(width, 0.5f), lineH = _text.fontSize * 0.1f * 1.25f;
            _text.rectTransform.sizeDelta = new Vector2(wrap, lineH * 2.2f);
            _text.transform.localPosition = new Vector3(0f, (b.min.y - b.center.y) * _scale - lineH * 0.9f, (b.max.z - b.center.z) * _scale + 0.0015f);
            // TextMeshPro reads toward +X; the card faces +Z: turn the text round to face the same way.
            _text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            _yaw = HeadYaw();
            _show = 0f;
            _lift = -rise;
            _pivot.gameObject.SetActive(true);
            Place();
        }

        /// <summary>The line's centre in the card's shape units (PropKit.CardLineY).</summary>
        const float LineY = -0.62f;

        void Update()
        {
            if (_pivot == null || !_pivot.gameObject.activeSelf) return;
            float k = 1f - Mathf.Exp(-Time.deltaTime * 2.5f);
            _yaw = Mathf.LerpAngle(_yaw, HeadYaw(), k);
            Place();
        }

        void Place()
        {
            float s = Mathf.Max(0.001f, _show);
            _pivot.localPosition = RestLocal + Vector3.up * (_lift + 0.004f * Mathf.Sin(Time.time * 1.3f));
            _pivot.localRotation = Quaternion.Euler(0f, _yaw, 1.5f * Mathf.Sin(Time.time * 0.7f));
            _pivot.localScale = Vector3.one * s;
        }

        /// <summary>Where the card rests (island-local): over the far half, <see cref="height"/> above the cap.</summary>
        public Vector3 RestLocal
        {
            get
            {
                if (island == null || island.map == null) return Vector3.up * height;
                var b = island.LocalBounds;
                return new Vector3(b.center.x, island.CapTopLocal + height, Mathf.Lerp(b.center.z, b.min.z, far));
            }
        }

        float HeadYaw()
        {
            var cam = Camera.main;
            if (cam == null || island == null) return _yaw;
            Vector3 toHead = island.transform.InverseTransformPoint(cam.transform.position) - RestLocal;
            toHead.y = 0f;
            return toHead.sqrMagnitude < 1e-6f ? _yaw : Mathf.Atan2(toHead.x, toHead.z) * Mathf.Rad2Deg;
        }

        /// <summary>Its own cue (card.rise / card.sink, made Nov 3), the logo's as a stand-in when it isn't there.</summary>
        void Sound(string id, string fallback)
        {
            var at = _pivot != null ? _pivot.position : island.transform.position;
            if (AudioService.Has(id)) GameAudio.At(id, at);
            else if (!string.IsNullOrEmpty(fallback)) GameAudio.At(fallback, at);
        }

        void Sparkle(int count)
        {
            if (fx == null || _pivot == null) return;
            float s = island.transform.lossyScale.x;
            fx.Burst(_pivot.position, count, RuneBoard.Gold, 0.1f * s, 0.8f, 0.004f * s, Vector3.up * 0.03f);
        }

        static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c = 1.7f;
            t -= 1f;
            return 1f + (c + 1f) * t * t * t + c * t * t;
        }

        /// <summary>Debug (bridge): show the card of <paramref name="map"/> now as if just arrived.</summary>
        public void DebugShow(WorldMap map) => OnTravelled(map);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Showing = false;
            ShowingRealm = null;
        }
    }
}
