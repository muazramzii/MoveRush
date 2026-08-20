using System;
using MoveRush.Core.Data;

namespace MoveRush.Core.Services
{
    /// <summary>
    /// Contract for the persistence layer. Consumers read and mutate <see cref="Current"/>
    /// and then request a write; they never touch the file system or a serialisation format,
    /// so the storage backend can later move to the cloud without touching call sites.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>The player profile currently held in memory. Never null after initialisation.</summary>
        PlayerData Current { get; }

        /// <summary>Raised after a profile has been loaded or newly created.</summary>
        event Action<PlayerData> Loaded;

        /// <summary>Raised after the profile has been written to disk successfully.</summary>
        event Action<PlayerData> Saved;

        /// <summary>
        /// Raised whenever the in-memory profile is modified through
        /// <see cref="Modify"/>, so views can refresh without polling.
        /// </summary>
        event Action<PlayerData> Changed;

        /// <summary>Reads the profile from storage, falling back to a fresh default profile.</summary>
        /// <returns>True when an existing profile was restored, false when defaults were created.</returns>
        bool Load();

        /// <summary>Writes the in-memory profile to storage.</summary>
        /// <returns>True when the write succeeded.</returns>
        bool Save();

        /// <summary>
        /// Applies a mutation to the in-memory profile, raises <see cref="Changed"/> and
        /// optionally writes immediately. Preferred over mutating <see cref="Current"/> directly.
        /// </summary>
        /// <param name="mutation">Action that changes the profile.</param>
        /// <param name="saveImmediately">When true the profile is written to storage at once.</param>
        void Modify(Action<PlayerData> mutation, bool saveImmediately = false);

        /// <summary>Deletes the stored profile and resets the in-memory profile to defaults.</summary>
        void DeleteSave();
    }
}
