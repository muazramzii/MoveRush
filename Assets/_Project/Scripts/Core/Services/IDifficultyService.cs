using System;

namespace MoveRush.Core.Services
{
    /// <summary>
    /// Authoritative pace of the run. It owns both the current speed and the distance travelled,
    /// so speed, scoring, spawn density and camera framing all read the same numbers instead of
    /// each deriving their own value from a transform position.
    /// </summary>
    public interface IDifficultyService
    {
        /// <summary>Current forward speed in meters per second.</summary>
        float CurrentSpeed { get; }

        /// <summary>Distance travelled during the current run, in meters.</summary>
        float Distance { get; }

        /// <summary>Zero based index of the active difficulty step.</summary>
        int Tier { get; }

        /// <summary>
        /// Speed expressed in the 0..1 range between the slowest and the fastest configured step.
        /// Presentation systems use this instead of raw speed, so re-tuning cannot break them.
        /// </summary>
        float NormalizedIntensity { get; }

        /// <summary>Raised when the speed changes, carrying the new speed.</summary>
        event Action<float> SpeedChanged;
    }
}
