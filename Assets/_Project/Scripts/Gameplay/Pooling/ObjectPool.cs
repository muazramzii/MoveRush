using System.Collections.Generic;
using MoveRush.Core.Utilities;
using UnityEngine;

namespace MoveRush.Gameplay.Pooling
{
    /// <summary>
    /// Generic reuse pool for prefab instances. Instances are created during prewarm and then
    /// activated and deactivated for the rest of the session, so a run never pays for an
    /// Instantiate or a Destroy and never triggers a garbage collection spike mid-jump.
    /// </summary>
    /// <typeparam name="T">Component type on the pooled prefab root.</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Stack<T> available;
        private readonly List<T> active;
        private readonly int maxSize;
        private readonly bool allowGrowth;

        /// <summary>Creates a pool and immediately prewarms it.</summary>
        /// <param name="prefab">Prefab to clone.</param>
        /// <param name="parent">Transform the instances are parented to while idle.</param>
        /// <param name="initialSize">Number of instances created up front.</param>
        /// <param name="maxSize">Hard ceiling on the number of live instances.</param>
        /// <param name="allowGrowth">
        /// When true an exhausted pool creates one more instance and logs a warning instead of
        /// dropping the request. Keep it on in development to surface an undersized prewarm.
        /// </param>
        public ObjectPool(T prefab, Transform parent, int initialSize, int maxSize = 256, bool allowGrowth = true)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxSize = Mathf.Max(1, maxSize);
            this.allowGrowth = allowGrowth;

            available = new Stack<T>(Mathf.Max(1, initialSize));
            active = new List<T>(Mathf.Max(1, initialSize));

            Prewarm(initialSize);
        }

        /// <summary>Number of idle instances waiting to be reused.</summary>
        public int CountInactive => available.Count;

        /// <summary>Number of instances currently handed out.</summary>
        public int CountActive => active.Count;

        /// <summary>Creates instances up front so no allocation happens during a run.</summary>
        /// <param name="count">Number of instances to add.</param>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count && CountInactive + CountActive < maxSize; i++)
            {
                T instance = Create();
                if (instance == null)
                {
                    return;
                }

                available.Push(instance);
            }
        }

        /// <summary>Takes an instance from the pool, activating and resetting it.</summary>
        /// <returns>A ready instance, or null when the pool is exhausted and cannot grow.</returns>
        public T Rent()
        {
            T instance = null;

            while (available.Count > 0 && instance == null)
            {
                instance = available.Pop();
            }

            if (instance == null)
            {
                if (!allowGrowth || CountActive >= maxSize)
                {
                    Log.Warning($"ObjectPool<{typeof(T).Name}>: exhausted at {CountActive} instances.");
                    return null;
                }

                Log.Warning($"ObjectPool<{typeof(T).Name}>: grew past its prewarm of {CountActive}. Raise the prewarm count.");
                instance = Create();
                if (instance == null)
                {
                    return null;
                }
            }

            active.Add(instance);
            instance.gameObject.SetActive(true);

            if (instance is IPoolable poolable)
            {
                poolable.OnSpawnedFromPool();
            }

            return instance;
        }

        /// <summary>Returns an instance to the pool, deactivating it.</summary>
        /// <param name="instance">Instance previously handed out by this pool.</param>
        public void Return(T instance)
        {
            if (instance == null || !active.Remove(instance))
            {
                return;
            }

            if (instance is IPoolable poolable)
            {
                poolable.OnReturnedToPool();
            }

            instance.transform.SetParent(parent, false);
            instance.gameObject.SetActive(false);
            available.Push(instance);
        }

        /// <summary>Returns every handed out instance. Used when a run is reset.</summary>
        public void ReturnAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Return(active[i]);
            }
        }

        /// <summary>Destroys every instance. Only used when the scene is torn down.</summary>
        public void Clear()
        {
            ReturnAll();

            while (available.Count > 0)
            {
                T instance = available.Pop();
                if (instance != null)
                {
                    Object.Destroy(instance.gameObject);
                }
            }
        }

        /// <summary>Instantiates one inactive instance.</summary>
        /// <returns>The new instance, or null when the prefab is missing.</returns>
        private T Create()
        {
            if (prefab == null)
            {
                Log.Error($"ObjectPool<{typeof(T).Name}>: the prefab is missing.");
                return null;
            }

            T instance = Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}
