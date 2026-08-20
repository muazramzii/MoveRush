using UnityEngine;

namespace MoveRush.Player.Config
{
    /// <summary>
    /// Feel of the character: how fast lanes are swapped, how a jump arcs and how a slide reads.
    /// Movement is tuned far more often than it is written, so every number lives here and none
    /// of it is duplicated on a prefab.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "MoveRush/Config/Player Movement Config", order = 13)]
    public class PlayerMovementConfig : ScriptableObject
    {
        [Header("Lane Change")]
        [Tooltip("Seconds the lateral smoothing takes to close most of the gap.")]
        [SerializeField, Range(0.02f, 0.5f)] private float laneChangeSmoothTime = 0.11f;

        [Tooltip("Ceiling on lateral speed in meters per second.")]
        [SerializeField, Min(1f)] private float maxLaneSpeed = 28f;

        [Tooltip("Buffers a lane change made while another one is still finishing.")]
        [SerializeField] private bool allowQueuedLaneChange = true;

        [Header("Jump")]
        [Tooltip("Peak height of a jump in meters.")]
        [SerializeField, Range(0.5f, 5f)] private float jumpHeight = 2.2f;

        [Tooltip("Downward acceleration in meters per second squared. Negative.")]
        [SerializeField] private float gravity = -32f;

        [Tooltip("Extra gravity applied while falling, which makes the arc feel snappy.")]
        [SerializeField, Range(1f, 3f)] private float fallGravityMultiplier = 1.55f;

        [Tooltip("Seconds a jump pressed just before landing is remembered for.")]
        [SerializeField, Range(0f, 0.4f)] private float jumpBufferTime = 0.14f;

        [Header("Slide")]
        [Tooltip("Seconds a slide lasts before the character stands back up.")]
        [SerializeField, Range(0.2f, 2f)] private float slideDuration = 0.85f;

        [Tooltip("Collider height while standing.")]
        [SerializeField, Min(0.2f)] private float standingHeight = 2f;

        [Tooltip("Collider height while sliding.")]
        [SerializeField, Min(0.1f)] private float slidingHeight = 0.9f;

        [Tooltip("Ends a slide early when the player jumps out of it.")]
        [SerializeField] private bool jumpCancelsSlide = true;

        /// <summary>Seconds the lateral smoothing takes.</summary>
        public float LaneChangeSmoothTime => laneChangeSmoothTime;

        /// <summary>Ceiling on lateral speed.</summary>
        public float MaxLaneSpeed => maxLaneSpeed;

        /// <summary>True when a lane change made mid-move is queued.</summary>
        public bool AllowQueuedLaneChange => allowQueuedLaneChange;

        /// <summary>Peak height of a jump in meters.</summary>
        public float JumpHeight => jumpHeight;

        /// <summary>Downward acceleration in meters per second squared.</summary>
        public float Gravity => gravity;

        /// <summary>Extra gravity applied while falling.</summary>
        public float FallGravityMultiplier => fallGravityMultiplier;

        /// <summary>Seconds an early jump press is remembered for.</summary>
        public float JumpBufferTime => jumpBufferTime;

        /// <summary>Seconds a slide lasts.</summary>
        public float SlideDuration => slideDuration;

        /// <summary>Collider height while standing.</summary>
        public float StandingHeight => standingHeight;

        /// <summary>Collider height while sliding.</summary>
        public float SlidingHeight => slidingHeight;

        /// <summary>True when jumping ends a slide early.</summary>
        public bool JumpCancelsSlide => jumpCancelsSlide;

        /// <summary>
        /// Take-off speed that reaches exactly <see cref="JumpHeight"/> under
        /// <see cref="Gravity"/>, derived from v = sqrt(2 * g * h). Deriving it means designers
        /// tune a height they can picture instead of a velocity they cannot.
        /// </summary>
        public float JumpVelocity => Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(gravity));

        /// <summary>Keeps the tuning physically sane.</summary>
        private void OnValidate()
        {
            if (gravity >= 0f)
            {
                gravity = -32f;
            }

            slidingHeight = Mathf.Min(slidingHeight, standingHeight);
        }
    }
}
