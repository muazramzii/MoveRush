using System.Collections.Generic;
using MoveRush.Gameplay.Coins;
using NUnit.Framework;
using UnityEngine;

namespace MoveRush.Tests
{
    /// <summary>
    /// Covers the four coin layouts named in the brief. A pattern that puts a coin outside the
    /// lane range would place it off the road entirely, which reads as a broken level rather than
    /// as a bug, so the lane bounds are asserted for every shape.
    /// </summary>
    public class CoinPatternTests
    {
        private const int LaneCount = 3;
        private const float Spacing = 2f;

        private readonly List<CoinPlacement> buffer = new List<CoinPlacement>();
        private readonly List<CoinPattern> created = new List<CoinPattern>();

        /// <summary>Destroys the patterns created during a test.</summary>
        [TearDown]
        public void TearDown()
        {
            foreach (CoinPattern pattern in created)
            {
                Object.DestroyImmediate(pattern);
            }

            created.Clear();
            buffer.Clear();
        }

        /// <summary>Builds a pattern asset of the given shape.</summary>
        /// <param name="shape">Layout to produce.</param>
        /// <param name="coinCount">Number of coins.</param>
        /// <returns>The pattern.</returns>
        private CoinPattern CreatePattern(CoinPatternShape shape, int coinCount)
        {
            CoinPattern pattern = TestConfigBuilder.Create<CoinPattern>(
                ("shape", shape),
                ("coinCount", coinCount),
                ("arcHeight", 1.4f));

            created.Add(pattern);
            return pattern;
        }

        [Test]
        [TestCase(CoinPatternShape.Straight)]
        [TestCase(CoinPatternShape.Zigzag)]
        [TestCase(CoinPatternShape.Triangle)]
        [TestCase(CoinPatternShape.Snake)]
        public void Build_ProducesTheRequestedCoinCount(CoinPatternShape shape)
        {
            CreatePattern(shape, 8).Build(buffer, LaneCount, 0, Spacing);

            Assert.AreEqual(8, buffer.Count);
        }

        /// <summary>Every shape must stay on the road, whichever lane it is anchored to.</summary>
        [Test]
        [TestCase(CoinPatternShape.Straight)]
        [TestCase(CoinPatternShape.Zigzag)]
        [TestCase(CoinPatternShape.Triangle)]
        [TestCase(CoinPatternShape.Snake)]
        public void Build_KeepsEveryCoinInsideTheLaneRange(CoinPatternShape shape)
        {
            CoinPattern pattern = CreatePattern(shape, 12);

            for (int anchor = 0; anchor < LaneCount; anchor++)
            {
                pattern.Build(buffer, LaneCount, anchor, Spacing);

                foreach (CoinPlacement placement in buffer)
                {
                    Assert.GreaterOrEqual(placement.Lane, 0);
                    Assert.Less(placement.Lane, LaneCount);
                }
            }
        }

        [Test]
        public void Build_SpacesCoinsEvenlyAlongTheTrack()
        {
            CreatePattern(CoinPatternShape.Straight, 5).Build(buffer, LaneCount, 1, Spacing);

            for (int i = 0; i < buffer.Count; i++)
            {
                Assert.AreEqual(i * Spacing, buffer[i].ForwardOffset, 0.0001f);
            }
        }

        [Test]
        public void Straight_KeepsEveryCoinInTheAnchorLane()
        {
            CreatePattern(CoinPatternShape.Straight, 6).Build(buffer, LaneCount, 2, Spacing);

            foreach (CoinPlacement placement in buffer)
            {
                Assert.AreEqual(2, placement.Lane);
                Assert.AreEqual(0f, placement.HeightOffset, 0.0001f);
            }
        }

