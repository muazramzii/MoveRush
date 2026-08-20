namespace MoveRush.Core.Events
{
    /// <summary>
    /// Raised on the <see cref="EventBus{T}"/> every time the application state changes.
    /// Immutable value type so subscribers can never mutate the broadcast payload.
    /// </summary>
    public readonly struct GameStateChangedEvent
    {
        /// <summary>State the application just left.</summary>
        public GameState Previous { get; }

        /// <summary>State the application has just entered.</summary>
        public GameState Current { get; }

        /// <summary>Creates a new state transition payload.</summary>
        /// <param name="previous">State that was active before the transition.</param>
        /// <param name="current">State that is active after the transition.</param>
        public GameStateChangedEvent(GameState previous, GameState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
