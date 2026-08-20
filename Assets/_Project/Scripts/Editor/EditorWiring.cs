using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MoveRush.EditorTools
{
    /// <summary>
    /// Helpers the setup tools share for writing into serialised private fields.
    /// Generated scenes and prefabs have to fill inspector references that are private by design,
    /// and going through <see cref="SerializedObject"/> is what lets the generator do that without
    /// forcing every component to expose public setters it would never otherwise need.
    /// </summary>
    public static class EditorWiring
    {
        /// <summary>Assigns an object reference into a serialised field.</summary>
        /// <param name="target">Component or asset that owns the field.</param>
        /// <param name="fieldName">Exact name of the private field.</param>
        /// <param name="value">Reference to store.</param>
        public static void SetReference(Object target, string fieldName, Object value)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                Debug.LogError($"[MoveRush] Field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Fills a serialised list of object references.</summary>
        /// <param name="target">Component or asset that owns the field.</param>
        /// <param name="fieldName">Exact name of the private field.</param>
        /// <param name="values">References to store, in order.</param>
        public static void SetReferenceList(Object target, string fieldName, IList<Object> values)
        {
            if (target == null || values == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);

            if (property == null || !property.isArray)
            {
                Debug.LogError($"[MoveRush] List field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }

            property.ClearArray();
            property.arraySize = values.Count;

            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Assigns a float into a serialised field.</summary>
        /// <param name="target">Component or asset that owns the field.</param>
        /// <param name="fieldName">Exact name of the private field.</param>
        /// <param name="value">Value to store.</param>
        public static void SetFloat(Object target, string fieldName, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                Debug.LogError($"[MoveRush] Field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Assigns an int into a serialised field.</summary>
        /// <param name="target">Component or asset that owns the field.</param>
        /// <param name="fieldName">Exact name of the private field.</param>
        /// <param name="value">Value to store.</param>
        public static void SetInt(Object target, string fieldName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                Debug.LogError($"[MoveRush] Field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Assigns a string into a serialised field.</summary>
        /// <param name="target">Component or asset that owns the field.</param>
        /// <param name="fieldName">Exact name of the private field.</param>
        /// <param name="value">Value to store.</param>
        public static void SetString(Object target, string fieldName, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                Debug.LogError($"[MoveRush] Field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Loads an asset, creating it from a fresh instance when it does not exist.</summary>
        /// <typeparam name="T">ScriptableObject type.</typeparam>
        /// <param name="folder">Project folder to create it in.</param>
        /// <param name="assetName">File name without extension.</param>
        /// <param name="created">True when the asset was created by this call.</param>
        /// <returns>The existing or newly created asset.</returns>
        public static T LoadOrCreate<T>(string folder, string assetName, out bool created) where T : ScriptableObject
        {
            EnsureFolder(folder);
            string path = $"{folder}/{assetName}.asset";

            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                created = false;
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            created = true;
            Debug.Log($"[MoveRush] Created {path}");
            return asset;
        }

        /// <summary>Creates a project folder and every missing parent folder.</summary>
        /// <param name="folder">Project relative folder path.</param>
        public static void EnsureFolder(string folder)
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
