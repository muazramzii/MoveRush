using System;
using MoveRush.Core;
using MoveRush.Core.Events;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using MoveRush.Gameplay.Config;
using MoveRush.Gameplay.Run;
using UnityEngine;

namespace MoveRush.Gameplay.Difficulty
{
    /// <summary>
    /// Owns the pace of a run: the distance travelled and the speed everything moves at.
    /// The speed eases towards the target of the current step instead of snapping to it, so the
    /// run tightens without ever surprising the player with an instant jump in speed.
    /// </summary>
    [DisallowMultipleComponent]
    public class DifficultyManager : MonoBehaviour, IDifficultyService, IRunSystem
    {
        [Tooltip("The distance to speed ramp.")]
        [SerializeField] private DifficultyConfig difficultyConfig;

        private IGameStateService gameState;

        /// <inheritdoc />
        public int RunOrder => -10;

        /// <inheritdoc />
        public float CurrentSpeed { get; private set; }

        /// <inheritdoc />
        public float Distance { get; private set; }

        /// <inheritdoc />
        public int Tier { get; private set; }

        /// <inheritdoc />
        public float NormalizedIntensity =>
            difficultyConfig != null ? difficultyConfig.GetNormalizedIntensity(CurrentSpeed) : 0f;

        /// <inheritdoc />
        public event Action<float> SpeedChanged;

        /// <summary>Registers the service before any system reads a speed.</summary>
        private void Awake()
        {
            ServiceLocator.Register<IDifficultyService>(this);
            CurrentSpeed = difficultyConfig != null ? difficultyConfig.StartSpeed : 5f;
        }

        /// <summary>Verifies the ramp is assigned.</summary>
        private void Start()
        {
            ServiceLocator.TryGet(out gameState);

            if (difficultyConfig == null)
            {
                Log.Error("DifficultyManager: assign the difficulty config.", this);
                enabled = false;
            }
        }

        /// <summary>Releases the registration with the scene.</summary>
        private void OnDestroy()
        {
            SpeedChanged = null;
            ServiceLocator.Unregister<IDifficultyService>();
        }

        /// <inheritdoc />
        public void OnRunReset()
        {
            Distance = 0f;
            Tier = 0;
            SetSpeed(difficultyConfig != null ? difficultyConfig.StartSpeed : 5f);
            EventBus<DifficultyChangedEvent>.Publish(new DifficultyChangedEvent(Tier, CurrentSpeed));
        }

        /// <inheritdoc />
        public void OnRunEnded()
        {
        }

        /// <summary>Advances the distance and eases the speed towards the current step.</summary>
        private void Update()
        {
            if (gameState == null || gameState.CurrentState != GameState.Gameplay)
            {
                return;
            }

            Distance += CurrentSpeed * Time.deltaTime;

            float target = difficultyConfig.GetTargetSpeed(Distance);
            SetSpeed(Mathf.MoveTowards(CurrentSpeed, target, difficultyConfig.Acceleration * Time.deltaTime));

            int tier = difficultyConfig.GetTier(Distance);
            if (tier != Tier)
            {
                Tier = tier;
                EventBus<DifficultyChangedEvent>.Publish(new DifficultyChangedEvent(Tier, target));
                Log.Info($"DifficultyManager: tier {Tier} at {Distance:0} m, easing to {target:0.0} m/s.", this);
            }
        }

        /// <summary>Applies a speed and notifies listeners when it actually changed.</summary>
        /// <param name="speed">New speed in meters per second.</param>
        private void SetSpeed(float speed)
        {
            if (Mathf.Approximately(speed, CurrentSpeed))
            {
                return;
            }

            CurrentSpeed = speed;
            SpeedChanged?.Invoke(CurrentSpeed);
        }
    }
}
