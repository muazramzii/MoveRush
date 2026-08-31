using MoveRush.Gameplay.Config;
using NUnit.Framework;
using UnityEngine;

namespace MoveRush.Tests
{
    /// <summary>
    /// Covers the difficulty ramp against the speeds named in the brief: 0 m at 5 m/s, 500 at 6,
    /// 1000 at 7, 2000 at 8 and 3000 at 9. The tier also gates which obstacles are allowed to
    /// spawn, so an off-by-one here changes what the player faces, not just how fast they move.
    /// </summary>
    public class DifficultyConfigTests
    {
        private DifficultyConfig config;

        /// <summary>Builds the specified ramp.</summary>
        [SetUp]
        public void SetUp()
        {
            config = TestConfigBuilder.CreateDifficulty(
                0.35f,
                (0f, 5f),
                (500f, 6f),
                (1000f, 7f),
                (2000f, 8f),
                (3000f, 9f));
        }

        /// <summary>Releases the instance between tests.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Ramp_ExposesItsEndpoints()
        {
            Assert.AreEqual(5, config.StepCount);
            Assert.AreEqual(5f, config.StartSpeed, 0.0001f);
            Assert.AreEqual(9f, config.MaxSpeed, 0.0001f);
        }

        [Test]
        [TestCase(0f, 5f)]
        [TestCase(499f, 5f)]
        [TestCase(500f, 6f)]
        [TestCase(999f, 6f)]
        [TestCase(1000f, 7f)]
        [TestCase(1999f, 7f)]
        [TestCase(2000f, 8f)]
        [TestCase(3000f, 9f)]
        [TestCase(99999f, 9f)]
        public void GetTargetSpeed_FollowsTheSpecifiedRamp(float distance, float expected)
        {
            Assert.AreEqual(expected, config.GetTargetSpeed(distance), 0.0001f);
        }

        [Test]
        [TestCase(0f, 0)]
        [TestCase(499f, 0)]
        [TestCase(500f, 1)]
        [TestCase(1000f, 2)]
        [TestCase(2000f, 3)]
        [TestCase(3000f, 4)]
        [TestCase(50000f, 4)]
        public void GetTier_MatchesTheActiveStep(float distance, int expected)
        {
            Assert.AreEqual(expected, config.GetTier(distance));
        }

        /// <summary>
        /// Intensity is what the camera and the spawner read instead of raw speed, so it has to
        /// stay a normalised 0 to 1 across the whole ramp however the speeds are re-tuned.
        /// </summary>
        [Test]
        [TestCase(5f, 0f)]
        [TestCase(7f, 0.5f)]
        [TestCase(9f, 1f)]
        public void GetNormalizedIntensity_SpansTheRamp(float speed, float expected)
        {
            Assert.AreEqual(expected, config.GetNormalizedIntensity(speed), 0.0001f);
        }

        [Test]
        [TestCase(-100f, 0f)]
        [TestCase(1000f, 1f)]
        public void GetNormalizedIntensity_ClampsOutsideTheRamp(float speed, float expected)
        {
            Assert.AreEqual(expected, config.GetNormalizedIntensity(speed), 0.0001f);
        }

        /// <summary>An unconfigured asset must fall back rather than divide by zero.</summary>
        [Test]
        public void EmptyRamp_FallsBackToASafeSpeed()
        {
            DifficultyConfig empty = TestConfigBuilder.CreateDifficulty(0.35f);

            Assert.AreEqual(0, empty.StepCount);
            Assert.AreEqual(5f, empty.GetTargetSpeed(0f), 0.0001f);
            Assert.AreEqual(0f, empty.GetNormalizedIntensity(5f), 0.0001f);

            Object.DestroyImmediate(empty);
        }
    }
}
