using System;
using System.Globalization;
using MoveRush.Core.Data;
using NUnit.Framework;
using UnityEngine;

namespace MoveRush.Tests
{
    /// <summary>
    /// Covers the save envelope and its JSON round trip. This is the exact path the save service
    /// takes to disk, so a serialisation gap here means a player loses progress silently - the
    /// write succeeds and only the reload reveals the loss.
    /// </summary>
    public class SaveFileTests
    {
        [Test]
        public void Wrap_StampsTheCurrentSchemaVersion()
        {
            SaveFile file = SaveFile.Wrap(new PlayerData());

            Assert.AreEqual(SaveFile.CurrentVersion, file.Version);
            Assert.IsNotNull(file.Player);
        }

        [Test]
        public void Wrap_WritesARoundTrippableTimestamp()
        {
            SaveFile file = SaveFile.Wrap(new PlayerData());

            Assert.IsTrue(
                DateTime.TryParse(
                    file.SavedAtUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime parsed),
                $"'{file.SavedAtUtc}' is not a round-trip formatted timestamp.");

            Assert.AreEqual(DateTimeKind.Utc, parsed.Kind);
        }

        /// <summary>
        /// The full profile has to survive a serialise and deserialise cycle. Every field is
        /// checked because JsonUtility silently drops anything it cannot see.
        /// </summary>
        [Test]
        public void JsonRoundTrip_PreservesTheWholeProfile()
        {
            PlayerData original = new PlayerData { Username = "Runner42" };
            original.AddCoins(1234);
            original.TrySetBestScore(98765);
            original.Settings.MasterVolume = 0.25f;
            original.Settings.Muted = true;
            original.Settings.LanguageCode = "ms";
            original.Settings.QualityLevel = 2;

            string json = JsonUtility.ToJson(SaveFile.Wrap(original), true);
            SaveFile restored = JsonUtility.FromJson<SaveFile>(json);

            Assert.AreEqual("Runner42", restored.Player.Username);
            Assert.AreEqual(1234, restored.Player.Coins);
            Assert.AreEqual(98765, restored.Player.BestScore);
            Assert.AreEqual(0.25f, restored.Player.Settings.MasterVolume, 0.0001f);
            Assert.IsTrue(restored.Player.Settings.Muted);
            Assert.AreEqual("ms", restored.Player.Settings.LanguageCode);
            Assert.AreEqual(2, restored.Player.Settings.QualityLevel);
        }

        [Test]
        public void JsonRoundTrip_PreservesProgression()
        {
            PlayerData original = new PlayerData();
            original.TrySetBestScore(10);

            SaveFile restored = JsonUtility.FromJson<SaveFile>(JsonUtility.ToJson(SaveFile.Wrap(original)));

            Assert.AreEqual(original.Level, restored.Player.Level);
            Assert.AreEqual(original.Xp, restored.Player.Xp);
        }

        /// <summary>A null payload must be replaced, never handed back to a caller.</summary>
        [Test]
        public void Player_IsNeverNull()
        {
            SaveFile file = new SaveFile { Player = null };

            Assert.IsNotNull(file.Player);
        }
    }
}
