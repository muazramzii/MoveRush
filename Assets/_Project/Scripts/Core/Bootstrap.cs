using System.Collections.Generic;
using MoveRush.Core.Config;
using MoveRush.Core.Events;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using UnityEngine;

namespace MoveRush.Core
{
    /// <summary>
    /// Composition root of the application. It is the single object marked
    /// <c>DontDestroyOnLoad</c>: every core service lives underneath it, so one persistent root
    /// survives scene changes instead of a scattered set of singletons.
    /// Responsibilities are limited to wiring - it configures the platform, publishes the
    /// configuration assets, initialises services in a deterministic order and hands control to
    /// the <see cref="GameManager"/>.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class Bootstrap : MonoBehaviour, IConfigProvider
    {
        [Header("Configuration")]
        [Tooltip("Global application settings. Required.")]
        [SerializeField] private GameConfig gameConfig;

        [Tooltip("Audio bank, mixer routing and default volumes. Required.")]
        [SerializeField] private AudioConfig audioConfig;

        [Tooltip("Player defaults and progression curve. Required.")]
        [SerializeField] private PlayerConfig playerConfig;

        [Tooltip("Lane positions and tile length shared by the player and the world. Required.")]
        [SerializeField] private TrackConfig trackConfig;

        [Header("Flow")]
        [Tooltip("Starts the splash and main menu flow automatically once services are ready.")]
        [SerializeField] private bool autoStartFlow = true;

        private static Bootstrap instance;
        private readonly List<IGameService> services = new List<IGameService>(8);

        /// <inheritdoc />
        public GameConfig Game => gameConfig;

        /// <inheritdoc />
        public AudioConfig Audio => audioConfig;

        /// <inheritdoc />
        public PlayerConfig Player => playerConfig;

        /// <inheritdoc />
        public TrackConfig Track => trackConfig;

        /// <summary>True once every service reported successful initialisation.</summary>
        public bool IsReady { get; private set; }

        /// <summary>
        /// Enforces the single-instance rule, persists the root and runs the start-up sequence.
        /// A second bootstrap - which happens when the bootstrap scene is loaded twice - destroys
        /// itself so the running services are never duplicated.
        /// </summary>
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Log.Warning("Bootstrap: a second instance was found and destroyed.", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (!ValidateConfiguration())
            {
                return;
            }

            ApplyPlatformSettings();
            RegisterConfiguration();
            InitializeServices();

            IsReady = true;
            Log.Info("Bootstrap: all services are ready.", this);

            if (autoStartFlow)
            {
                StartFlow();
            }
        }

        /// <summary>
        /// Starts the front-end flow. Exposed publicly so an automated test or a development
        /// scene can drive the flow manually with <c>autoStartFlow</c> disabled.
        /// </summary>
        public void StartFlow()
        {
            if (!IsReady)
            {
                Log.Error("Bootstrap: StartFlow was called before initialisation completed.", this);
                return;
            }

            if (ServiceLocator.TryGet(out IGameStateService stateService) && stateService is GameManager gameManager)
            {
                gameManager.BeginStartupFlow();
            }
        }

        /// <summary>Verifies that every required configuration asset is assigned.</summary>
        /// <returns>True when the bootstrap can continue.</returns>
        private bool ValidateConfiguration()
        {
            if (gameConfig != null && audioConfig != null && playerConfig != null && trackConfig != null)
            {
                return true;
            }

            Log.Error(
                "Bootstrap: GameConfig, AudioConfig, PlayerConfig and TrackConfig must all be assigned. " +
                "Run 'MoveRush/Setup/Run Phase 1 Setup' and 'MoveRush/Setup/Run Phase 2 Setup' to generate and wire them.",
                this);
            return false;
        }

        /// <summary>Applies the frame rate, VSync and logging options declared in configuration.</summary>
        private void ApplyPlatformSettings()
        {
            Log.VerboseEnabled = gameConfig.VerboseLogging;
            QualitySettings.vSyncCount = gameConfig.VSyncCount;
            Application.targetFrameRate = gameConfig.TargetFrameRate;
            Application.runInBackground = gameConfig.RunInBackground;
        }

        /// <summary>Publishes the configuration assets so services can resolve them.</summary>
        private void RegisterConfiguration()
        {
            ServiceLocator.Register<IConfigProvider>(this);
        }

        /// <summary>
        /// Collects every service under the persistent root and initialises it in ascending
        /// <see cref="IGameService.InitializationOrder"/>, which removes any dependency on
        /// Unity's non-deterministic Awake ordering.
        /// </summary>
        private void InitializeServices()
        {
            services.Clear();
            services.AddRange(GetComponentsInChildren<IGameService>(true));
            services.Sort((left, right) => left.InitializationOrder.CompareTo(right.InitializationOrder));

            for (int i = 0; i < services.Count; i++)
            {
                services[i].Initialize();
            }
        }

        /// <summary>Shuts services down in reverse order and releases every static registry.</summary>
        private void OnApplicationQuit()
        {
            if (instance != this)
            {
                return;
            }

            for (int i = services.Count - 1; i >= 0; i--)
            {
                services[i].Shutdown();
            }

            services.Clear();
            ServiceLocator.Clear();
            EventBusRegistry.ClearAll();
            IsReady = false;
        }

        /// <summary>Releases the static instance handle when the root is destroyed.</summary>
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
