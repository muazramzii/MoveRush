namespace MoveRush.Core.Events
{
    /// <summary>Raised once when an asynchronous scene load begins.</summary>
    public readonly struct SceneLoadStartedEvent
    {
        /// <summary>Name of the scene being loaded.</summary>
        public string SceneName { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="sceneName">Name of the scene being loaded.</param>
        public SceneLoadStartedEvent(string sceneName) => SceneName = sceneName;
    }

    /// <summary>Raised every frame while an asynchronous scene load is running.</summary>
    public readonly struct SceneLoadProgressEvent
    {
        /// <summary>Name of the scene being loaded.</summary>
        public string SceneName { get; }

        /// <summary>Normalised progress in the 0..1 range.</summary>
        public float Progress { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="sceneName">Name of the scene being loaded.</param>
        /// <param name="progress">Normalised progress in the 0..1 range.</param>
        public SceneLoadProgressEvent(string sceneName, float progress)
        {
            SceneName = sceneName;
            Progress = progress;
        }
    }

    /// <summary>Raised once when an asynchronous scene load has fully completed.</summary>
    public readonly struct SceneLoadCompletedEvent
    {
        /// <summary>Name of the scene that finished loading.</summary>
        public string SceneName { get; }

        /// <summary>Creates the payload.</summary>
        /// <param name="sceneName">Name of the scene that finished loading.</param>
        public SceneLoadCompletedEvent(string sceneName) => SceneName = sceneName;
    }
}
