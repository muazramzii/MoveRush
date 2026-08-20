using MoveRush.Gameplay.Coins;
using MoveRush.Gameplay.Difficulty;
using MoveRush.Gameplay.Obstacles;
using MoveRush.Gameplay.Pooling;
using MoveRush.Gameplay.Presentation;
using MoveRush.Gameplay.Run;
using MoveRush.Gameplay.Score;
using MoveRush.Gameplay.World;
using MoveRush.Player;
using MoveRush.Player.Cameras;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace MoveRush.EditorTools
{
    /// <summary>
    /// Assembles the gameplay scene: the system rack, the character, the Cinemachine rig and the
    /// temporary HUD, with every inspector reference filled in. Generating it means the wiring is
    /// reproducible and reviewable rather than a checklist somebody follows by hand.
    /// </summary>
    public static class GameSceneBuilder
    {
        /// <summary>Builds the whole scene contents into the currently open scene.</summary>
        /// <param name="assets">Configs and prefabs produced by the generator.</param>
        public static void Build(RunnerAssetSet assets)
        {
            CreateLighting();

            // The pool has to exist before any spawner asks for one, and the director collects
            // its systems from the children below, so both live on the rack root itself.
            GameObject systems = new GameObject("[Systems]");
            systems.AddComponent<PoolService>();
            systems.AddComponent<RunDirector>();

            DifficultyManager difficulty = CreateSystem<DifficultyManager>(systems.transform, "Difficulty");
            EditorWiring.SetReference(difficulty, "difficultyConfig", assets.Difficulty);

            ScoreManager score = CreateSystem<ScoreManager>(systems.transform, "Score");
            EditorWiring.SetReference(score, "scoreConfig", assets.Score);

            GameObject worldObject = new GameObject("World");
            worldObject.transform.SetParent(systems.transform, false);
            TilePool tilePool = worldObject.AddComponent<TilePool>();
            WorldGenerator generator = worldObject.AddComponent<WorldGenerator>();

            CoinManager coins = CreateSystem<CoinManager>(systems.transform, "Coins");
            ObstacleSpawner obstacles = CreateSystem<ObstacleSpawner>(systems.transform, "Obstacles");

            GameObject presentationObject = new GameObject("Presentation");
            presentationObject.transform.SetParent(systems.transform, false);
            VfxManager vfx = presentationObject.AddComponent<VfxManager>();
            presentationObject.AddComponent<GameplayAudioBinder>();

            WireTilePool(tilePool, assets);
            WireWorldGenerator(generator, tilePool, coins, obstacles, assets);
            WireCoinManager(coins, assets);
            WireObstacleSpawner(obstacles, assets);
            WireVfx(vfx, assets);

            PlayerController player = CreatePlayer(assets);
            CreateCameraRig(player);
            HudBuilder.BuildRunHud();
        }

        /// <summary>Adds a directional light so the placeholder geometry reads clearly.</summary>
        private static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Directional Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }

        /// <summary>Creates a named child carrying one system component.</summary>
        /// <typeparam name="T">Component to add.</typeparam>
        /// <param name="parent">System rack transform.</param>
        /// <param name="objectName">Name of the child.</param>
        /// <returns>The created component.</returns>
        private static T CreateSystem<T>(Transform parent, string objectName) where T : Component
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(parent, false);
            return child.AddComponent<T>();
        }

        /// <summary>Fills the weighted tile list with the generated road tile.</summary>
        /// <param name="tilePool">Pool to configure.</param>
        /// <param name="assets">Generated assets.</param>
        private static void WireTilePool(TilePool tilePool, RunnerAssetSet assets)
        {
            SerializedObject serialized = new SerializedObject(tilePool);
            SerializedProperty variants = serialized.FindProperty("variants");
            variants.ClearArray();
            variants.arraySize = 1;

            SerializedProperty element = variants.GetArrayElementAtIndex(0);
            element.FindPropertyRelative("prefab").objectReferenceValue = assets.TilePrefab;
            element.FindPropertyRelative("weight").floatValue = 1f;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Points the generator at the pool and the two content spawners.</summary>
        /// <param name="generator">Generator to configure.</param>
        /// <param name="tilePool">Tile supplier.</param>
        /// <param name="coins">Coin spawner.</param>
        /// <param name="obstacles">Obstacle spawner.</param>
        /// <param name="assets">Generated assets.</param>
        private static void WireWorldGenerator(
            WorldGenerator generator,
            TilePool tilePool,
            CoinManager coins,
            ObstacleSpawner obstacles,
            RunnerAssetSet assets)
        {
            EditorWiring.SetReference(generator, "tilePool", tilePool);
            EditorWiring.SetReference(generator, "coinManager", coins);
            EditorWiring.SetReference(generator, "obstacleSpawner", obstacles);
            EditorWiring.SetReference(generator, "runnerConfig", assets.Runner);
        }

        /// <summary>Gives the coin spawner its prefab, its patterns and its tuning.</summary>
        /// <param name="coins">Spawner to configure.</param>
        /// <param name="assets">Generated assets.</param>
        private static void WireCoinManager(CoinManager coins, RunnerAssetSet assets)
        {
            EditorWiring.SetReference(coins, "coinPrefab", assets.CoinPrefab);
            EditorWiring.SetReference(coins, "runnerConfig", assets.Runner);
            EditorWiring.SetReferenceList(coins, "patterns", assets.CoinPatterns);
        }

        /// <summary>Gives the obstacle spawner its spawn table and its tuning.</summary>
        /// <param name="obstacles">Spawner to configure.</param>
        /// <param name="assets">Generated assets.</param>
        private static void WireObstacleSpawner(ObstacleSpawner obstacles, RunnerAssetSet assets)
        {
            EditorWiring.SetReference(obstacles, "runnerConfig", assets.Runner);
            EditorWiring.SetReferenceList(obstacles, "definitions", assets.ObstacleDefinitions);
        }

        /// <summary>Gives the effect manager its three bursts.</summary>
        /// <param name="vfx">Manager to configure.</param>
        /// <param name="assets">Generated assets.</param>
        private static void WireVfx(VfxManager vfx, RunnerAssetSet assets)
        {
            EditorWiring.SetReference(vfx, "coinEffect", assets.CoinEffect);
            EditorWiring.SetReference(vfx, "hitEffect", assets.HitEffect);
            EditorWiring.SetReference(vfx, "landEffect", assets.LandEffect);
            EditorWiring.SetReference(vfx, "runnerConfig", assets.Runner);
        }

        /// <summary>Instantiates the character at the centre lane.</summary>
        /// <param name="assets">Generated assets.</param>
        /// <returns>The scene instance of the character.</returns>
        private static PlayerController CreatePlayer(RunnerAssetSet assets)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(assets.PlayerPrefab.gameObject);
            instance.transform.position = Vector3.zero;
            return instance.GetComponent<PlayerController>();
        }

        /// <summary>
        /// Builds the Cinemachine rig. The virtual camera only drives position: its rotation is
        /// authored once and left alone, which removes the yaw sway that makes a chase camera
        /// uncomfortable over a long run.
        /// </summary>
        /// <param name="player">Character the camera follows.</param>
        private static void CreateCameraRig(PlayerController player)
        {
            Vector3 offset = new Vector3(0f, 3.4f, -6.8f);
            Quaternion tilt = Quaternion.Euler(11f, 0f, 0f);

            GameObject rig = new GameObject("[Camera]");

            GameObject brainObject = new GameObject("Main Camera", typeof(UnityEngine.Camera), typeof(CinemachineBrain));
            brainObject.tag = "MainCamera";
            brainObject.transform.SetParent(rig.transform, false);
            brainObject.transform.SetPositionAndRotation(offset, tilt);

            UnityEngine.Camera camera = brainObject.GetComponent<UnityEngine.Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.42f, 0.58f, 0.75f);
            camera.farClipPlane = 400f;

            GameObject virtualObject = new GameObject(
                "RunCamera",
                typeof(CinemachineCamera),
                typeof(CinemachineFollow),
                typeof(CinemachineImpulseSource),
                typeof(RunnerCameraController));

            virtualObject.transform.SetParent(rig.transform, false);
            virtualObject.transform.SetPositionAndRotation(offset, tilt);

            CinemachineCamera virtualCamera = virtualObject.GetComponent<CinemachineCamera>();
            virtualCamera.Follow = player.transform;
            virtualCamera.Lens.FieldOfView = 58f;

            CinemachineFollow follow = virtualObject.GetComponent<CinemachineFollow>();
            follow.FollowOffset = offset;

            RunnerCameraController controller = virtualObject.GetComponent<RunnerCameraController>();
            EditorWiring.SetReference(controller, "runCamera", virtualCamera);
            EditorWiring.SetReference(controller, "landingImpulse", virtualObject.GetComponent<CinemachineImpulseSource>());
        }
    }
}
