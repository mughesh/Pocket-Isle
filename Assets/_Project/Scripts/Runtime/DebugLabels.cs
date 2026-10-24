using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Shows or hides the world-space debug text (gesture log, thumb probe, placement info). Hidden by default;
    /// the explorer's Inspector bool sets the start value, and five quick left-hand thumb taps toggle it in development
    /// builds. Labels call <see cref="Track"/> once and follow from then on.
    /// </summary>
    public static class DebugLabels
    {
        static bool _visible;

        public static event Action<bool> Changed;

        public static bool Visible
        {
            get => _visible;
            set
            {
                if (_visible == value) return;
                _visible = value;
                Changed?.Invoke(value);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _visible = false;
            Changed = null;
        }

        /// <summary>Shows <paramref name="label"/> only while debug labels are visible.</summary>
        public static void Track(GameObject label)
        {
            if (label == null) return;
            label.SetActive(_visible);
            Action<bool> apply = null;
            apply = on =>
            {
                if (label == null) Changed -= apply;
                else label.SetActive(on);
            };
            Changed += apply;
        }
    }
}
