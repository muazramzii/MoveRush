using System.Collections.Generic;
using MoveRush.Core;
using MoveRush.Core.Config;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using MoveRush.Gameplay.Coins;
using MoveRush.Gameplay.Config;
using MoveRush.Gameplay.Obstacles;
using MoveRush.Gameplay.Pooling;
using MoveRush.Gameplay.Run;
using UnityEngine;

namespace MoveRush.Gameplay.World
{
    /// <summary>
    /// Streams the endless track. It keeps a fixed window of tiles alive around the character:
    /// a tile that falls behind the window is recycled and immediately reused ahead, so the world
    /// is infinite while the instance count stays constant and nothing is ever destroyed.
    /// </summary>
    [DisallowMultipleComponent]
    public class WorldGenerator : MonoBehaviour, IRunSystem
    {
        [Header("Dependencies")]
        [Tooltip("Supplier of road tiles.")]
        [SerializeField] private TilePool tilePool;

        [Tooltip("Populates a new tile with coins. Optional.")]
        [SerializeField] private CoinManager coinManager;

        [Tooltip("Populates a new tile with obstacles. Optional.")]
        [SerializeField] private ObstacleSpawner obstacleSpawner;

        [Header("Configuration")]
        [Tooltip("Streaming and density tuning.")]
        [SerializeField] private RunnerConfig runnerConfig;

        private readonly Queue<RoadTile> activeTiles = new Queue<RoadTile>(16);
        private IConfigProvider configProvider;
        private IPlayerService player;
        private IGameStateService gameState;
        private IPoolService poolService;
        private float nextTileStartZ;
        private float runStartZ;

        /// <inheritdoc />
        public int RunOrder => 0;

        /// <summary>Resolves services and prewarms the tile pool before the first run.</summary>
        private void Start()
        {
            ServiceLocator.TryGet(out configProvider);
            ServiceLocator.TryGet(out player);
            ServiceLocator.TryGet(out gameState);
            ServiceLocator.TryGet(out poolService);

            if (runnerConfig == null || tilePool == null)
            {
                Log.Error("WorldGenerator: assign the runner config and the tile pool.", this);
                enabled = false;
                return;
            }

            tilePool.Prewarm(runnerConfig.TilePrewarmCount);
        }

        /// <summary>Keeps the tile window centred on the character.</summary>
        private void Update()
        {
            if (gameState == null || gameState.CurrentState != GameState.Gameplay)
            {
                return;
            }

            StreamTiles();
        }

        /// <inheritdoc />
        public void OnRunReset()
        {
            RecycleAll();

            // A retry continues from wherever the previous run ended, so the track is rebuilt
            // around the character instead of at the world origin. Snapping to the tile grid keeps
            // the seams aligned no matter where that is.
            runStartZ = FocusZ;
            nextTileStartZ = Mathf.Floor((runStartZ - TileLength * runnerConfig.TilesBehind) / TileLength) * TileLength;

            int initialTiles = runnerConfig.TilesAhead + runnerConfig.TilesBehind;
            for (int i = 0; i < initialTiles; i++)
            {
                SpawnNextTile();
            }
        }

        /// <inheritdoc />
        public void OnRunEnded()
        {
        }

        /// <summary>Length of one tile, taken from the shared track configuration.</summary>
        private float TileLength => configProvider?.Track != null ? configProvider.Track.TileLength : 20f;

        /// <summary>World Z position the streaming window is centred on.</summary>
        private float FocusZ => player?.Position.z ?? 0f;

        /// <summary>Spawns tiles ahead and recycles the ones that fell behind.</summary>
        private void StreamTiles()
        {
            float spawnHorizon = FocusZ + TileLength * runnerConfig.TilesAhead;
            while (nextTileStartZ < spawnHorizon)
            {
                if (!SpawnNextTile())
                {
                    break;
                }
            }

            float recycleHorizon = FocusZ - TileLength * runnerConfig.TilesBehind;
            while (activeTiles.Count > 0 && activeTiles.Peek().EndZ < recycleHorizon)
            {
                RecycleOldest();
            }
        }

        /// <summary>Places one tile at the head of the track and populates it.</summary>
        /// <returns>True when a tile was placed.</returns>
        private bool SpawnNextTile()
        {
            RoadTile tile = tilePool.Rent();
            if (tile == null)
            {
                return false;
            }

            tile.Place(new Vector3(0f, GroundHeight, nextTileStartZ), TileLength);
            activeTiles.Enqueue(tile);

            // Content is placed against the distance into the run, not the world position, so the
            // safe opening stretch works identically on the first run and on every retry.
            float tileRunDistance = nextTileStartZ - runStartZ;
            obstacleSpawner?.Populate(tile, tileRunDistance);
            coinManager?.Populate(tile, tileRunDistance);

            nextTileStartZ += TileLength;
            return true;
        }

        /// <summary>Recycles the tile furthest behind the character.</summary>
        private void RecycleOldest()
        {
            RoadTile tile = activeTiles.Dequeue();
            if (tile == null)
            {
                return;
            }

            if (poolService == null)
            {
                ServiceLocator.TryGet(out poolService);
            }

            tile.ReleaseContent(poolService);
            tilePool.Return(tile);
        }

        /// <summary>Recycles every live tile. Used when a run resets.</summary>
        private void RecycleAll()
        {
            while (activeTiles.Count > 0)
            {
                RecycleOldest();
            }
        }

        /// <summary>Ground height taken from the shared track configuration.</summary>
        private float GroundHeight => configProvider?.Track != null ? configProvider.Track.GroundHeight : 0f;
    }
}
