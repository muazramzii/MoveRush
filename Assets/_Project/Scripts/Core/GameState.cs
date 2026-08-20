namespace MoveRush.Core
{
    /// <summary>
    /// High level application states handled by the <see cref="GameManager"/>.
    /// Values are explicit and are only ever appended: saved data and inspector references
    /// keep working when a new state joins the list.
    /// </summary>
    public enum GameState
    {
        /// <summary>Engine and core services are being initialised. Entry state.</summary>
        Boot = 0,

        /// <summary>Branding / legal splash presentation.</summary>
        Splash = 1,

        /// <summary>Main menu is active and awaiting player input.</summary>
        MainMenu = 2,

        /// <summary>An asynchronous scene load is in progress.</summary>
        Loading = 3,

        /// <summary>A run is active and the player is in control.</summary>
        Gameplay = 4,

        /// <summary>A run is suspended. Time scale is zero.</summary>
        Paused = 5,

        /// <summary>The run has ended and the result is being presented.</summary>
        GameOver = 6
    }
}
