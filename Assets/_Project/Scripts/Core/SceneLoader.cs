using System;
using System.Collections;
using MoveRush.Core.Config;
using MoveRush.Core.Events;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoveRush.Core
{
    /// <summary>
    /// Asynchronous single-mode scene loader. It owns the whole load lifecycle: progress
    /// reporting, an enforced minimum display time so the loading screen cannot flash, and
    /// deferred activation so the new scene only appears once everything is ready.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneLoader : MonoBehaviour, ISceneLoader, IGameService
    {
        private const float ActivationThreshold = 0.9f;

        private IConfigProvider configProvider;
        private Coroutine loadRoutine;

        /// <inheritdoc />
        public int InitializationOrder => 20;

        /// <inheritdoc />
        public bool IsInitialized { get; private set; }

        /// <inheritdoc />
        public bool IsLoading { get; private set; }

        /// <inheritdoc />
        public float Progress { get; private set; }

        /// <inheritdoc />
        public string LoadingSceneName { get; private set; }

        /// <inheritdoc />
        public event Action<string> LoadStarted;

        /// <inheritdoc />
        public event Action<float> LoadProgressed;

        /// <inheritdoc />
        public event Action<string> LoadCompleted;

        /// <inheritdoc />
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            ServiceLocator.TryGet(out configProvider);
            ServiceLocator.Register<ISceneLoader>(this);
            IsInitialized = true;
            Log.Info("SceneLoader initialised.", this);
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            if (loadRoutine != null)
            {
                StopCoroutine(loadRoutine);
                loadRoutine = null;
            }

            LoadStarted = null;
            LoadProgressed = null;
            LoadCompleted = null;

            ServiceLocator.Unregister<ISceneLoader>();
            IsInitialized = false;
        }

        /// <inheritdoc />
        public void LoadScene(string sceneName, Action onCompleted = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Log.Error("SceneLoader: cannot load a scene with an empty name.", this);
                return;
            }

            if (IsLoading)
            {
                Log.Warning($"SceneLoader: '{sceneName}' was ignored because '{LoadingSceneName}' is still loading.", this);
                return;
            }

            loadRoutine = StartCoroutine(LoadRoutine(sceneName, onCompleted));
        }

        /// <summary>
        /// Drives the load: reports progress up to the activation threshold, honours the minimum
        /// loading duration from configuration, then activates the scene and fires completion.
        /// </summary>
        /// <param name="sceneName">Scene to load.</param>
        /// <param name="onCompleted">Optional completion callback.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator LoadRoutine(string sceneName, Action onCompleted)
        {
            IsLoading = true;
            LoadingSceneName = sceneName;
            Progress = 0f;

            LoadStarted?.Invoke(sceneName);
            EventBus<SceneLoadStartedEvent>.Publish(new SceneLoadStartedEvent(sceneName));
            Log.Info($"SceneLoader: loading '{sceneName}'.", this);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Log.Error($"SceneLoader: '{sceneName}' is not in the build settings.", this);
                ResetState();
                yield break;
            }

            operation.allowSceneActivation = false;

            float minimumDuration = configProvider?.Game != null ? configProvider.Game.MinimumLoadingDuration : 0f;
            float startTime = Time.unscaledTime;

            while (operation.progress < ActivationThreshold)
            {
                ReportProgress(sceneName, Mathf.Clamp01(operation.progress / ActivationThreshold) * 0.99f);
                yield return null;
            }

            while (Time.unscaledTime - startTime < minimumDuration)
            {
                yield return null;
            }

            ReportProgress(sceneName, 1f);
            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                yield return null;
            }

            ResetState();
            Log.Info($"SceneLoader: '{sceneName}' is active.", this);

            LoadCompleted?.Invoke(sceneName);
            EventBus<SceneLoadCompletedEvent>.Publish(new SceneLoadCompletedEvent(sceneName));
            onCompleted?.Invoke();
        }

        /// <summary>Publishes a progress value to both the direct event and the event bus.</summary>
        /// <param name="sceneName">Scene being loaded.</param>
        /// <param name="value">Normalised progress in the 0..1 range.</param>
        private void ReportProgress(string sceneName, float value)
        {
            Progress = value;
            LoadProgressed?.Invoke(value);
            EventBus<SceneLoadProgressEvent>.Publish(new SceneLoadProgressEvent(sceneName, value));
        }

        /// <summary>Returns the loader to its idle state.</summary>
        private void ResetState()
        {
            IsLoading = false;
            LoadingSceneName = null;
            loadRoutine = null;
        }
    }
}
