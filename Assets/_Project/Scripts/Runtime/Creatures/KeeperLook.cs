using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// A Keeper's two looks on one baked mesh (step 9c, Hearthstag): the baked palette is the calmed one (warm chestnut
    /// fur, bright gold crown); <see cref="calm"/> = 0 paints it frightened (ash-grey fur, crystals flickering dull red).
    /// Each crown crystal (parts named <c>CrownCrystal</c>) has its own brightness (<see cref="SetCrystal"/>): cracked
    /// ones go dark, a broken shield dims them all, phase 2 burns them brighter (<see cref="crownBoost"/>).
    /// Repaints the CreatureView's pieces every frame (one creature, ~12 pieces).
    /// </summary>
    [RequireComponent(typeof(CreatureView))]
    public class KeeperLook : MonoBehaviour
    {
        [Range(0f, 1f)] public float calm;
        [Tooltip("Whole crown brightness (phase 2 > 1).")]
        public float crownBoost = 1f;
        [Tooltip("Step 10e: the frightened body and belly colours (Hearthstag: ash; Galeowl: storm grey).")]
        public Color frightenedFur = new Color(0.55f, 0.52f, 0.5f);
        public Color frightenedBelly = new Color(0.8f, 0.78f, 0.74f);
        static readonly Color DullCrystal = new Color(0.55f, 0.2f, 0.14f);

        CreatureView _view;
        PieceStyle[] _calmStyles;
        readonly List<int> _crystals = new List<int>();
        float[] _crystalTarget, _crystalNow;

        public int CrystalCount => _crystals.Count;

        void Awake() => Init();

        void Init()
        {
            if (_view != null) return;
            _view = GetComponent<CreatureView>();
            _calmStyles = new PieceStyle[_view.pieces.Count];
            for (int i = 0; i < _view.pieces.Count; i++)
            {
                _calmStyles[i] = _view.pieces[i].style;
                var r = _view.pieces[i].renderer;
                if (r != null && r.name.Contains("CrownCrystal")) _crystals.Add(i);
            }
            _crystalTarget = new float[_crystals.Count];
            _crystalNow = new float[_crystals.Count];
            for (int i = 0; i < _crystals.Count; i++) _crystalTarget[i] = _crystalNow[i] = 1f;
        }

        /// <summary>Crystal <paramref name="index"/> brightness (0 = dark / cracked, 1 = lit). Eases there.</summary>
        public void SetCrystal(int index, float brightness)
        {
            Init();
            if (index >= 0 && index < _crystalTarget.Length) _crystalTarget[index] = brightness;
        }

        public void SetAllCrystals(float brightness)
        {
            Init();
            for (int i = 0; i < _crystalTarget.Length; i++) _crystalTarget[i] = brightness;
        }

        /// <summary>Snaps the crystals to their targets (no easing).</summary>
        public void Snap()
        {
            Init();
            for (int i = 0; i < _crystalNow.Length; i++) _crystalNow[i] = _crystalTarget[i];
        }

        void LateUpdate() => Paint();

        /// <summary>Repaints now (LateUpdate does it every frame; edit-mode shots call it).</summary>
        public void Paint()
        {
            Init();
            if (_view == null) return;
            float scared = 1f - Mathf.Clamp01(calm);
            float t = Time.time;
            for (int i = 0; i < _view.pieces.Count && i < _calmStyles.Length; i++)
            {
                var c = _calmStyles[i];
                var s = c;
                int crystal = _crystals.IndexOf(i);
                if (crystal >= 0)
                {
                    _crystalNow[crystal] = Mathf.MoveTowards(_crystalNow[crystal], _crystalTarget[crystal], Time.deltaTime * 3f);
                    float lit = _crystalNow[crystal];
                    // Frightened: dull red, flickering and dropping out now and then.
                    float flicker = Mathf.Lerp(1f, 0.45f + 0.55f * Mathf.PerlinNoise(t * 5f + crystal * 3.1f, crystal), scared);
                    Color colour = Color.Lerp(DullCrystal, c.baseColor, calm);
                    s.baseColor = s.top = Color.Lerp(colour * 0.4f, colour, lit);
                    s.tipColor = Color.Lerp(DullCrystal * 1.3f, c.tipColor, calm) * Mathf.Lerp(0.5f, 1f, lit);
                    s.glow = Mathf.Lerp(0.3f, c.glow, calm) * Mathf.Lerp(0.08f, 1f, lit) * flicker * crownBoost;
                }
                else if (i == 0)
                {
                    s.baseColor = Color.Lerp(frightenedFur, c.baseColor, calm);
                    s.top = Color.Lerp(frightenedFur, c.top, calm);
                    if (c.patchColors != null)
                    {
                        s.patchColors = (Color[])c.patchColors.Clone();
                        for (int p = 0; p < s.patchColors.Length; p++) s.patchColors[p] = Color.Lerp(frightenedBelly, c.patchColors[p], calm);
                    }
                }
                else if (_view.pieces[i].renderer != null && _view.pieces[i].renderer.name.StartsWith("Part_"))
                {
                    s.baseColor = s.top = Color.Lerp(frightenedFur, c.baseColor, calm);
                    s.tipColor = Color.Lerp(frightenedBelly, c.tipColor, calm);
                    s.bellyColor = Color.Lerp(frightenedBelly, c.bellyColor, calm);
                }
                _view.pieces[i].style = s;
            }
            _view.ApplyStyles();
        }

        /// <summary>World centre of the crown (for sparks and the shield shell), or the creature's position.</summary>
        public Vector3 CrownWorld
        {
            get
            {
                Init();
                if (_crystals.Count == 0) return transform.position;
                Vector3 sum = Vector3.zero;
                foreach (int i in _crystals) sum += _view.pieces[i].renderer.bounds.center;
                return sum / _crystals.Count;
            }
        }

        /// <summary>World position of crystal <paramref name="index"/> (crack sparks).</summary>
        public Vector3 CrystalWorld(int index)
        {
            Init();
            return index >= 0 && index < _crystals.Count ? _view.pieces[_crystals[index]].renderer.bounds.center : CrownWorld;
        }
    }
}
