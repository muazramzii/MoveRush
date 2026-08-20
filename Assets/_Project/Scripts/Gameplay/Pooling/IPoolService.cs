using UnityEngine;

namespace MoveRush.Gameplay.Pooling
{
    /// <summary>
    /// Scene-scoped access to every pool. Systems request instances by prefab and never call
    /// Instantiate or Destroy themselves, which is what keeps a run allocation free.
    /// </summary>
    public interface IPoolService
    {
        /// <summary>Creates instances up front for a prefab.</summary>
        /// <typeparam name="T">Component type on the prefab root.</typeparam>
        /// <param name="prefab">Prefab to prewarm.</param>
        /// <param name="count">Number of instances to create.</param>
        void Prewarm<T>(T prefab, int count) where T : Component;

        /// <summary>Takes an instance of a prefab from its pool.</summary>
        /// <typeparam name="T">Component type on the prefab root.</typeparam>
        /// <param name="prefab">Prefab to clone from.</param>
        /// <returns>A ready instance, or null when the pool is exhausted.</returns>
        T Rent<T>(T prefab) where T : Component;

        /// <summary>Returns an instance to the pool that produced it.</summary>
        /// <param name="instance">Instance to return.</param>
        void Return(Component instance);

        /// <summary>Returns every live instance across all pools. Used when a run resets.</summary>
        void ReturnAll();
    }
}
