using MoveRush.Gameplay.Config;
using NUnit.Framework;
using UnityEngine;

namespace MoveRush.Tests
{
    /// <summary>
    /// Covers the score and reward formulas. The brief specifies
    /// coins * 10 + distance * 2 + combo * 50, and this is the only place that number is defined,
    /// so it is pinned here rather than trusted to stay right by inspection.
    /// </summary>
    public class ScoreConfigTests
    {
        private ScoreConfig config;

        /// <summary>Creates a config carrying the shipped default weights.</summary>
        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<ScoreConfig>();
        }

        /// <summary>Releases the instance between tests.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Defaults_MatchTheSpecifiedWeights()
        {
            Assert.AreEqual(10, config.CoinMultiplier);
            Assert.AreEqual(2, config.DistanceMultiplier);
            Assert.AreEqual(50, config.ComboMultiplier);
        }

        [Test]
        [TestCase(0, 0f, 0, 0)]
        [TestCase(1, 0f, 0, 10)]
        [TestCase(0, 100f, 0, 200)]
        [TestCase(0, 0f, 1, 50)]
        [TestCase(3, 100f, 2, 330)]
        public void CalculateScore_AppliesTheFormula(int coins, float distance, int combo, int expected)
        {
            Assert.AreEqual(expected, config.CalculateScore(coins, distance, combo));
        }

        /// <summary>
        /// Distance is floored, not rounded. The score is recomputed every frame from a
        /// continuously growing distance, and flooring is what stops it flickering between two
        /// values as the fractional part crosses the halfway point.
        /// </summary>
        [Test]
        [TestCase(10.0f, 20)]
        [TestCase(10.4f, 20)]
        [TestCase(10.9f, 20)]
        [TestCase(11.0f, 22)]
        public void CalculateScore_FloorsDistance(float distance, int expected)
        {
            Assert.AreEqual(expected, config.CalculateScore(0, distance, 0));
        }

        [Test]
        [TestCase(0, 0f, 0)]
        [TestCase(3, 0f, 6)]
        [TestCase(0, 100f, 10)]
        [TestCase(3, 100.7f, 16)]
        public void CalculateXp_RewardsCoinsAndDistance(int coins, float distance, int expected)
        {
            Assert.AreEqual(expected, config.CalculateXp(coins, distance));
        }

        /// <summary>A run that ended immediately must not award negative experience.</summary>
        [Test]
        public void CalculateXp_NeverReturnsNegative()
        {
            Assert.GreaterOrEqual(config.CalculateXp(0, 0f), 0);
        }
    }
}
