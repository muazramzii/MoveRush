using UnityEditor;
using UnityEngine;

namespace MoveRush.EditorTools
{
    /// <summary>
    /// Builds the placeholder art the vertical slice runs on. Generating blocking geometry from
    /// primitives means the slice is playable and tunable before any art exists, and every prefab
    /// keeps the same structure the final art will drop into.
    /// </summary>
    public static class PrimitiveBuilder
    {
        /// <summary>Folder the generated prefabs are written to.</summary>
        public const string PrefabFolder = "Assets/_Project/Prefabs/Runner";

        /// <summary>Folder the generated materials are written to.</summary>
        public const string MaterialFolder = "Assets/_Project/Materials";

        /// <summary>
        /// Loads or creates an unlit-safe material. The URP Lit shader is used when the pipeline
        /// is installed, with the built-in Standard shader as a fallback so the generator still
        /// works before the URP assets have been created.
        /// </summary>
        /// <param name="materialName">File name without extension.</param>
        /// <param name="color">Base colour.</param>
        /// <returns>The material asset.</returns>
        public static Material GetMaterial(string materialName, Color color)
        {
            EditorWiring.EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/{materialName}.mat";

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader);

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>Creates a collider-free cube used purely as blocking geometry.</summary>
        /// <param name="objectName">Name of the created object.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="localPosition">Local position.</param>
        /// <param name="localScale">Local scale.</param>
        /// <param name="material">Material to apply.</param>
        /// <returns>The created object.</returns>
        public static GameObject CreateBox(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            return CreatePrimitive(PrimitiveType.Cube, objectName, parent, localPosition, localScale, material);
        }

        /// <summary>Creates a collider-free primitive.</summary>
        /// <param name="type">Primitive shape.</param>
        /// <param name="objectName">Name of the created object.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="localPosition">Local position.</param>
        /// <param name="localScale">Local scale.</param>
        /// <param name="material">Material to apply.</param>
        /// <returns>The created object.</returns>
        public static GameObject CreatePrimitive(
            PrimitiveType type,
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject instance = GameObject.CreatePrimitive(type);
            instance.name = objectName;

            Collider collider = instance.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localScale = localScale;

            if (material != null)
            {
                instance.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            return instance;
        }

        /// <summary>Creates an empty child object.</summary>
        /// <param name="objectName">Name of the created object.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="localPosition">Local position.</param>
        /// <returns>The created object.</returns>
        public static GameObject CreateEmpty(string objectName, Transform parent, Vector3 localPosition)
        {
            GameObject instance = new GameObject(objectName);
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            return instance;
        }

        /// <summary>
        /// Saves a scene object as a prefab and removes the temporary instance, returning the
        /// component the callers actually need a reference to.
        /// </summary>
        /// <typeparam name="T">Component to fetch from the saved prefab.</typeparam>
        /// <param name="root">Temporary scene object to save.</param>
        /// <param name="prefabName">File name without extension.</param>
        /// <returns>The component on the saved prefab asset.</returns>
        public static T SaveAsPrefab<T>(GameObject root, string prefabName) where T : Component
        {
            EditorWiring.EnsureFolder(PrefabFolder);
            string path = $"{PrefabFolder}/{prefabName}.prefab";

            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            Debug.Log($"[MoveRush] Created {path}");
            return asset != null ? asset.GetComponent<T>() : null;
        }

        /// <summary>Adds a box trigger to an object.</summary>
        /// <param name="target">Object that receives the collider.</param>
        /// <param name="center">Local centre of the box.</param>
        /// <param name="size">Size of the box.</param>
        /// <returns>The created collider.</returns>
        public static BoxCollider AddTrigger(GameObject target, Vector3 center, Vector3 size)
        {
            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = center;
            collider.size = size;
            return collider;
        }
    }
}
