using MoveRush.Core.Events;
using MoveRush.Player.Config;
using UnityEngine;

namespace MoveRush.Player
{
    /// <summary>
    /// Vertical motion. Gravity is integrated by hand rather than handed to the physics engine:
    /// a runner needs a jump arc that is identical every single time, and a rigidbody cannot
    /// promise that once it starts touching obstacle colliders.
    /// Falling uses a heavier gravity than rising, the standard trick that keeps a jump snappy
    /// without making it feel floaty at the apex.
    /// </summary>
    [DisallowMultipleComponent]
    public class JumpController : MonoBehaviour
    {
        private PlayerMovementConfig config;
        private float groundY;
        private float verticalVelocity;
        private float bufferTimer;
        private float airTime;

        /// <summary>True while the character is off the ground.</summary>
        public bool IsAirborne { get; private set; }

        /// <summary>True while the character is moving downwards.</summary>
        public bool IsFalling => IsAirborne && verticalVelocity < 0f;

        /// <summary>Injects the tuning and the height of the ground plane.</summary>
        /// <param name="movementConfig">Movement tuning.</param>
        /// <param name="groundHeight">World Y the character stands on.</param>
        public void Initialize(PlayerMovementConfig movementConfig, float groundHeight)
        {
            config = movementConfig;
            groundY = groundHeight;
            ResetState();
        }

        /// <summary>
        /// Requests a jump. A press made just before landing is buffered instead of being lost,
        /// which is what stops a correctly timed jump from feeling ignored.
        /// </summary>
        /// <returns>True when the jump started immediately.</returns>
        public bool TryJump()
        {
            if (config == null)
            {
                return false;
            }

            if (IsAirborne)
            {
                bufferTimer = config.JumpBufferTime;
                return false;
            }

            Launch();
            return true;
        }

        /// <summary>Advances the vertical motion and returns the world Y for this frame.</summary>
        /// <param name="deltaTime">Frame time.</param>
        /// <param name="currentY">World Y at the start of the frame.</param>
        /// <returns>World Y position of the character.</returns>
        public float Evaluate(float deltaTime, float currentY)
        {
            if (config == null)
            {
                return groundY;
            }

            if (bufferTimer > 0f)
            {
                bufferTimer -= deltaTime;
            }

            if (!IsAirborne)
            {
                if (bufferTimer > 0f)
                {
                    Launch();
                }

                return groundY;
            }

            float gravity = config.Gravity * (verticalVelocity < 0f ? config.FallGravityMultiplier : 1f);
            verticalVelocity += gravity * deltaTime;
            airTime += deltaTime;

            float nextY = currentY + verticalVelocity * deltaTime;

            if (nextY <= groundY)
            {
                Land();
                return groundY;
            }

            return nextY;
        }

        /// <summary>Clears every airborne state. Used when a run resets.</summary>
        public void ResetState()
        {
            IsAirborne = false;
            verticalVelocity = 0f;
            bufferTimer = 0f;
            airTime = 0f;
        }

        /// <summary>Starts the ascent and announces the take-off.</summary>
        private void Launch()
        {
            verticalVelocity = config.JumpVelocity;
            IsAirborne = true;
            bufferTimer = 0f;
            airTime = 0f;

            EventBus<PlayerJumpedEvent>.Publish(new PlayerJumpedEvent(transform.position));
        }

        /// <summary>
        /// Ends the descent and announces the landing with an impact strength, which the camera
        /// turns into a shake proportional to the height of the fall.
        /// </summary>
        private void Land()
        {
            float impact = Mathf.Clamp01(Mathf.Abs(verticalVelocity) / Mathf.Max(1f, config.JumpVelocity));
            float landedAirTime = airTime;

            IsAirborne = false;
            verticalVelocity = 0f;
            airTime = 0f;

            Vector3 position = transform.position;
            position.y = groundY;

            EventBus<PlayerLandedEvent>.Publish(new PlayerLandedEvent(position, landedAirTime, impact));
        }
    }
}
