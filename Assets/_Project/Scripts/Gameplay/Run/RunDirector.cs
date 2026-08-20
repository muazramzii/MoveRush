using System.Collections.Generic;
using MoveRush.Core;
using MoveRush.Core.Events;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using UnityEngine;

namespace MoveRush.Gameplay.Run
{
    /// <summary>
    /// Bridges the application state machine to the systems that make up a run. It is the only
    /// object in the gameplay scene that talks to <see cref="IGameStateService"/>, so the run
    /// systems stay ignorant of application flow and can be tested on their own.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    [DisallowMultipleComponent]
    public class RunDirector : MonoBehaviour
    {
        [Tooltip("Systems driven by the director. Left empty, every IRunSystem below this object is used.")]
        [SerializeField] private MonoBehaviour[] explicitSystems;

        [Tooltip("Seconds the result stays on screen before a control can restart the run.")]
        [SerializeField, Min(0f)] private float restartDelay = 0.6f;

        private readonly List<IRunSystem> systems = new List<IRunSystem>(8);
        private IGameStateService gameState;
        private IPlayerService player;
        private IInputProvider input;
        private float gameOverTime;

        /// <summary>Collects the run systems before the first state change can arrive.</summary>
        private void Awake()
        {
            CollectSystems();
        }

        /// <summary>Subscribes to the flow and starts the run if the state is already gameplay.</summary>
        private void Start()
        {
            ServiceLocator.TryGet(out gameState);
            ServiceLocator.TryGet(out player);
            ServiceLocator.TryGet(out input);

            if (gameState == null)
            {
                Log.Error("RunDirector: no game state service is registered. Start from the Bootstrap scene.", this);
                return;
            }

            gameState.StateChanged += OnStateChanged;
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);

            if (input != null)
            {
                input.CommandIssued += OnCommandIssued;
            }

            if (gameState.CurrentState == GameState.Gameplay)
            {
                BeginRun();
            }
        }

        /// <summary>Releases every subscription with the scene.</summary>
        private void OnDestroy()
        {
            if (gameState != null)
            {
                gameState.StateChanged -= OnStateChanged;
            }

            if (input != null)
            {
                input.CommandIssued -= OnCommandIssued;
            }

            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
        }

        /// <summary>
        /// Restarts on any control once the result has been on screen long enough. The delay
        /// stops the key press that was mid-flight when the player died from skipping the result.
        /// </summary>
        /// <param name="command">Command that was issued.</param>
        private void OnCommandIssued(InputCommand command)
        {
            if (gameState == null || gameState.CurrentState != GameState.GameOver)
            {
                return;
            }

            if (Time.unscaledTime >= gameOverTime + restartDelay)
            {
                gameState.RestartRun();
            }
        }

        /// <summary>Fills the system list from the inspector, or from the children below.</summary>
        private void CollectSystems()
        {
            systems.Clear();

            if (explicitSystems != null && explicitSystems.Length > 0)
            {
                for (int i = 0; i < explicitSystems.Length; i++)
                {
                    if (explicitSystems[i] is IRunSystem system)
                    {
                        systems.Add(system);
                    }
                }
            }
            else
            {
                systems.AddRange(GetComponentsInChildren<IRunSystem>(true));
            }

            systems.Sort((left, right) => left.RunOrder.CompareTo(right.RunOrder));
            Log.Info($"RunDirector: driving {systems.Count} run systems.", this);
        }

        /// <summary>Resets every system and hands control back to the player.</summary>
        private void BeginRun()
        {
            for (int i = 0; i < systems.Count; i++)
            {
                systems[i].OnRunReset();
            }

            if (player == null)
            {
                ServiceLocator.TryGet(out player);
            }

            player?.PrepareForRun();
            input?.SetEnabled(true);

            Log.Info("RunDirector: run started.", this);
        }

        /// <summary>Stops run activity and suppresses input.</summary>
        private void EndRun()
        {
            gameOverTime = Time.unscaledTime;

            for (int i = 0; i < systems.Count; i++)
            {
                systems[i].OnRunEnded();
            }

            Log.Info("RunDirector: run ended.", this);
        }

        /// <summary>Translates a state change into a run reset or a run stop.</summary>
        /// <param name="previous">State that was left.</param>
        /// <param name="current">State that was entered.</param>
        private void OnStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.Gameplay)
            {
                BeginRun();
            }
            else if (current == GameState.GameOver)
            {
                EndRun();
            }
        }

        /// <summary>
        /// A player death is the only thing that ends a run, and it arrives as an event rather
        /// than a direct call, so obstacles never need a reference to the state machine.
        /// </summary>
        /// <param name="payload">Death payload.</param>
        private void OnPlayerDied(PlayerDiedEvent payload)
        {
            gameState?.EndRun();
        }
    }
}
