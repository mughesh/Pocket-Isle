using UnityEngine;

namespace Diorama
{
    public enum MoveKind { Attack, Support }

    /// <summary>How a move looks. Four templates, recoloured by element.</summary>
    public enum MoveAnimation { Shoot, Lunge, Slam, Shield }

    public enum SupportEffect
    {
        None,
        [Tooltip("Halves the next hit taken.")]
        Shield,
        [Tooltip("Raises attack by one stage.")]
        RaiseAttack,
        [Tooltip("Heals a share of max HP.")]
        Heal,
    }

    /// <summary>One battle move. Every species has three: an element attack, a Neutral attack and a support.</summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Battle/Move", fileName = "Move")]
    public class MoveData : ScriptableObject
    {
        public string displayName;
        public Element element;
        public MoveKind kind;
        [Tooltip("Attack strength (about 35-55).")]
        [Min(0)] public int power = 40;
        [Tooltip("Chance to hit, percent. Support moves always work.")]
        [Range(1, 100)] public int accuracy = 100;
        public MoveAnimation animation;
        [Tooltip("Shoot only: the projectile arcs (water).")]
        public bool arc;

        [Header("Support")]
        public SupportEffect effect;
        [Tooltip("Heal: share of max HP restored.")]
        [Range(0f, 1f)] public float healShare = 0.3f;

        public string Name => string.IsNullOrEmpty(displayName) ? name : displayName;
    }
}
