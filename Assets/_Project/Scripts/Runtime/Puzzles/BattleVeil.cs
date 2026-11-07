using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 17 D: props that live in the room outside the island (Galewick's flying and tied kites and their strings, a
    /// carried ember, the scoop's water ball) leave the sky while a battle is up: <see cref="Shown"/> eases to 0 over
    /// <see cref="Seconds"/> when the battle starts and back to 1 when it ends. The prop shader is opaque toon, so the
    /// "fade" is a quick shrink to nothing (then hidden); strings thin out the same way.
    /// </summary>
    public static class BattleVeil
    {
        public const float Seconds = 0.3f;

        static float _shown = 1f;
        static int _frame = -1;
        static bool _was;

        /// <summary>1 = shown, 0 = gone (a battle is up). Eased once per frame, whoever asks first.</summary>
        public static float Shown
        {
            get
            {
                if (_frame == Time.frameCount) return _shown;
                _frame = Time.frameCount;
                bool battle = GameConditions.Check("battle");
                if (battle != _was)
                {
                    _was = battle;
                    Debug.Log(battle ? "[BattleVeil] Battle: room props (kites, carried ember, water) fade out." : "[BattleVeil] Battle over: room props come back.");
                }
                _shown = Mathf.MoveTowards(_shown, battle ? 0f : 1f, Time.unscaledDeltaTime / Seconds);
                return _shown;
            }
        }

        /// <summary>Shown, smoothed (for a scale).</summary>
        public static float Scale => Mathf.SmoothStep(0f, 1f, Shown);

        /// <summary>Still visible at all.</summary>
        public static bool Visible => Shown > 0.001f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            _shown = 1f;
            _frame = -1;
            _was = false;
        }
    }
}
