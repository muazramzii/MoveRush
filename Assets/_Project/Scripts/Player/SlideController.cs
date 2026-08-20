using MoveRush.Core.Events;
using MoveRush.Player.Config;
using UnityEngine;

namespace MoveRush.Player
{
    /// <summary>
    /// Timed slide that shrinks the character collider so a laser gate passes overhead.
    /// The collider is the gameplay truth here: the visual squash is cosmetic, but the reduced
    /// capsule is what actually decides whether the slide worked.
    /// </summary>
    [DisallowMultipleComponent]
    public class SlideController : MonoBehaviour
    {
        [Tooltip("Capsule resized while sliding. Required.")]
        [SerializeField] private CapsuleCollider bodyCollider;

        [Tooltip("Optional visual squashed while sliding.")]
        [SerializeField] private Transform visualRoot;

        private PlayerMovementConfig config;
        private Vector3 visualBaseScale;
        private float slideTimer;

        /// <summary>True while the slide is active.</summary>
        public bool IsSliding { get; private set; }

        /// <summary>Injects the tuning and caches the standing pose.</summary>
        /// <param name="movementConfig">Movement tuning.</param>
        public void Initialize(PlayerMovementConfig movementConfig)
        {
            config = movementConfig;
            visualBaseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            Stand(false);
        }

        /// <summary>Starts a slide when the character is on the ground and not already sliding.</summary>
        /// <param name="isAirborne">True when the character is currently in the air.</param>
        /// <returns>True when the slide started.</returns>
        public bool TrySlide(bool isAirborne)
        {
            if (config == null || IsSliding || isAirborne)
            {
                return false;
            }

            IsSliding = true;
            slideTimer = config.SlideDuration;
            ApplyPose(true);

            EventBus<PlayerSlideStartedEvent>.Publish(new PlayerSlideStartedEvent(transform.position));
            return true;
        }

        /// <summary>Counts the slide down and stands the character back up when it expires.</summary>
        /// <param name="deltaTime">Frame time.</param>
        public void Tick(float deltaTime)
        {
            if (!IsSliding)
            {
                return;
            }

            slideTimer -= deltaTime;

            if (slideTimer <= 0f)
            {
                Stand(true);
            }
        }

        /// <summary>Ends a slide early, for example because the player jumped out of it.</summary>
        public void Cancel()
        {
            if (IsSliding)
            {
                Stand(true);
            }
        }

        /// <summary>Clears the slide state without announcing anything. Used when a run resets.</summary>
        public void ResetState() => Stand(false);

        /// <summary>Returns the character to the standing pose.</summary>
        /// <param name="announce">True to publish the slide ended event.</param>
        private void Stand(bool announce)
        {
            bool wasSliding = IsSliding;

            IsSliding = false;
            slideTimer = 0f;
            ApplyPose(false);

            if (announce && wasSliding)
            {
                EventBus<PlayerSlideEndedEvent>.Publish(new PlayerSlideEndedEvent(transform.position));
            }
        }

        /// <summary>Resizes the capsule and squashes the visual.</summary>
        /// <param name="sliding">True for the sliding pose.</param>
        private void ApplyPose(bool sliding)
        {
            if (config == null)
            {
                return;
            }

            float height = sliding ? config.SlidingHeight : config.StandingHeight;

            if (bodyCollider != null)
            {
                bodyCollider.height = height;
                bodyCollider.center = new Vector3(0f, height * 0.5f, 0f);
            }

            if (visualRoot == null)
            {
                return;
            }

            float ratio = config.StandingHeight <= 0f ? 1f : height / config.StandingHeight;
            visualRoot.localScale = new Vector3(visualBaseScale.x, visualBaseScale.y * ratio, visualBaseScale.z);
            visualRoot.localPosition = new Vector3(0f, 0f, 0f);
        }
    }
}