        /// <summary>Zigzag has to actually change lane, otherwise it is just a straight run.</summary>
        [Test]
        public void Zigzag_AlternatesBetweenTwoNeighbouringLanes()
        {
            CreatePattern(CoinPatternShape.Zigzag, 8).Build(buffer, LaneCount, 0, Spacing);

            HashSet<int> lanes = new HashSet<int>();
            foreach (CoinPlacement placement in buffer)
            {
                lanes.Add(placement.Lane);
            }

            Assert.AreEqual(2, lanes.Count, "Zigzag should use exactly two lanes.");
            Assert.IsTrue(lanes.Contains(0));
            Assert.IsTrue(lanes.Contains(1));
        }

        /// <summary>Anchored to the last lane, zigzag must step inwards rather than off the road.</summary>
        [Test]
        public void Zigzag_StepsInwardsFromTheOutermostLane()
        {
            CreatePattern(CoinPatternShape.Zigzag, 8).Build(buffer, LaneCount, LaneCount - 1, Spacing);

            HashSet<int> lanes = new HashSet<int>();
            foreach (CoinPlacement placement in buffer)
            {
                lanes.Add(placement.Lane);
            }

            Assert.AreEqual(2, lanes.Count);
            Assert.IsTrue(lanes.Contains(LaneCount - 1));
            Assert.IsTrue(lanes.Contains(LaneCount - 2));
        }

        /// <summary>
        /// Triangle is the jump-arc pattern: one lane, with the coins rising to a peak in the
        /// middle and returning to ground level at both ends so the run can be collected in a
        /// single jump.
        /// </summary>
        [Test]
        public void Triangle_FormsAnArcInASingleLane()
        {
            CreatePattern(CoinPatternShape.Triangle, 5).Build(buffer, LaneCount, 1, Spacing);

            foreach (CoinPlacement placement in buffer)
            {
                Assert.AreEqual(1, placement.Lane, "Triangle should not change lane.");
            }

            Assert.AreEqual(0f, buffer[0].HeightOffset, 0.0001f);
            Assert.AreEqual(0f, buffer[buffer.Count - 1].HeightOffset, 0.0001f);
            Assert.Greater(buffer[2].HeightOffset, buffer[1].HeightOffset);
            Assert.Greater(buffer[2].HeightOffset, buffer[3].HeightOffset);
        }

        /// <summary>Snake has to sweep the whole track, not just two lanes.</summary>
        [Test]
        public void Snake_VisitsEveryLane()
        {
            CreatePattern(CoinPatternShape.Snake, 12).Build(buffer, LaneCount, 0, Spacing);

            HashSet<int> lanes = new HashSet<int>();
            foreach (CoinPlacement placement in buffer)
            {
                lanes.Add(placement.Lane);
            }

            Assert.AreEqual(LaneCount, lanes.Count, "Snake should sweep across every lane.");
        }

        /// <summary>A sweep may only move one lane at a time, or it is not collectable.</summary>
        [Test]
        public void Snake_NeverSkipsALane()
        {
            CreatePattern(CoinPatternShape.Snake, 16).Build(buffer, LaneCount, 0, Spacing);

            for (int i = 1; i < buffer.Count; i++)
            {
                int step = Mathf.Abs(buffer[i].Lane - buffer[i - 1].Lane);
                Assert.LessOrEqual(step, 1, $"Snake jumped {step} lanes at index {i}.");
            }
        }

        /// <summary>The caller owns the buffer, so Build has to reset it before filling.</summary>
        [Test]
        public void Build_ClearsTheCallerBuffer()
        {
            buffer.Add(new CoinPlacement(0, 99f, 99f));

            CreatePattern(CoinPatternShape.Straight, 3).Build(buffer, LaneCount, 0, Spacing);

            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(0f, buffer[0].ForwardOffset, 0.0001f);
        }

        [Test]
        public void Build_IgnoresATrackWithNoLanes()
        {
            CreatePattern(CoinPatternShape.Snake, 5).Build(buffer, 0, 0, Spacing);

            Assert.IsEmpty(buffer);
        }

        [Test]
        public void GetLength_SpansTheGapsBetweenCoins()
        {
            CoinPattern pattern = CreatePattern(CoinPatternShape.Straight, 5);

            Assert.AreEqual(8f, pattern.GetLength(Spacing), 0.0001f);
        }
    }
}
