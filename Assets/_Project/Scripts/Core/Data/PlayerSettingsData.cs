using System;
using UnityEngine;

namespace MoveRush.Core.Data
{
    /// <summary>
    /// Serialisable player preferences stored inside the player profile.
    /// Defaults are supplied by the audio and game configuration assets at creation time,
    /// never hard-coded at the call site.
    /// </summary>
    [Serializable]
    public class PlayerSettingsData
    {
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 1f;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private bool muted;
        [SerializeField] private bool vibrationEnabled = true;
        [SerializeField] private int qualityLevel;
        [SerializeField] private string languageCode = "en";

        /// <summary>Linear master volume in the 0..1 range.</summary>
        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = Mathf.Clamp01(value);
        }

        /// <summary>Linear music volume in the 0..1 range.</summary>
        public float MusicVolume
        {
            get => musicVolume;
            set => musicVolume = Mathf.Clamp01(value);
        }

        /// <summary>Linear sound effect volume in the 0..1 range.</summary>
        public float SfxVolume
        {
            get => sfxVolume;
            set => sfxVolume = Mathf.Clamp01(value);
        }

        /// <summary>True when all audio output is muted.</summary>
        public bool Muted
        {
            get => muted;
            set => muted = value;
        }

        /// <summary>True when haptic feedback is allowed on supported devices.</summary>
        public bool VibrationEnabled
        {
            get => vibrationEnabled;
            set => vibrationEnabled = value;
        }

        /// <summary>Index into Unity's quality settings levels.</summary>
        public int QualityLevel
        {
            get => qualityLevel;
            set => qualityLevel = Mathf.Max(0, value);
        }

        /// <summary>ISO 639-1 language code used for localisation.</summary>
        public string LanguageCode
        {
            get => languageCode;
            set => languageCode = string.IsNullOrWhiteSpace(value) ? "en" : value;
        }

        /// <summary>Creates a settings block seeded from configuration defaults.</summary>
        /// <param name="defaultMaster">Default linear master volume.</param>
        /// <param name="defaultMusic">Default linear music volume.</param>
        /// <param name="defaultSfx">Default linear sound effect volume.</param>
        /// <param name="defaultQualityLevel">Default quality settings index.</param>
        /// <param name="defaultLanguageCode">Default ISO 639-1 language code.</param>
        /// <returns>A populated settings instance.</returns>
        public static PlayerSettingsData CreateDefault(
            float defaultMaster,
            float defaultMusic,
            float defaultSfx,
            int defaultQualityLevel,
            string defaultLanguageCode)
        {
            return new PlayerSettingsData
            {
                MasterVolume = defaultMaster,
                MusicVolume = defaultMusic,
                SfxVolume = defaultSfx,
                Muted = false,
                VibrationEnabled = true,
                QualityLevel = defaultQualityLevel,
                LanguageCode = defaultLanguageCode
            };
        }
    }
}
