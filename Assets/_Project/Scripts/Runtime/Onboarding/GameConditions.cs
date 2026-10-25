using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Named yes/no states any system can publish (battle, swap, canWalk, ...), read by name from data: dialogue lines
    /// (<see cref="DialogueTrigger.validWhile"/>) and lessons (<see cref="LessonData"/>). An expression is names joined by
    /// <c>|</c> (or) and <c>&amp;</c> (and, binds tighter), each with an optional <c>!</c>: <c>battle &amp; !swap | free</c>.
    /// Empty = true. An unknown name reads false and logs one error (a typo in the data, not a silent pass).
    /// </summary>
    public static class GameConditions
    {
        static readonly Dictionary<string, Func<bool>> _states = new Dictionary<string, Func<bool>>();
        static readonly HashSet<string> _warned = new HashSet<string>();

        /// <summary>Publishes a state by name. Re-registering replaces it.</summary>
        public static void Register(string name, Func<bool> state) => _states[name] = state;

        public static void Unregister(string name) => _states.Remove(name);

        public static bool IsKnown(string name) => _states.ContainsKey(name);

        /// <summary>Evaluates an expression; <paramref name="extra"/> answers names first (lesson events), null = not mine.</summary>
        public static bool Check(string expression, Func<string, bool?> extra = null)
        {
            if (string.IsNullOrWhiteSpace(expression)) return true;
            foreach (var any in expression.Split('|'))
            {
                bool all = true;
                foreach (var raw in any.Split('&'))
                {
                    var term = raw.Trim();
                    if (term.Length == 0) continue;
                    bool not = term[0] == '!';
                    if (not) term = term.Substring(1).Trim();
                    if (Value(term, extra) == not)
                    {
                        all = false;
                        break;
                    }
                }
                if (all) return true;
            }
            return false;
        }

        static bool Value(string name, Func<string, bool?> extra)
        {
            var mine = extra?.Invoke(name);
            if (mine.HasValue) return mine.Value;
            if (_states.TryGetValue(name, out var f)) return f != null && f();
            if (_warned.Add(name)) Debug.LogError($"[GameConditions] Unknown condition '{name}' (reads false). Known: {string.Join(", ", _states.Keys)}.");
            return false;
        }

        /// <summary>Clears everything (tests, domain reload).</summary>
        public static void Reset()
        {
            _states.Clear();
            _warned.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();
    }
}
