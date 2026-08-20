using MoveRush.Core.Config;
using MoveRush.Gameplay.Coins;
using MoveRush.Gameplay.Config;
using MoveRush.Gameplay.Obstacles;
using MoveRush.Gameplay.Presentation;
using MoveRush.Gameplay.World;
using MoveRush.Player;
using MoveRush.Player.Config;

namespace MoveRush.EditorTools
{
    /// <summary>
    /// Everything the Phase 2 generator produces, passed to the scene builder in one object so
    /// the build steps stay readable instead of taking a dozen parameters each.
    /// </summary>
    public class RunnerAssetSet
    {
        /// <summary>Lane positions and tile length.</summary>
        public TrackConfig Track { get; set; }

        /// <summary>World streaming and coin tuning.</summary>
        public RunnerConfig Runner { get; set; }

        /// <summary>The difficulty ramp.</summary>
        public DifficultyConfig Difficulty { get; set; }

        /// <summary>Score formula and run rewards.</summary>
        public ScoreConfig Score { get; set; }

        /// <summary>Character movement feel.</summary>
        public PlayerMovementConfig Movement { get; set; }

        /// <summary>Road tile prefab.</summary>
        public RoadTile TilePrefab { get; set; }

        /// <summary>Coin prefab.</summary>
        public Coin CoinPrefab { get; set; }

        /// <summary>Coin patterns available to the spawner.</summary>
        public CoinPattern[] CoinPatterns { get; set; }

        /// <summary>Obstacle spawn table.</summary>
        public ObstacleDefinition[] ObstacleDefinitions { get; set; }

        /// <summary>Burst played where a coin is collected.</summary>
        public PooledEffect CoinEffect { get; set; }

        /// <summary>Burst played where the character is hit.</summary>
        public PooledEffect HitEffect { get; set; }

        /// <summary>Burst played where the character lands.</summary>
        public PooledEffect LandEffect { get; set; }

        /// <summary>Character prefab.</summary>
        public PlayerController PlayerPrefab { get; set; }
    }
}
