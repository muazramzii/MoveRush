using MoveRush.Gameplay.UI;
using TMPro;
using UnityEngine;

namespace MoveRush.EditorTools
{
    /// <summary>
    /// Builds the temporary run readout. It is intentionally crude - the production UI is a later
    /// phase - but it is a real screen: it implements the Phase 1 screen contract and reads only
    /// from events, so swapping it for the designed HUD touches nothing else.
    /// </summary>
    public static class HudBuilder
    {
        /// <summary>Builds the in-run HUD canvas and returns the view component.</summary>
        /// <returns>The HUD view, or null when TextMeshPro resources are missing.</returns>
        public static RunHudView BuildRunHud()
        {
            if (!HasTextMeshProResources())
            {
                return null;
            }

            GameObject canvasObject = CreateCanvas("[HUD]");
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

            GameObject hudRoot = CreateChild("HudRoot", canvasRect);
            GameObject overRoot = CreateChild("GameOverRoot", canvasRect);

            TMP_Text score = CreateText("ScoreText", hudRoot.transform, new Vector2(0.5f, 1f),
                new Vector2(0f, -70f), new Vector2(600f, 90f), 64f, TextAlignmentOptions.Center, "0");
            TMP_Text distance = CreateText("DistanceText", hudRoot.transform, new Vector2(0f, 1f),
                new Vector2(190f, -60f), new Vector2(340f, 60f), 36f, TextAlignmentOptions.Left, "0 M");
            TMP_Text coins = CreateText("CoinsText", hudRoot.transform, new Vector2(1f, 1f),
                new Vector2(-190f, -60f), new Vector2(340f, 60f), 36f, TextAlignmentOptions.Right, "0");
            TMP_Text combo = CreateText("ComboText", hudRoot.transform, new Vector2(0.5f, 1f),
                new Vector2(0f, -150f), new Vector2(600f, 60f), 34f, TextAlignmentOptions.Center, string.Empty);

            TMP_Text finalScore = CreateText("FinalScoreText", overRoot.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0f, 60f), new Vector2(900f, 100f), 72f, TextAlignmentOptions.Center, "SCORE 0");
            TMP_Text highScore = CreateText("HighScoreText", overRoot.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f), new Vector2(900f, 70f), 40f, TextAlignmentOptions.Center, string.Empty);
            TMP_Text hint = CreateText("HintText", overRoot.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0f, -110f), new Vector2(1100f, 70f), 34f, TextAlignmentOptions.Center,
                "PRESS A, D, SPACE OR S TO RUN AGAIN");

            combo.color = new Color(1f, 0.85f, 0.2f);
            highScore.color = new Color(0.3f, 1f, 0.55f);
            overRoot.SetActive(false);

            RunHudView view = canvasObject.AddComponent<RunHudView>();
            EditorWiring.SetReference(view, "hudRoot", hudRoot);
            EditorWiring.SetReference(view, "gameOverRoot", overRoot);
            EditorWiring.SetReference(view, "scoreText", score);
            EditorWiring.SetReference(view, "distanceText", distance);
            EditorWiring.SetReference(view, "coinsText", coins);
            EditorWiring.SetReference(view, "comboText", combo);
            EditorWiring.SetReference(view, "finalScoreText", finalScore);
            EditorWiring.SetReference(view, "highScoreText", highScore);
            EditorWiring.SetReference(view, "hintText", hint);

            return view;
        }

        /// <summary>Builds the main menu prompt that starts a run.</summary>
        public static void BuildMenuPrompt()
        {
            if (!HasTextMeshProResources())
            {
                return;
            }

            GameObject canvasObject = CreateCanvas("[MainMenuHUD]");
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

            CreateText("TitleText", canvasRect, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f),
                new Vector2(1000f, 140f), 96f, TextAlignmentOptions.Center, "MOVERUSH");
            CreateText("PromptText", canvasRect, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f),
                new Vector2(1200f, 80f), 36f, TextAlignmentOptions.Center,
                "A / D LANE     SPACE JUMP     S SLIDE");
            CreateText("StartText", canvasRect, new Vector2(0.5f, 0.5f), new Vector2(0f, -130f),
                new Vector2(1200f, 80f), 40f, TextAlignmentOptions.Center, "PRESS ANY CONTROL TO START");
        }

        /// <summary>Creates a screen space canvas that scales with the window.</summary>
        /// <param name="canvasName">Name of the canvas object.</param>
        /// <returns>The canvas object.</returns>
        private static GameObject CreateCanvas(string canvasName)
        {
            GameObject canvasObject = new GameObject(canvasName, typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvasObject;
        }

        /// <summary>Creates a stretched empty child used as a group root.</summary>
        /// <param name="childName">Name of the child.</param>
        /// <param name="parent">Parent rect transform.</param>
        /// <returns>The created object.</returns>
        private static GameObject CreateChild(string childName, RectTransform parent)
        {
            GameObject child = new GameObject(childName, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return child;
        }

        /// <summary>Creates one anchored TextMeshPro label.</summary>
        /// <param name="labelName">Name of the object.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="anchor">Anchor and pivot, in the 0..1 range.</param>
        /// <param name="anchoredPosition">Offset from the anchor.</param>
        /// <param name="size">Size of the rect.</param>
        /// <param name="fontSize">Font size.</param>
        /// <param name="alignment">Text alignment.</param>
        /// <param name="content">Initial text.</param>
        /// <returns>The created label.</returns>
        private static TMP_Text CreateText(
            string labelName,
            Transform parent,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            TextAlignmentOptions alignment,
            string content)
        {
            GameObject labelObject = new GameObject(labelName, typeof(TextMeshProUGUI));
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();

            RectTransform rect = label.rectTransform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            label.text = content;
            label.fontSize = fontSize;
            label.alignment = alignment;

            return label;
        }

        /// <summary>
        /// Verifies the TextMeshPro runtime assets exist. Without them every label would render
        /// with no font, so the generator warns once and skips the UI rather than producing a
        /// broken scene the developer then has to debug.
        /// </summary>
        /// <returns>True when TextMeshPro is ready to use.</returns>
        private static bool HasTextMeshProResources()
        {
            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
            {
                return true;
            }

            Debug.LogWarning(
                "[MoveRush] TextMeshPro resources are missing. Run Window > TextMeshPro > " +
                "Import TMP Essential Resources, then run the Phase 2 setup again to build the HUD.");
            return false;
        }
    }
}
