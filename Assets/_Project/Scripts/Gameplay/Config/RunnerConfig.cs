using UnityEngine;

namespace MoveRush.Gameplay.Config
{
    /// <summary>
    /// Tuning for world streaming, coin layout and obstacle density. Every number a designer
    /// touches while balancing the run lives here, so balancing never requires a recompile.
    /// </summary>
    [CreateAssetMenu(fileName = "RunnerConfig", menuName = "MoveRush/Config/Runner Config", order = 10)]
    public class RunnerConfig : ScriptableObject
    {
        [Header("World Streaming")]
        [Tooltip("Number of tiles kept alive ahead of the character.")]
        [SerializeField, Range(3, 16)] private int tilesAhead = 6;

        [Tooltip("Number of tiles kept alive behind the character before recycling.")]
        [SerializeField, Range(1, 4)] private int tilesBehind = 1;

        [Tooltip("Instances prewarmed per tile prefab. Must cover tilesAhead plus tilesBehind.")]
        [SerializeField, Range(4, 32)] private int tilePrewarmCount = 10;

        [Header("Coins")]
        [Tooltip("Height above the ground coins float at.")]
        [SerializeField] private float coinHeight = 1.1f;

        [Tooltip("Distance in meters between two coins of the same pattern.")]
        [SerializeField, Min(0.5f)] private float coinSpacing = 2f;

        [Tooltip("Chance that a given tile receives a coin pattern.")]
        [SerializeField, Range(0f, 1f)] private float coinPatternChance = 0.85f;

        [Tooltip("Coins prewarmed in the pool. Must cover the largest pattern times live tiles.")]
        [SerializeField, Range(16, 256)] private int coinPrewarmCount = 96;

        [Tooltip("Score value of a single coin, before the score multiplier.")]
        [SerializeField, Min(1)] private int coinValue = 1;

        [Header("Coin Collection")]
        [Tooltip("Radius the magnet pulls coins from. Zero disables magnetic collection.")]
        [SerializeField, Min(0f)] private float magnetRadius;

        [Tooltip("Speed a magnetised coin travels towards the character.")]
        [SerializeField, Min(1f)] private float magnetSpeed = 14f;

        [Tooltip("Seconds the collection animation plays before the coin is recycled.")]
        [SerializeField, Range(0.05f, 1f)] private float collectAnimationDuration = 0.22f;

        [Tooltip("Degrees per second the idle coin spins at.")]
        [SerializeField] private float coinSpinSpeed = 140f;

        [Header("Obstacles")]
        [Tooltip("Meters at the start of a run that never receive an obstacle.")]
        [SerializeField, Min(0f)] private float safeStartDistance = 45f;

        [Tooltip("Minimum gap in meters between two obstacle rows.")]
        [SerializeField, Min(2f)] private float minObstacleGap = 9f;

        [Tooltip("Obstacle rows placed on a tile at the lowest difficulty tier.")]
        [SerializeField, Range(0, 4)] private int minRowsPerTile = 1;

        [Tooltip("Obstacle rows placed on a tile at the highest difficulty tier.")]
        [SerializeField, Range(1, 6)] private int maxRowsPerTile = 3;

        [Tooltip("Lanes that must always stay clear in a row. Keeps the run fair.")]
        [SerializeField, Range(1, 2)] private int minFreeLanes = 1;

        [Tooltip("Instances prewarmed per obstacle prefab. Covers the worst case of every live tile carrying full rows.")]
        [SerializeField, Range(2, 32)] private int obstaclePrewarmCount = 14;

        [Header("Effects")]
        [Tooltip("Instances prewarmed per effect prefab.")]
        [SerializeField, Range(2, 32)] private int effectPrewarmCount = 12;

        /// <summary>Number of tiles kept alive ahead of the character.</summary>
        public int TilesAhead => tilesAhead;

        /// <summary>Number of tiles kept alive behind the character.</summary>
        public int TilesBehind => tilesBehind;

        /// <summary>Instances prewarmed per tile prefab.</summary>
        public int TilePrewarmCount => tilePrewarmCount;

        /// <summary>Height above the ground coins float at.</summary>
        public float CoinHeight => coinHeight;

        /// <summary>Distance in meters between two coins of a pattern.</summary>
        public float CoinSpacing => coinSpacing;

        /// <summary>Chance that a tile receives a coin pattern.</summary>
        public float CoinPatternChance => coinPatternChance;

        /// <summary>Coins prewarmed in the pool.</summary>
        public int CoinPrewarmCount => coinPrewarmCount;

        /// <summary>Score value of a single coin.</summary>
        public int CoinValue => coinValue;

        /// <summary>Radius the magnet pulls coins from. Zero disables it.</summary>
        public float MagnetRadius => magnetRadius;

        /// <summary>Speed a magnetised coin travels at.</summary>
        public float MagnetSpeed => magnetSpeed;

        /// <summary>Seconds the collection animation plays.</summary>
        public float CollectAnimationDuration => collectAnimationDuration;

        /// <summary>Degrees per second an idle coin spins at.</summary>
        public float CoinSpinSpeed => coinSpinSpeed;

        /// <summary>Meters at the start of a run that never receive an obstacle.</summary>
        public float SafeStartDistance => safeStartDistance;

        /// <summary>Minimum gap in meters between two obstacle rows.</summary>
        public float MinObstacleGap => minObstacleGap;

        /// <summary>Obstacle rows placed on a tile at the lowest tier.</summary>
        public int MinRowsPerTile => minRowsPerTile;

        /// <summary>Obstacle rows placed on a tile at the highest tier.</summary>
        public int MaxRowsPerTile => maxRowsPerTile;

        /// <summary>Lanes that must always stay clear in a row.</summary>
        public int MinFreeLanes => minFreeLanes;

        /// <summary>Instances prewarmed per obstacle prefab.</summary>
        public int ObstaclePrewarmCount => obstaclePrewarmCount;

        /// <summary>Instances prewarmed per effect prefab.</summary>
        public int EffectPrewarmCount => effectPrewarmCount;

        /// <summary>Keeps the row counts consistent.</summary>
        private void OnValidate()
        {
            maxRowsPerTile = Mathf.Max(minRowsPerTile, maxRowsPerTile);
        }
    }
}
