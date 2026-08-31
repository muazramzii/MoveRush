using MoveRush.Core.Data;
using NUnit.Framework;

namespace MoveRush.Tests
{
    /// <summary>
    /// Covers the clamping on stored preferences. Volumes are written straight into the audio
    /// mixer, so an out of range value that reaches the profile survives a restart and produces
    /// silence or clipping the player cannot undo from the options screen.
    /// </summary>
    public class PlayerSettingsDataTests
    {
        [Test]
        [TestCase(-1f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1f, 1f)]
        [TestCase(4f, 1f)]
        public void Volumes_ClampToTheUnitRange(float input, float expected)
        {
            PlayerSettingsData settings = new PlayerSettingsData
            {
                MasterVolume = input,
                MusicVolume = input,
                SfxVolume = input
            };

            Assert.AreEqual(expected, settings.MasterVolume, 0.0001f);
            Assert.AreEqual(expected, settings.MusicVolume, 0.0001f);
            Assert.AreEqual(expected, settings.SfxVolume, 0.0001f);
        }

        [Test]
        public void QualityLevel_NeverGoesNegative()
        {
            PlayerSettingsData settings = new PlayerSettingsData { QualityLevel = -3 };

            Assert.AreEqual(0, settings.QualityLevel);
        }

        /// <summary>A blank language code must fall back rather than break localisation lookups.</summary>
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void LanguageCode_FallsBackToEnglish(string input)
        {
            PlayerSettingsData settings = new PlayerSettingsData { LanguageCode = input };

            Assert.AreEqual("en", settings.LanguageCode);
        }

        [Test]
        public void LanguageCode_KeepsARealValue()
        {
            PlayerSettingsData settings = new PlayerSettingsData { LanguageCode = "ms" };

            Assert.AreEqual("ms", settings.LanguageCode);
        }

        [Test]
        public void CreateDefault_AppliesTheSuppliedDefaults()
        {
            PlayerSettingsData settings = PlayerSettingsData.CreateDefault(0.9f, 0.4f, 0.8f, 3, "de");

            Assert.AreEqual(0.9f, settings.MasterVolume, 0.0001f);
            Assert.AreEqual(0.4f, settings.MusicVolume, 0.0001f);
            Assert.AreEqual(0.8f, settings.SfxVolume, 0.0001f);
            Assert.AreEqual(3, settings.QualityLevel);
            Assert.AreEqual("de", settings.LanguageCode);
            Assert.IsFalse(settings.Muted);
            Assert.IsTrue(settings.VibrationEnabled);
        }
    }
}
