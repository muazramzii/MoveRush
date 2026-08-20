using MoveRush.Core.Config;
using MoveRush.Gameplay.Coins;
using MoveRush.Gameplay.Config;
using MoveRush.Gameplay.Obstacles;
using MoveRush.Player.Config;
using UnityEditor;
using UnityEngine;

namespace MoveRush.EditorTools
{
    /// <summary>
    /// Creates and seeds every Phase 2 configuration asset. Existing assets are loaded and left
    /// alone, so re-running the setup never overwrites tuning a designer has already done.
    /// </summary>
    public static class RunnerAssetFactory
    {
        private const string ConfigFolder = "Assets/_Project/ScriptableObjects/Config";
        private const string PatternFolder = "Assets/_Project/ScriptableObjects/CoinPatterns";
        private const string ObstacleFolder = "Assets/_Project/ScriptableObjects/Obstacles";

        /// <summary>Creates the track geometry asset with the three specified lanes.</summary>
        /// <returns>The track config.</returns>
        public static TrackConfig CreateTrackConfig()
        {
            TrackConfig asset = EditorWiring.LoadOrCreate<TrackConfig>(ConfigFolder, "TrackConfig", out bool created);

            if (created)
            {
                SerializedObject serialized = new SerializedObject(asset);
                SerializedProperty lanes = serialized.FindProperty("laneOffsets");
                lanes.arraySize = 3;
                lanes.GetArrayElementAtIndex(0).floatValue = -2.5f;
                lanes.GetArrayElementAtIndex(1).floatValue = 0f;
                lanes.GetArrayElementAtIndex(2).floatValue = 2.5f;
                serialized.FindProperty("tileLength").floatValue = 20f;
                serialized.FindProperty("laneWidth").floatValue = 2.5f;
                serialized.FindProperty("groundHeight").floatValue = 0f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return asset;
        }

        /// <summary>Creates the world and coin tuning asset.</summary>
        /// <returns>The runner config.</returns>
        public static RunnerConfig CreateRunnerConfig()
        {
            return EditorWiring.LoadOrCreate<RunnerConfig>(ConfigFolder, "RunnerConfig", out _);
        }

        /// <summary>Creates the score formula asset.</summary>
        /// <returns>The score config.</returns>
        public static ScoreConfig CreateScoreConfig()
        {
            return EditorWiring.LoadOrCreate<ScoreConfig>(ConfigFolder, "ScoreConfig", out _);
        }

        /// <summary>Creates the movement feel asset.</summary>
        /// <returns>The player movement config.</returns>
        public static PlayerMovementConfig CreateMovementConfig()
        {
            return EditorWiring.LoadOrCreate<PlayerMovementConfig>(ConfigFolder, "PlayerMovementConfig", out _);
        }

        /// <summary>
        /// Creates the difficulty ramp and seeds it with the specified steps:
        /// 0 m at 5 m/s, 500 m at 6, 1000 m at 7, 2000 m at 8 and 3000 m at 9.
        /// </summary>
        /// <returns>The difficulty config.</returns>
        public static DifficultyConfig CreateDifficultyConfig()
        {
            DifficultyConfig asset =
                EditorWiring.LoadOrCreate<DifficultyConfig>(ConfigFolder, "DifficultyConfig", out bool created);

            if (!created)
            {
                return asset;
            }

            SerializedObject serialized = new SerializedObject(asset);
            SerializedProperty steps = serialized.FindProperty("steps");
            steps.ClearArray();

            AddStep(steps, 0, 0f, 5f);
            AddStep(steps, 1, 500f, 6f);
            AddStep(steps, 2, 1000f, 7f);
            AddStep(steps, 3, 2000f, 8f);
            AddStep(steps, 4, 3000f, 9f);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        /// <summary>Creates the four coin patterns named in the brief.</summary>
        /// <returns>The pattern assets, in order: straight, zigzag, triangle, snake.</returns>
        public static CoinPattern[] CreateCoinPatterns()
        {
            return new[]
            {
                CreatePattern("CoinPattern_Straight", CoinPatternShape.Straight, 8, 3f),
                CreatePattern("CoinPattern_Zigzag", CoinPatternShape.Zigzag, 10, 2f),
                CreatePattern("CoinPattern_Triangle", CoinPatternShape.Triangle, 7, 1.5f),
                CreatePattern("CoinPattern_Snake", CoinPatternShape.Snake, 12, 1.5f)
            };
        }

        /// <summary>
        /// Creates the obstacle spawn table. Tiers gate what can appear: barriers and laser gates
        /// teach jump and slide from the first meters, buses arrive at 500 m and trains at 1000 m.
        /// </summary>
        /// <param name="barrier">Barrier prefab.</param>
        /// <param name="laserGate">Laser gate prefab.</param>
        /// <param name="bus">Bus prefab.</param>
        /// <param name="train">Train prefab.</param>
        /// <returns>The definition assets.</returns>
        public static ObstacleDefinition[] CreateObstacleDefinitions(
            ObstacleBase barrier,
            ObstacleBase laserGate,
            ObstacleBase bus,
            ObstacleBase train)
        {
            return new[]
            {
                CreateDefinition("ObstacleDefinition_Barrier", barrier, 3f, 0),
                CreateDefinition("ObstacleDefinition_LaserGate", laserGate, 2f, 0),
                CreateDefinition("ObstacleDefinition_Bus", bus, 2f, 1),
                CreateDefinition("ObstacleDefinition_Train", train, 2f, 2)
            };
        }

        /// <summary>Creates one coin pattern asset.</summary>
        /// <param name="assetName">File name without extension.</param>
        /// <param name="shape">Shape of the pattern.</param>
        /// <param name="count">Number of coins.</param>
        /// <param name="weight">Relative pick chance.</param>
        /// <returns>The pattern asset.</returns>
        private static CoinPattern CreatePattern(string assetName, CoinPatternShape shape, int count, float weight)
        {
            CoinPattern asset = EditorWiring.LoadOrCreate<CoinPattern>(PatternFolder, assetName, out bool created);

            if (created)
            {
                EditorWiring.SetInt(asset, "shape", (int)shape);
                EditorWiring.SetInt(asset, "coinCount", count);
                EditorWiring.SetFloat(asset, "weight", weight);
            }

            return asset;
        }

        /// <summary>Creates one obstacle definition asset.</summary>
        /// <param name="assetName">File name without extension.</param>
        /// <param name="prefab">Obstacle prefab.</param>
        /// <param name="weight">Relative pick chance.</param>
        /// <param name="minTier">Difficulty tier the obstacle unlocks at.</param>
        /// <returns>The definition asset.</returns>
        private static ObstacleDefinition CreateDefinition(string assetName, ObstacleBase prefab, float weight, int minTier)
        {
            ObstacleDefinition asset =
                EditorWiring.LoadOrCreate<ObstacleDefinition>(ObstacleFolder, assetName, out bool created);

            if (created)
            {
                EditorWiring.SetFloat(asset, "weight", weight);
                EditorWiring.SetInt(asset, "minTier", minTier);
            }

            EditorWiring.SetReference(asset, "prefab", prefab);
            return asset;
        }

        /// <summary>Writes one entry into the serialised difficulty ramp.</summary>
        /// <param name="steps">Serialised list property.</param>
        /// <param name="index">Index to write.</param>
        /// <param name="distance">Distance the step activates at.</param>
        /// <param name="speed">Speed of the step.</param>
        private static void AddStep(SerializedProperty steps, int index, float distance, float speed)
        {
            steps.InsertArrayElementAtIndex(index);
            SerializedProperty element = steps.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("distance").floatValue = distance;
            element.FindPropertyRelative("speed").floatValue = speed;
        }
    }
}
