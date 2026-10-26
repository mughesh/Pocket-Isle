using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The look of one capture pod: a glass bubble with a brass band and a tiny copy of its creature inside (the baked
    /// creature mesh, scaled down and slowly turning; outlines dropped to save draw calls). Glows when it is the lead
    /// or hovered; a fainted creature's pod turns grey and cloudy with flickering cracks of light, and the creature lies
    /// down. While the creature is out in battle the bubble is empty.
    /// The prefab is made by Tools > Pocket Isle > Set Up MR_Main (6 capture); sizes are in metres at island scale 1.
    /// </summary>
    public class PodView : MonoBehaviour
    {
        public MeshRenderer glass;
        public MeshRenderer band;
        [Tooltip("The tiny creature goes here (pod centre).")]
        public Transform miniAnchor;
        [Tooltip("Body material for the Isling inside (Creature_Plain: Diorama/Toon without the outline pass).")]
        public Material miniMaterial;
        [Tooltip("Glass radius at scale 1 (metres).")]
        public float radius = 0.015f;
        [Tooltip("Height of the tiny creature inside (metres at scale 1).")]
        public float miniHeight = 0.017f;
        [Tooltip("Degrees per second.")]
        public float spinSpeed = 30f;

        [Header("Colours")]
        public Color emptyTint = new Color(0.78f, 0.9f, 1f);
        public Color brass = new Color(0.79f, 0.6f, 0.26f);
        public Color dimBrass = new Color(0.45f, 0.38f, 0.28f);
        [Tooltip("Pods active (battle, lodge): the band of a pod that can be grabbed now.")]
        public Color brightBrass = new Color(1f, 0.8f, 0.38f);

        static readonly int TintId = Shader.PropertyToID("_Tint");
        static readonly int GlowId = Shader.PropertyToID("_Glow");
        static readonly int GreyId = Shader.PropertyToID("_Grey");
        static readonly int CrackId = Shader.PropertyToID("_Crack");
        static readonly int ShatterId = Shader.PropertyToID("_Shatter");
        static readonly int GoldId = Shader.PropertyToID("_Gold");
        static readonly int WhiteId = Shader.PropertyToID("_White");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>Glow target 0..1 (lead pod, armed cradle pod).</summary>
        public float Glow { get; set; }
        /// <summary>Hover target 0..1 (gaze or a fingertip near it): ~8% bigger, a white rim (step 9h).</summary>
        public float Hover { get; set; }
        /// <summary>Step 9h focus language: the player can act on this pod right now (gold rim).</summary>
        public float Actionable { get; set; }
        /// <summary>Dialogue highlight 0..1 (a hint points at this pod): brighter and a little bigger.</summary>
        public float Pulse { get; set; }
        /// <summary>0..1: white cracks race over the glass (the pod is about to shatter).</summary>
        public float Shatter { get; set; }
        /// <summary>Hides the tiny creature (it is out on the battle field).</summary>
        public bool IslingOut { get; set; }
        /// <summary>0..1: this pod can be grabbed now (pods active); the band brightens.</summary>
        public float Grabbable { get; set; }
        /// <summary>Someone else moves this pod (a flight); its rack leaves it alone.</summary>
        public bool Detached { get; set; }

        public IslingInstance Isling { get; private set; }

        GameObject _mini;
        CreatureView _miniView;
        bool _miniFainted;
        float _glow, _hover, _spin, _gold;
        MaterialPropertyBlock _glassBlock, _bandBlock;
        Vector3 _baseScale = Vector3.one;
        bool _hasBaseScale;

        /// <summary>World radius right now.</summary>
        public float WorldRadius => radius * transform.lossyScale.x;

        /// <summary>Sets the grab volume (the pod's sphere collider) to this diameter in metres at island scale 1.</summary>
        public void SetGrabDiameter(float diameter)
        {
            var col = GetComponent<SphereCollider>();
            if (col != null) col.radius = diameter * 0.5f / Mathf.Max(1e-4f, BaseScale.x);
        }

        /// <summary>Scale the rack wants (hover adds on top). Set it instead of localScale.</summary>
        public Vector3 BaseScale
        {
            get => _hasBaseScale ? _baseScale : transform.localScale;
            set
            {
                _baseScale = value;
                _hasBaseScale = true;
            }
        }

        void Awake()
        {
            _glassBlock = new MaterialPropertyBlock();
            _bandBlock = new MaterialPropertyBlock();
            if (!_hasBaseScale) BaseScale = transform.localScale;
        }

        /// <summary>The species whose tiny copy is inside right now (it changes when the creature evolves).</summary>
        public IslingSpecies ShownSpecies => _shownSpecies;

        IslingSpecies _shownSpecies;

        /// <summary>Puts this creature in the pod (null = empty pod).</summary>
        public void Show(IslingInstance creature)
        {
            Isling = creature;
            BuildMini(creature != null ? creature.species : null);
        }

        /// <summary>Shows a species' tiny copy without changing the creature (the in-pod evolution show).</summary>
        public void ShowSpecies(IslingSpecies species) => BuildMini(species);

        void BuildMini(IslingSpecies species)
        {
            if (species == _shownSpecies && (_mini != null || species == null)) return;
            _shownSpecies = species;
            if (_mini != null) Destroy(_mini);
            _mini = null;
            _miniView = null;
            _miniFainted = false;
            var prefab = species != null ? species.Prefab : null;
            if (prefab == null || miniAnchor == null) return;
            _mini = Instantiate(prefab, miniAnchor, false);
            _mini.name = $"Mini {species.displayName}";
            foreach (var r in _mini.GetComponentsInChildren<Renderer>())
            {
                // No outline: too thin to see at this size (the plain material has the outline pass off).
                if (miniMaterial != null && r.sharedMaterial != null && r.sharedMaterial.shader == miniMaterial.shader) r.sharedMaterial = miniMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            foreach (var c in _mini.GetComponentsInChildren<Collider>()) Destroy(c);
            _miniView = _mini.GetComponent<CreatureView>();
            float height = _miniView != null && _miniView.height > 1e-4f ? _miniView.height : 0.05f;
            float centre = _miniView != null ? _miniView.centerHeight : height * 0.5f;
            float s = miniHeight / height;
            _mini.transform.localScale = Vector3.one * s;
            _mini.transform.localPosition = Vector3.down * (centre * s);
            if (_miniView != null) _miniView.lookAtPlayer = false;
        }

        /// <summary>Unparents the brass band (to the pod's parent) so it can fall on its own; the pod stops tinting it.</summary>
        public Transform DetachBand()
        {
            if (band == null) return null;
            var t = band.transform;
            t.SetParent(transform.parent, true);
            band = null;
            return t;
        }

        void Update()
        {
            bool fainted = Isling != null && Isling.IsFainted;
            _glow = Mathf.MoveTowards(_glow, fainted ? 0f : Glow, Time.deltaTime * 3f);
            _hover = Mathf.MoveTowards(_hover, Hover, Time.deltaTime * 6f);
            _gold = Mathf.MoveTowards(_gold, fainted ? 0f : Actionable, Time.deltaTime * 4f);
            transform.localScale = BaseScale * (1f + 0.08f * _hover + 0.08f * Pulse);

            if (_mini != null)
            {
                _mini.SetActive(!IslingOut);
                if (_miniView != null && fainted != _miniFainted)
                {
                    _miniFainted = fainted;
                    if (fainted) _miniView.Faint();
                    else _miniView.Revive();
                }
                if (!fainted)
                {
                    _spin = (_spin + spinSpeed * Time.deltaTime) % 360f;
                    miniAnchor.localRotation = Quaternion.Euler(0f, _spin, 0f);
                }
            }

            if (glass != null)
            {
                bool full = Isling != null && Isling.species != null && !IslingOut;
                var tint = full ? Color.Lerp(emptyTint, ElementStyle.Of(Isling.Element).main, 0.45f) : emptyTint;
                glass.GetPropertyBlock(_glassBlock);
                _glassBlock.SetColor(TintId, tint);
                _glassBlock.SetFloat(GlowId, _glow + 0.4f * _hover + 0.8f * Pulse);
                _glassBlock.SetFloat(GreyId, fainted ? 1f : 0f);
                _glassBlock.SetFloat(CrackId, fainted ? 1f : 0f);
                _glassBlock.SetFloat(ShatterId, Shatter);
                _glassBlock.SetFloat(GoldId, _gold);
                _glassBlock.SetFloat(WhiteId, _hover);
                glass.SetPropertyBlock(_glassBlock);
            }
            if (band != null)
            {
                band.GetPropertyBlock(_bandBlock);
                _bandBlock.SetColor(BaseColorId, fainted ? dimBrass : Color.Lerp(brass, brightBrass, Grabbable));
                band.SetPropertyBlock(_bandBlock);
            }
        }
    }
}
