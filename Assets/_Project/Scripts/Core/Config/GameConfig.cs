using UnityEngine;

namespace MoveRush.Core.Config
{
    /// <summary>
    /// Global application settings: scene names, start-up timings, platform defaults and
    /// persistence options. Every value that a designer or producer may want to tune lives
    /// here rather than in code, so no system needs a recompile to change behaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "MoveRush/Config/Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [Header("Scenes")]
        [Tooltip("Persistent scene that owns the core services. Must be build index 0.")]
        [SerializeField] private string bootstrapSceneName = "Bootstrap";

        [Tooltip("Branding scene shown immediately after boot.")]
        [SerializeField] private string splashSceneName = "Splash";

        [Tooltip("Front-end scene the player returns to from anywhere in the game.")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Tooltip("Scene that hosts a run.")]
        [SerializeField] private string gameSceneName = "Game";

        [Header("Start-up")]
        [Tooltip("Seconds the splash screen stays on screen before the main menu is requested.")]
        [SerializeField, Range(0f, 10f)] private float splashDuration = 2f;

        [Tooltip("Minimum seconds a loading screen is shown, to avoid a jarring one-frame flash.")]
        [SerializeField, Range(0f, 5f)] private float minimumLoadingDuration = 0.5f;

        [Header("Platform")]
        [Tooltip("Frame rate requested at boot. Use -1 for the platform default.")]
        [SerializeField] private int targetFrameRate = 60;

        [Tooltip("VSync count applied at boot. Mobile builds normally use 0.")]
        [SerializeField, Range(0, 2)] private int vSyncCount;

        [Tooltip("Keeps the game simulating when the application loses focus.")]
        [SerializeField] private bool runInBackground;

        [Tooltip("Quality settings index applied to a brand new profile.")]
        [SerializeField] private int defaultQualityLevel;

        [Tooltip("ISO 639-1 language code applied to a brand new profile.")]
        [SerializeField] private string defaultLanguageCode = "en";

        [Header("Persistence")]
        [Tooltip("File name, including extension, written inside the persistent data path.")]
        [SerializeField] private string saveFileName = "moverush.save.json";

        [Tooltip("Writes readable JSON. Disable for release builds to reduce file size.")]
        [SerializeField] private bool prettyPrintSave = true;

        [Tooltip("Seconds between automatic saves. Zero disables the auto-save timer.")]
        [SerializeField, Range(0f, 300f)] private float autoSaveInterval = 60f;

        [Header("Diagnostics")]
        [Tooltip("Enables informational logging. Warnings and errors are always emitted.")]
        [SerializeField] private bool verboseLogging = true;

        /// <summary>Name of the persistent bootstrap scene.</summary>
        public string BootstrapSceneName => bootstrapSceneName;

        /// <summary>Name of the splash scene.</summary>
        public string SplashSceneName => splashSceneName;

        /// <summary>Name of the main menu scene.</summary>
        public string MainMenuSceneName => mainMenuSceneName;

        /// <summary>Name of the scene that hosts a run.</summary>
        public string GameSceneName => gameSceneName;

        /// <summary>Seconds the splash screen remains visible.</summary>
        public float SplashDuration => splashDuration;

        /// <summary>Minimum seconds a loading screen stays visible.</summary>
        public float MinimumLoadingDuration => minimumLoadingDuration;

        /// <summary>Frame rate requested at boot, or -1 for the platform default.</summary>
        public int TargetFrameRate => targetFrameRate;

        /// <summary>VSync count applied at boot.</summary>
        public int VSyncCount => vSyncCount;

        /// <summary>True when the game keeps running unfocused.</summary>
        public bool RunInBackground => runInBackground;

        /// <summary>Quality settings index used for new profiles.</summary>
        public int DefaultQualityLevel => defaultQualityLevel;

        /// <summary>Language code used for new profiles.</summary>
        public string DefaultLanguageCode => defaultLanguageCode;

        /// <summary>File name of the save payload inside the persistent data path.</summary>
        public string SaveFileName => saveFileName;

        /// <summary>True when the save payload is written as indented JSON.</summary>
        public bool PrettyPrintSave => prettyPrintSave;

        /// <summary>Seconds between automatic saves. Zero disables auto-saving.</summary>
        public float AutoSaveInterval => autoSaveInterval;

        /// <summary>True when informational logging is enabled.</summary>
        public bool VerboseLogging => verboseLogging;

        /// <summary>Keeps designer-entered values inside their valid ranges.</summary>
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(saveFileName))
            {
                saveFileName = "moverush.save.json";
            }

            if (targetFrameRate != -1)
            {
                targetFrameRate = Mathf.Clamp(targetFrameRate, 30, 240);
            }

            defaultQualityLevel = Mathf.Max(0, defaultQualityLevel);
        }
    }
}
