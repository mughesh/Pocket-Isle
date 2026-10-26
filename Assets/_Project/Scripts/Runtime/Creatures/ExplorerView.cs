using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 9f: the explorer's motion in code, on the baked explorer (<see cref="CreatureView"/>, recipe "Explorer":
    /// head + tunic body, hood, two arm nubs, two leg nubs, backpack, swinging lantern). <see cref="GestureTestPiece"/>
    /// places it and feeds it the walk state each frame; this poses the parts after the view's own idle.
    /// - Walk: a bounce per tile, arm and leg nubs swing opposite, the hood bobs. Run: faster swing and a forward lean.
    /// - Idle: now and then a look to one side and back; the lantern sways (more while walking).
    /// - Hop: the view's squash-and-jump. Cheer: both arms up and a hop.
    /// </summary>
    [RequireComponent(typeof(CreatureView))]
    public class ExplorerView : MonoBehaviour
    {
        [Tooltip("Arm and leg swing while walking, degrees.")]
        public float walkSwing = 28f;
        public float runSwing = 42f;
        [Tooltip("Forward lean while running, degrees.")]
        public float runLean = 12f;
        [Tooltip("Bounce per tile, metres at zoom 1.")]
        public float bounce = 0.0018f;
        [Tooltip("Glance to the side while standing, degrees.")]
        public float glance = 35f;
        public float cheerSeconds = 1.1f;

        public CreatureView View { get; private set; }

        /// <summary>Set by the explorer every frame: walking (or running), and how far through the current tile.</summary>
        public bool Moving { get; set; }
        public bool Running { get; set; }
        public float StepPhase { get; set; }

        /// <summary>Lift above the feet this frame (the bounce), metres at zoom 1.</summary>
        public float Lift { get; private set; }

        /// <summary>Forward lean this frame (degrees) and the idle glance (degrees of yaw).</summary>
        public float Lean { get; private set; }
        public float Glance { get; private set; }

        Transform _armL, _armR, _legL, _legR, _hood, _lantern;
        Quaternion _armLBase, _armRBase, _legLBase, _legRBase, _hoodBase, _lanternBase;
        float _move, _run, _cheerStart = -10f, _nextGlance, _glanceTarget;

        void Awake()
        {
            View = GetComponent<CreatureView>();
            View.lookAtPlayer = false;
            _armL = Find("Part_ExplorerArm_L", out _armLBase);
            _armR = Find("Part_ExplorerArm_R", out _armRBase);
            _legL = Find("Part_ExplorerLeg_L", out _legLBase);
            _legR = Find("Part_ExplorerLeg_R", out _legRBase);
            _hood = Find("Part_ExplorerHood", out _hoodBase);
            _lantern = Find("Part_ExplorerLantern", out _lanternBase);
            if (_armL == null || _armR == null || _legL == null || _legR == null || _lantern == null)
                Debug.LogError("[ExplorerView] Explorer parts missing (re-run Step9fSetup.Explorer).", this);
            _nextGlance = Time.time + 3f;
        }

        Transform Find(string name, out Quaternion baseRotation)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                {
                    baseRotation = t.localRotation;
                    return t;
                }
            baseRotation = Quaternion.identity;
            return null;
        }

        public void Hop() => View.Hop();

        /// <summary>Both arms up and a hop (a puzzle solved).</summary>
        public void Cheer()
        {
            _cheerStart = Time.time;
            View.Hop();
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime, t = Time.time;
            _move = Mathf.MoveTowards(_move, Moving ? 1f : 0f, dt * 6f);
            _run = Mathf.MoveTowards(_run, Moving && Running ? 1f : 0f, dt * 4f);

            // One swing cycle per tile (a stride each way), a bounce on each half.
            float stride = Moving ? Mathf.Sin(StepPhase * Mathf.PI * 2f) : 0f;
            float swing = Mathf.Lerp(walkSwing, runSwing, _run) * _move * stride;
            Lift = bounce * _move * Mathf.Abs(Mathf.Sin(StepPhase * Mathf.PI * 2f)) * (1f + 0.4f * _run);
            Lean = runLean * _run;

            // Idle glance: every few seconds while standing, look to one side, then back.
            if (Moving) _glanceTarget = 0f;
            else if (t >= _nextGlance)
            {
                _glanceTarget = Mathf.Approximately(_glanceTarget, 0f) ? (Random.value < 0.5f ? -glance : glance) : 0f;
                _nextGlance = t + (Mathf.Approximately(_glanceTarget, 0f) ? Random.Range(3f, 6f) : Random.Range(0.8f, 1.4f));
            }
            Glance = Mathf.Lerp(Glance, _glanceTarget, 1f - Mathf.Exp(-dt * 5f));

            float cheer = Mathf.Clamp01((t - _cheerStart) / cheerSeconds);
            float up = cheer < 1f ? Mathf.Sin(Mathf.Min(1f, cheer * 3f) * Mathf.PI * 0.5f) * (cheer > 0.75f ? (1f - cheer) / 0.25f : 1f) : 0f;
            float wave = up * 12f * Mathf.Sin(t * 18f);

            Pose(_armL, _armLBase, Quaternion.Euler(Mathf.Lerp(swing, -160f + wave, up), 0f, 0f));
            Pose(_armR, _armRBase, Quaternion.Euler(Mathf.Lerp(-swing, -160f - wave, up), 0f, 0f));
            Pose(_legL, _legLBase, Quaternion.Euler(-swing, 0f, 0f));
            Pose(_legR, _legRBase, Quaternion.Euler(swing, 0f, 0f));
            float bob = 4f * _move * Mathf.Abs(Mathf.Sin(StepPhase * Mathf.PI * 2f)) + 1.5f * Mathf.Sin(t * 1.7f);
            Pose(_hood, _hoodBase, Quaternion.Euler(bob, 0f, 0f));
            float sway = (5f + 9f * _move) * Mathf.Sin(t * (2.2f + 3f * _move)) + 10f * _run;
            Pose(_lantern, _lanternBase, Quaternion.Euler(sway, 0f, 3f * Mathf.Sin(t * 1.3f)));
        }

        static void Pose(Transform part, Quaternion baseRotation, Quaternion offset)
        {
            if (part != null) part.localRotation = baseRotation * offset;
        }
    }
}
