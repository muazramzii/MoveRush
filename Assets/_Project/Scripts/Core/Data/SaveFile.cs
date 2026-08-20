using System;
using UnityEngine;

namespace MoveRush.Core.Data
{
    /// <summary>
    /// Envelope written to disk around the player profile. Storing a schema version and a
    /// timestamp next to the payload is what makes future save migrations possible instead of
    /// forcing a wipe when the profile shape changes.
    /// </summary>
    [Serializable]
    public class SaveFile
    {
        /// <summary>Schema version understood by the current build.</summary>
        public const int CurrentVersion = 1;

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private string savedAtUtc = string.Empty;
        [SerializeField] private string applicationVersion = string.Empty;
        [SerializeField] private PlayerData player = new PlayerData();

        /// <summary>Schema version the payload was written with.</summary>
        public int Version
        {
            get => version;
            set => version = value;
        }

        /// <summary>Round-trip formatted UTC timestamp of the last successful write.</summary>
        public string SavedAtUtc
        {
            get => savedAtUtc;
            set => savedAtUtc = value;
        }

        /// <summary>Application version that produced the file, useful for support tickets.</summary>
        public string ApplicationVersion
        {
            get => applicationVersion;
            set => applicationVersion = value;
        }

        /// <summary>The serialised player profile. Never null.</summary>
        public PlayerData Player
        {
            get => player ??= new PlayerData();
            set => player = value ?? new PlayerData();
        }

        /// <summary>Wraps a profile in a stamped envelope ready to be written.</summary>
        /// <param name="data">Profile to wrap.</param>
        /// <returns>A populated envelope.</returns>
        public static SaveFile Wrap(PlayerData data)
        {
            return new SaveFile
            {
                Version = CurrentVersion,
                SavedAtUtc = DateTime.UtcNow.ToString("O"),
                ApplicationVersion = Application.version,
                Player = data
            };
        }
    }
}
