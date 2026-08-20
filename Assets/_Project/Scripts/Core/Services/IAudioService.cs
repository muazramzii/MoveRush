using UnityEngine;

namespace MoveRush.Core.Services
{
    /// <summary>Mixer channels the audio service exposes to the rest of the game.</summary>
    public enum AudioChannel
    {
        /// <summary>Global output volume.</summary>
        Master = 0,

        /// <summary>Background music bed.</summary>
        Music = 1,

        /// <summary>One-shot sound effects.</summary>
        Sfx = 2
    }

    /// <summary>
    /// Contract for audio playback. Callers reference clips by identifier so gameplay code
    /// never holds direct asset references and the audio bank stays data-driven.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>Plays a music track by identifier, cross-fading from the current track.</summary>
        /// <param name="musicId">Identifier declared in the audio configuration asset.</param>
        /// <param name="loop">Whether the track should repeat.</param>
        void PlayMusic(string musicId, bool loop = true);

        /// <summary>Fades out and stops the current music track.</summary>
        void StopMusic();

        /// <summary>Plays a one-shot sound effect by identifier through the effect pool.</summary>
        /// <param name="sfxId">Identifier declared in the audio configuration asset.</param>
        /// <param name="volumeScale">Extra multiplier applied on top of the channel volume.</param>
        void PlaySfx(string sfxId, float volumeScale = 1f);

        /// <summary>Plays a clip directly, for cases where the clip is already resolved.</summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="volumeScale">Extra multiplier applied on top of the channel volume.</param>
        void PlaySfx(AudioClip clip, float volumeScale = 1f);

        /// <summary>Reads the linear 0..1 volume of a channel.</summary>
        /// <param name="channel">Channel to query.</param>
        /// <returns>Linear volume in the 0..1 range.</returns>
        float GetVolume(AudioChannel channel);

        /// <summary>
        /// Sets the linear 0..1 volume of a channel and persists it to the player profile.
        /// </summary>
        /// <param name="channel">Channel to change.</param>
        /// <param name="linearVolume">Linear volume in the 0..1 range.</param>
        void SetVolume(AudioChannel channel, float linearVolume);

        /// <summary>Mutes or unmutes all output without losing the individual channel volumes.</summary>
        /// <param name="muted">True to mute.</param>
        void SetMuted(bool muted);
    }
}
