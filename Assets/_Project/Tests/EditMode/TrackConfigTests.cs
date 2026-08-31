using MoveRush.Core.Config;
using NUnit.Framework;
using UnityEngine;

namespace MoveRush.Tests
{
    /// <summary>
    /// Covers the shared track geometry. Both the character and the world generator read their
    /// lane positions from here, so a wrong offset would desynchronise the player from the
    /// obstacles without either system looking wrong on its own.
    /// </summary>
    public class TrackConfigTests
    {
        private TrackConfig track;

        /// <summary>Creates a config carrying the specified three-lane defaults.</summary>
        [SetUp]
        public void SetUp()
        {
            track = ScriptableObject.CreateInstance<TrackConfig>();
        }

        /// <summary>Releases the instance between tests.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(track);
        }

        [Test]
        public void Defaults_DescribeThreeLanes()
        {
            Assert.AreEqual(3, track.LaneCount);
            Assert.AreEqual(20f, track.TileLength);
            Assert.AreEqual(0f, track.GroundHeight);
        }

        [Test]
        [TestCase(0, -2.5f)]
        [TestCase(1, 0f)]
        [TestCase(2, 2.5f)]
        public void GetLaneOffset_ReturnsSpecifiedPositions(int lane, float expected)
        {
            Assert.AreEqual(expected, track.GetLaneOffset(lane), 0.0001f);
        }

        /// <summary>The centre lane is where a run starts, so it must be the middle of three.</summary>
        [Test]
        public void CenterLaneIndex_IsTheMiddleLane()
        {
            Assert.AreEqual(1, track.CenterLaneIndex);
            Assert.AreEqual(0f, track.GetLaneOffset(track.CenterLaneIndex), 0.0001f);
        }

        /// <summary>
        /// Out of range indices clamp instead of throwing. Lane changes are driven by player
        /// input at the edges of the track, so this path runs constantly during normal play.
        /// </summary>
        [Test]
        [TestCase(-5, 0)]
        [TestCase(-1, 0)]
        [TestCase(3, 2)]
        [TestCase(99, 2)]
        public void ClampLane_KeepsIndicesInRange(int input, int expected)
        {
            Assert.AreEqual(expected, track.ClampLane(input));
        }

        [Test]
        public void GetLaneOffset_ClampsOutOfRangeIndices()
        {
            Assert.AreEqual(-2.5f, track.GetLaneOffset(-10), 0.0001f);
            Assert.AreEqual(2.5f, track.GetLaneOffset(10), 0.0001f);
        }

        [Test]
        public void TrackWidth_CoversEveryLane()
        {
            Assert.AreEqual(track.LaneCount * track.LaneWidth, track.TrackWidth, 0.0001f);
        }
    }
}
