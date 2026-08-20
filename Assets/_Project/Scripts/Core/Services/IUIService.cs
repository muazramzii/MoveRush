namespace MoveRush.Core.Services
{
    /// <summary>
    /// Contract for the UI layer. Screens register themselves at runtime and are addressed by
    /// identifier, so systems can request a view without holding a reference to a prefab or a
    /// concrete component. Phase 1 only guarantees the loading overlay behaviour.
    /// </summary>
    public interface IUIService
    {
        /// <summary>Identifier of the screen currently shown, or null when none is active.</summary>
        string ActiveScreenId { get; }

        /// <summary>Adds a screen to the registry and prepares it. Safe to call from Awake.</summary>
        /// <param name="screen">Screen instance to register.</param>
        void RegisterScreen(IUIScreen screen);

        /// <summary>Removes a screen from the registry, hiding it first when it is visible.</summary>
        /// <param name="screen">Screen instance to remove.</param>
        void UnregisterScreen(IUIScreen screen);

        /// <summary>Shows a registered screen and hides the previously active one.</summary>
        /// <param name="screenId">Identifier of the screen to show.</param>
        /// <returns>True when the screen was found and shown.</returns>
        bool ShowScreen(string screenId);

        /// <summary>Hides a registered screen.</summary>
        /// <param name="screenId">Identifier of the screen to hide.</param>
        /// <returns>True when the screen was found and hidden.</returns>
        bool HideScreen(string screenId);

        /// <summary>Shows or hides the loading overlay that covers asynchronous scene loads.</summary>
        /// <param name="visible">True to show the overlay.</param>
        void SetLoadingOverlayVisible(bool visible);
    }
}
