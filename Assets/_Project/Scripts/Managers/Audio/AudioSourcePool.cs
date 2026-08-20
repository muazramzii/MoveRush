using UnityEngine;

namespace MoveRush.Managers.Audio
{
    /// <summary>
    /// Fixed-size ring of reusable audio sources for one-shot sound effects.
    /// Pooling replaces the per-shot GameObject that <c>AudioSource.PlayClipAtPoint</c> creates,
    /// which keeps effect playback allocation-free on mobile hardware.
    /// </summary>
    public class AudioSourcePool
    {
        private readonly AudioSource[] sources;
        private int nextIndex;

        /// <summary>Creates the pool and its child sources.</summary>
        /// <param name="parent">Transform the pooled sources are parented to.</param>
        /// <param name="size">Number of sources, clamped to at least one.</param>
        public AudioSourcePool(Transform parent, int size)
        {
            int count = Mathf.Max(1, size);
            sources = new AudioSource[count];

            for (int i = 0; i < count; i++)
            {
                GameObject holder = new GameObject($"Sfx_{i:00}");
                holder.transform.SetParent(parent, false);

                AudioSource source = holder.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                sources[i] = source;
            }
        }

        /// <summary>Number of sources in the pool.</summary>
        public int Size => sources.Length;

        /// <summary>
        /// Returns the next available source. When every source is busy the oldest one is
        /// recycled, so a burst of effects steals the longest-running voice instead of dropping
        /// the new sound entirely.
        /// </summary>
        /// <returns>A source ready to play a clip.</returns>
        public AudioSource Rent()
        {
            for (int i = 0; i < sources.Length; i++)
            {
                int index = (nextIndex + i) % sources.Length;
                if (!sources[index].isPlaying)
                {
                    nextIndex = (index + 1) % sources.Length;
                    return sources[index];
                }
            }

            AudioSource recycled = sources[nextIndex];
            nextIndex = (nextIndex + 1) % sources.Length;
            return recycled;
        }

        /// <summary>Stops every pooled source immediately.</summary>
        public void StopAll()
        {
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i].Stop();
            }
        }
    }
}
