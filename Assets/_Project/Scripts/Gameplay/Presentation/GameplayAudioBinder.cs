using MoveRush.Core;
using MoveRush.Core.Events;
using MoveRush.Core.Services;
using UnityEngine;

namespace MoveRush.Gameplay.Presentation
{
    /// <summary>
    /// Maps gameplay events onto audio identifiers. Gameplay code never names a clip and never
    /// touches the audio service, so re-scoring the game is an inspector change here plus new
    /// entries in the Phase 1 audio bank. Empty identifiers are skipped, which keeps the console
    /// clean until the bank actually has clips in it.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameplayAudioBinder : MonoBehaviour
    {
        [Header("Sound Identifiers")]
        [Tooltip("Played when a coin is collected. Must match an id in the AudioConfig bank.")]
        [SerializeField] private string coinSfxId = string.Empty;

        [Tooltip("Played when the character jumps.")]
        [SerializeField] private string jumpSfxId = string.Empty;

        [Tooltip("Played when the character starts a slide.")]
        [SerializeField] private string slideSfxId = string.Empty;

        [Tooltip("Played when the character hits an obstacle.")]
        [SerializeField] private string hitSfxId = string.Empty;

        [Header("Music")]
        [Tooltip("Music started when a run begins.")]
        [SerializeField] private string runMusicId = string.Empty;

        private IAudioService audioService;
        private IGameStateService gameState;

        /// <summary>Subscribes to every event that has a sound attached.</summary>
        private void Awake()
        {
            EventBus<CoinCollectedEvent>.Subscribe(OnCoinCollected);
            EventBus<PlayerJumpedEvent>.Subscribe(OnPlayerJumped);
            EventBus<PlayerSlideStartedEvent>.Subscribe(OnSlideStarted);
            EventBus<ObstacleHitEvent>.Subscribe(OnObstacleHit);
        }

        /// <summary>Resolves the audio service and starts the run music.</summary>
        private void Start()
        {
            ServiceLocator.TryGet(out audioService);
            ServiceLocator.TryGet(out gameState);

            if (gameState != null)
            {
                gameState.StateChanged += OnStateChanged;
            }

            PlayMusic();
        }

        /// <summary>Releases every subscription with the scene.</summary>
        private void OnDestroy()
        {
            EventBus<CoinCollectedEvent>.Unsubscribe(OnCoinCollected);
            EventBus<PlayerJumpedEvent>.Unsubscribe(OnPlayerJumped);
            EventBus<PlayerSlideStartedEvent>.Unsubscribe(OnSlideStarted);
            EventBus<ObstacleHitEvent>.Unsubscribe(OnObstacleHit);

            if (gameState != null)
            {
                gameState.StateChanged -= OnStateChanged;
            }
        }

        /// <summary>Plays a sound when the identifier is configured.</summary>
        /// <param name="sfxId">Identifier from the audio bank.</param>
        private void Play(string sfxId)
        {
            if (audioService != null && !string.IsNullOrEmpty(sfxId))
            {
                audioService.PlaySfx(sfxId);
            }
        }

        /// <summary>Starts the run music when one is configured.</summary>
        private void PlayMusic()
        {
            if (audioService != null && !string.IsNullOrEmpty(runMusicId))
            {
                audioService.PlayMusic(runMusicId);
            }
        }

        /// <summary>Restarts the music when a new run begins.</summary>
        /// <param name="previous">State that was left.</param>
        /// <param name="current">State that was entered.</param>
        private void OnStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.Gameplay && previous == GameState.GameOver)
            {
                PlayMusic();
            }
        }

        /// <summary>Plays the coin sound.</summary>
        /// <param name="payload">Coin payload.</param>
        private void OnCoinCollected(CoinCollectedEvent payload) => Play(coinSfxId);

        /// <summary>Plays the jump sound.</summary>
        /// <param name="payload">Jump payload.</param>
        private void OnPlayerJumped(PlayerJumpedEvent payload) => Play(jumpSfxId);

        /// <summary>Plays the slide sound.</summary>
        /// <param name="payload">Slide payload.</param>
        private void OnSlideStarted(PlayerSlideStartedEvent payload) => Play(slideSfxId);

        /// <summary>Plays the impact sound.</summary>
        /// <param name="payload">Hit payload.</param>
        private void OnObstacleHit(ObstacleHitEvent payload) => Play(hitSfxId);
    }
}
