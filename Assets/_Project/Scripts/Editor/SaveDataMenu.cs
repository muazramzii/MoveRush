using System.IO;
using MoveRush.Managers.Persistence;
using UnityEditor;
using UnityEngine;

namespace MoveRush.EditorTools
{
    /// <summary>
    /// Editor shortcuts for inspecting and clearing the local player profile. QA needs a
    /// reliable way to reproduce a first-run experience, and support needs the file path;
    /// both are one menu click away instead of a manual hunt through the persistent data path.
    /// </summary>
    public static class SaveDataMenu
    {
        private const string MenuRoot = "MoveRush/Save Data/";

        /// <summary>Reveals the folder that holds the profile in the operating system's file browser.</summary>
        [MenuItem(MenuRoot + "Reveal Save Location", priority = 0)]
        private static void RevealSaveLocation()
        {
            string path = GetSavePath();
            string folder = Path.GetDirectoryName(path);

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                Debug.LogWarning($"[MoveRush] No save folder exists yet. Expected location: {path}");
                return;
            }

            EditorUtility.RevealInFinder(File.Exists(path) ? path : folder);
        }

        /// <summary>Prints the resolved profile path and its contents to the console.</summary>
        [MenuItem(MenuRoot + "Log Save Contents", priority = 1)]
        private static void LogSaveContents()
        {
            string path = GetSavePath();

            if (!File.Exists(path))
            {
                Debug.Log($"[MoveRush] No profile stored yet. Expected location: {path}");
                return;
            }

            Debug.Log($"[MoveRush] Profile at {path}:\n{File.ReadAllText(path)}");
        }

        /// <summary>Deletes the stored profile after an explicit confirmation.</summary>
        [MenuItem(MenuRoot + "Delete Save File", priority = 2)]
        private static void DeleteSaveFile()
        {
            string path = GetSavePath();

            if (!File.Exists(path))
            {
                Debug.Log($"[MoveRush] Nothing to delete at {path}");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete save file",
                $"Permanently delete the local player profile?\n\n{path}",
                "Delete",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            File.Delete(path);
            DeleteIfPresent(path + ".bak");
            DeleteIfPresent(path + ".tmp");
            Debug.Log($"[MoveRush] Deleted the profile at {path}");
        }

        /// <summary>
        /// Resolves the profile path, preferring the location recorded by the running game and
        /// falling back to the default file name in the persistent data path.
        /// </summary>
        /// <returns>Absolute path to the profile file.</returns>
        private static string GetSavePath()
        {
            string stored = PlayerPrefs.GetString(JsonSaveRepository.SavePathPrefsKey, string.Empty);
            return string.IsNullOrEmpty(stored)
                ? Path.Combine(Application.persistentDataPath, "moverush.save.json")
                : stored;
        }

        /// <summary>Deletes a file when it exists.</summary>
        /// <param name="path">Absolute path to delete.</param>
        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
