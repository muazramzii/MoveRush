using System;
using System.Collections.Generic;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using UnityEngine;

namespace MoveRush.Managers
{
    /// <summary>
    /// Runtime registry and router for full-screen views. Screens register themselves and are
    /// addressed by identifier, so no system needs a reference to a UI prefab.
    /// Phase 1 deliberately ships no screen layouts: this class provides the seam the menus,
    /// HUD and popups will plug into, plus the loading overlay hook driven by the scene loader.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour, IUIService, IGameService
    {
        [Header("Loading Overlay")]
        [Tooltip("Optional overlay object toggled while a scene load is running.")]
        [SerializeField] private GameObject loadingOverlay;

        private readonly Dictionary<string, IUIScreen> screens = new Dictionary<string, IUIScreen>(8);
        private ISceneLoader sceneLoader;

        /// <summary>
        /// Runs after the scene loader (order 20) so the loading events can be hooked during
        /// initialisation rather than on the first frame, which would miss the very first load.
        /// </summary>
        public int InitializationOrder => 25;

        /// <inheritdoc />
        public bool IsInitialized { get; private set; }

        /// <inheritdoc />
        public string ActiveScreenId { get; private set; }

        /// <summary>
        /// Raised when the loading overlay is shown or hidden. A future overlay prefab can react
        /// to this instead of being wired into the inspector.
        /// </summary>
        public event Action<bool> LoadingOverlayVisibilityChanged;

        /// <summary>Raised every frame during a scene load, carrying normalised progress.</summary>
        public event Action<float> LoadingProgressChanged;

        /// <inheritdoc />
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            ServiceLocator.Register<IUIService>(this);
            SubscribeToSceneLoader();
            SetLoadingOverlayVisible(false);

            IsInitialized = true;
            Log.Info("UIManager initialised.", this);
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            UnsubscribeFromSceneLoader();

            screens.Clear();
            ActiveScreenId = null;
            LoadingOverlayVisibilityChanged = null;
            LoadingProgressChanged = null;

            ServiceLocator.Unregister<IUIService>();
            IsInitialized = false;
        }

        /// <inheritdoc />
        public void RegisterScreen(IUIScreen screen)
        {
            if (screen == null || string.IsNullOrWhiteSpace(screen.ScreenId))
            {
                Log.Error("UIManager: a screen must provide a non-empty ScreenId.", this);
                return;
            }

            if (screens.ContainsKey(screen.ScreenId))
            {
                Log.Warning($"UIManager: screen '{screen.ScreenId}' is already registered.", this);
                return;
            }

            screens.Add(screen.ScreenId, screen);
            screen.Prepare();
        }

        /// <inheritdoc />
        public void UnregisterScreen(IUIScreen screen)
        {
            if (screen == null || !screens.ContainsKey(screen.ScreenId))
            {
                return;
            }

            if (screen.IsVisible)
            {
                screen.Hide();
            }

            if (ActiveScreenId == screen.ScreenId)
            {
                ActiveScreenId = null;
            }

            screens.Remove(screen.ScreenId);
        }

        /// <inheritdoc />
        public bool ShowScreen(string screenId)
        {
            if (!screens.TryGetValue(screenId ?? string.Empty, out IUIScreen screen))
            {
                Log.Warning($"UIManager: no screen registered with id '{screenId}'.", this);
                return false;
            }

            if (!string.IsNullOrEmpty(ActiveScreenId) &&
                ActiveScreenId != screenId &&
                screens.TryGetValue(ActiveScreenId, out IUIScreen previous))
            {
                previous.Hide();
            }

            screen.Show();
            ActiveScreenId = screenId;
            return true;
        }

        /// <inheritdoc />
        public bool HideScreen(string screenId)
        {
            if (!screens.TryGetValue(screenId ?? string.Empty, out IUIScreen screen))
            {
                return false;
            }

            screen.Hide();

            if (ActiveScreenId == screenId)
            {
                ActiveScreenId = null;
            }

            return true;
        }

        /// <inheritdoc />
        public void SetLoadingOverlayVisible(bool visible)
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(visible);
            }

            LoadingOverlayVisibilityChanged?.Invoke(visible);
        }

        /// <summary>Hooks the scene loader so the overlay follows every load automatically.</summary>
        private void SubscribeToSceneLoader()
        {
            if (sceneLoader != null || !ServiceLocator.TryGet(out sceneLoader))
            {
                return;
            }

            sceneLoader.LoadStarted += OnLoadStarted;
            sceneLoader.LoadProgressed += OnLoadProgressed;
            sceneLoader.LoadCompleted += OnLoadCompleted;
        }

        /// <summary>Detaches from the scene loader events.</summary>
        private void UnsubscribeFromSceneLoader()
        {
            if (sceneLoader == null)
            {
                return;
            }

            sceneLoader.LoadStarted -= OnLoadStarted;
            sceneLoader.LoadProgressed -= OnLoadProgressed;
            sceneLoader.LoadCompleted -= OnLoadCompleted;
            sceneLoader = null;
        }

        /// <summary>Shows the overlay when a load begins.</summary>
        /// <param name="sceneName">Scene being loaded.</param>
        private void OnLoadStarted(string sceneName) => SetLoadingOverlayVisible(true);

        /// <summary>Forwards load progress to any listening view.</summary>
        /// <param name="progress">Normalised progress in the 0..1 range.</param>
        private void OnLoadProgressed(float progress) => LoadingProgressChanged?.Invoke(progress);

        /// <summary>Hides the overlay when a load completes.</summary>
        /// <param name="sceneName">Scene that finished loading.</param>
        private void OnLoadCompleted(string sceneName) => SetLoadingOverlayVisible(false);

        /// <summary>Releases event subscriptions if the object is destroyed before shutdown.</summary>
        private void OnDestroy() => UnsubscribeFromSceneLoader();
    }
}
