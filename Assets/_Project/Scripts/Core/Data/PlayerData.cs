using System;
using MoveRush.Core.Config;
using UnityEngine;

namespace MoveRush.Core.Data
{
    /// <summary>
    /// The complete player profile that is serialised to JSON by the save service.
    /// Fields are private and surfaced through guarded properties so no caller can write an
    /// invalid value (negative coins, empty username, ...) into a persisted profile.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        [SerializeField] private string username = "Player";
        [SerializeField] private int level = 1;
        [SerializeField] private int xp;
        [SerializeField] private int coins;
        [SerializeField] private int bestScore;
        [SerializeField] private PlayerSettingsData settings = new PlayerSettingsData();

        /// <summary>Display name shown in menus and on leaderboards.</summary>
        public string Username
        {
            get => username;
            set => username = string.IsNullOrWhiteSpace(value) ? username : value.Trim();
        }

        /// <summary>Current progression level. Always at least one.</summary>
        public int Level
        {
            get => level;
            private set => level = Mathf.Max(1, value);
        }

        /// <summary>Experience accumulated towards the next level.</summary>
        public int Xp
        {
            get => xp;
            private set => xp = Mathf.Max(0, value);
        }

        /// <summary>Soft currency balance.</summary>
        public int Coins
        {
            get => coins;
            private set => coins = Mathf.Max(0, value);
        }

        /// <summary>Highest score the player has achieved.</summary>
        public int BestScore
        {
            get => bestScore;
            private set => bestScore = Mathf.Max(0, value);
        }

        /// <summary>Player preferences. Never null.</summary>
        public PlayerSettingsData Settings
        {
            get => settings ??= new PlayerSettingsData();
            set => settings = value ?? new PlayerSettingsData();
        }

        /// <summary>Builds a fresh profile using the values declared in the player configuration.</summary>
        /// <param name="playerConfig">Configuration asset holding the starting values.</param>
        /// <param name="audioConfig">Configuration asset holding the default volumes.</param>
        /// <param name="defaultQualityLevel">Quality settings index applied to a new profile.</param>
        /// <param name="defaultLanguageCode">Language code applied to a new profile.</param>
        /// <returns>A profile populated from configuration.</returns>
        public static PlayerData CreateDefault(
            PlayerConfig playerConfig,
            AudioConfig audioConfig,
            int defaultQualityLevel,
            string defaultLanguageCode)
        {
            PlayerData data = new PlayerData
            {
                Username = playerConfig != null ? playerConfig.DefaultUsername : "Player",
                Level = playerConfig != null ? playerConfig.StartingLevel : 1,
                Xp = 0,
                Coins = playerConfig != null ? playerConfig.StartingCoins : 0,
                BestScore = 0
            };

            data.Settings = PlayerSettingsData.CreateDefault(
                audioConfig != null ? audioConfig.DefaultMasterVolume : 1f,
                audioConfig != null ? audioConfig.DefaultMusicVolume : 1f,
                audioConfig != null ? audioConfig.DefaultSfxVolume : 1f,
                defaultQualityLevel,
                defaultLanguageCode);

            return data;
        }

        /// <summary>
        /// Adds experience and applies as many level-ups as the accumulated total allows,
        /// using the curve declared in the player configuration.
        /// </summary>
        /// <param name="amount">Experience to grant. Values below one are ignored.</param>
        /// <param name="config">Configuration providing the level curve and level cap.</param>
        /// <returns>The number of levels gained.</returns>
        public int AddXp(int amount, PlayerConfig config)
        {
            if (amount <= 0 || config == null)
            {
                return 0;
            }

            Xp += amount;

            int levelsGained = 0;
            int required = config.GetXpRequiredForLevel(Level);

            while (Level < config.MaxLevel && required > 0 && Xp >= required)
            {
                Xp -= required;
                Level++;
                levelsGained++;
                required = config.GetXpRequiredForLevel(Level);
            }

            if (Level >= config.MaxLevel)
            {
                Xp = 0;
            }

            return levelsGained;
        }

        /// <summary>Adds coins to the balance.</summary>
        /// <param name="amount">Amount to add. Values below one are ignored.</param>
        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Coins += amount;
        }

        /// <summary>Spends coins when the balance allows it.</summary>
        /// <param name="amount">Amount to spend.</param>
        /// <returns>True when the balance covered the cost and coins were deducted.</returns>
        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0 || Coins < amount)
            {
                return false;
            }

            Coins -= amount;
            return true;
        }

        /// <summary>Records a score, keeping it only when it beats the stored best.</summary>
        /// <param name="score">Score achieved in the last run.</param>
        /// <returns>True when a new personal best was stored.</returns>
        public bool TrySetBestScore(int score)
        {
            if (score <= BestScore)
            {
                return false;
            }

            BestScore = score;
            return true;
        }
    }
}
