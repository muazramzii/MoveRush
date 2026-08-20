using UnityEngine;

namespace MoveRush.Core.Events
{
    /// <summary>Raised when the character leaves the ground. Published as OnPlayerJump.</summary>
    public readonly struct PlayerJumpedEvent
    {
        /// <summary>World position of the take-off.</summary>
        public Vector3 Position { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="position">World position of the take-off.</param>
        public PlayerJumpedEvent(Vector3 position) => Position = position;
    }

    /// <summary>Raised when the character touches down after being airborne.</summary>
    public readonly struct PlayerLandedEvent
    {
        /// <summary>World position of the landing.</summary>
        public Vector3 Position { get; }

        /// <summary>Seconds the character spent in the air.</summary>
        public float AirTime { get; }

        /// <summary>Impact strength in the 0..1 range, used to scale the camera shake.</summary>
        public float Impact { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="position">World position of the landing.</param>
        /// <param name="airTime">Seconds spent in the air.</param>
        /// <param name="impact">Impact strength in the 0..1 range.</param>
        public PlayerLandedEvent(Vector3 position, float airTime, float impact)
        {
            Position = position;
            AirTime = airTime;
            Impact = impact;
        }
    }

    /// <summary>Raised when the character commits to a different lane.</summary>
    public readonly struct PlayerLaneChangedEvent
    {
        /// <summary>Lane the character came from.</summary>
        public int PreviousLane { get; }

        /// <summary>Lane the character is heading for.</summary>
        public int CurrentLane { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="previousLane">Lane the character came from.</param>
        /// <param name="currentLane">Lane the character is heading for.</param>
        public PlayerLaneChangedEvent(int previousLane, int currentLane)
        {
            PreviousLane = previousLane;
            CurrentLane = currentLane;
        }
    }

    /// <summary>Raised when a slide begins.</summary>
    public readonly struct PlayerSlideStartedEvent
    {
        /// <summary>World position where the slide began.</summary>
        public Vector3 Position { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="position">World position where the slide began.</param>
        public PlayerSlideStartedEvent(Vector3 position) => Position = position;
    }

    /// <summary>Raised when a slide ends and the collider returns to full height.</summary>
    public readonly struct PlayerSlideEndedEvent
    {
        /// <summary>World position where the slide ended.</summary>
        public Vector3 Position { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="position">World position where the slide ended.</param>
        public PlayerSlideEndedEvent(Vector3 position) => Position = position;
    }

    /// <summary>Raised once when the character is killed.</summary>
    public readonly struct PlayerDiedEvent
    {
        /// <summary>Identifier of what killed the character.</summary>
        public string Cause { get; }

        /// <summary>World position of the death.</summary>
        public Vector3 Position { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="cause">Identifier of what killed the character.</param>
        /// <param name="position">World position of the death.</param>
        public PlayerDiedEvent(string cause, Vector3 position)
        {
            Cause = cause;
            Position = position;
        }
    }
}
