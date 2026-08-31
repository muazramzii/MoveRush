using System;
using MoveRush.Core.Events;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MoveRush.Tests
{
    /// <summary>
    /// Covers the publish and subscribe channel every decoupled system in the game talks over.
    /// The bus is static, so each test clears it on both sides to stay independent of the order
    /// the runner picks.
    /// </summary>
    public class EventBusTests
    {
        /// <summary>Payload used only by these tests.</summary>
        private readonly struct ProbeEvent
        {
            /// <summary>Value carried to the subscriber.</summary>
            public int Value { get; }

            /// <summary>Creates the payload.</summary>
            /// <param name="value">Value to carry.</param>
            public ProbeEvent(int value) => Value = value;
        }

        /// <summary>Starts every test from an empty channel.</summary>
        [SetUp]
        public void SetUp() => EventBus<ProbeEvent>.Clear();

        /// <summary>Leaves the channel empty for whatever runs next.</summary>
        [TearDown]
        public void TearDown() => EventBus<ProbeEvent>.Clear();

        [Test]
        public void Publish_DeliversThePayload()
        {
            int received = 0;
            Action<ProbeEvent> handler = payload => received = payload.Value;
            EventBus<ProbeEvent>.Subscribe(handler);

            EventBus<ProbeEvent>.Publish(new ProbeEvent(42));

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Publish_ReachesEverySubscriber()
        {
            int first = 0;
            int second = 0;
            EventBus<ProbeEvent>.Subscribe(payload => first = payload.Value);
            EventBus<ProbeEvent>.Subscribe(payload => second = payload.Value);

            EventBus<ProbeEvent>.Publish(new ProbeEvent(7));

            Assert.AreEqual(7, first);
            Assert.AreEqual(7, second);
        }

        /// <summary>
        /// A component that subscribes in both Awake and OnEnable would otherwise receive every
        /// event twice and, for example, score a coin twice.
        /// </summary>
        [Test]
        public void Subscribe_IgnoresADuplicateHandler()
        {
            int calls = 0;
            Action<ProbeEvent> handler = _ => calls++;

            EventBus<ProbeEvent>.Subscribe(handler);
            EventBus<ProbeEvent>.Subscribe(handler);
            EventBus<ProbeEvent>.Publish(new ProbeEvent(1));

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int calls = 0;
            Action<ProbeEvent> handler = _ => calls++;
            EventBus<ProbeEvent>.Subscribe(handler);

            EventBus<ProbeEvent>.Unsubscribe(handler);
            EventBus<ProbeEvent>.Publish(new ProbeEvent(1));

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Unsubscribe_IsSafeWhenCalledTwice()
        {
            Action<ProbeEvent> handler = _ => { };
            EventBus<ProbeEvent>.Subscribe(handler);

            EventBus<ProbeEvent>.Unsubscribe(handler);

            Assert.DoesNotThrow(() => EventBus<ProbeEvent>.Unsubscribe(handler));
            Assert.DoesNotThrow(() => EventBus<ProbeEvent>.Unsubscribe(null));
        }

        [Test]
        public void Subscribe_IgnoresNull()
        {
            Assert.DoesNotThrow(() => EventBus<ProbeEvent>.Subscribe(null));
            Assert.DoesNotThrow(() => EventBus<ProbeEvent>.Publish(new ProbeEvent(1)));
        }

        /// <summary>
        /// A destroyed object whose handler throws must not silence the systems behind it in the
        /// list. Scoring should still happen even if a visual effect blew up first.
        /// </summary>
        [Test]
        public void Publish_ContinuesAfterAHandlerThrows()
        {
            bool reachedLast = false;
            EventBus<ProbeEvent>.Subscribe(_ => throw new InvalidOperationException("probe"));
            EventBus<ProbeEvent>.Subscribe(_ => reachedLast = true);

            LogAssert.ignoreFailingMessages = true;
            EventBus<ProbeEvent>.Publish(new ProbeEvent(1));
            LogAssert.ignoreFailingMessages = false;

            Assert.IsTrue(reachedLast, "A throwing handler must not stop the ones behind it.");
        }

        /// <summary>
        /// Unsubscribing from inside a handler is normal during teardown, and must not disturb
        /// the publish that is already in flight.
        /// </summary>
        [Test]
        public void Publish_ToleratesUnsubscribingDuringDispatch()
        {
            int secondCalls = 0;
            Action<ProbeEvent> second = _ => secondCalls++;

            EventBus<ProbeEvent>.Subscribe(_ => EventBus<ProbeEvent>.Unsubscribe(second));
            EventBus<ProbeEvent>.Subscribe(second);

            Assert.DoesNotThrow(() => EventBus<ProbeEvent>.Publish(new ProbeEvent(1)));
            Assert.AreEqual(1, secondCalls, "The in-flight publish should still reach the removed handler.");
        }

        /// <summary>
        /// A handler is allowed to publish the same event type again - a hit handler raising a
        /// follow-up hit, for instance. The nested publish must not cut the outer one short, or
        /// subscribers registered after the republisher would silently stop being notified.
        /// </summary>
        [Test]
        public void Publish_SurvivesAHandlerRepublishingTheSameEvent()
        {
            int lastHandlerCalls = 0;
            bool republished = false;

            EventBus<ProbeEvent>.Subscribe(_ =>
            {
                if (republished)
                {
                    return;
                }

                republished = true;
                EventBus<ProbeEvent>.Publish(new ProbeEvent(2));
            });

            EventBus<ProbeEvent>.Subscribe(_ => lastHandlerCalls++);

            EventBus<ProbeEvent>.Publish(new ProbeEvent(1));

            Assert.AreEqual(
                2,
                lastHandlerCalls,
                "The handler should receive the nested publish and the outer one.");
        }

        [Test]
        public void Clear_RemovesEverySubscriber()
        {
            int calls = 0;
            EventBus<ProbeEvent>.Subscribe(_ => calls++);

            EventBus<ProbeEvent>.Clear();
            EventBus<ProbeEvent>.Publish(new ProbeEvent(1));

            Assert.AreEqual(0, calls);
        }
    }
}
