using System;
using MoveRush.Core.Config;
using MoveRush.Core.Events;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using MoveRush.Gameplay.Config;
using MoveRush.Gameplay.Run;
using UnityEngine;

namespace MoveRush.Gameplay.Score
{
    /// <summary>
    /// Derives the run score from gameplay events. Nothing pushes a score in: coins, clears and
    /// hits arrive as events, distance is read from the difficulty service, and the total is
    /// recomputed from the formula asset. On a game over it commits coins, experience and a new
    /// best score to the Phase 1 profile through <see cref="ISaveService"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScoreManager : MonoBehaviour, IScoreService, IRunSystem
    {
        [Tooltip("Weights of the score formula and the run rewards.")]
        [SerializeField] private ScoreConfig scoreConfig;

        private IDifficultyService difficulty;
        private ISaveService saveService;
        private IConfigProvider configProvider;
        private int lastPublishedScore;

        /// <inheritdoc />
        public int RunOrder => 20;

        /// <inheritdoc />
        public int Score { get; private set; }

        /// <inheritdoc />
        public int Coins { get; private set; }

        /// <inheritdoc />
        public float Distance { get; private set; }

        /// <inheritdoc />
        public int Combo { get; private set; }

        /// <inheritdoc />
        public int BestCombo { get; private set; }

        /// <inheritdoc />
        public int HighScore { get; private set; }

        /// <inheritdoc />
        public bool IsNewHighScore => Score > HighScore;

        /// <inheritdoc />
        public event Action<int> ScoreChanged;

        /// <summary>Registers the service and subscribes to the events the score is built from.</summary>
        private void Awake()
        {
            ServiceLocator.Register<IScoreService>(this);

            EventBus<CoinCollectedEvent>.Subscribe(OnCoinCollected);
            EventBus<ObstacleClearedEvent>.Subscribe(OnObstacleCleared);
            EventBus<ObstacleHitEvent>.Subscribe(OnObstacleHit);
        }

        /// <summary>Resolves the services the score depends on.</summary>
        private void Start()
        {
            ServiceLocator.TryGet(out difficulty);
            ServiceLocator.TryGet(out saveService);
            ServiceLocator.TryGet(out configProvider);

            HighScore = saveService?.Current?.BestScore ?? 0;

            if (scoreConfig == null)
            {
                Log.Error("ScoreManager: assign the score config.", this);
                enabled = false;
            }
        }

        /// <summary>Releases every subscription with the scene.</summary>
        private void OnDestroy()
        {
            EventBus<CoinCollectedEvent>.Unsubscribe(OnCoinCollected);
            EventBus<ObstacleClearedEvent>.Unsubscribe(OnObstacleCleared);
            EventBus<ObstacleHitEvent>.Unsubscribe(OnObstacleHit);

            ScoreChanged = null;
            ServiceLocator.Unregister<IScoreService>();
        }

        /// <summary>Tracks distance and republishes the score only when the value actually moves.</summary>
        private void Update()
        {
            if (difficulty == null)
            {
                return;
            }

            Distance = difficulty.Distance;
            Recalculate();
        }

        /// <inheritdoc />
        public void OnRunReset()
        {
            Coins = 0;
            Combo = 0;
            BestCombo = 0;
            Distance = 0f;
            Score = 0;
            lastPublishedScore = -1;

            HighScore = saveService?.Current?.BestScore ?? HighScore;

            EventBus<ComboChangedEvent>.Publish(new ComboChangedEvent(0, 0));
            Recalculate();
        }

        /// <inheritdoc />
        public void OnRunEnded()
        {
            bool newHighScore = IsNewHighScore;
            CommitToProfile(newHighScore);

            EventBus<RunEndedEvent>.Publish(new RunEndedEvent(Score, Distance, Coins, BestCombo, newHighScore));
        }

        /// <summary>Recomputes the total and raises the change notifications once per new value.</summary>
        private void Recalculate()
        {
            Score = scoreConfig.CalculateScore(Coins, Distance, Combo);

            if (Score == lastPublishedScore)
            {
                return;
            }

            lastPublishedScore = Score;
            ScoreChanged?.Invoke(Score);
            EventBus<ScoreChangedEvent>.Publish(new ScoreChangedEvent(Score, Distance, Coins, Combo));
        }

        /// <summary>Writes the run rewards into the stored profile.</summary>
        /// <param name="newHighScore">True when the run beat the stored best score.</param>
        private void CommitToProfile(bool newHighScore)
        {
            if (saveService == null)
            {
                return;
            }

            int finalScore = Score;
            int finalCoins = Coins;
            float finalDistance = Distance;
            PlayerConfig playerConfig = configProvider?.Player;

            saveService.Modify(data =>
            {
                if (scoreConfig.GrantCoinsToProfile)
                {
                    data.AddCoins(finalCoins);
                }

                data.AddXp(scoreConfig.CalculateXp(finalCoins, finalDistance), playerConfig);
                data.TrySetBestScore(finalScore);
            }, true);

            if (newHighScore)
            {
                HighScore = finalScore;
            }
        }

        /// <summary>Adds the value of a collected coin.</summary>
        /// <param name="payload">Coin payload.</param>
        private void OnCoinCollected(CoinCollectedEvent payload)
        {
            Coins += Mathf.Max(1, payload.Value);
            Recalculate();
        }

        /// <summary>Extends the clear streak.</summary>
        /// <param name="payload">Clear payload.</param>
        private void OnObstacleCleared(ObstacleClearedEvent payload)
        {
            Combo++;
            BestCombo = Mathf.Max(BestCombo, Combo);

            EventBus<ComboChangedEvent>.Publish(new ComboChangedEvent(Combo, BestCombo));
            Recalculate();
        }

        /// <summary>Breaks the clear streak.</summary>
        /// <param name="payload">Hit payload.</param>
        private void OnObstacleHit(ObstacleHitEvent payload)
        {
            if (Combo == 0)
            {
                return;
            }

            Combo = 0;
            EventBus<ComboChangedEvent>.Publish(new ComboChangedEvent(0, BestCombo));
            Recalculate();
        }
    }
}
