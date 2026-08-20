using System.Collections;
using MoveRush.Core.Config;
using UnityEngine;

namespace MoveRush.Managers.Audio
{
    /// <summary>
    /// Two-source music player that cross-fades between tracks. Keeping the fade logic here
    /// leaves <see cref="AudioManager"/> responsible only for routing and volume policy.
    /// </summary>
    [DisallowMultipleComponent]
    public class MusicPlayer : MonoBehaviour
    {
        private AudioSource primary;
        private AudioSource secondary;
        private AudioConfig config;
        private Coroutine fadeRoutine;
        private float channelVolume = 1f;
        private float clipTrim = 1f;

        /// <summary>Clip currently assigned to the active source, or null when idle.</summary>
        public AudioClip CurrentClip => primary != null ? primary.clip : null;

        /// <summary>True while a cross-fade is running.</summary>
        public bool IsFading => fadeRoutine != null;

        /// <summary>Creates the two music sources. Call once before any playback.</summary>
        /// <param name="audioConfig">Configuration providing the fade duration.</param>
        public void Initialize(AudioConfig audioConfig)
        {
            config = audioConfig;
            primary = CreateSource("Music_A");
            secondary = CreateSource("Music_B");
        }

        /// <summary>Cross-fades from the current track to a new one.</summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="loop">Whether the new track repeats.</param>
        /// <param name="trim">Per-clip volume trim declared in the audio bank.</param>
        public void Play(AudioClip clip, bool loop, float trim)
        {
            if (clip == null || primary == null)
            {
                return;
            }

            if (primary.isPlaying && primary.clip == clip)
            {
                return;
            }

            StopFade();
            clipTrim = Mathf.Clamp01(trim);
            fadeRoutine = StartCoroutine(CrossFadeRoutine(clip, loop));
        }

        /// <summary>Stops both sources immediately and cancels any running fade.</summary>
        public void Stop()
        {
            StopFade();

            if (primary != null)
            {
                primary.Stop();
            }

            if (secondary != null)
            {
                secondary.Stop();
            }
        }

        /// <summary>
        /// Sets the channel volume the active track plays at. Ignored while a fade is running,
        /// because the fade owns the volume until it completes.
        /// </summary>
        /// <param name="linearVolume">Linear volume in the 0..1 range.</param>
        public void SetChannelVolume(float linearVolume)
        {
            channelVolume = Mathf.Clamp01(linearVolume);

            if (!IsFading && primary != null)
            {
                primary.volume = channelVolume * clipTrim;
            }
        }

        /// <summary>Fades the outgoing track down while the incoming track fades up.</summary>
        /// <param name="clip">Clip to fade in.</param>
        /// <param name="loop">Whether the new track repeats.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator CrossFadeRoutine(AudioClip clip, bool loop)
        {
            AudioSource outgoing = primary;
            AudioSource incoming = secondary;

            incoming.clip = clip;
            incoming.loop = loop;
            incoming.volume = 0f;
            incoming.Play();

            float target = channelVolume * clipTrim;
            float duration = Mathf.Max(0.01f, config != null ? config.MusicFadeDuration : 0.5f);
            float outgoingStart = outgoing.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                incoming.volume = Mathf.Lerp(0f, target, t);
                outgoing.volume = Mathf.Lerp(outgoingStart, 0f, t);
                yield return null;
            }

            outgoing.Stop();
            incoming.volume = target;

            primary = incoming;
            secondary = outgoing;
            fadeRoutine = null;
        }

        /// <summary>Cancels a running fade without touching playback state.</summary>
        private void StopFade()
        {
            if (fadeRoutine == null)
            {
                return;
            }

            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        /// <summary>Creates a looping child source with playback disabled on awake.</summary>
        /// <param name="sourceName">Name of the child object.</param>
        /// <returns>The created source.</returns>
        private AudioSource CreateSource(string sourceName)
        {
            GameObject holder = new GameObject(sourceName);
            holder.transform.SetParent(transform, false);

            AudioSource source = holder.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }
    }
}
