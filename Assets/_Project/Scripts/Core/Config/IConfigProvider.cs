namespace MoveRush.Core.Config
{
    /// <summary>
    /// Read-only access to the configuration assets wired into the bootstrap scene.
    /// Services resolve this contract instead of holding their own inspector references,
    /// which guarantees every system reads the exact same tuning data.
    /// </summary>
    public interface IConfigProvider
    {
        /// <summary>Global application settings.</summary>
        GameConfig Game { get; }

        /// <summary>Audio bank, mixer routing and default volumes.</summary>
        AudioConfig Audio { get; }

        /// <summary>Player defaults and the progression curve.</summary>
        PlayerConfig Player { get; }

        /// <summary>Lane positions and tile length shared by the player and the world.</summary>
        TrackConfig Track { get; }
    }
}
