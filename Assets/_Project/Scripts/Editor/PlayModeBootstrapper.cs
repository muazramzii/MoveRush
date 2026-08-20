using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MoveRush.EditorTools
{
    /// <summary>
    /// Forces play mode to always start from the Bootstrap scene, whatever scene the developer
    /// happens to have open. Without this, pressing Play inside a work-in-progress scene skips
    /// service initialisation and produces "no service registered" errors that look like bugs
    /// but are only a workflow artefact.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeBootstrapper
    {
        private const string MenuPath = "MoveRush/Setup/Always Start From Bootstrap";
        private const string PreferenceKey = "MoveRush.AlwaysStartFromBootstrap";
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        /// <summary>Applies the stored preference as soon as the editor loads the assemblies.</summary>
        static PlayModeBootstrapper()
        {
            EditorApplication.delayCall += Apply;
        }

        /// <summary>True when play mode is forced to start from the Bootstrap scene.</summary>
        private static bool IsEnabled
        {
            get => EditorPrefs.GetBool(PreferenceKey, true);
            set => EditorPrefs.SetBool(PreferenceKey, value);
        }

        /// <summary>Toggles the behaviour from the menu.</summary>
        [MenuItem(MenuPath, priority = 20)]
        private static void Toggle()
        {
            IsEnabled = !IsEnabled;
            Apply();
        }

        /// <summary>Draws the check mark next to the menu entry.</summary>
        /// <returns>Always true; the entry is never disabled.</returns>
        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, IsEnabled);
            return true;
        }

        /// <summary>Sets or clears Unity's play mode start scene.</summary>
        private static void Apply()
        {
            if (!IsEnabled)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            SceneAsset bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
            if (bootstrapScene == null)
            {
                return;
            }

            EditorSceneManager.playModeStartScene = bootstrapScene;
        }
    }
}
