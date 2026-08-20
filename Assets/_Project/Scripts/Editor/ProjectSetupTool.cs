using System.Collections.Generic;
using System.IO;
using MoveRush.Core;
using MoveRush.Core.Config;
using MoveRush.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoveRush.EditorTools
{
    /// <summary>
    /// One-click generator for the Phase 1 foundation. It creates the three configuration
    /// assets, builds the Bootstrap, Splash and MainMenu scenes, wires the persistent service
    /// hierarchy and registers the scenes in the build settings.
    /// Generating these from code keeps the setup reproducible: any team member can rebuild the
    /// exact same project skeleton, and the result is reviewable as a diff.
    /// </summary>
    public static class ProjectSetupTool
    {
        private const string ConfigFolder = "Assets/_Project/ScriptableObjects/Config";
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string MenuRoot = "MoveRush/Setup/";

        /// <summary>Runs the complete Phase 1 setup: configuration assets, scenes and build settings.</summary>
        [MenuItem(MenuRoot + "Run Phase 1 Setup", priority = 0)]
        public static void RunFullSetup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            GameConfig gameConfig = CreateConfigAsset<GameConfig>("GameConfig");
            AudioConfig audioConfig = CreateConfigAsset<AudioConfig>("AudioConfig");
            PlayerConfig playerConfig = CreateConfigAsset<PlayerConfig>("PlayerConfig");
            TrackConfig trackConfig = RunnerAssetFactory.CreateTrackConfig();

            EnsureFolder(SceneFolder);
            string bootstrapPath = CreateBootstrapScene(gameConfig, audioConfig, playerConfig, trackConfig);
            string splashPath = CreateSimpleScene(gameConfig.SplashSceneName, "SplashRoot");
            string mainMenuPath = CreateSimpleScene(gameConfig.MainMenuSceneName, "MainMenuRoot");

            RegisterBuildScenes(bootstrapPath, splashPath, mainMenuPath);

            EditorSceneManager.OpenScene(bootstrapPath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MoveRush] Phase 1 setup complete: configs, scenes and build settings are ready.");
        }

        /// <summary>Creates only the configuration assets, leaving existing ones untouched.</summary>
        [MenuItem(MenuRoot + "Create Config Assets", priority = 1)]
        public static void CreateConfigAssetsOnly()
        {
            CreateConfigAsset<GameConfig>("GameConfig");
            CreateConfigAsset<AudioConfig>("AudioConfig");
            CreateConfigAsset<PlayerConfig>("PlayerConfig");
            RunnerAssetFactory.CreateTrackConfig();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Loads a configuration asset, creating it when it does not exist yet.
        /// Existing assets are never overwritten so designer tuning survives a re-run.
        /// </summary>
        /// <typeparam name="T">ScriptableObject type to create.</typeparam>
        /// <param name="assetName">File name without extension.</param>
        /// <returns>The existing or newly created asset.</returns>
        private static T CreateConfigAsset<T>(string assetName) where T : ScriptableObject
        {
            EnsureFolder(ConfigFolder);
            string path = $"{ConfigFolder}/{assetName}.asset";

            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[MoveRush] Created {path}");
            return asset;
        }

        /// <summary>
        /// Builds the Bootstrap scene: one persistent root carrying the composition root, the
        /// state machine, the scene loader and the three managers as children.
        /// </summary>
        /// <param name="gameConfig">Game configuration to assign.</param>
        /// <param name="audioConfig">Audio configuration to assign.</param>
        /// <param name="playerConfig">Player configuration to assign.</param>
        /// <param name="trackConfig">Track geometry to assign.</param>
        /// <returns>Asset path of the saved scene.</returns>
        private static string CreateBootstrapScene(
            GameConfig gameConfig,
            AudioConfig audioConfig,
            PlayerConfig playerConfig,
            TrackConfig trackConfig)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = new GameObject("[Bootstrap]");
            Bootstrap bootstrap = root.AddComponent<Bootstrap>();
            root.AddComponent<SceneLoader>();
            root.AddComponent<GameManager>();
            root.AddComponent<AudioListener>();

            AddManagerChild<SaveManager>(root, "SaveManager");
            AddManagerChild<AudioManager>(root, "AudioManager");
            AddManagerChild<UIManager>(root, "UIManager");

            AssignConfigs(bootstrap, gameConfig, audioConfig, playerConfig, trackConfig);
            CreateCamera("Boot Camera");

            string path = $"{SceneFolder}/{gameConfig.BootstrapSceneName}.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[MoveRush] Created {path}");
            return path;
        }

        /// <summary>Creates a minimal scene holding a camera and an empty content root.</summary>
        /// <param name="sceneName">Name of the scene, taken from configuration.</param>
        /// <param name="rootName">Name of the empty content root object.</param>
        /// <returns>Asset path of the saved scene.</returns>
        private static string CreateSimpleScene(string sceneName, string rootName)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera("Main Camera");
            GameObject contentRoot = new GameObject(rootName);
            contentRoot.transform.position = Vector3.zero;

            string path = $"{SceneFolder}/{sceneName}.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[MoveRush] Created {path}");
            return path;
        }

        /// <summary>Adds a manager component on a dedicated child object.</summary>
        /// <typeparam name="T">Manager component type.</typeparam>
        /// <param name="parent">Persistent root object.</param>
        /// <param name="childName">Name of the child object.</param>
        private static void AddManagerChild<T>(GameObject parent, string childName) where T : Component
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent.transform, false);
            child.AddComponent<T>();
        }

        /// <summary>
        /// Writes the configuration references into the bootstrap's serialised private fields.
        /// </summary>
        /// <param name="bootstrap">Bootstrap component to configure.</param>
        /// <param name="gameConfig">Game configuration to assign.</param>
        /// <param name="audioConfig">Audio configuration to assign.</param>
        /// <param name="playerConfig">Player configuration to assign.</param>
        /// <param name="trackConfig">Track geometry to assign.</param>
        private static void AssignConfigs(
            Bootstrap bootstrap,
            GameConfig gameConfig,
            AudioConfig audioConfig,
            PlayerConfig playerConfig,
            TrackConfig trackConfig)
        {
            SerializedObject serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("gameConfig").objectReferenceValue = gameConfig;
            serialized.FindProperty("audioConfig").objectReferenceValue = audioConfig;
            serialized.FindProperty("playerConfig").objectReferenceValue = playerConfig;
            serialized.FindProperty("trackConfig").objectReferenceValue = trackConfig;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Creates a camera without an audio listener. The single listener lives on the
        /// persistent bootstrap root, which avoids the duplicate-listener warning on every load.
        /// </summary>
        /// <param name="cameraName">Name of the camera object.</param>
        private static void CreateCamera(string cameraName)
        {
            GameObject cameraObject = new GameObject(cameraName, typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
        }

        /// <summary>Rewrites the build settings so Bootstrap is always index zero.</summary>
        /// <param name="scenePaths">Scene asset paths in the desired build order.</param>
        private static void RegisterBuildScenes(params string[] scenePaths)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(scenePaths.Length);
            foreach (string path in scenePaths)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Creates a project folder and every missing parent folder.</summary>
        /// <param name="folder">Project-relative folder path, for example "Assets/_Project/Scenes".</param>
        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string leaf = Path.GetFileName(folder);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
