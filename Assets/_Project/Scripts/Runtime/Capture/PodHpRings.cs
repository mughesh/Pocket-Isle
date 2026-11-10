using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 29 C (playtests: nobody noticed their Islings were hurt before a Keeper): an HP ring round every rim pod,
    /// always (not only in battle). Green over half, yellow over a quarter, red below; at a quarter or less it blinks slowly
    /// with a small red "!" badge; a fainted Isling's ring is grey with a small "zz". A heal refills the rings with a sweep
    /// (<see cref="Sweep"/>, one after another for the lodge). Plain C#, owned by <see cref="PodRack"/>; material from
    /// Resources/PodRing (Diorama/Pod Ring).
    /// </summary>
    public class PodHpRings
    {
        public static readonly Color Green = new Color(0.44f, 0.86f, 0.42f), Yellow = new Color(0.97f, 0.8f, 0.28f),
            Red = new Color(0.96f, 0.3f, 0.24f), Grey = new Color(0.55f, 0.55f, 0.57f);
        public const float LowShare = 0.25f;

        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int FillColorId = Shader.PropertyToID("_FillColor");
        static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        static readonly int InnerId = Shader.PropertyToID("_Inner");
        static readonly int OuterId = Shader.PropertyToID("_Outer");

        /// <summary>Ring size relative to the pod's diameter.</summary>
        public float sizeOfPod = DefaultSize;
        /// <summary>Nov 11: slim, just round the glass (2.1 overlapped the next pod).</summary>
        public const float DefaultSize = 1.42f;
        /// <summary>Fill speed (share per second) when HP changes; a sweep runs at this rate too.</summary>
        public float fillRate = 1.6f;

        class Ring
        {
            public Transform root;
            public MeshRenderer renderer;
            public TextMeshPro badge;
            public float shown = -1f;
            public float sweepAt;
            public bool chimed = true;
        }

        readonly Material _material;
        readonly Mesh _quad;
        readonly Ring[] _rings = new Ring[Party.Size];
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        public PodHpRings(Material material, Mesh quad)
        {
            _material = material;
            _quad = quad;
            if (material == null) Debug.LogError("[PodHpRings] No Resources/PodRing material: no HP rings on the pods.");
        }

        /// <summary>The ring's colour for an HP share (pure).</summary>
        public static Color ColourFor(float share, bool fainted) =>
            fainted ? Grey : share > 0.5f ? Green : share > LowShare ? Yellow : Red;

        /// <summary>Low enough to blink and show "!" (pure).</summary>
        public static bool Low(float share, bool fainted) => !fainted && share <= LowShare;

        /// <summary>The badge text: "!" when low, "zz" when fainted, else none (pure).</summary>
        public static string Badge(float share, bool fainted) => fainted ? "zz" : Low(share, fainted) ? "!" : "";

        /// <summary>HP share of an Isling (0..1).</summary>
        public static float Share(IslingInstance c) => c != null && c.MaxHp > 0 ? Mathf.Clamp01(c.currentHp / (float)c.MaxHp) : 0f;

        /// <summary>Rings shown last frame (tests, the bridge).</summary>
        public int Shown { get; private set; }

        /// <summary>The fill a ring shows now (tests, the bridge); -1 = none.</summary>
        public float ShownFill(int slot) => slot >= 0 && slot < _rings.Length && _rings[slot] != null ? _rings[slot].shown : -1f;

        /// <summary>
        /// The rings refill from empty, one after another <paramref name="stagger"/> s apart (0 = all at once), each with a
        /// soft chime as it starts (when <paramref name="chime"/>).
        /// </summary>
        public void Sweep(float stagger, bool chime)
        {
            for (int i = 0; i < _rings.Length; i++)
            {
                var r = _rings[i];
                if (r == null) continue;
                r.shown = 0f;
                r.sweepAt = Time.time + i * stagger;
                r.chimed = !chime;
            }
        }

        public void Tick(PodRack rack, Party party, bool hidden)
        {
            Shown = 0;
            var cam = Camera.main;
            for (int slot = 0; slot < _rings.Length; slot++)
            {
                var pod = rack.Pod(slot);
                var c = party != null ? party.At(slot) : null;
                bool show = !hidden && _material != null && pod != null && c != null && c.species != null && pod.isActiveAndEnabled &&
                            !pod.Detached && pod.transform.lossyScale.x > 1e-4f;
                var r = _rings[slot];
                if (!show)
                {
                    if (r != null && r.root.gameObject.activeSelf) r.root.gameObject.SetActive(false);
                    continue;
                }
                if (r == null) r = _rings[slot] = Make(slot, pod.gameObject.layer);
                if (!r.root.gameObject.activeSelf) r.root.gameObject.SetActive(true);
                Shown++;

                float share = Share(c);
                bool fainted = c.IsFainted;
                // Nov 11: in battle the ring follows the HP bar, which only drops when the hit lands (the model's HP
                // changes when the turn is worked out, before the wild's move plays).
                var stage = BattleStage.Live;
                if (stage != null && stage.ShownHp(c, out float barHp, out int barMax))
                {
                    share = Mathf.Clamp01(barHp / Mathf.Max(1, barMax));
                    fainted = barHp <= 0f;
                }
                if (r.shown < 0f) r.shown = share;
                if (Time.time >= r.sweepAt)
                {
                    if (!r.chimed)
                    {
                        r.chimed = true;
                        GameAudio.At(Diorama.Audio.AudioIds.ShrineHeal, pod.transform.position).SetVolume(0.45f);
                    }
                    r.shown = Mathf.MoveTowards(r.shown, share, Time.deltaTime * fillRate);
                }

                float size = pod.WorldRadius * 2f * sizeOfPod;
                r.root.position = pod.transform.position;
                if (cam != null)
                {
                    Vector3 away = r.root.position - cam.transform.position;
                    if (away.sqrMagnitude > 1e-6f)
                    {
                        r.root.rotation = Quaternion.LookRotation(away, Vector3.up);
                        // A little toward the head, so the rim's rock and the pod's own glass never cover it.
                        r.root.position -= away.normalized * pod.WorldRadius * 0.9f;
                    }
                }
                r.root.localScale = Vector3.one * size;

                bool low = Low(share, fainted);
                float blink = low ? 0.55f + 0.45f * Mathf.Sin(Time.time * 4f) : 1f;
                _block.Clear();
                _block.SetFloat(FillId, fainted ? 1f : r.shown);
                _block.SetColor(FillColorId, ColourFor(r.shown, fainted));
                _block.SetFloat(AlphaId, fainted ? 0.85f : blink);
                // Nov 11: slim, just outside the pod's glass (the thick 2.1x ring overlapped the next pod).
                _block.SetFloat(InnerId, 0.75f);
                _block.SetFloat(OuterId, 0.95f);
                r.renderer.SetPropertyBlock(_block);

                string badge = Badge(share, fainted);
                if (r.badge.text != badge) r.badge.text = badge;
                var bc = fainted ? new Color(0.85f, 0.85f, 0.9f) : Red;
                bc.a = fainted ? 0.95f : blink;
                r.badge.color = bc;
            }
        }

        Ring Make(int slot, int layer)
        {
            var root = new GameObject($"Pod HP Ring {slot + 1}").transform;
            root.gameObject.layer = layer;
            var quad = new GameObject("Ring");
            quad.layer = layer;
            quad.transform.SetParent(root, false);
            quad.AddComponent<MeshFilter>().sharedMesh = _quad;
            var mr = quad.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var badgeGo = new GameObject("Badge");
            badgeGo.layer = layer;
            badgeGo.transform.SetParent(root, false);
            // Top-right of the ring, a little toward the head.
            badgeGo.transform.localPosition = new Vector3(0.36f, 0.36f, -0.05f);
            var t = badgeGo.AddComponent<TextMeshPro>();
            t.fontSize = 2.6f;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.outlineWidth = 0.25f;
            t.outlineColor = new Color32(30, 15, 10, 255);
            t.rectTransform.sizeDelta = new Vector2(0.6f, 0.4f);
            t.text = "";
            return new Ring { root = root, renderer = mr, badge = t };
        }

        public void Destroy()
        {
            foreach (var r in _rings)
                if (r != null && r.root != null) Object.Destroy(r.root.gameObject);
        }
    }
}
