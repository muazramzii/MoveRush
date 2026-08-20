using System;
using System.Collections;
using MoveRush.Core.Config;
using MoveRush.Core.Events;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using UnityEngine;

namespace MoveRush.Core
{
    /// <summary>
    /// Owns the application state machine and the flow between the front end and a run.
    /// It never reaches into other managers: it raises <see cref="StateChanged"/> and publishes a
    /// <see cref="GameStateChangedEvent"/>, and interested systems react on their own. Scene
    /// loading is delegated to <see cref="ISceneLoader"/>, and the legal transitions live in
    /// <see cref="GameStateTransitions"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour, IGameStateService, IGameService
    {
        private IConfigProvider configProvider;
        private ISceneLoader sceneLoader;
        private Coroutine splashRoutine;

        /// <inheritdoc />
        public int InitializationOrder => 30;

        /// <inheritdoc />
        public bool IsInitialized { get; private set; }

        /// <inheritdoc />
        public GameState CurrentState { get; private set; } = GameState.Boot;

        /// <inheritdoc />
        public GameState PreviousState { get; private set; } = GameState.Boot;

        /// <inheritdoc />
        public bool IsRunActive => CurrentState == GameState.Gameplay || CurrentState == GameState.Paused;

        /// <inheritdoc />
        public event Action<GameState, GameState> StateChanged;

        /// <inheritdoc />
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            ServiceLocator.TryGet(out configProvider);
            ServiceLocator.TryGet(out sceneLoader);
            ServiceLocator.Register<IGameStateService>(this);

            CurrentState = GameState.Boot;
            PreviousState = GameState.Boot;
            IsInitialized = true;
            Log.Info("GameManager initialised in state Boot.", this);
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            StopSplashTimer();
            Time.timeScale = 1f;
            StateChanged = null;
            ServiceLocator.Unregister<IGameStateService>();
            IsInitialized = false;
        }

        /// <summary>
        /// Starts the front-end flow once every service is ready. Called by the bootstrapper,
        /// which is the only object that knows initialisation has finished.
        /// </summary>
        public void BeginStartupFlow()
        {
            string splashScene = configProvider?.Game != null ? configProvider.Game.SplashSceneName : null;
            if (string.IsNullOrWhiteSpace(splashScene))
            {
                Log.Error("GameManager: no splash scene configured, going straight to the main menu.", this);
                GoToMainMenu();
                return;
            }

            LoadSceneThenEnter(splashScene, GameState.Splash);
        }

        /// <inheritdoc />
        public void GoToMainMenu()
        {
            string menuScene = configProvider?.Game != null ? configProvider.Game.MainMenuSceneName : null;
            if (string.IsNullOrWhiteSpace(menuScene))
            {
                Log.Error("GameManager: no main menu scene configured.", this);
                return;
            }

            LoadSceneThenEnter(menuScene, GameState.MainMenu);
        }

        /// <inheritdoc />
        public void StartRun()
        {
            string gameScene = configProvider?.Game != null ? configProvider.Game.GameSceneName : null;
            if (string.IsNullOrWhiteSpace(gameScene))
            {
                Log.Error("GameManager: no gameplay scene configured.", this);
                return;
            }

            LoadSceneThenEnter(gameScene, GameState.Gameplay);
        }

        /// <inheritdoc />
        public void RestartRun()
        {
            if (CurrentState != GameState.GameOver)
            {
                Log.Warning($"GameManager: a restart was requested from {CurrentState} and was ignored.", this);
                return;
            }

            RequestState(GameState.Gameplay);
        }

        /// <inheritdoc />
        public void EndRun()
        {
            if (CurrentState == GameState.Gameplay)
            {
                RequestState(GameState.GameOver);
            }
        }

        /// <inheritdoc />
        public void PauseRun() => RequestState(GameState.Paused);

        /// <inheritdoc />
        public void ResumeRun() => RequestState(GameState.Gameplay);

        /// <inheritdoc />
        public bool CanTransitionTo(GameState nextState) => GameStateTransitions.IsAllowed(CurrentState, nextState);

        /// <inheritdoc />
        public bool RequestState(GameState nextState)
        {
            if (!CanTransitionTo(nextState))
            {
                Log.Warning($"GameManager: rejected transition {CurrentState} -> {nextState}.", this);
                return false;
            }

            ApplyState(nextState);
            return true;
        }

        /// <summary>
        /// Moves into the loading state and enters <paramref name="targetState"/> once the scene
        /// is active. Centralising this keeps every scene change consistent.
        /// </summary>
        /// <param name="sceneName">Scene to load.</param>
        /// <param name="targetState">State entered after the load completes.</param>
        private void LoadSceneThenEnter(string sceneName, GameState targetState)
        {
            if (sceneLoader == null && !ServiceLocator.TryGet(out sceneLoader))
            {
                Log.Error("GameManager: no scene loader is registered, the flow cannot continue.", this);
                return;
            }

            StopSplashTimer();
            RequestState(GameState.Loading);
            sceneLoader.LoadScene(sceneName, () => RequestState(targetState));
        }

        /// <summary>Performs the transition and runs the entry behaviour of the new state.</summary>
        /// <param name="nextState">State to enter.</param>
        private void ApplyState(GameState nextState)
        {
            PreviousState = CurrentState;
            CurrentState = nextState;

            Log.Info($"GameManager: {PreviousState} -> {CurrentState}.", this);

            OnStateEntered(CurrentState);

            StateChanged?.Invoke(PreviousState, CurrentState);
            EventBus<GameStateChangedEvent>.Publish(new GameStateChangedEvent(PreviousState, CurrentState));
        }

        /// <summary>
        /// Runs the behaviour attached to entering a state. Time scale is owned here so no other
        /// system can leave the game frozen after an unexpected transition.
        /// </summary>
        /// <param name="state">State that was just entered.</param>
        private void OnStateEntered(GameState state)
        {
            Time.timeScale = state == GameState.Paused ? 0f : 1f;

            if (state == GameState.Splash)
            {
                StopSplashTimer();
                splashRoutine = StartCoroutine(SplashRoutine());
            }
        }

        /// <summary>Holds the splash screen for the configured duration, then opens the menu.</summary>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator SplashRoutine()
        {
            float duration = configProvider?.Game != null ? configProvider.Game.SplashDuration : 0f;
            if (duration > 0f)
            {
                yield return new WaitForSecondsRealtime(duration);
            }

            splashRoutine = null;
            GoToMainMenu();
        }

        /// <summary>Cancels a pending splash timer, if one is running.</summary>
        private void StopSplashTimer()
        {
            if (splashRoutine == null)
            {
                return;
            }

            StopCoroutine(splashRoutine);
            splashRoutine = null;
        }
    }
}
