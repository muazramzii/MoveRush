using MoveRush.Core.Config;
using MoveRush.Core.Data;
using NUnit.Framework;
using UnityEngine;

namespace MoveRush.Tests
{
    /// <summary>
    /// Covers the persisted player profile: the progression curve and the guards that stop an
    /// invalid value ever reaching disk. These rules run on every finished run, and a mistake
    /// here corrupts real player progress rather than just misbehaving on screen.
    /// </summary>
    public class PlayerDataTests
    {
        private PlayerConfig playerConfig;
        private AudioConfig audioConfig;

        /// <summary>Creates the configs the profile factory reads its defaults from.</summary>
        [SetUp]
        public void SetUp()
        {
            playerConfig = ScriptableObject.CreateInstance<PlayerConfig>();
            audioConfig = ScriptableObject.CreateInstance<AudioConfig>();
        }

        /// <summary>Releases the instances between tests.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(playerConfig);
            Object.DestroyImmediate(audioConfig);
        }

        [Test]
        public void CreateDefault_SeedsFromConfiguration()
        {
            PlayerData data = PlayerData.CreateDefault(playerConfig, audioConfig, 2, "fr");

            Assert.AreEqual(playerConfig.DefaultUsername, data.Username);
            Assert.AreEqual(playerConfig.StartingLevel, data.Level);
            Assert.AreEqual(playerConfig.StartingCoins, data.Coins);
            Assert.AreEqual(0, data.Xp);
            Assert.AreEqual(0, data.BestScore);
            Assert.AreEqual(2, data.Settings.QualityLevel);
            Assert.AreEqual("fr", data.Settings.LanguageCode);
        }

        /// <summary>A missing config must still produce a usable profile rather than throwing.</summary>
        [Test]
        public void CreateDefault_ToleratesMissingConfigs()
        {
            PlayerData data = PlayerData.CreateDefault(null, null, 0, "en");

            Assert.IsNotNull(data.Settings);
            Assert.AreEqual(1, data.Level);
        }

        [Test]
        public void AddXp_LevelsUpWhenTheRequirementIsMet()
        {
            PlayerData data = PlayerData.CreateDefault(playerConfig, audioConfig, 0, "en");
            int required = playerConfig.GetXpRequiredForLevel(1);

            int gained = data.AddXp(required, playerConfig);

            Assert.AreEqual(1, gained);
            Assert.AreEqual(2, data.Level);
            Assert.AreEqual(0, data.Xp);
        }

        /// <summary>
        /// A single large award has to apply every level it covers and carry the remainder,
        /// because a finished run grants its whole XP total in one call.
        /// </summary>
        [Test]
        public void AddXp_AppliesMultipleLevelsAndKeepsTheRemainder()
        {
            PlayerData data = PlayerData.CreateDefault(playerConfig, audioConfig, 0, "en");
            int first = playerConfig.GetXpRequiredForLevel(1);
            int second = playerConfig.GetXpRequiredForLevel(2);

            int gained = data.AddXp(first + second + 10, playerConfig);

            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, data.Level);
            Assert.AreEqual(10, data.Xp);
        }

        [Test]
        public void AddXp_IgnoresNonPositiveAwardsAndMissingConfig()
        {
            PlayerData data = PlayerData.CreateDefault(playerConfig, audioConfig, 0, "en");

            Assert.AreEqual(0, data.AddXp(0, playerConfig));
            Assert.AreEqual(0, data.AddXp(-50, playerConfig));
            Assert.AreEqual(0, data.AddXp(100, null));
            Assert.AreEqual(0, data.Xp);
        }

        [Test]
        public void AddCoins_IgnoresNonPositiveAmounts()
        {
            PlayerData data = PlayerData.CreateDefault(playerConfig, audioConfig, 0, "en");

            data.AddCoins(25);
            data.AddCoins(-10);
            data.AddCoins(0);

            Assert.AreEqual(25, data.Coins);
        }

        [Test]
        public void TrySpendCoins_RefusesToOverdraw()
        {
            PlayerData data = PlayerData.CreateDefault(playerConfig, audioConfig, 0, "en");
            data.AddCoins(30);

            Assert.IsFalse(data.TrySpendCoins(31));
            Assert.AreEqual(30, data.Coins, "A refused purchase must not change the balance.");

            Assert.IsTrue(data.TrySpendCoins(30));
            Assert.AreEqual(0, data.Coins);
        }

        [Test]
        public void TrySetBestScore_OnlyKeepsAnImprovement()
        {
            PlayerData data = PlayerData.CreateDefault(playerConfig, audioConfig, 0, "en");

            Assert.IsTrue(data.TrySetBestScore(500));
            Assert.IsFalse(data.TrySetBestScore(499));
            Assert.IsFalse(data.TrySetBestScore(500));
            Assert.AreEqual(500, data.BestScore);
        }

        /// <summary>A blank name must never overwrite a name the player chose.</summary>
        [Test]
        public void Username_RejectsBlankValues()
        {
            PlayerData data = PlayerData.CreateDefault(playerConfig, audioConfig, 0, "en");
            data.Username = "Runner42";

            data.Username = "   ";
            data.Username = string.Empty;
            data.Username = null;

            Assert.AreEqual("Runner42", data.Username);
        }

        [Test]
        public void Username_TrimsSurroundingWhitespace()
        {
            PlayerData data = PlayerData.CreateDefault(playerConfig, audioConfig, 0, "en");

            data.Username = "  Runner42  ";

            Assert.AreEqual("Runner42", data.Username);
        }

        [Test]
        public void Settings_IsNeverNull()
        {
            PlayerData data = new PlayerData { Settings = null };

            Assert.IsNotNull(data.Settings);
        }
    }
}
