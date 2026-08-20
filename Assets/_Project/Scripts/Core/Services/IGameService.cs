namespace MoveRush.Core.Services
{
    /// <summary>
    /// Contract implemented by every long lived system owned by the bootstrapper.
    /// Keeping initialisation explicit (instead of relying on Unity's Awake order) makes the
    /// start-up sequence deterministic, ordered and testable.
    /// </summary>
    public interface IGameService
    {
        /// <summary>
        /// Relative start-up order. Lower values are initialised first and shut down last.
        /// Services with an equal order are initialised in hierarchy order.
        /// </summary>
        int InitializationOrder { get; }

        /// <summary>True once <see cref="Initialize"/> has completed successfully.</summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Prepares the service for use. Implementations must be idempotent: calling it twice
        /// is a no-op rather than an error.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Releases resources, unsubscribes from events and flushes pending work.
        /// Called in reverse initialisation order when the application quits.
        /// </summary>
        void Shutdown();
    }
}
