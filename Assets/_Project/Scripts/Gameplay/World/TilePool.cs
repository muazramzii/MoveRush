using System;
using System.Collections.Generic;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using MoveRush.Gameplay.Pooling;
using UnityEngine;

namespace MoveRush.Gameplay.World
{
    /// <summary>One tile variant and how often it should be picked.</summary>
    [Serializable]
    public class WeightedTile
    {
        [Tooltip("Tile prefab.")]
        [SerializeField] private RoadTile prefab;

        [Tooltip("Relative chance of being picked. Higher is more common.")]
        [SerializeField, Min(0f)] private float weight = 1f;

        /// <summary>Tile prefab.</summary>
        public RoadTile Prefab => prefab;

        /// <summary>Relative chance of being picked.</summary>
        public float Weight => weight;
    }

    /// <summary>
    /// Weighted supplier of road tiles on top of the generic pool. Splitting it from
    /// <see cref="WorldGenerator"/> keeps the generator concerned with streaming only, and lets
    /// art add tile variants without touching generation code.
    /// </summary>
    [DisallowMultipleComponent]
    public class TilePool : MonoBehaviour
    {
        [Tooltip("Tile variants. At least one entry is required.")]
        [SerializeField] private List<WeightedTile> variants = new List<WeightedTile>();

        private IPoolService poolService;

        /// <summary>Creates instances of every variant up front.</summary>
        /// <param name="countPerVariant">Instances to create for each variant.</param>
        public void Prewarm(int countPerVariant)
        {
            if (!TryResolvePool())
            {
                return;
            }

            for (int i = 0; i < variants.Count; i++)
            {
                if (variants[i]?.Prefab != null)
                {
                    poolService.Prewarm(variants[i].Prefab, countPerVariant);
                }
            }
        }

        /// <summary>Takes a random tile from the pool, weighted by the configured chances.</summary>
        /// <returns>A pooled tile, or null when no variant is configured.</returns>
        public RoadTile Rent()
        {
            if (!TryResolvePool())
            {
                return null;
            }

            RoadTile prefab = PickWeighted();
            if (prefab == null)
            {
                Log.Error("TilePool: no tile variants are configured.", this);
                return null;
            }

            return poolService.Rent(prefab);
        }

        /// <summary>Returns a tile to its pool.</summary>
        /// <param name="tile">Tile to return.</param>
        public void Return(RoadTile tile)
        {
            if (TryResolvePool())
            {
                poolService.Return(tile);
            }
        }

        /// <summary>Picks a variant using the configured weights.</summary>
        /// <returns>The chosen prefab, or null when the list is empty.</returns>
        private RoadTile PickWeighted()
        {
            float total = 0f;
            for (int i = 0; i < variants.Count; i++)
            {
                if (variants[i]?.Prefab != null)
                {
                    total += Mathf.Max(0f, variants[i].Weight);
                }
            }

            if (total <= 0f)
            {
                return variants.Count > 0 ? variants[0]?.Prefab : null;
            }

            float roll = UnityEngine.Random.value * total;

            for (int i = 0; i < variants.Count; i++)
            {
                if (variants[i]?.Prefab == null)
                {
                    continue;
                }

                roll -= Mathf.Max(0f, variants[i].Weight);
                if (roll <= 0f)
                {
                    return variants[i].Prefab;
                }
            }

            return variants[variants.Count - 1]?.Prefab;
        }

        /// <summary>Resolves the pool service on first use.</summary>
        /// <returns>True when the service is available.</returns>
        private bool TryResolvePool()
        {
            return poolService != null || ServiceLocator.TryGet(out poolService);
        }
    }
}
