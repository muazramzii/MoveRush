using System.Collections.Generic;
using MoveRush.Core.Config;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using MoveRush.Gameplay.Config;
using MoveRush.Gameplay.Pooling;
using MoveRush.Gameplay.Run;
using MoveRush.Gameplay.World;
using UnityEngine;

namespace MoveRush.Gameplay.Obstacles
{
    /// <summary>
    /// Fills streamed tiles with obstacles. Three rules keep the run hard but fair: a row never
    /// blocks every lane, rows are never closer together than the configured gap, and the opening
    /// meters of a run stay empty. Density and the unlocked obstacle set follow the difficulty
    /// tier, so pressure rises with the speed rather than at random.
    /// </summary>
    [DisallowMultipleComponent]
    public class ObstacleSpawner : MonoBehaviour, IRunSystem
    {
        [Header("Spawn Table")]
        [Tooltip("Obstacles that can be picked, each with its own weight and unlock tier.")]
        [SerializeField] private List<ObstacleDefinition> definitions = new List<ObstacleDefinition>();

        [Header("Configuration")]
        [Tooltip("Density and fairness tuning.")]
        [SerializeField] private RunnerConfig runnerConfig;

        private readonly List<int> laneBag = new List<int>(4);
        private readonly List<ObstacleDefinition> unlocked = new List<ObstacleDefinition>(8);
        private IConfigProvider configProvider;
        private IPoolService poolService;
        private IDifficultyService difficulty;

        /// <inheritdoc />
        public int RunOrder => 5;

        /// <summary>Resolves services and prewarms one pool per obstacle prefab.</summary>
        private void Start()
        {
            ServiceLocator.TryGet(out configProvider);
            ServiceLocator.TryGet(out poolService);
            ServiceLocator.TryGet(out difficulty);

            if (runnerConfig == null)
            {
                Log.Error("ObstacleSpawner: assign the runner config.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i]?.Prefab != null)
                {
                    poolService?.Prewarm(definitions[i].Prefab, runnerConfig.ObstaclePrewarmCount);
                }
            }
        }

        /// <inheritdoc />
        public void OnRunReset()
        {
        }

        /// <inheritdoc />
        public void OnRunEnded()
        {
        }

        /// <summary>Places obstacle rows on a freshly streamed tile.</summary>
        /// <param name="tile">Tile being populated.</param>
        /// <param name="tileRunDistance">Distance into the run at which the tile begins.</param>
        public void Populate(RoadTile tile, float tileRunDistance)
        {
            if (tile == null || !enabled || definitions.Count == 0 || configProvider?.Track == null)
            {
                return;
            }

            if (tileRunDistance + tile.Length <= runnerConfig.SafeStartDistance)
            {
                return;
            }

            const float margin = 2f;
            float usable = Mathf.Max(0f, tile.Length - margin * 2f);
            int rowCapacity = Mathf.FloorToInt(usable / runnerConfig.MinObstacleGap);
            int rows = Mathf.Clamp(GetRowCount(), 0, Mathf.Max(0, rowCapacity));

            if (rows <= 0)
            {
                return;
            }

            float step = usable / rows;

            for (int row = 0; row < rows; row++)
            {
                float offset = margin + step * row + Random.Range(0f, step * 0.35f);
                if (tileRunDistance + offset >= runnerConfig.SafeStartDistance)
                {
                    SpawnRow(tile, tile.StartZ + offset);
                }
            }
        }

        /// <summary>Scales the number of rows with the current difficulty tier.</summary>
        /// <returns>Number of rows to place on one tile.</returns>
        private int GetRowCount()
        {
            float intensity = difficulty?.NormalizedIntensity ?? 0f;
            float rows = Mathf.Lerp(runnerConfig.MinRowsPerTile, runnerConfig.MaxRowsPerTile, intensity);
            return Mathf.RoundToInt(rows);
        }

        /// <summary>Blocks a random subset of lanes at one position, always leaving a way through.</summary>
        /// <param name="tile">Tile being populated.</param>
        /// <param name="z">World Z of the row.</param>
        private void SpawnRow(RoadTile tile, float z)
        {
            TrackConfig track = configProvider.Track;
            int laneCount = track.LaneCount;
            if (laneCount <= 0)
            {
                return;
            }

            int maxBlocked = Mathf.Max(1, laneCount - runnerConfig.MinFreeLanes);
            int blocked = Random.Range(1, maxBlocked + 1);

            ShuffleLanes(laneCount);
            RefreshUnlocked();

            if (unlocked.Count == 0)
            {
                return;
            }

            for (int i = 0; i < blocked && i < laneBag.Count; i++)
            {
                SpawnOne(tile, track, laneBag[i], z);
            }
        }

        /// <summary>Rents one obstacle, places it and reserves the road it covers.</summary>
        /// <param name="tile">Tile being populated.</param>
        /// <param name="track">Track geometry.</param>
        /// <param name="lane">Lane to block.</param>
        /// <param name="z">World Z of the row.</param>
        private void SpawnOne(RoadTile tile, TrackConfig track, int lane, float z)
        {
            ObstacleDefinition definition = PickWeighted();
            if (definition?.Prefab == null)
            {
                return;
            }

            ObstacleBase obstacle = poolService?.Rent(definition.Prefab);
            if (obstacle == null)
            {
                return;
            }

            obstacle.transform.SetParent(tile.ContentRoot, false);
            obstacle.transform.position = new Vector3(track.GetLaneOffset(lane), track.GroundHeight, z);
            obstacle.transform.rotation = Quaternion.identity;

            float half = obstacle.LengthMeters * 0.5f;
            tile.AddContent(obstacle);
            tile.ReserveLane(lane, z - half - 1.5f, z + half + 1.5f);
        }

        /// <summary>Rebuilds the list of obstacles unlocked at the current tier.</summary>
        private void RefreshUnlocked()
        {
            int tier = difficulty?.Tier ?? 0;
            unlocked.Clear();

            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null && definitions[i].IsUnlocked(tier))
                {
                    unlocked.Add(definitions[i]);
                }
            }
        }

        /// <summary>Picks an unlocked obstacle using the configured weights.</summary>
        /// <returns>The chosen definition, or null when none is unlocked.</returns>
        private ObstacleDefinition PickWeighted()
        {
            float total = 0f;
            for (int i = 0; i < unlocked.Count; i++)
            {
                total += Mathf.Max(0f, unlocked[i].Weight);
            }

            if (total <= 0f)
            {
                return unlocked.Count > 0 ? unlocked[0] : null;
            }

            float roll = Random.value * total;

            for (int i = 0; i < unlocked.Count; i++)
            {
                roll -= Mathf.Max(0f, unlocked[i].Weight);
                if (roll <= 0f)
                {
                    return unlocked[i];
                }
            }

            return unlocked[unlocked.Count - 1];
        }

        /// <summary>Shuffles the lane order so the blocked lanes differ from row to row.</summary>
        /// <param name="laneCount">Number of lanes on the track.</param>
        private void ShuffleLanes(int laneCount)
        {
            laneBag.Clear();
            for (int i = 0; i < laneCount; i++)
            {
                laneBag.Add(i);
            }

            for (int i = laneBag.Count - 1; i > 0; i--)
            {
                int swap = Random.Range(0, i + 1);
                (laneBag[i], laneBag[swap]) = (laneBag[swap], laneBag[i]);
            }
        }
    }
}
