using UnityEngine;

namespace MoveRush.Core.Config
{
    /// <summary>
    /// Player profile defaults and the progression curve. Keeping the curve here means the
    /// economy can be re-balanced by editing an asset, and later by a remote config download,
    /// without touching the progression code that reads it.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "MoveRush/Config/Player Config", order = 2)]
    public class PlayerConfig : ScriptableObject
    {
        [Header("New Profile")]
        [Tooltip("Username assigned before the player picks one.")]
        [SerializeField] private string defaultUsername = "Runner";

        [Tooltip("Level a brand new profile starts at.")]
        [SerializeField, Min(1)] private int startingLevel = 1;

        [Tooltip("Coins granted to a brand new profile.")]
        [SerializeField, Min(0)] private int startingCoins;

        [Header("Progression")]
        [Tooltip("Highest level the player can reach.")]
        [SerializeField, Min(1)] private int maxLevel = 100;

        [Tooltip("Experience required to advance from level one to level two.")]
        [SerializeField, Min(1)] private int baseXpPerLevel = 100;

        [Tooltip("Multiplier applied to the requirement of each subsequent level.")]
        [SerializeField, Range(1f, 3f)] private float xpGrowthRate = 1.15f;

        /// <summary>Username assigned to a new profile.</summary>
        public string DefaultUsername => defaultUsername;

        /// <summary>Level a new profile starts at.</summary>
        public int StartingLevel => startingLevel;

        /// <summary>Coins granted to a new profile.</summary>
        public int StartingCoins => startingCoins;

        /// <summary>Highest reachable level.</summary>
        public int MaxLevel => maxLevel;

        /// <summary>
        /// Experience needed to advance from <paramref name="level"/> to the next level, using a
        /// geometric curve: base * growth^(level - 1).
        /// </summary>
        /// <param name="level">Level the player is currently on.</param>
        /// <returns>Required experience, or zero when the level cap has been reached.</returns>
        public int GetXpRequiredForLevel(int level)
        {
            if (level >= maxLevel)
            {
                return 0;
            }

            int normalized = Mathf.Max(1, level);
            float required = baseXpPerLevel * Mathf.Pow(xpGrowthRate, normalized - 1);
            return Mathf.Max(1, Mathf.RoundToInt(required));
        }

        /// <summary>Keeps designer-entered values consistent.</summary>
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(defaultUsername))
            {
                defaultUsername = "Runner";
            }

            startingLevel = Mathf.Clamp(startingLevel, 1, maxLevel);
        }
    }
}
