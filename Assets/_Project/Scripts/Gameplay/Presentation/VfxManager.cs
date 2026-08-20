using MoveRush.Core.Events;
using MoveRush.Core.Services;
using MoveRush.Gameplay.Config;
using MoveRush.Gameplay.Pooling;
using MoveRush.Gameplay.Run;
using UnityEngine;

namespace MoveRush.Gameplay.Presentation
{
    /// <summary>
    /// Turns gameplay events into pooled visual bursts. It exists so the coin and the obstacle
    /// stay concerned with rules only: they announce what happened, and presentation is decided
    /// in one place that art can re-skin without touching gameplay code.
    /// </summary>
    [DisallowMultipleComponent]
    public class VfxManager : MonoBehaviour, IRunSystem
    {
        [Header("Effect Prefabs")]
        [Tooltip("Played where a coin is collected.")]
        [SerializeField] private PooledEffect coinEffect;

        [Tooltip("Played where the character hits an obstacle.")]
        [SerializeField] private PooledEffect hitEffect;

        [Tooltip("Played where the character lands after a jump.")]
        [SerializeField] private PooledEffect landEffect;

        [Header("Configuration")]
        [Tooltip("Supplies the prewarm count for the effect pools.")]
        [SerializeField] private RunnerConfig runnerConfig;

        private IPoolService poolService;

        /// <inheritdoc />
        public int RunOrder => 30;

        /// <summary>Subscribes to the events that produce an effect.</summary>
        private void Awake()
        {
            EventBus<CoinCollectedEvent>.Subscribe(OnCoinCollected);
            EventBus<ObstacleHitEvent>.Subscribe(OnObstacleHit);
            EventBus<PlayerLandedEvent>.Subscribe(OnPlayerLanded);
        }

        /// <summary>Prewarms one pool per effect prefab.</summary>
        private void Start()
        {
            ServiceLocator.TryGet(out poolService);

            int prewarm = runnerConfig != null ? runnerConfig.EffectPrewarmCount : 8;
            Prewarm(coinEffect, prewarm);
            Prewarm(hitEffect, 2);
            Prewarm(landEffect, 4);
        }

        /// <summary>Releases every subscription with the scene.</summary>
        private void OnDestroy()
        {
            EventBus<CoinCollectedEvent>.Unsubscribe(OnCoinCollected);
            EventBus<ObstacleHitEvent>.Unsubscribe(OnObstacleHit);
            EventBus<PlayerLandedEvent>.Unsubscribe(OnPlayerLanded);
        }

        /// <inheritdoc />
        public void OnRunReset()
        {
        }

        /// <inheritdoc />
        public void OnRunEnded()
        {
        }

        /// <summary>Creates instances of an effect up front.</summary>
        /// <param name="prefab">Effect prefab.</param>
        /// <param name="count">Instances to create.</param>
        private void Prewarm(PooledEffect prefab, int count)
        {
            if (prefab != null)
            {
                poolService?.Prewarm(prefab, count);
            }
        }

        /// <summary>Rents an effect and plays it at a position.</summary>
        /// <param name="prefab">Effect prefab.</param>
        /// <param name="position">World position to play at.</param>
        private void Spawn(PooledEffect prefab, Vector3 position)
        {
            if (prefab == null || poolService == null)
            {
                return;
            }

            PooledEffect effect = poolService.Rent(prefab);
            effect?.Play(position, poolService);
        }

        /// <summary>Plays the coin burst.</summary>
        /// <param name="payload">Coin payload.</param>
        private void OnCoinCollected(CoinCollectedEvent payload) => Spawn(coinEffect, payload.Position);

        /// <summary>Plays the impact burst.</summary>
        /// <param name="payload">Hit payload.</param>
        private void OnObstacleHit(ObstacleHitEvent payload) => Spawn(hitEffect, payload.Position);

        /// <summary>Plays the landing puff.</summary>
        /// <param name="payload">Landing payload.</param>
        private void OnPlayerLanded(PlayerLandedEvent payload) => Spawn(landEffect, payload.Position);
    }
}
