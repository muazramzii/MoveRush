using MoveRush.Gameplay.Pooling;
using UnityEngine;

namespace MoveRush.Tests
{
    /// <summary>
    /// Test double that records the pool callbacks it receives, so a test can assert that a
    /// recycled instance really was reset rather than merely reactivated.
    /// </summary>
    public class PoolableProbe : MonoBehaviour, IPoolable
    {
        /// <summary>Number of times the instance was handed out.</summary>
        public int SpawnCount { get; private set; }

        /// <summary>Number of times the instance was returned.</summary>
        public int ReturnCount { get; private set; }

        /// <inheritdoc />
        public void OnSpawnedFromPool() => SpawnCount++;

        /// <inheritdoc />
        public void OnReturnedToPool() => ReturnCount++;
    }
}
