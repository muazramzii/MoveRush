using MoveRush.Gameplay.Obstacles;
using UnityEngine;

namespace MoveRush.EditorTools
{
    /// <summary>
    /// Generates the four obstacle prefabs. Each one is sized so its trigger volume alone decides
    /// the outcome: a barrier sits low enough to be jumped, a laser gate hangs high enough to be
    /// slid under, and the two vehicles are tall enough that only a lane change gets past them.
    /// The state checks in the obstacle classes then act as a second, redundant guard.
    /// </summary>
    public static class ObstaclePrefabFactory
    {
        private const float LaneWidth = 2.2f;

        /// <summary>Creates the low barrier, cleared with a jump or a lane change.</summary>
        /// <returns>The barrier prefab.</returns>
        public static ObstacleBase CreateBarrier()
        {
            GameObject root = new GameObject("Obstacle_Barrier");
            Material material = PrimitiveBuilder.GetMaterial("M_Barrier", new Color(0.95f, 0.45f, 0.1f));

            PrimitiveBuilder.CreateBox("Mesh", root.transform, new Vector3(0f, 0.4f, 0f),
                new Vector3(LaneWidth, 0.8f, 0.5f), material);

            PrimitiveBuilder.AddTrigger(root, new Vector3(0f, 0.4f, 0f), new Vector3(LaneWidth, 0.8f, 0.6f));

            Barrier barrier = root.AddComponent<Barrier>();
            EditorWiring.SetString(barrier, "obstacleId", "barrier");
            EditorWiring.SetFloat(barrier, "lengthMeters", 1.2f);
            EditorWiring.SetFloat(barrier, "clearOffset", 1.5f);

            return PrimitiveBuilder.SaveAsPrefab<ObstacleBase>(root, "Obstacle_Barrier");
        }

        /// <summary>Creates the overhead laser gate, cleared with a slide or a lane change.</summary>
        /// <returns>The laser gate prefab.</returns>
        public static ObstacleBase CreateLaserGate()
        {
            GameObject root = new GameObject("Obstacle_LaserGate");
            Material frame = PrimitiveBuilder.GetMaterial("M_LaserFrame", new Color(0.25f, 0.25f, 0.3f));
            Material beam = PrimitiveBuilder.GetMaterial("M_LaserBeam", new Color(1f, 0.15f, 0.25f));

            PrimitiveBuilder.CreateBox("Post_L", root.transform, new Vector3(-1.05f, 1.2f, 0f),
                new Vector3(0.16f, 2.4f, 0.16f), frame);
            PrimitiveBuilder.CreateBox("Post_R", root.transform, new Vector3(1.05f, 1.2f, 0f),
                new Vector3(0.16f, 2.4f, 0.16f), frame);
            PrimitiveBuilder.CreateBox("Beam", root.transform, new Vector3(0f, 1.55f, 0f),
                new Vector3(2.1f, 0.14f, 0.14f), beam);

            // The trigger starts at y 1.1, which the standing capsule reaches and the sliding one
            // does not, so the geometry and the slide rule agree.
            PrimitiveBuilder.AddTrigger(root, new Vector3(0f, 1.55f, 0f), new Vector3(LaneWidth, 0.9f, 0.4f));

            LaserGate gate = root.AddComponent<LaserGate>();
            EditorWiring.SetString(gate, "obstacleId", "laser_gate");
            EditorWiring.SetFloat(gate, "lengthMeters", 1f);
            EditorWiring.SetFloat(gate, "clearOffset", 1.5f);

            return PrimitiveBuilder.SaveAsPrefab<ObstacleBase>(root, "Obstacle_LaserGate");
        }

        /// <summary>Creates the bus, a short high blocker that can only be dodged.</summary>
        /// <returns>The bus prefab.</returns>
        public static ObstacleBase CreateBus()
        {
            GameObject root = new GameObject("Obstacle_Bus");
            Material body = PrimitiveBuilder.GetMaterial("M_Bus", new Color(0.2f, 0.5f, 0.9f));
            Material glass = PrimitiveBuilder.GetMaterial("M_Glass", new Color(0.1f, 0.15f, 0.2f));

            PrimitiveBuilder.CreateBox("Mesh", root.transform, new Vector3(0f, 1.2f, 0f),
                new Vector3(LaneWidth, 2.4f, 4.5f), body);
            PrimitiveBuilder.CreateBox("Windscreen", root.transform, new Vector3(0f, 1.7f, -2.27f),
                new Vector3(LaneWidth * 0.85f, 0.9f, 0.06f), glass);

            PrimitiveBuilder.AddTrigger(root, new Vector3(0f, 1.2f, 0f), new Vector3(LaneWidth, 2.4f, 4.5f));

            Bus bus = root.AddComponent<Bus>();
            EditorWiring.SetString(bus, "obstacleId", "bus");
            EditorWiring.SetFloat(bus, "lengthMeters", 4.5f);
            EditorWiring.SetFloat(bus, "clearOffset", 3f);

            return PrimitiveBuilder.SaveAsPrefab<ObstacleBase>(root, "Obstacle_Bus");
        }

        /// <summary>Creates the train, a long blocker that forces an early lane decision.</summary>
        /// <returns>The train prefab.</returns>
        public static ObstacleBase CreateTrain()
        {
            GameObject root = new GameObject("Obstacle_Train");
            Material body = PrimitiveBuilder.GetMaterial("M_Train", new Color(0.8f, 0.2f, 0.25f));
            Material trim = PrimitiveBuilder.GetMaterial("M_TrainTrim", new Color(0.9f, 0.85f, 0.3f));

            PrimitiveBuilder.CreateBox("Mesh", root.transform, new Vector3(0f, 1.3f, 0f),
                new Vector3(LaneWidth, 2.6f, 9f), body);
            PrimitiveBuilder.CreateBox("Stripe", root.transform, new Vector3(0f, 1.9f, 0f),
                new Vector3(LaneWidth * 1.01f, 0.25f, 9.01f), trim);

            PrimitiveBuilder.AddTrigger(root, new Vector3(0f, 1.3f, 0f), new Vector3(LaneWidth, 2.6f, 9f));

            Train train = root.AddComponent<Train>();
            EditorWiring.SetString(train, "obstacleId", "train");
            EditorWiring.SetFloat(train, "lengthMeters", 9f);
            EditorWiring.SetFloat(train, "clearOffset", 5.5f);

            return PrimitiveBuilder.SaveAsPrefab<ObstacleBase>(root, "Obstacle_Train");
        }
    }
}
