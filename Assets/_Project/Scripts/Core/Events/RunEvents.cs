using UnityEngine;

namespace MoveRush.Core.Events
{
    /// <summary>Raised for every collected coin. Published as OnCoinCollected.</summary>
    public readonly struct CoinCollectedEvent
    {
        /// <summary>World position the coin was collected at.</summary>
        public Vector3 Position { get; }

        /// <summary>Value of the coin in coin units.</summary>
        public int Value { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="position">World position the coin was collected at.</param>
        /// <param name="value">Value of the coin in coin units.</param>
        public CoinCollectedEvent(Vector3 position, int value)
        {
            Position = position;
            Value = value;
        }
    }

    /// <summary>Raised when an obstacle ends the run. Published as OnObstacleHit.</summary>
    public readonly struct ObstacleHitEvent
    {
        /// <summary>Identifier of the obstacle that was hit.</summary>
        public string ObstacleId { get; }

        /// <summary>World position of the impact.</summary>
        public Vector3 Position { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="obstacleId">Identifier of the obstacle that was hit.</param>
        /// <param name="position">World position of the impact.</param>
        public ObstacleHitEvent(string obstacleId, Vector3 position)
        {
            ObstacleId = obstacleId;
            Position = position;
        }
    }

    /// <summary>Raised when the character passes an obstacle without touching it.</summary>
    public readonly struct ObstacleClearedEvent
    {
        /// <summary>Identifier of the obstacle that was cleared.</summary>
        public string ObstacleId { get; }

        /// <summary>World position of the obstacle.</summary>
        public Vector3 Position { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="obstacleId">Identifier of the obstacle that was cleared.</param>
        /// <param name="position">World position of the obstacle.</param>
        public ObstacleClearedEvent(string obstacleId, Vector3 position)
        {
            ObstacleId = obstacleId;
            Position = position;
        }
    }

    /// <summary>Raised when the clear streak changes. Published as OnComboChanged.</summary>
    public readonly struct ComboChangedEvent
    {
        /// <summary>Current streak of cleared obstacles.</summary>
        public int Combo { get; }

        /// <summary>Best streak reached during the run.</summary>
        public int BestCombo { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="combo">Current streak of cleared obstacles.</param>
        /// <param name="bestCombo">Best streak reached during the run.</param>
        public ComboChangedEvent(int combo, int bestCombo)
        {
            Combo = combo;
            BestCombo = bestCombo;
        }
    }

    /// <summary>Raised whenever the run score is recalculated.</summary>
    public readonly struct ScoreChangedEvent
    {
        /// <summary>Total score.</summary>
        public int Score { get; }

        /// <summary>Distance travelled in meters.</summary>
        public float Distance { get; }

        /// <summary>Coins collected during the run.</summary>
        public int Coins { get; }

        /// <summary>Current clear streak.</summary>
        public int Combo { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="score">Total score.</param>
        /// <param name="distance">Distance travelled in meters.</param>
        /// <param name="coins">Coins collected during the run.</param>
        /// <param name="combo">Current clear streak.</param>
        public ScoreChangedEvent(int score, float distance, int coins, int combo)
        {
            Score = score;
            Distance = distance;
            Coins = coins;
            Combo = combo;
        }
    }

    /// <summary>Raised when the run moves into a new difficulty step.</summary>
    public readonly struct DifficultyChangedEvent
    {
        /// <summary>Zero based index of the new step.</summary>
        public int Tier { get; }

        /// <summary>Target speed of the new step in meters per second.</summary>
        public float Speed { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="tier">Zero based index of the new step.</param>
        /// <param name="speed">Target speed of the new step.</param>
        public DifficultyChangedEvent(int tier, float speed)
        {
            Tier = tier;
            Speed = speed;
        }
    }

    /// <summary>Raised once when a run finishes. Published as OnGameOver.</summary>
    public readonly struct RunEndedEvent
    {
        /// <summary>Final score.</summary>
        public int Score { get; }

        /// <summary>Distance travelled in meters.</summary>
        public float Distance { get; }

        /// <summary>Coins collected during the run.</summary>
        public int Coins { get; }

        /// <summary>Best streak reached during the run.</summary>
        public int BestCombo { get; }

        /// <summary>True when the run beat the stored best score.</summary>
        public bool IsNewHighScore { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="score">Final score.</param>
        /// <param name="distance">Distance travelled in meters.</param>
        /// <param name="coins">Coins collected during the run.</param>
        /// <param name="bestCombo">Best streak reached during the run.</param>
        /// <param name="isNewHighScore">True when the run beat the stored best score.</param>
        public RunEndedEvent(int score, float distance, int coins, int bestCombo, bool isNewHighScore)
        {
            Score = score;
            Distance = distance;
            Coins = coins;
            BestCombo = bestCombo;
            IsNewHighScore = isNewHighScore;
        }
    }
}
