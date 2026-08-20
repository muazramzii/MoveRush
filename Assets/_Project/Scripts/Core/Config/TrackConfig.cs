using UnityEngine;

namespace MoveRush.Core.Config
{
    /// <summary>
    /// Geometry of the run track. Lane positions and tile length are shared truth between the
    /// player assembly and the gameplay assembly, so they live in Core and are published through
    /// <see cref="IConfigProvider"/> instead of being duplicated in two configs that can drift.
    /// </summary>
    [CreateAssetMenu(fileName = "TrackConfig", menuName = "MoveRush/Config/Track Config", order = 3)]
    public class TrackConfig : ScriptableObject
    {
        [Header("Lanes")]
        [Tooltip("World X position of each lane, ordered left to right.")]
        [SerializeField] private float[] laneOffsets = { -2.5f, 0f, 2.5f };

        [Tooltip("Width of a single lane, used to size tiles and obstacle colliders.")]
        [SerializeField, Min(0.1f)] private float laneWidth = 2.5f;

        [Header("Track")]
        [Tooltip("Length of one road tile in meters.")]
        [SerializeField, Min(1f)] private float tileLength = 20f;

        [Tooltip("World Y position the character stands on.")]
        [SerializeField] private float groundHeight;

        /// <summary>Number of lanes on the track.</summary>
        public int LaneCount => laneOffsets?.Length ?? 0;

        /// <summary>Width of a single lane in meters.</summary>
        public float LaneWidth => laneWidth;

        /// <summary>Length of one road tile in meters.</summary>
        public float TileLength => tileLength;

        /// <summary>World Y position of the ground plane.</summary>
        public float GroundHeight => groundHeight;

        /// <summary>Index of the middle lane, where a run starts.</summary>
        public int CenterLaneIndex => Mathf.Max(0, LaneCount / 2);

        /// <summary>Total width covered by the lanes.</summary>
        public float TrackWidth => LaneCount * laneWidth;

        /// <summary>Returns the world X position of a lane, clamping out of range indices.</summary>
        /// <param name="laneIndex">Zero based lane index.</param>
        /// <returns>World X position of the lane centre.</returns>
        public float GetLaneOffset(int laneIndex)
        {
            return LaneCount == 0 ? 0f : laneOffsets[ClampLane(laneIndex)];
        }

        /// <summary>Clamps a lane index into the valid range.</summary>
        /// <param name="laneIndex">Index to clamp.</param>
        /// <returns>A valid lane index.</returns>
        public int ClampLane(int laneIndex) => Mathf.Clamp(laneIndex, 0, Mathf.Max(0, LaneCount - 1));

        /// <summary>Guarantees the asset always describes a usable track.</summary>
        private void OnValidate()
        {
            if (laneOffsets == null || laneOffsets.Length == 0)
            {
                laneOffsets = new[] { -2.5f, 0f, 2.5f };
            }
        }
    }
}
