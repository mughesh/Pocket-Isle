using UnityEngine;

namespace Diorama
{
    public enum Effectiveness { Normal, Super, NotVery }

    /// <summary>
    /// Element matchups: Water beats Fire, Fire beats Wind, Wind beats Water. Neutral has no strengths or weaknesses.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Battle/Type Chart", fileName = "TypeChart")]
    public class TypeChart : ScriptableObject
    {
        [Tooltip("Damage multiplier when the move's element beats the defender's.")]
        public float strong = 1.5f;
        [Tooltip("Damage multiplier when the defender's element beats the move's.")]
        public float weak = 0.67f;
        [Tooltip("A move against a creature of its own element counts as weak (Fire on Fire). Neutral never resists.")]
        public bool sameElementResists = true;

        /// <summary>True when <paramref name="attack"/> beats <paramref name="defend"/>.</summary>
        public static bool Beats(Element attack, Element defend) =>
            attack == Element.Water && defend == Element.Fire ||
            attack == Element.Fire && defend == Element.Wind ||
            attack == Element.Wind && defend == Element.Water;

        public Effectiveness Matchup(Element move, Element defender)
        {
            if (move == Element.Neutral || defender == Element.Neutral) return Effectiveness.Normal;
            if (Beats(move, defender)) return Effectiveness.Super;
            if (Beats(defender, move) || sameElementResists && move == defender) return Effectiveness.NotVery;
            return Effectiveness.Normal;
        }

        public float Multiplier(Effectiveness e) => e == Effectiveness.Super ? strong : e == Effectiveness.NotVery ? weak : 1f;

        public float Multiplier(Element move, Element defender) => Multiplier(Matchup(move, defender));
    }
}
