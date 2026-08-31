using MoveRush.Gameplay.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace MoveRush.Tests
{
    /// <summary>
    /// Covers the reuse pool the whole runner depends on. The brief forbids instantiating during
    /// gameplay, so the guarantee being tested is not just that renting works but that a returned
    /// instance is the one handed out next.
    /// </summary>
    public class ObjectPoolTests
    {
        private PoolableProbe prefab;
        private Transform container;

        /// <summary>Creates the source object and the parent the pool parks instances under.</summary>
        [SetUp]
        public void SetUp()
        {
            prefab = new GameObject("PoolPrefab").AddComponent<PoolableProbe>();
            prefab.gameObject.SetActive(false);
            container = new GameObject("PoolContainer").transform;
        }

        /// <summary>Destroys everything the test created.</summary>
        [TearDown]
        public void TearDown()
        {
            if (container != null)
            {
                Object.DestroyImmediate(container.gameObject);
            }

            if (prefab != null)
            {
                Object.DestroyImmediate(prefab.gameObject);
            }
        }

        [Test]
        public void Prewarm_CreatesInstancesUpFrontAndLeavesThemInactive()
        {
            ObjectPool<PoolableProbe> pool = new ObjectPool<PoolableProbe>(prefab, container, 4);

            Assert.AreEqual(4, pool.CountInactive);
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void Rent_ActivatesTheInstanceAndSignalsIt()
        {
            ObjectPool<PoolableProbe> pool = new ObjectPool<PoolableProbe>(prefab, container, 2);

            PoolableProbe rented = pool.Rent();

            Assert.IsNotNull(rented);
            Assert.IsTrue(rented.gameObject.activeSelf);
            Assert.AreEqual(1, rented.SpawnCount);
            Assert.AreEqual(1, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);
        }

        [Test]
        public void Return_DeactivatesTheInstanceAndSignalsIt()
        {
            ObjectPool<PoolableProbe> pool = new ObjectPool<PoolableProbe>(prefab, container, 2);
            PoolableProbe rented = pool.Rent();

            pool.Return(rented);

            Assert.IsFalse(rented.gameObject.activeSelf);
            Assert.AreEqual(1, rented.ReturnCount);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(2, pool.CountInactive);
        }

        /// <summary>
        /// The core promise of pooling: a returned instance is reused rather than replaced by a
        /// fresh one. Without this the pool would quietly become an allocator.
        /// </summary>
        [Test]
        public void Rent_ReusesAReturnedInstance()
        {
            ObjectPool<PoolableProbe> pool = new ObjectPool<PoolableProbe>(prefab, container, 1);

            PoolableProbe first = pool.Rent();
            pool.Return(first);
            PoolableProbe second = pool.Rent();

            Assert.AreSame(first, second);
            Assert.AreEqual(2, second.SpawnCount, "A reused instance must be reset again.");
        }

        /// <summary>
        /// A coin returns itself when collected and its tile returns it again when recycled, so
        /// the double return is a path that runs constantly and must not corrupt the pool.
        /// </summary>
        [Test]
        public void Return_IgnoresAnInstanceThatIsAlreadyBack()
        {
            ObjectPool<PoolableProbe> pool = new ObjectPool<PoolableProbe>(prefab, container, 1);
            PoolableProbe rented = pool.Rent();

            pool.Return(rented);
            pool.Return(rented);

            Assert.AreEqual(1, pool.CountInactive, "A repeated return must not duplicate the instance.");
            Assert.AreEqual(1, rented.ReturnCount);
        }

        [Test]
        public void Return_IgnoresNull()
        {
            ObjectPool<PoolableProbe> pool = new ObjectPool<PoolableProbe>(prefab, container, 1);

            Assert.DoesNotThrow(() => pool.Return(null));
            Assert.AreEqual(1, pool.CountInactive);
        }

        /// <summary>Resetting a run hands everything back in one call.</summary>
        [Test]
        public void ReturnAll_RecallsEveryLiveInstance()
        {
            ObjectPool<PoolableProbe> pool = new ObjectPool<PoolableProbe>(prefab, container, 3);
            pool.Rent();
            pool.Rent();
            pool.Rent();

            pool.ReturnAll();

            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(3, pool.CountInactive);
        }

        /// <summary>
        /// With growth disabled an exhausted pool returns null instead of allocating, which is
        /// how a build can be made to prove it never instantiates mid-run.
        /// </summary>
        [Test]
        public void Rent_ReturnsNullWhenExhaustedAndGrowthIsDisabled()
        {
            ObjectPool<PoolableProbe> pool = new ObjectPool<PoolableProbe>(prefab, container, 1, 8, false);
            pool.Rent();

            Assert.IsNull(pool.Rent());
        }

        [Test]
        public void Rent_GrowsBeyondThePrewarmWhenAllowed()
        {
            ObjectPool<PoolableProbe> pool = new ObjectPool<PoolableProbe>(prefab, container, 1);
            pool.Rent();

            Assert.IsNotNull(pool.Rent());
            Assert.AreEqual(2, pool.CountActive);
        }

        [Test]
        public void Prewarm_StopsAtTheCeiling()
        {
            ObjectPool<PoolableProbe> pool = new ObjectPool<PoolableProbe>(prefab, container, 10, 4);

            Assert.AreEqual(4, pool.CountInactive);
        }
    }
}
