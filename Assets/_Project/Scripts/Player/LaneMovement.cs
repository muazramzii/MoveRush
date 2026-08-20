using MoveRush.Core.Config;
using MoveRush.Core.Events;
using MoveRush.Player.Config;
using UnityEngine;

namespace MoveRush.Player
{
    /// <summary>
    /// Lateral motion between the fixed lanes. The target lane snaps instantly so the game reads
    /// the input immediately, while the visible position is eased with SmoothDamp - which is what
    /// makes a lane change feel responsive and smooth at the same time.
    /// </summary>
    [DisallowMultipleComponent]
    public class LaneMovement : MonoBehaviour
    {
        private TrackConfig track;
        private PlayerMovementConfig config;
        private float currentX;
        private float targetX;
        private float damperVelocity;

        /// <summary>Lane the character is heading for.</summary>
        public int CurrentLane { get; private set; }

        /// <summary>Lateral speed normalised into the -1..1 range, for the camera tilt.</summary>
        public float LateralVelocityNormalized =>
            config == null ? 0f : Mathf.Clamp(damperVelocity / config.MaxLaneSpeed, -1f, 1f);

        /// <summary>True while the character is still sliding across to the target lane.</summary>
        public bool IsChangingLane => Mathf.Abs(targetX - currentX) > 0.02f;

        /// <summary>Injects the shared track geometry and the movement tuning.</summary>
        /// <param name="trackConfig">Lane positions.</param>
        /// <param name="movementConfig">Movement tuning.</param>
        public void Initialize(TrackConfig trackConfig, PlayerMovementConfig movementConfig)
        {
            track = trackConfig;
            config = movementConfig;
            ResetToLane(track != null ? track.CenterLaneIndex : 0);
        }

        /// <summary>
        /// Requests a lane change. A request made mid-move is queued rather than dropped, so a
        /// quick double tap moves two lanes instead of being swallowed.
        /// </summary>
        /// <param name="direction">Negative for left, positive for right.</param>
        /// <returns>True when the lane actually changed.</returns>
        public bool TryMove(int direction)
        {
            if (track == null || direction == 0)
            {
                return false;
            }

            int step = direction < 0 ? -1 : 1;

            if (IsChangingLane && config != null && !config.AllowQueuedLaneChange)
            {
                return false;
            }

            int nextLane = track.ClampLane(CurrentLane + step);
            if (nextLane == CurrentLane)
            {
                return false;
            }

            int previousLane = CurrentLane;
            CurrentLane = nextLane;
            targetX = track.GetLaneOffset(CurrentLane);

            EventBus<PlayerLaneChangedEvent>.Publish(new PlayerLaneChangedEvent(previousLane, CurrentLane));
            return true;
        }

        /// <summary>Advances the smoothing and returns the world X for this frame.</summary>
        /// <param name="deltaTime">Frame time.</param>
        /// <returns>World X position of the character.</returns>
        public float Evaluate(float deltaTime)
        {
            if (config == null)
            {
                return currentX;
            }

            currentX = Mathf.SmoothDamp(
                currentX,
                targetX,
                ref damperVelocity,
                config.LaneChangeSmoothTime,
                config.MaxLaneSpeed,
                deltaTime);

            return currentX;
        }

        /// <summary>Snaps to a lane without any easing. Used when a run resets.</summary>
        /// <param name="lane">Lane to snap to.</param>
        public void ResetToLane(int lane)
        {
            CurrentLane = track != null ? track.ClampLane(lane) : 0;
            targetX = track != null ? track.GetLaneOffset(CurrentLane) : 0f;
            currentX = targetX;
            damperVelocity = 0f;
        }
    }
}
