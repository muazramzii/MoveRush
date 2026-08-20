using System;

namespace MoveRush.Core.Services
{
    /// <summary>
    /// Contract for the application state machine. Systems observe <see cref="StateChanged"/>
    /// and request transitions instead of calling each other, which keeps the flow event-driven
    /// and free of circular references between managers.
    /// </summary>
    public interface IGameStateService
    {
        /// <summary>State the application is currently in.</summary>
        GameState CurrentState { get; }

        /// <summary>State the application was in before the current one.</summary>
        GameState PreviousState { get; }

        /// <summary>True while a run is active or suspended.</summary>
        bool IsRunActive { get; }

        /// <summary>Raised after a transition, carrying the previous and the new state.</summary>
        event Action<GameState, GameState> StateChanged;

        /// <summary>
        /// Requests a transition. Illegal transitions are rejected and logged rather than
        /// throwing, so a mis-wired button can never crash a running build.
        /// </summary>
        /// <param name="nextState">State to move into.</param>
        /// <returns>True when the transition was accepted.</returns>
        bool RequestState(GameState nextState);

        /// <summary>Returns true when a transition from the current state is legal.</summary>
        /// <param name="nextState">State to test.</param>
        /// <returns>True when the transition is allowed.</returns>
        bool CanTransitionTo(GameState nextState);

        /// <summary>Loads the main menu scene through the loading state.</summary>
        void GoToMainMenu();

        /// <summary>Loads the gameplay scene and enters the gameplay state.</summary>
        void StartRun();

        /// <summary>
        /// Restarts immediately after a game over without reloading the scene. Every run system
        /// resets from its pool, which is what makes a retry instant.
        /// </summary>
        void RestartRun();

        /// <summary>Ends the active run and moves to the game over state.</summary>
        void EndRun();

        /// <summary>Suspends the active run and stops time.</summary>
        void PauseRun();

        /// <summary>Resumes a suspended run and restores time.</summary>
        void ResumeRun();
    }
}
