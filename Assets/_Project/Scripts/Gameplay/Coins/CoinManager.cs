using System.Collections.Generic;
using MoveRush.Core.Config;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using MoveRush.Gameplay.Config;
using MoveRush.Gameplay.Pooling;
using MoveRush.Gameplay.Run;
using MoveRush.Gameplay.World;
using UnityEngine;

namespace MoveRush.Gameplay.Coins
{
    /// <summary>
    /// Lays coin patterns onto freshly streamed tiles. It asks the tile which lanes are still
    /// free, so a coin can never end up buried inside a train, and every coin it places is
    /// handed to the tile which recycles it later.
    /// </summary>
    [DisallowMultipleComponent]
    public class CoinManager : MonoBehaviour, IRunSystem
    {
        [Header("Prefab")]
        [Tooltip("Pooled coin prefab.")]
        [SerializeField] private Coin coinPrefab;

        [Header("Patterns")]
        [Tooltip("Patterns picked from, weighted by each asset.")]
        [SerializeField] private List<CoinPattern> patterns = new List<CoinPattern>();

        [Header("Configuration")]
        [Tooltip("Coin tuning values.")]
        [SerializeField] private RunnerConfig runnerConfig;

        private readonly List<CoinPlacement> buffer = new List<CoinPlacement>(24);
        private IConfigProvider configProvider;
        private IPoolService poolService;
        private IPlayerService player;

        /// <inheritdoc />
        public int RunOrder => 10;

        /// <summary>Resolves services and prewarms the coin pool.</summary>
        private void Start()
        {
            ServiceLocator.TryGet(out configProvider);
            ServiceLocator.TryGet(out poolService);
            ServiceLocator.TryGet(out player);

            if (coinPrefab == null || runnerConfig == null)
            {
                Log.Error("CoinManager: assign the coin prefab and the runner config.", this);
                enabled = false;
                return;
            }

            poolService?.Prewarm(coinPrefab, runnerConfig.CoinPrewarmCount);
        }

        /// <inheritdoc />
        public void OnRunReset()
        {
        }

        /// <inheritdoc />
        public void OnRunEnded()
        {
        }

        /// <summary>Places one coin pattern on a tile, if the roll succeeds.</summary>
        /// <param name="tile">Tile being populated.</param>
        /// <param name="tileRunDistance">Distance into the run at which the tile begins.</param>
        public void Populate(RoadTile tile, float tileRunDistance)
        {
            if (tile == null || !enabled || patterns.Count == 0)
            {
                return;
            }

            if (Random.value > runnerConfig.CoinPatternChance)
            {
                return;
            }

            CoinPattern pattern = PickWeighted();
            TrackConfig track = configProvider?.Track;
            if (pattern == null || track == null)
            {
                return;
            }

            int laneCount = track.LaneCount;
            pattern.Build(buffer, laneCount, Random.Range(0, laneCount), runnerConfig.CoinSpacing);

            float patternLength = pattern.GetLength(runnerConfig.CoinSpacing);
            float margin = 2f;
            float usable = Mathf.Max(0f, tile.Length - patternLength - margin * 2f);
            float startZ = tile.StartZ + margin + Random.Range(0f, usable);

            PlacePattern(tile, track, startZ);
        }

        /// <summary>Rents and positions every coin of the prepared pattern.</summary>
        /// <param name="tile">Tile being populated.</param>
        /// <param name="track">Track geometry.</param>
        /// <param name="startZ">World Z the pattern starts at.</param>
        private void PlacePattern(RoadTile tile, TrackConfig track, float startZ)
        {
            for (int i = 0; i < buffer.Count; i++)
            {
                CoinPlacement placement = buffer[i];
                float z = startZ + placement.ForwardOffset;

                if (!tile.IsLaneFree(placement.Lane, z - 0.7f, z + 0.7f))
                {
                    continue;
                }

                Coin coin = poolService?.Rent(coinPrefab);
                if (coin == null)
                {
                    return;
                }

                coin.transform.SetParent(tile.ContentRoot, false);
                coin.transform.position = new Vector3(
                    track.GetLaneOffset(placement.Lane),
                    track.GroundHeight + runnerConfig.CoinHeight + placement.HeightOffset,
                    z);

                coin.Configure(runnerConfig, player, poolService, tile);
                tile.AddContent(coin);
            }
        }

        /// <summary>Picks a pattern using the configured weights.</summary>
        /// <returns>The chosen pattern, or null when the list is empty.</returns>
        private CoinPattern PickWeighted()
        {
            float total = 0f;
            for (int i = 0; i < patterns.Count; i++)
            {
                if (patterns[i] != null)
                {
                    total += Mathf.Max(0f, patterns[i].Weight);
                }
            }

            if (total <= 0f)
            {
                return patterns.Count > 0 ? patterns[0] : null;
            }

            float roll = Random.value * total;

            for (int i = 0; i < patterns.Count; i++)
            {
                if (patterns[i] == null)
                {
                    continue;
                }

                roll -= Mathf.Max(0f, patterns[i].Weight);
                if (roll <= 0f)
                {
                    return patterns[i];
                }
            }

            return patterns[patterns.Count - 1];
        }
    }
}
