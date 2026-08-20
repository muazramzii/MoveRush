using MoveRush.Core.Config;
using MoveRush.Core.Data;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using MoveRush.Managers.Audio;
using UnityEngine;

namespace MoveRush.Managers
{
    /// <summary>
    /// Audio service and volume policy. It resolves clips from <see cref="AudioConfig"/> by
    /// identifier, delegates playback to <see cref="MusicPlayer"/> and <see cref="AudioSourcePool"/>,
    /// and keeps the mix in sync with the volumes stored in the player profile.
    /// Routing through an audio mixer is optional, so the project works before the mixer asset
    /// exists and upgrades to it without a code change.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour, IAudioService, IGameService
    {
        private IConfigProvider configProvider;
        private ISaveService saveService;
        private AudioConfig config;
        private MusicPlayer musicPlayer;
        private AudioSourcePool sfxPool;

        /// <inheritdoc />
        public int InitializationOrder => 10;

        /// <inheritdoc />
        public bool IsInitialized { get; private set; }

        /// <inheritdoc />
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            ServiceLocator.TryGet(out configProvider);
            ServiceLocator.TryGet(out saveService);
            config = configProvider?.Audio;

            if (config == null)
            {
                Log.Error("AudioManager: no AudioConfig is assigned, audio playback is disabled.", this);
                return;
            }

            musicPlayer = gameObject.AddComponent<MusicPlayer>();
            musicPlayer.Initialize(config);
            sfxPool = new AudioSourcePool(transform, config.SfxPoolSize);

            if (saveService != null)
            {
                saveService.Loaded += OnProfileChanged;
                saveService.Changed += OnProfileChanged;
            }

            ApplyVolumes();
            ServiceLocator.Register<IAudioService>(this);

            IsInitialized = true;
            Log.Info("AudioManager initialised.", this);
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            if (saveService != null)
            {
                saveService.Loaded -= OnProfileChanged;
                saveService.Changed -= OnProfileChanged;
            }

            musicPlayer?.Stop();
            sfxPool?.StopAll();

            ServiceLocator.Unregister<IAudioService>();
            IsInitialized = false;
        }

        /// <inheritdoc />
        public void PlayMusic(string musicId, bool loop = true)
        {
            AudioEntry entry = config != null ? config.FindMusic(musicId) : null;
            if (entry?.Clip == null)
            {
                Log.Warning($"AudioManager: unknown music id '{musicId}'.", this);
                return;
            }

            musicPlayer.SetChannelVolume(GetEffectiveVolume(AudioChannel.Music));
            musicPlayer.Play(entry.Clip, loop, entry.Volume);
        }

        /// <inheritdoc />
        public void StopMusic() => musicPlayer?.Stop();

        /// <inheritdoc />
        public void PlaySfx(string sfxId, float volumeScale = 1f)
        {
            AudioEntry entry = config != null ? config.FindSfx(sfxId) : null;
            if (entry?.Clip == null)
            {
                Log.Warning($"AudioManager: unknown sfx id '{sfxId}'.", this);
                return;
            }

            PlaySfx(entry.Clip, volumeScale * entry.Volume);
        }

        /// <inheritdoc />
        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxPool == null)
            {
                return;
            }

            AudioSource source = sfxPool.Rent();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volumeScale) * GetEffectiveVolume(AudioChannel.Sfx);
            source.Play();
        }

        /// <inheritdoc />
        public float GetVolume(AudioChannel channel)
        {
            PlayerSettingsData settings = saveService?.Current?.Settings;
            if (settings == null)
            {
                return 1f;
            }

            switch (channel)
            {
                case AudioChannel.Music:
                    return settings.MusicVolume;
                case AudioChannel.Sfx:
                    return settings.SfxVolume;
                default:
                    return settings.MasterVolume;
            }
        }

        /// <inheritdoc />
        public void SetVolume(AudioChannel channel, float linearVolume)
        {
            float clamped = Mathf.Clamp01(linearVolume);

            saveService?.Modify(data =>
            {
                switch (channel)
                {
                    case AudioChannel.Music:
                        data.Settings.MusicVolume = clamped;
                        break;
                    case AudioChannel.Sfx:
                        data.Settings.SfxVolume = clamped;
                        break;
                    default:
                        data.Settings.MasterVolume = clamped;
                        break;
                }
            });

            ApplyVolumes();
        }

        /// <inheritdoc />
        public void SetMuted(bool muted)
        {
            saveService?.Modify(data => data.Settings.Muted = muted);
            ApplyVolumes();
        }

        /// <summary>
        /// Pushes the stored volumes to the mixer when one is configured, and always keeps the
        /// music player in sync so playback is correct with or without a mixer.
        /// </summary>
        private void ApplyVolumes()
        {
            if (config == null)
            {
                return;
            }

            if (config.Mixer != null)
            {
                config.Mixer.SetFloat(config.MasterVolumeParameter, ToDecibels(AudioChannel.Master));
                config.Mixer.SetFloat(config.MusicVolumeParameter, ToDecibels(AudioChannel.Music));
                config.Mixer.SetFloat(config.SfxVolumeParameter, ToDecibels(AudioChannel.Sfx));
            }

            musicPlayer?.SetChannelVolume(config.Mixer != null
                ? 1f
                : GetEffectiveVolume(AudioChannel.Music));
        }

        /// <summary>Converts a channel volume into the decibel value the mixer expects.</summary>
        /// <param name="channel">Channel to convert.</param>
        /// <returns>Volume expressed in decibels.</returns>
        private float ToDecibels(AudioChannel channel)
        {
            float linear = channel == AudioChannel.Master
                ? GetVolume(AudioChannel.Master) * (IsMuted ? 0f : 1f)
                : GetVolume(channel);

            return config.LinearToDecibels(linear);
        }

        /// <summary>
        /// Channel volume after the master trim and the mute flag are applied. Used when no
        /// mixer is configured and the volume has to be baked into the source.
        /// </summary>
        /// <param name="channel">Channel to evaluate.</param>
        /// <returns>Linear volume in the 0..1 range.</returns>
        private float GetEffectiveVolume(AudioChannel channel)
        {
            if (IsMuted)
            {
                return 0f;
            }

            return config != null && config.Mixer != null
                ? GetVolume(channel)
                : GetVolume(AudioChannel.Master) * GetVolume(channel);
        }

        /// <summary>True when the profile has audio muted.</summary>
        private bool IsMuted => saveService?.Current?.Settings?.Muted ?? false;

        /// <summary>Re-applies the mix whenever the player profile is loaded or changed.</summary>
        /// <param name="data">Profile that was loaded or modified.</param>
        private void OnProfileChanged(PlayerData data) => ApplyVolumes();
    }
}
