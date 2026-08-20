using MoveRush.Core.Events;
using MoveRush.Core.Services;
using MoveRush.Gameplay.Pooling;
using UnityEngine;

namespace MoveRush.Gameplay.Obstacles
{
    /// <summary>How the character is expected to get past an obstacle.</summary>
    public enum ObstacleAvoidance
    {
        /// <summary>Only a lane change gets past it.</summary>
        Dodge = 0,

        /// <summary>A jump or a lane change gets past it.</summary>
        Jump = 1,

        /// <summary>A slide or a lane change gets past it.</summary>
        Slide = 2
    }

    /// <summary>
    /// Shared behaviour of every obstacle. The obstacle decides whether a pass counts as a hit,
    /// because only it knows its own rule, and it reads the character through
    /// <see cref="IPlayerService"/> so the gameplay assembly never references the player assembly.
    /// Outcomes are announced as events; the obstacle never touches score, audio or the flow.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class ObstacleBase : MonoBehaviour, IPoolable
    {
        [Header("Identity")]
        [Tooltip("Identifier used in events, analytics and logs.")]
        [SerializeField] private string obstacleId = "obstacle";

        [Tooltip("Length along Z in meters. Used to reserve the lane it occupies.")]
        [SerializeField, Min(0.5f)] private float lengthMeters = 2f;

        [Tooltip("Meters past the obstacle at which a pass counts as cleared.")]
        [SerializeField, Min(0f)] private float clearOffset = 1.5f;

        private IPlayerService player;
        private bool resolved;

        /// <summary>Identifier used in events and logs.</summary>
        public string ObstacleId => obstacleId;

        /// <summary>Length along Z in meters.</summary>
        public float LengthMeters => lengthMeters;

        /// <summary>How the character is expected to get past this obstacle.</summary>
        public abstract ObstacleAvoidance Avoidance { get; }

        /// <inheritdoc />
        public void OnSpawnedFromPool()
        {
            resolved = false;

            if (player == null)
            {
                ServiceLocator.TryGet(out player);
            }

            OnPrepared();
        }

        /// <inheritdoc />
        public void OnReturnedToPool()
        {
            resolved = true;
        }

        /// <summary>Hook for subclasses that need to reset visuals when reused.</summary>
        protected virtual void OnPrepared()
        {
        }

        /// <summary>
        /// Decides whether the character state gets it past this obstacle. Implemented per type
        /// so a new obstacle adds a rule instead of extending a switch somewhere else.
        /// </summary>
        /// <param name="playerService">Character being tested.</param>
        /// <returns>True when the obstacle does not hit.</returns>
        protected abstract bool IsAvoidedBy(IPlayerService playerService);

        /// <summary>Kills the character unless its current state avoids this obstacle.</summary>
        /// <param name="other">Collider that entered the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (resolved || player == null || !player.IsAlive || !other.CompareTag("Player"))
            {
                return;
            }

            if (IsAvoidedBy(player))
            {
                return;
            }

            resolved = true;
            EventBus<ObstacleHitEvent>.Publish(new ObstacleHitEvent(obstacleId, transform.position));
            player.Kill(obstacleId);
        }

        /// <summary>Awards the clear once the character is safely past.</summary>
        private void Update()
        {
            if (resolved || player == null || !player.IsAlive)
            {
                return;
            }

            if (player.Position.z > transform.position.z + clearOffset)
            {
                resolved = true;
                EventBus<ObstacleClearedEvent>.Publish(new ObstacleClearedEvent(obstacleId, transform.position));
            }
        }
    }
}
