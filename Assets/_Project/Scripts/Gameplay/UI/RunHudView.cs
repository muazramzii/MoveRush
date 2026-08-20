using MoveRush.Core;
using MoveRush.Core.Events;
using MoveRush.Core.Services;
using TMPro;
using UnityEngine;

namespace MoveRush.Gameplay.UI
{
    /// <summary>
    /// Temporary run readout: score, distance, coins, combo and the result panel.
    /// It is deliberately plain - the production UI arrives in a later phase - but it is wired
    /// the way the real one will be: it implements <see cref="IUIScreen"/>, registers with the
    /// Phase 1 UI manager and is driven purely by events, so replacing the visuals later touches
    /// no gameplay code.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunHudView : MonoBehaviour, IUIScreen
    {
        [Header("Identity")]
        [Tooltip("Identifier the UI manager addresses this screen by.")]
        [SerializeField] private string screenId = "RunHud";

        [Header("Roots")]
        [Tooltip("Container holding the live readout.")]
        [SerializeField] private GameObject hudRoot;

        [Tooltip("Container holding the result panel.")]
        [SerializeField] private GameObject gameOverRoot;

        [Header("Live Readout")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text comboText;

        [Header("Result Panel")]
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private TMP_Text hintText;

        private IUIService uiService;

        /// <inheritdoc />
        public string ScreenId => screenId;

        /// <inheritdoc />
        public bool IsVisible { get; private set; }

        /// <summary>Subscribes to the events the readout is built from.</summary>
        private void Awake()
        {
            EventBus<ScoreChangedEvent>.Subscribe(OnScoreChanged);
            EventBus<ComboChangedEvent>.Subscribe(OnComboChanged);
            EventBus<RunEndedEvent>.Subscribe(OnRunEnded);
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
        }

        /// <summary>Registers the screen with the UI manager and shows it.</summary>
        private void Start()
        {
            if (ServiceLocator.TryGet(out uiService))
            {
                uiService.RegisterScreen(this);
                uiService.ShowScreen(screenId);
            }
            else
            {
                Prepare();
                Show();
            }
        }

        /// <summary>Releases every subscription with the scene.</summary>
        private void OnDestroy()
        {
            EventBus<ScoreChangedEvent>.Unsubscribe(OnScoreChanged);
            EventBus<ComboChangedEvent>.Unsubscribe(OnComboChanged);
            EventBus<RunEndedEvent>.Unsubscribe(OnRunEnded);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);

            uiService?.UnregisterScreen(this);
        }

        /// <inheritdoc />
        public void Prepare()
        {
            SetResultVisible(false);
            SetText(scoreText, "0");
            SetText(distanceText, "0 M");
            SetText(coinsText, "0");
            SetText(comboText, string.Empty);
        }

        /// <inheritdoc />
        public void Show()
        {
            IsVisible = true;

            if (hudRoot != null)
            {
                hudRoot.SetActive(true);
            }
        }

        /// <inheritdoc />
        public void Hide()
        {
            IsVisible = false;

            if (hudRoot != null)
            {
                hudRoot.SetActive(false);
            }
        }

        /// <summary>Refreshes the live readout.</summary>
        /// <param name="payload">Score payload.</param>
        private void OnScoreChanged(ScoreChangedEvent payload)
        {
            SetText(scoreText, payload.Score.ToString());
            SetText(distanceText, $"{Mathf.FloorToInt(payload.Distance)} M");
            SetText(coinsText, payload.Coins.ToString());
        }

        /// <summary>Shows the combo only once it is worth showing.</summary>
        /// <param name="payload">Combo payload.</param>
        private void OnComboChanged(ComboChangedEvent payload)
        {
            SetText(comboText, payload.Combo > 1 ? $"COMBO x{payload.Combo}" : string.Empty);
        }

        /// <summary>Fills and reveals the result panel.</summary>
        /// <param name="payload">Run result payload.</param>
        private void OnRunEnded(RunEndedEvent payload)
        {
            SetText(finalScoreText, $"SCORE {payload.Score}");
            SetText(highScoreText, payload.IsNewHighScore ? "NEW BEST" : string.Empty);
            SetText(hintText, "PRESS A, D, SPACE OR S TO RUN AGAIN");
            SetResultVisible(true);
        }

        /// <summary>Hides the result panel when a new run starts.</summary>
        /// <param name="payload">State change payload.</param>
        private void OnGameStateChanged(GameStateChangedEvent payload)
        {
            if (payload.Current == GameState.Gameplay)
            {
                SetResultVisible(false);
            }
        }

        /// <summary>Toggles the result panel.</summary>
        /// <param name="visible">True to show the panel.</param>
        private void SetResultVisible(bool visible)
        {
            if (gameOverRoot != null)
            {
                gameOverRoot.SetActive(visible);
            }
        }

        /// <summary>Writes a label, tolerating a field that has not been assigned.</summary>
        /// <param name="label">Label to write to.</param>
        /// <param name="value">Text to display.</param>
        private static void SetText(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }
    }
}
