using UnityEngine;

namespace MoveRush.Gameplay.Config
{
    /// <summary>
    /// Weights of the score formula and the profile rewards a finished run grants.
    /// The formula is data, not code, so the economy can be re-balanced from the inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "ScoreConfig", menuName = "MoveRush/Config/Score Config", order = 12)]
    public class ScoreConfig : ScriptableObject
    {
        [Header("Score Formula")]
        [Tooltip("Points per collected coin.")]
        [SerializeField, Min(0)] private int coinMultiplier = 10;

        [Tooltip("Points per meter travelled.")]
        [SerializeField, Min(0)] private int distanceMultiplier = 2;

        [Tooltip("Points per obstacle in the current clear streak.")]
        [SerializeField, Min(0)] private int comboMultiplier = 50;

        [Header("Run Rewards")]
        [Tooltip("Adds the coins collected during a run to the stored profile.")]
        [SerializeField] private bool grantCoinsToProfile = true;

        [Tooltip("Experience granted per collected coin.")]
        [SerializeField, Min(0)] private int xpPerCoin = 2;

        [Tooltip("Experience granted per meter travelled.")]
        [SerializeField, Range(0f, 1f)] private float xpPerMeter = 0.1f;

        /// <summary>Points per collected coin.</summary>
        public int CoinMultiplier => coinMultiplier;

        /// <summary>Points per meter travelled.</summary>
        public int DistanceMultiplier => distanceMultiplier;

        /// <summary>Points per obstacle in the current clear streak.</summary>
        public int ComboMultiplier => comboMultiplier;

        /// <summary>True when run coins are added to the stored profile.</summary>
        public bool GrantCoinsToProfile => grantCoinsToProfile;

        /// <summary>Experience granted per collected coin.</summary>
        public int XpPerCoin => xpPerCoin;

        /// <summary>Experience granted per meter travelled.</summary>
        public float XpPerMeter => xpPerMeter;

        /// <summary>Applies the formula.</summary>
        /// <param name="coins">Coins collected.</param>
        /// <param name="distance">Distance travelled in meters.</param>
        /// <param name="combo">Current clear streak.</param>
        /// <returns>The total score.</returns>
        public int CalculateScore(int coins, float distance, int combo)
        {
            return coins * coinMultiplier
                   + Mathf.FloorToInt(distance) * distanceMultiplier
                   + combo * comboMultiplier;
        }

        /// <summary>Experience earned by a finished run.</summary>
        /// <param name="coins">Coins collected.</param>
        /// <param name="distance">Distance travelled in meters.</param>
        /// <returns>Experience to grant.</returns>
        public int CalculateXp(int coins, float distance)
        {
            return Mathf.Max(0, coins * xpPerCoin + Mathf.FloorToInt(distance * xpPerMeter));
        }
    }
}
