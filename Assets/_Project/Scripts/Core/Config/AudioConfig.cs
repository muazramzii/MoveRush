using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace MoveRush.Core.Config
{
    /// <summary>
    /// One named entry in the audio bank. Systems request clips by identifier so no gameplay
    /// script ever holds a direct asset reference.
    /// </summary>
    [Serializable]
    public class AudioEntry
    {
        [Tooltip("Identifier used by callers, for example 'ui_click' or 'menu_theme'.")]
        [SerializeField] private string id = string.Empty;

        [Tooltip("Clip played for this identifier.")]
        [SerializeField] private AudioClip clip;

        [Tooltip("Per-clip volume trim applied on top of the channel volume.")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        /// <summary>Identifier used by callers.</summary>
        public string Id => id;

        /// <summary>Clip played for this identifier.</summary>
        public AudioClip Clip => clip;

        /// <summary>Per-clip volume trim in the 0..1 range.</summary>
        public float Volume => volume;
    }

    /// <summary>
    /// Audio bank and mixer routing configuration. Holds the default volumes used to seed a new
    /// player profile, the exposed mixer parameter names and the clip catalogue.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "MoveRush/Config/Audio Config", order = 1)]
    public class AudioConfig : ScriptableObject
    {
        [Header("Mixer")]
        [Tooltip("Optional mixer. When empty, volumes are applied directly to the audio sources.")]
        [SerializeField] private AudioMixer mixer;

        [Tooltip("Exposed mixer parameter controlling the master bus.")]
        [SerializeField] private string masterVolumeParameter = "MasterVolume";

        [Tooltip("Exposed mixer parameter controlling the music bus.")]
        [SerializeField] private string musicVolumeParameter = "MusicVolume";

        [Tooltip("Exposed mixer parameter controlling the sound effect bus.")]
        [SerializeField] private string sfxVolumeParameter = "SfxVolume";

        [Header("Defaults")]
        [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 1f;

        [Tooltip("Decibel value treated as silence when converting a linear volume for the mixer.")]
        [SerializeField] private float minimumDecibels = -80f;

        [Header("Playback")]
        [Tooltip("Number of pooled sources available for overlapping sound effects.")]
        [SerializeField, Range(1, 32)] private int sfxPoolSize = 8;

        [Tooltip("Seconds used to cross-fade between two music tracks.")]
        [SerializeField, Range(0f, 5f)] private float musicFadeDuration = 0.75f;

        [Header("Bank")]
        [SerializeField] private List<AudioEntry> musicEntries = new List<AudioEntry>();
        [SerializeField] private List<AudioEntry> sfxEntries = new List<AudioEntry>();

        /// <summary>Optional audio mixer used for bus routing.</summary>
        public AudioMixer Mixer => mixer;

        /// <summary>Exposed mixer parameter for the master bus.</summary>
        public string MasterVolumeParameter => masterVolumeParameter;

        /// <summary>Exposed mixer parameter for the music bus.</summary>
        public string MusicVolumeParameter => musicVolumeParameter;

        /// <summary>Exposed mixer parameter for the sound effect bus.</summary>
        public string SfxVolumeParameter => sfxVolumeParameter;

        /// <summary>Default linear master volume for a new profile.</summary>
        public float DefaultMasterVolume => defaultMasterVolume;

        /// <summary>Default linear music volume for a new profile.</summary>
        public float DefaultMusicVolume => defaultMusicVolume;

        /// <summary>Default linear sound effect volume for a new profile.</summary>
        public float DefaultSfxVolume => defaultSfxVolume;

        /// <summary>Decibel value that represents silence.</summary>
        public float MinimumDecibels => minimumDecibels;

        /// <summary>Number of pooled sound effect sources.</summary>
        public int SfxPoolSize => sfxPoolSize;

        /// <summary>Seconds used to cross-fade music tracks.</summary>
        public float MusicFadeDuration => musicFadeDuration;

        /// <summary>Looks up a music entry by identifier.</summary>
        /// <param name="id">Identifier declared in the bank.</param>
        /// <returns>The matching entry, or null when the identifier is unknown.</returns>
        public AudioEntry FindMusic(string id) => Find(musicEntries, id);

        /// <summary>Looks up a sound effect entry by identifier.</summary>
        /// <param name="id">Identifier declared in the bank.</param>
        /// <returns>The matching entry, or null when the identifier is unknown.</returns>
        public AudioEntry FindSfx(string id) => Find(sfxEntries, id);

        /// <summary>
        /// Converts a linear 0..1 volume into the logarithmic decibel scale the mixer expects.
        /// </summary>
        /// <param name="linearVolume">Linear volume in the 0..1 range.</param>
        /// <returns>Volume expressed in decibels.</returns>
        public float LinearToDecibels(float linearVolume)
        {
            float clamped = Mathf.Clamp(linearVolume, 0.0001f, 1f);
            return linearVolume <= 0.0001f ? minimumDecibels : Mathf.Log10(clamped) * 20f;
        }

        private static AudioEntry Find(List<AudioEntry> entries, string id)
        {
            if (entries == null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && string.Equals(entries[i].Id, id, StringComparison.Ordinal))
                {
                    return entries[i];
                }
            }

            return null;
        }
    }
}
