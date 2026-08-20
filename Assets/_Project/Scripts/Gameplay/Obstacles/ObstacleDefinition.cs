using UnityEngine;

namespace MoveRush.Gameplay.Obstacles
{
    /// <summary>
    /// One entry in the spawn table: which obstacle, how likely it is, and how far into a run it
    /// starts appearing. Gating by tier is what stops the first hundred meters from throwing the
    /// hardest obstacle at a player who has not learned the controls yet.
    /// </summary>
    [CreateAssetMenu(fileName = "ObstacleDefinition", menuName = "MoveRush/Obstacles/Obstacle Definition", order = 30)]
    public class ObstacleDefinition : ScriptableObject
    {
        [Tooltip("Pooled obstacle prefab.")]
        [SerializeField] private ObstacleBase prefab;

        [Tooltip("Relative chance of being picked once unlocked.")]
        [SerializeField, Min(0f)] private float weight = 1f;

        [Tooltip("Difficulty tier at which this obstacle starts appearing.")]
        [SerializeField, Min(0)] private int minTier;

        /// <summary>Pooled obstacle prefab.</summary>
        public ObstacleBase Prefab => prefab;

        /// <summary>Relative chance of being picked.</summary>
        public float Weight => weight;

        /// <summary>Difficulty tier at which this obstacle unlocks.</summary>
        public int MinTier => minTier;

        /// <summary>Returns true when the obstacle is allowed at a difficulty tier.</summary>
        /// <param name="tier">Current difficulty tier.</param>
        /// <returns>True when unlocked.</returns>
        public bool IsUnlocked(int tier) => tier >= minTier && prefab != null;
    }
}
