using System;

namespace MoveRush.Core.Services
{
    /// <summary>
    /// Contract for asynchronous scene loading. Callers pass a scene name and receive progress
    /// notifications; they never touch <c>SceneManager</c> directly, so an addressables-backed
    /// loader can replace this implementation without changing a single call site.
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>True while a load operation is running.</summary>
        bool IsLoading { get; }

        /// <summary>Normalised progress of the running load in the 0..1 range.</summary>
        float Progress { get; }

        /// <summary>Name of the scene currently being loaded, or null when idle.</summary>
        string LoadingSceneName { get; }

        /// <summary>Raised when a load begins, carrying the scene name.</summary>
        event Action<string> LoadStarted;

        /// <summary>Raised every frame during a load, carrying normalised progress.</summary>
        event Action<float> LoadProgressed;

        /// <summary>Raised when a load has completed, carrying the scene name.</summary>
        event Action<string> LoadCompleted;

        /// <summary>
        /// Loads a scene asynchronously in single mode. A request made while another load is
        /// running is ignored and logged rather than queued.
        /// </summary>
        /// <param name="sceneName">Name of the scene as listed in the build settings.</param>
        /// <param name="onCompleted">Optional callback invoked once the scene is active.</param>
        void LoadScene(string sceneName, Action onCompleted = null);
    }
}
