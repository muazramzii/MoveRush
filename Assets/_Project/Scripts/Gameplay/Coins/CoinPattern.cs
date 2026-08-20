using System.Collections.Generic;
using UnityEngine;

namespace MoveRush.Gameplay.Coins
{
    /// <summary>Shapes a coin run can take across the lanes.</summary>
    public enum CoinPatternShape
    {
        /// <summary>All coins in a single lane.</summary>
        Straight = 0,

        /// <summary>Alternates between two neighbouring lanes.</summary>
        Zigzag = 1,

        /// <summary>One lane, with the coins rising and falling along a jump arc.</summary>
        Triangle = 2,

        /// <summary>Sweeps across every lane and back again.</summary>
        Snake = 3
    }

    /// <summary>One coin position produced by a pattern, relative to the pattern start.</summary>
    public readonly struct CoinPlacement
    {
        /// <summary>Lane the coin sits in.</summary>
        public int Lane { get; }

        /// <summary>Distance in meters from the start of the pattern.</summary>
        public float ForwardOffset { get; }

        /// <summary>Extra height above the configured coin height.</summary>
        public float HeightOffset { get; }

        /// <summary>Creates the placement.</summary>
        /// <param name="lane">Lane the coin sits in.</param>
        /// <param name="forwardOffset">Distance from the start of the pattern.</param>
        /// <param name="heightOffset">Extra height above the configured coin height.</param>
        public CoinPlacement(int lane, float forwardOffset, float heightOffset)
        {
            Lane = lane;
            ForwardOffset = forwardOffset;
            HeightOffset = heightOffset;
        }
    }

    /// <summary>
    /// A reusable coin layout. Patterns are assets rather than code branches, so a designer can
    /// add a new coin run, weight it and ship it without a programmer touching the spawner.
    /// </summary>
    [CreateAssetMenu(fileName = "CoinPattern", menuName = "MoveRush/Coins/Coin Pattern", order = 20)]
    public class CoinPattern : ScriptableObject
    {
        [Tooltip("Shape the coins are laid out in.")]
        [SerializeField] private CoinPatternShape shape = CoinPatternShape.Straight;

        [Tooltip("Number of coins in the pattern.")]
        [SerializeField, Range(2, 24)] private int coinCount = 8;

        [Tooltip("Relative chance of this pattern being picked.")]
        [SerializeField, Min(0f)] private float weight = 1f;

        [Tooltip("Peak height of the triangle arc, in meters above the base coin height.")]
        [SerializeField, Range(0f, 3f)] private float arcHeight = 1.4f;

        /// <summary>Shape the coins are laid out in.</summary>
        public CoinPatternShape Shape => shape;

        /// <summary>Number of coins in the pattern.</summary>
        public int CoinCount => coinCount;

        /// <summary>Relative chance of this pattern being picked.</summary>
        public float Weight => weight;

        /// <summary>Length of the pattern in meters at a given spacing.</summary>
        /// <param name="spacing">Distance between two coins.</param>
        /// <returns>Length in meters.</returns>
        public float GetLength(float spacing) => Mathf.Max(0f, coinCount - 1) * spacing;

        /// <summary>
        /// Writes the placements of this pattern into a caller-owned list. The caller supplies
        /// the buffer so populating a tile every few seconds never allocates.
        /// </summary>
        /// <param name="results">Buffer that receives the placements. Cleared first.</param>
        /// <param name="laneCount">Number of lanes on the track.</param>
        /// <param name="startLane">Lane the pattern is anchored to.</param>
        /// <param name="spacing">Distance in meters between two coins.</param>
        public void Build(List<CoinPlacement> results, int laneCount, int startLane, float spacing)
        {
            if (results == null || laneCount <= 0)
            {
                return;
            }

            results.Clear();

            int lastLane = laneCount - 1;
            int anchor = Mathf.Clamp(startLane, 0, lastLane);
            int neighbour = anchor < lastLane ? anchor + 1 : Mathf.Max(0, anchor - 1);

            for (int i = 0; i < coinCount; i++)
            {
                float forward = i * spacing;
                int lane = anchor;
                float height = 0f;

                switch (shape)
                {
                    case CoinPatternShape.Zigzag:
                        lane = (i / 2) % 2 == 0 ? anchor : neighbour;
                        break;

                    case CoinPatternShape.Triangle:
                        float t = coinCount > 1 ? (float)i / (coinCount - 1) : 0f;
                        height = arcHeight * Mathf.Sin(Mathf.PI * t);
                        break;

                    case CoinPatternShape.Snake:
                        int period = Mathf.Max(1, laneCount * 2 - 2);
                        int phase = i % period;
                        lane = phase < laneCount ? phase : period - phase;
                        break;
                }

                results.Add(new CoinPlacement(Mathf.Clamp(lane, 0, lastLane), forward, height));
            }
        }
    }
}
