using UnityEngine;

namespace MoveRush.Core.Services
{
    /// <summary>
    /// Read-only view of the player character, published for the systems that must react to it:
    /// obstacles deciding whether a pass counts as a hit, coins tracking a magnet target, the
    /// camera reading lateral motion. Exposing it as a Core contract is what keeps the gameplay
    /// assembly and the player assembly free of any reference to each other.
    /// </summary>
    public interface IPlayerService
    {
        /// <summary>Transform of the character root.</summary>
        Transform Transform { get; }

        /// <summary>World position of the character root.</summary>
        Vector3 Position { get; }

        /// <summary>False once the character has been killed and the run is over.</summary>
        bool IsAlive { get; }

        /// <summary>True while the character is off the ground.</summary>
        bool IsAirborne { get; }

        /// <summary>True while the character is sliding with a reduced collider.</summary>
        bool IsSliding { get; }

        /// <summary>Zero based index of the lane the character is heading for.</summary>
        int CurrentLane { get; }

        /// <summary>Current forward speed in meters per second.</summary>
        float ForwardSpeed { get; }

        /// <summary>Normalised lateral velocity in the -1..1 range, used for camera tilt.</summary>
        float LateralVelocityNormalized { get; }

        /// <summary>
        /// Ends the run for the character. Implementations are idempotent, so two obstacles
        /// touching the player in the same frame cannot raise two game overs.
        /// </summary>
        /// <param name="cause">Identifier of what killed the player, for analytics and logs.</param>
        void Kill(string cause);

        /// <summary>Returns the character to its start-of-run pose and clears any death state.</summary>
        void PrepareForRun();
    }
}
