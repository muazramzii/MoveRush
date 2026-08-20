using System.Collections.Generic;
using MoveRush.Core;
using MoveRush.Core.Config;
using MoveRush.Gameplay.Obstacles;
using MoveRush.Gameplay.UI;
using MoveRush.Player.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoveRush.EditorTools
{
    /// <summary>
    /// One-click generator for the Phase 2 vertical slice. It creates the runner configuration
    /// assets, builds every placeholder prefab, assembles the gameplay scene, patches the Phase 1
    /// Bootstrap and MainMenu scenes and rewrites the build settings.
    /// Generating the slice keeps it reproducible: the wiring is code that can be reviewed and
    /// re-run, instead of a hand assembly nobody can repeat identically.
    /// </summary>
    public static class Phase2SetupTool
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string BootstrapScenePath = SceneFolder + "/Bootstrap.unity";
        private const string SplashScenePath = SceneFolder + "/Splash.unity";
        private const string MainMenuScenePath = SceneFolder + "/MainMenu.unity";
        private const string GameScenePath = SceneFolder + "/Game.unity";

        /// <summary>Runs the complete Phase 2 setup.</summary>
        [MenuItem("MoveRush/Setup/Run Phase 2 Setup", priority = 2)]
        public static void RunSetup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureInputHandling();

            RunnerAssetSet assets = CreateConfigAssets();
            BuildPrefabs(assets);
            BuildGameScene(assets);

            PatchBootstrapScene(assets.Track);
            PatchMainMenuScene();
            RegisterBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            Debug.Log("[MoveRush] Phase 2 setup complete. Press Play to run the vertical slice.");
        }

        /// <summary>Creates the configuration assets the slice reads its tuning from.</summary>
        /// <returns>A partially filled asset set.</returns>
        private static RunnerAssetSet CreateConfigAssets()
        {
            return new RunnerAssetSet
            {
                Track = RunnerAssetFactory.CreateTrackConfig(),
                Runner = RunnerAssetFactory.CreateRunnerConfig(),
                Difficulty = RunnerAssetFactory.CreateDifficultyConfig(),
                Score = RunnerAssetFactory.CreateScoreConfig(),
                Movement = RunnerAssetFactory.CreateMovementConfig()
            };
        }

        /// <summary>
        /// Builds every placeholder prefab. A scratch scene is opened first so the temporary
        /// objects the factories create can never end up saved into a real scene.
        /// </summary>
        /// <param name="assets">Asset set to fill.</param>
        private static void BuildPrefabs(RunnerAssetSet assets)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            assets.TilePrefab = RunnerPrefabFactory.CreateRoadTile();
            assets.CoinPrefab = RunnerPrefabFactory.CreateCoin();
            assets.PlayerPrefab = RunnerPrefabFactory.CreatePlayer(assets.Movement);

            assets.CoinEffect = RunnerPrefabFactory.CreateEffect(
                "Effect_CoinPickup", new Color(1f, 0.85f, 0.2f), 0.3f, 0.35f);
            assets.HitEffect = RunnerPrefabFactory.CreateEffect(
                "Effect_Impact", new Color(1f, 0.25f, 0.2f), 0.45f, 0.6f);
            assets.LandEffect = RunnerPrefabFactory.CreateEffect(
                "Effect_Landing", new Color(0.8f, 0.8f, 0.85f), 0.3f, 0.5f);

            ObstacleBase barrier = ObstaclePrefabFactory.CreateBarrier();
            ObstacleBase laserGate = ObstaclePrefabFactory.CreateLaserGate();
            ObstacleBase bus = ObstaclePrefabFactory.CreateBus();
            ObstacleBase train = ObstaclePrefabFactory.CreateTrain();

            assets.CoinPatterns = RunnerAssetFactory.CreateCoinPatterns();
            assets.ObstacleDefinitions = RunnerAssetFactory.CreateObstacleDefinitions(barrier, laserGate, bus, train);

            AssetDatabase.SaveAssets();
        }

        /// <summary>Assembles and saves the gameplay scene.</summary>
        /// <param name="assets">Generated assets.</param>
        private static void BuildGameScene(RunnerAssetSet assets)
        {
            EditorWiring.EnsureFolder(SceneFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameSceneBuilder.Build(assets);
            EditorSceneManager.SaveScene(scene, GameScenePath);

            Debug.Log($"[MoveRush] Created {GameScenePath}");
        }

        /// <summary>
        /// Adds the two things Phase 2 needs from the persistent root: the shared track geometry,
        /// and the keyboard input provider that the Phase 3 pose input will later replace.
        /// </summary>
        /// <param name="track">Track configuration to assign.</param>
        private static void PatchBootstrapScene(TrackConfig track)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath) == null)
            {
                Debug.LogWarning("[MoveRush] No Bootstrap scene found. Run 'MoveRush/Setup/Run Phase 1 Setup' first.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            Bootstrap bootstrap = Object.FindFirstObjectByType<Bootstrap>(FindObjectsInactive.Include);

            if (bootstrap == null)
            {
                Debug.LogWarning("[MoveRush] The Bootstrap scene has no Bootstrap component to patch.");
                return;
            }

            EditorWiring.SetReference(bootstrap, "trackConfig", track);

            if (bootstrap.GetComponent<KeyboardInputProvider>() == null)
            {
                bootstrap.gameObject.AddComponent<KeyboardInputProvider>();
                Debug.Log("[MoveRush] Added KeyboardInputProvider to the persistent root.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Adds the prompt and the starter that launch a run from the menu.</summary>
        private static void PatchMainMenuScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) == null)
            {
                Debug.LogWarning("[MoveRush] No MainMenu scene found. Run 'MoveRush/Setup/Run Phase 1 Setup' first.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            if (Object.FindFirstObjectByType<RunStarter>(FindObjectsInactive.Include) == null)
            {
                GameObject starter = new GameObject("[MainMenu]");
                starter.AddComponent<RunStarter>();
                HudBuilder.BuildMenuPrompt();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Rewrites the build settings, keeping Bootstrap at index zero.</summary>
        private static void RegisterBuildScenes()
        {
            string[] paths = { BootstrapScenePath, SplashScenePath, MainMenuScenePath, GameScenePath };
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(paths.Length);

            foreach (string path in paths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>
        /// Switches the project to accept both input backends. The keyboard provider reads the
        /// Input System package, which returns nothing at all while the project is still set to
        /// the legacy manager only - a silent failure that looks exactly like broken controls.
        /// </summary>
        private static void EnsureInputHandling()
        {
            Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings == null || settings.Length == 0)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(settings[0]);
            SerializedProperty handler = serialized.FindProperty("activeInputHandler");

            if (handler == null || handler.intValue == 2)
            {
                return;
            }

            handler.intValue = 2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            Debug.LogWarning(
                "[MoveRush] Active Input Handling was set to 'Both'. Unity will ask to restart the editor: " +
                "accept, then run 'MoveRush/Setup/Run Phase 2 Setup' again to finish.");
        }
    }
}
