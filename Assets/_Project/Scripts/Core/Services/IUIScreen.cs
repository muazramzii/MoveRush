namespace MoveRush.Core.Services
{
    /// <summary>
    /// Contract every full-screen view implements so the UI manager can drive it without
    /// knowing the concrete view type. Actual screen layouts are intentionally out of scope
    /// for Phase 1 - this is the seam they will plug into.
    /// </summary>
    public interface IUIScreen
    {
        /// <summary>Stable identifier used to show or hide the screen.</summary>
        string ScreenId { get; }

        /// <summary>True while the screen is visible.</summary>
        bool IsVisible { get; }

        /// <summary>Called once when the screen registers itself with the UI manager.</summary>
        void Prepare();

        /// <summary>Makes the screen visible and gives it focus.</summary>
        void Show();

        /// <summary>Hides the screen and releases focus.</summary>
        void Hide();
    }
}
