using System;

namespace MoveRush.Core.Services
{
    /// <summary>
    /// Source of player intent. The character never reads a keyboard, a touch screen or a pose
    /// estimator directly - it consumes <see cref="InputCommand"/> values from whichever provider
    /// is registered. Replacing keyboard control with the Phase 3 pose input is therefore a swap
    /// of one component on the bootstrap root, with no gameplay code touched.
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>True while the provider is allowed to emit commands.</summary>
        bool IsEnabled { get; }

        /// <summary>Raised once per issued command.</summary>
        event Action<InputCommand> CommandIssued;

        /// <summary>
        /// Enables or suppresses command emission. Input is suppressed outside a run so a menu
        /// key press cannot make the character jump.
        /// </summary>
        /// <param name="enabled">True to allow commands.</param>
        void SetEnabled(bool enabled);
    }
}
