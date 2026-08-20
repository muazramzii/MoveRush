using MoveRush.Core.Events;
using MoveRush.Core.Services;
using MoveRush.Gameplay.Config;
using MoveRush.Gameplay.Pooling;
using MoveRush.Gameplay.World;
using UnityEngine;

namespace MoveRush.Gameplay.Coins
{
    /// <summary>
    /// A single collectable. It owns its idle spin, the optional magnet attraction and the
    /// collection animation, and announces the pickup as an event - it never touches the score,
    /// the audio service or the profile itself.
    /// </summary>
    [DisallowMultipleComponent]
    public class Coin : MonoBehaviour, IPoolable
    {
        /// <summary>Lifecycle of a coin between being placed and being recycled.</summary>
        private enum CoinState
        {
            /// <summary>Spinning in place, waiting to be picked up.</summary>
            Idle = 0,

            /// <summary>Flying towards the character because the magnet is active.</summary>
            Attracting = 1,

            /// <summary>Playing the collection animation before returning to the pool.</summary>
            Collecting = 2
        }

        [Tooltip("Mesh that spins. Falls back to this transform.")]
        [SerializeField] private Transform visual;

        [Tooltip("Trigger used for pickup. Disabled while the collection animation plays.")]
        [SerializeField] private Collider pickupCollider;

        private RunnerConfig config;
        private IPlayerService player;
        private IPoolService poolService;
        private RoadTile owner;
        private CoinState state;
        private float collectTimer;
        private Vector3 baseScale;

        /// <summary>Caches the authored scale so the collection animation can restore it.</summary>
        private void Awake()
        {
            baseScale = Visual.localScale;
        }

        /// <summary>Transform that spins and scales.</summary>
        private Transform Visual => visual != null ? visual : transform;

        /// <summary>
        /// Injects everything the coin needs. Pooled objects cannot use constructors, so this is
        /// the seam where a freshly rented coin learns about its run.
        /// </summary>
        /// <param name="runnerConfig">Coin tuning values.</param>
        /// <param name="playerService">Character the magnet tracks.</param>
        /// <param name="pool">Service the coin returns itself to.</param>
        /// <param name="owningTile">Tile that holds this coin, notified when it is collected.</param>
        public void Configure(RunnerConfig runnerConfig, IPlayerService playerService, IPoolService pool, RoadTile owningTile)
        {
            config = runnerConfig;
            player = playerService;
            poolService = pool;
            owner = owningTile;
        }

        /// <inheritdoc />
        public void OnSpawnedFromPool()
        {
            state = CoinState.Idle;
            collectTimer = 0f;
            Visual.localScale = baseScale == Vector3.zero ? Vector3.one : baseScale;

            if (pickupCollider != null)
            {
                pickupCollider.enabled = true;
            }
        }

        /// <inheritdoc />
        public void OnReturnedToPool()
        {
            owner = null;
            state = CoinState.Idle;
        }

        /// <summary>Drives the spin, the magnet and the collection animation.</summary>
        private void Update()
        {
            switch (state)
            {
                case CoinState.Idle:
                    Spin();
                    TryAttract();
                    break;

                case CoinState.Attracting:
                    Spin();
                    MoveTowardsPlayer();
                    break;

                case CoinState.Collecting:
                    Animate();
                    break;
            }
        }

        /// <summary>Collects the coin when the character touches it.</summary>
        /// <param name="other">Collider that entered the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (state != CoinState.Collecting && other.CompareTag("Player"))
            {
                Collect();
            }
        }

        /// <summary>Rotates the visual at the configured speed.</summary>
        private void Spin()
        {
            float speed = config != null ? config.CoinSpinSpeed : 140f;
            Visual.Rotate(Vector3.up, speed * Time.deltaTime, Space.World);
        }

        /// <summary>
        /// Starts the magnet when the character is close enough. A radius of zero keeps the
        /// feature dormant, so the pickup is already wired for the upgrade without changing play.
        /// </summary>
        private void TryAttract()
        {
            if (config == null || player == null || config.MagnetRadius <= 0f || !player.IsAlive)
            {
                return;
            }

            if ((player.Position - transform.position).sqrMagnitude <= config.MagnetRadius * config.MagnetRadius)
            {
                state = CoinState.Attracting;
            }
        }

        /// <summary>Flies towards the character and collects on contact.</summary>
        private void MoveTowardsPlayer()
        {
            if (player == null)
            {
                state = CoinState.Idle;
                return;
            }

            Vector3 target = player.Position + Vector3.up;
            float speed = config != null ? config.MagnetSpeed : 14f;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if ((target - transform.position).sqrMagnitude <= 0.16f)
            {
                Collect();
            }
        }

        /// <summary>Announces the pickup and starts the collection animation.</summary>
        private void Collect()
        {
            if (state == CoinState.Collecting)
            {
                return;
            }

            state = CoinState.Collecting;
            collectTimer = 0f;

            if (pickupCollider != null)
            {
                pickupCollider.enabled = false;
            }

            int value = config != null ? config.CoinValue : 1;
            EventBus<CoinCollectedEvent>.Publish(new CoinCollectedEvent(transform.position, value));
        }

        /// <summary>Scales the coin up and out, then recycles it.</summary>
        private void Animate()
        {
            float duration = config != null ? config.CollectAnimationDuration : 0.22f;
            collectTimer += Time.deltaTime;

            float t = duration <= 0f ? 1f : Mathf.Clamp01(collectTimer / duration);
            Visual.localScale = baseScale * (1f + t) * (1f - t);
            transform.position += Vector3.up * (4f * Time.deltaTime);

            if (t >= 1f)
            {
                Despawn();
            }
        }

        /// <summary>Returns the coin to its pool and drops it from the owning tile.</summary>
        private void Despawn()
        {
            owner?.RemoveContent(this);
            owner = null;

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
