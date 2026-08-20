namespace MoveRush.Core.Services
{
    /// <summary>
    /// Storage abstraction used by the save service. It only moves raw text in and out of a
    /// backing store, which separates "where bytes live" from "what the bytes mean"
    /// (Single Responsibility). A cloud or encrypted repository can be dropped in later.
    /// </summary>
    public interface ISaveRepository
    {
        /// <summary>Absolute location of the backing store, for diagnostics and support tooling.</summary>
        string Location { get; }

        /// <summary>Returns true when a persisted payload exists.</summary>
        /// <returns>True when data is present.</returns>
        bool Exists();

        /// <summary>Reads the persisted payload.</summary>
        /// <param name="payload">Raw text that was stored, or null on failure.</param>
        /// <returns>True when a payload was read successfully.</returns>
        bool TryRead(out string payload);

        /// <summary>Writes the payload, replacing any existing data atomically.</summary>
        /// <param name="payload">Raw text to persist.</param>
        /// <returns>True when the write succeeded.</returns>
        bool Write(string payload);

        /// <summary>Deletes the persisted payload if it exists.</summary>
        /// <returns>True when data was deleted or nothing was stored.</returns>
        bool Delete();
    }
}
