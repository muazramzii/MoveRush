using System.Collections.Generic;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using UnityEngine;

namespace MoveRush.Gameplay.Pooling
{
    /// <summary>
    /// Scene-scoped registry of pools, keyed by prefab. It is deliberately not a bootstrap
    /// service: pooled instances are scene objects, so the registry has to die with the scene
    /// that owns them rather than persist and hand out destroyed references.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public class PoolService : MonoBehaviour, IPoolService
    {
        private readonly Dictionary<int, IPoolHandle> handles = new Dictionary<int, IPoolHandle>(16);
        private Transform container;

        /// <summary>Registers the service before any spawner runs.</summary>
        private void Awake()
        {
            container = new GameObject("[Pooled]").transform;
            container.SetParent(transform, false);

            ServiceLocator.Register<IPoolService>(this);
        }

        /// <inheritdoc />
        public void Prewarm<T>(T prefab, int count) where T : Component
        {
            GetOrCreate(prefab)?.Pool.Prewarm(count);
        }

        /// <inheritdoc />
        public T Rent<T>(T prefab) where T : Component
        {
            PoolHandle<T> handle = GetOrCreate(prefab);
            if (handle == null)
            {
                return null;
            }

            T instance = handle.Pool.Rent();
            if (instance == null)
            {
                return null;
            }

            if (!instance.TryGetComponent(out PooledInstance marker))
            {
                marker = instance.gameObject.AddComponent<PooledInstance>();
            }

            marker.PoolKey = prefab.GetInstanceID();
            return instance;
        }

        /// <inheritdoc />
        public void Return(Component instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!instance.TryGetComponent(out PooledInstance marker) ||
                !handles.TryGetValue(marker.PoolKey, out IPoolHandle handle))
            {
                Log.Warning($"PoolService: '{instance.name}' did not come from a pool and was left alone.", instance);
                return;
            }

            handle.ReturnComponent(instance);
        }

        /// <inheritdoc />
        public void ReturnAll()
        {
            foreach (KeyValuePair<int, IPoolHandle> pair in handles)
            {
                pair.Value.ReturnAll();
            }
        }

        /// <summary>Finds the pool for a prefab, creating it on first use.</summary>
        /// <typeparam name="T">Component type on the prefab root.</typeparam>
        /// <param name="prefab">Prefab to look up.</param>
        /// <returns>The typed pool handle, or null when the prefab is missing or mis-typed.</returns>
        private PoolHandle<T> GetOrCreate<T>(T prefab) where T : Component
        {
            if (prefab == null)
            {
                Log.Error("PoolService: a null prefab was requested.", this);
                return null;
            }

            int key = prefab.GetInstanceID();

            if (handles.TryGetValue(key, out IPoolHandle existing))
            {
                if (existing is PoolHandle<T> typed)
                {
                    return typed;
                }

                Log.Error($"PoolService: '{prefab.name}' is already pooled under a different component type.", this);
                return null;
            }

            PoolHandle<T> handle = new PoolHandle<T>(new ObjectPool<T>(prefab, container, 0));
            handles.Add(key, handle);
            return handle;
        }

        /// <summary>Destroys every pooled instance when the scene is unloaded.</summary>
        private void OnDestroy()
        {
            foreach (KeyValuePair<int, IPoolHandle> pair in handles)
            {
                pair.Value.Clear();
            }

            handles.Clear();
            ServiceLocator.Unregister<IPoolService>();
        }

        /// <summary>Type-erased view of a pool so pools of different types share one dictionary.</summary>
        private interface IPoolHandle
        {
            /// <summary>Returns one instance to its pool.</summary>
            /// <param name="instance">Instance to return.</param>
            void ReturnComponent(Component instance);

            /// <summary>Returns every live instance.</summary>
            void ReturnAll();

            /// <summary>Destroys every instance.</summary>
            void Clear();
        }

        /// <summary>Typed pool handle stored behind <see cref="IPoolHandle"/>.</summary>
        /// <typeparam name="T">Component type on the pooled prefab root.</typeparam>
        private sealed class PoolHandle<T> : IPoolHandle where T : Component
        {
            /// <summary>Creates the handle around a pool.</summary>
            /// <param name="pool">Pool being wrapped.</param>
            public PoolHandle(ObjectPool<T> pool) => Pool = pool;

            /// <summary>The wrapped pool.</summary>
            public ObjectPool<T> Pool { get; }

            /// <inheritdoc />
            public void ReturnComponent(Component instance) => Pool.Return(instance as T);

            /// <inheritdoc />
            public void ReturnAll() => Pool.ReturnAll();

            /// <inheritdoc />
            public void Clear() => Pool.Clear();
        }
    }
}
