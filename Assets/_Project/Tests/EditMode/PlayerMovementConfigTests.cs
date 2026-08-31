using MoveRush.Player.Config;
using NUnit.Framework;
using UnityEngine;

namespace MoveRush.Tests
{
    /// <summary>
    /// Covers the derived jump velocity. Designers tune a jump height they can picture, and the
    /// take-off speed is solved from it, so this test checks the physics actually agrees: a jump
    /// launched at the derived velocity must peak at exactly the configured height.
    /// </summary>
    public class PlayerMovementConfigTests
    {
        private PlayerMovementConfig config;

        /// <summary>Creates a config carrying the shipped defaults.</summary>
        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
        }

        /// <summary>Releases the instance between tests.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        /// <summary>
        /// Apex height for a launch velocity v under gravity g is v squared over 2g. Solving that
        /// back must return the configured jump height, whatever the two values are tuned to.
        /// </summary>
        [Test]
        [TestCase(2.2f, -32f)]
        [TestCase(1f, -9.81f)]
        [TestCase(4.5f, -50f)]
        public void JumpVelocity_ReachesExactlyTheConfiguredHeight(float height, float gravity)
        {
            TestConfigBuilder.SetField(config, "jumpHeight", height);
            TestConfigBuilder.SetField(config, "gravity", gravity);

            float apex = config.JumpVelocity * config.JumpVelocity / (2f * Mathf.Abs(gravity));

            Assert.AreEqual(height, apex, 0.001f);
        }

        [Test]
        public void JumpVelocity_IsAlwaysPositive()
        {
            Assert.Greater(config.JumpVelocity, 0f);
        }

        /// <summary>Falling must be at least as fast as rising, or the jump feels floaty.</summary>
        [Test]
        public void Defaults_MakeTheFallNoSlowerThanTheRise()
        {
            Assert.GreaterOrEqual(config.FallGravityMultiplier, 1f);
            Assert.Less(config.Gravity, 0f);
        }

        /// <summary>The sliding capsule has to be shorter than the standing one.</summary>
        [Test]
        public void Defaults_MakeTheSlidingPoseShorter()
        {
            Assert.Less(config.SlidingHeight, config.StandingHeight);
        }
    }
}
