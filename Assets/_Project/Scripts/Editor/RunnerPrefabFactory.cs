using MoveRush.Gameplay.Coins;
using MoveRush.Gameplay.Presentation;
using MoveRush.Gameplay.World;
using MoveRush.Player;
using MoveRush.Player.Config;
using UnityEngine;

namespace MoveRush.EditorTools
{
    /// <summary>
    /// Generates the road, coin, effect and character prefabs. The character is built as a root
    /// that carries logic and physics, with a separate visual pivot at the feet: the slide scales
    /// that pivot and the animator rotates it, so neither ever fights the other for the transform.
    /// </summary>
    public static class RunnerPrefabFactory
    {
        private const float TrackWidth = 7.5f;
        private const float TileLength = 20f;

        /// <summary>
        /// Creates the road tile. The ground is a single stretched cube, which lets the tile
        /// resize itself at runtime when the configured tile length changes.
        /// </summary>
        /// <returns>The road tile prefab.</returns>
        public static RoadTile CreateRoadTile()
        {
            GameObject root = new GameObject("RoadTile");
            Material road = PrimitiveBuilder.GetMaterial("M_Road", new Color(0.17f, 0.18f, 0.21f));
            Material stripe = PrimitiveBuilder.GetMaterial("M_LaneStripe", new Color(0.85f, 0.85f, 0.8f));

            GameObject ground = PrimitiveBuilder.CreateBox("Ground", root.transform,
                new Vector3(0f, -0.25f, TileLength * 0.5f), new Vector3(TrackWidth, 0.5f, TileLength), road);

            // The stripes are children of the stretched ground, so they follow the tile length
            // automatically instead of needing their own resize pass.
            PrimitiveBuilder.CreateBox("Stripe_L", ground.transform, new Vector3(-0.1667f, 0.51f, 0f),
                new Vector3(0.02f, 0.1f, 1f), stripe);
            PrimitiveBuilder.CreateBox("Stripe_R", ground.transform, new Vector3(0.1667f, 0.51f, 0f),
                new Vector3(0.02f, 0.1f, 1f), stripe);

            GameObject content = PrimitiveBuilder.CreateEmpty("Content", root.transform, Vector3.zero);

            RoadTile tile = root.AddComponent<RoadTile>();
            EditorWiring.SetReference(tile, "contentRoot", content.transform);
            EditorWiring.SetReference(tile, "groundRoot", ground.transform);
            EditorWiring.SetFloat(tile, "length", TileLength);

            return PrimitiveBuilder.SaveAsPrefab<RoadTile>(root, "RoadTile");
        }

        /// <summary>Creates the coin, a trigger with a spinning disc.</summary>
        /// <returns>The coin prefab.</returns>
        public static Coin CreateCoin()
        {
            GameObject root = new GameObject("Coin");
            Material gold = PrimitiveBuilder.GetMaterial("M_Coin", new Color(1f, 0.82f, 0.15f));

            GameObject visual = PrimitiveBuilder.CreatePrimitive(PrimitiveType.Cylinder, "Visual", root.transform,
                Vector3.zero, new Vector3(0.45f, 0.05f, 0.45f), gold);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.55f;

            Coin coin = root.AddComponent<Coin>();
            EditorWiring.SetReference(coin, "visual", visual.transform);
            EditorWiring.SetReference(coin, "pickupCollider", trigger);

            return PrimitiveBuilder.SaveAsPrefab<Coin>(root, "Coin");
        }

        /// <summary>Creates a pooled burst effect.</summary>
        /// <param name="prefabName">File name without extension.</param>
        /// <param name="color">Colour of the burst.</param>
        /// <param name="lifetime">Seconds the burst lives for.</param>
        /// <param name="size">Diameter of the burst at spawn.</param>
        /// <returns>The effect prefab.</returns>
        public static PooledEffect CreateEffect(string prefabName, Color color, float lifetime, float size)
        {
            GameObject root = new GameObject(prefabName);
            Material material = PrimitiveBuilder.GetMaterial($"M_{prefabName}", color);

            GameObject burst = PrimitiveBuilder.CreatePrimitive(PrimitiveType.Sphere, "Burst", root.transform,
                Vector3.zero, Vector3.one * size, material);

            PooledEffect effect = root.AddComponent<PooledEffect>();
            EditorWiring.SetReference(effect, "scaleRoot", burst.transform);
            EditorWiring.SetFloat(effect, "lifetime", lifetime);
            EditorWiring.SetFloat(effect, "endScale", 2.4f);

            return PrimitiveBuilder.SaveAsPrefab<PooledEffect>(root, prefabName);
        }

        /// <summary>
        /// Creates the character. The rigidbody is kinematic and gravity free on purpose: motion
        /// is integrated by the movement modules, and the body exists only so obstacle and coin
        /// triggers fire against it.
        /// </summary>
        /// <param name="movementConfig">Movement tuning to assign.</param>
        /// <returns>The player prefab.</returns>
        public static PlayerController CreatePlayer(PlayerMovementConfig movementConfig)
        {
            GameObject root = new GameObject("Player") { tag = "Player" };
            Material bodyMaterial = PrimitiveBuilder.GetMaterial("M_Player", new Color(0.2f, 0.85f, 0.5f));
            Material faceMaterial = PrimitiveBuilder.GetMaterial("M_PlayerFace", new Color(0.1f, 0.2f, 0.15f));

            GameObject visual = PrimitiveBuilder.CreateEmpty("Visual", root.transform, Vector3.zero);
            GameObject body = PrimitiveBuilder.CreatePrimitive(PrimitiveType.Capsule, "Body", visual.transform,
                new Vector3(0f, 1f, 0f), new Vector3(0.8f, 1f, 0.8f), bodyMaterial);

            // A face marker makes the running direction obvious with placeholder art.
            PrimitiveBuilder.CreateBox("Face", body.transform, new Vector3(0f, 0.32f, 0.45f),
                new Vector3(0.5f, 0.18f, 0.15f), faceMaterial);

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.4f;
            capsule.center = new Vector3(0f, 1f, 0f);

            LaneMovement lane = root.AddComponent<LaneMovement>();
            JumpController jump = root.AddComponent<JumpController>();
            SlideController slide = root.AddComponent<SlideController>();
            CharacterAnimator animator = root.AddComponent<CharacterAnimator>();
            PlayerController controller = root.AddComponent<PlayerController>();

            EditorWiring.SetReference(slide, "bodyCollider", capsule);
            EditorWiring.SetReference(slide, "visualRoot", visual.transform);
            EditorWiring.SetReference(animator, "visualRoot", visual.transform);

            EditorWiring.SetReference(controller, "movementConfig", movementConfig);
            EditorWiring.SetReference(controller, "laneMovement", lane);
            EditorWiring.SetReference(controller, "jumpController", jump);
            EditorWiring.SetReference(controller, "slideController", slide);
            EditorWiring.SetReference(controller, "characterAnimator", animator);

            return PrimitiveBuilder.SaveAsPrefab<PlayerController>(root, "Player");
        }
    }
}
