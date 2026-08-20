using MoveRush.Gameplay.Pooling;
using UnityEngine;

namespace MoveRush.Gameplay.Presentation
{
    /// <summary>
    /// A short lived visual burst that returns itself to the pool when it finishes. Effects are
    /// the most frequently spawned objects in a runner, so they are pooled exactly like coins
    /// and obstacles rather than instantiated per event.
    /// </summary>
    [DisallowMultipleComponent]
    public class PooledEffect : MonoBehaviour, IPoolable
    {
        [Tooltip("Seconds the effect stays alive before recycling itself.")]
        [SerializeField, Min(0.05f)] private float lifetime = 0.5f;

        [Tooltip("Optional particle system played on spawn.")]
        [SerializeField] private ParticleSystem particles;

        [Tooltip("Optional transform scaled up over the lifetime.")]
        [SerializeField] private Transform scaleRoot;

        [Tooltip("Scale reached at the end of the lifetime.")]
        [SerializeField] private float endScale = 2.2f;

        private IPoolService poolService;
        private float timer;
        private Vector3 baseScale;

        /// <summary>Caches the authored scale of the visual.</summary>
        private void Awake()
        {
            baseScale = scaleRoot != null ? scaleRoot.localScale : Vector3.one;
        }

        /// <summary>Positions the effect and starts it.</summary>
        /// <param name="position">World position to play at.</param>
        /// <param name="pool">Service the effect returns itself to.</param>
        public void Play(Vector3 position, IPoolService pool)
        {
            poolService = pool;
            transform.position = position;
            timer = 0f;

            if (particles != null)
            {
                particles.Clear(true);
                particles.Play(true);
            }
        }

        /// <inheritdoc />
        public void OnSpawnedFromPool()
        {
            timer = 0f;

            if (scaleRoot != null)
            {
                scaleRoot.localScale = baseScale;
            }
        }

        /// <inheritdoc />
        public void OnReturnedToPool()
        {
            if (particles != null)
            {
                particles.Stop(true);
            }
        }

        /// <summary>Runs the fade and recycles the effect when the lifetime is over.</summary>
        private void Update()
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / lifetime);

            if (scaleRoot != null)
            {
                scaleRoot.localScale = baseScale * Mathf.Lerp(1f, endScale, t);
            }

            if (t < 1f)
            {
                return;
            }

            if (poolService != null)
            {
                poolService.Return(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
