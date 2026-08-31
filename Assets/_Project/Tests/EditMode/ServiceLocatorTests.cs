using System;
using MoveRush.Core.Services;
using NUnit.Framework;

namespace MoveRush.Tests
{
    /// <summary>
    /// Covers the composition root every system resolves its dependencies through.
    /// The locator is static, so each test clears it on both sides to stay independent of the
    /// order the runner happens to pick.
    /// </summary>
    public class ServiceLocatorTests
    {
        /// <summary>Contract used only by these tests.</summary>
        private interface IProbeService
        {
            /// <summary>Identifies which instance was resolved.</summary>
            string Id { get; }
        }

        /// <summary>Implementation used only by these tests.</summary>
        private class ProbeService : IProbeService
        {
            /// <summary>Creates a probe with an identifier.</summary>
            /// <param name="id">Identifier to report.</param>
            public ProbeService(string id) => Id = id;

            /// <inheritdoc />
            public string Id { get; }
        }

        /// <summary>
        /// Removes only this fixture's own registration. Clearing the whole locator would give
        /// the same isolation but would also destroy any unrelated state the editor session holds.
        /// </summary>
        [SetUp]
        public void SetUp() => ServiceLocator.Unregister<IProbeService>();

        /// <summary>Leaves nothing of this fixture behind for whatever runs next.</summary>
        [TearDown]
        public void TearDown() => ServiceLocator.Unregister<IProbeService>();

        [Test]
        public void Get_ReturnsTheRegisteredInstance()
        {
            ProbeService instance = new ProbeService("first");
            ServiceLocator.Register<IProbeService>(instance);

            Assert.AreSame(instance, ServiceLocator.Get<IProbeService>());
        }

        [Test]
        public void Get_ThrowsWhenNothingIsRegistered()
        {
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<IProbeService>());
        }

        [Test]
        public void TryGet_ReportsWhetherTheServiceExists()
        {
            Assert.IsFalse(ServiceLocator.TryGet(out IProbeService missing));
            Assert.IsNull(missing);

            ServiceLocator.Register<IProbeService>(new ProbeService("first"));

            Assert.IsTrue(ServiceLocator.TryGet(out IProbeService found));
            Assert.AreEqual("first", found.Id);
        }

        [Test]
        public void Register_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => ServiceLocator.Register<IProbeService>(null));
        }

        /// <summary>
        /// A scene reload registers a replacement before the old instance is collected, so the
        /// newest registration has to win rather than the first one sticking.
        /// </summary>
        [Test]
        public void Register_ReplacesAnExistingRegistration()
        {
            ServiceLocator.Register<IProbeService>(new ProbeService("first"));
            ServiceLocator.Register<IProbeService>(new ProbeService("second"));

            Assert.AreEqual("second", ServiceLocator.Get<IProbeService>().Id);
        }

        [Test]
        public void Unregister_RemovesTheService()
        {
            ServiceLocator.Register<IProbeService>(new ProbeService("first"));

            ServiceLocator.Unregister<IProbeService>();

            Assert.IsFalse(ServiceLocator.IsRegistered<IProbeService>());
        }

        [Test]
        public void Unregister_IsSafeWhenNothingIsRegistered()
        {
            Assert.DoesNotThrow(ServiceLocator.Unregister<IProbeService>);
        }

        [Test]
        public void Clear_RemovesEverything()
        {
            ServiceLocator.Register<IProbeService>(new ProbeService("first"));

            ServiceLocator.Clear();

            Assert.IsFalse(ServiceLocator.IsRegistered<IProbeService>());
        }
    }
}
