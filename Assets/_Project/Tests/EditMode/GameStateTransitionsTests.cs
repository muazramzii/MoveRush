using MoveRush.Core;
using NUnit.Framework;

namespace MoveRush.Tests
{
    /// <summary>
    /// Locks down the application transition table. The table is the one place that decides which
    /// flows are possible at all, so an accidental edit here would silently allow a run to start
    /// from the splash screen or strand the player on a game over.
    /// </summary>
    public class GameStateTransitionsTests
    {
        [Test]
        [TestCase(GameState.Boot, GameState.Splash)]
        [TestCase(GameState.Boot, GameState.Loading)]
        [TestCase(GameState.Splash, GameState.Loading)]
        [TestCase(GameState.Splash, GameState.MainMenu)]
        [TestCase(GameState.MainMenu, GameState.Loading)]
        [TestCase(GameState.Loading, GameState.Gameplay)]
        [TestCase(GameState.Loading, GameState.MainMenu)]
        [TestCase(GameState.Gameplay, GameState.Paused)]
        [TestCase(GameState.Gameplay, GameState.GameOver)]
        [TestCase(GameState.Paused, GameState.Gameplay)]
        [TestCase(GameState.GameOver, GameState.Gameplay)]
        public void IsAllowed_PermitsSupportedFlows(GameState from, GameState to)
        {
            Assert.IsTrue(GameStateTransitions.IsAllowed(from, to), $"{from} -> {to} should be allowed.");
        }

        [Test]
        [TestCase(GameState.Boot, GameState.Gameplay)]
        [TestCase(GameState.Boot, GameState.MainMenu)]
        [TestCase(GameState.Boot, GameState.GameOver)]
        [TestCase(GameState.Splash, GameState.Gameplay)]
        [TestCase(GameState.MainMenu, GameState.Gameplay)]
        [TestCase(GameState.MainMenu, GameState.GameOver)]
        [TestCase(GameState.Gameplay, GameState.MainMenu)]
        [TestCase(GameState.GameOver, GameState.Splash)]
        [TestCase(GameState.Paused, GameState.GameOver)]
        public void IsAllowed_RejectsUnsupportedFlows(GameState from, GameState to)
        {
            Assert.IsFalse(GameStateTransitions.IsAllowed(from, to), $"{from} -> {to} should be rejected.");
        }

        /// <summary>
        /// A state cannot transition to itself. The state machine relies on this to make a
        /// repeated request a no-op rather than a second round of entry behaviour.
        /// </summary>
        [Test]
        public void IsAllowed_RejectsSelfTransitions()
        {
            foreach (GameState state in System.Enum.GetValues(typeof(GameState)))
            {
                Assert.IsFalse(GameStateTransitions.IsAllowed(state, state), $"{state} -> {state} should be rejected.");
            }
        }

        /// <summary>Every state must offer a way out, otherwise the flow can dead-end.</summary>
        [Test]
        public void GetTargets_EveryStateHasAnExit()
        {
            foreach (GameState state in System.Enum.GetValues(typeof(GameState)))
            {
                Assert.IsNotEmpty(GameStateTransitions.GetTargets(state), $"{state} has no reachable state.");
            }
        }

        /// <summary>Gameplay must always be escapable both by pausing and by dying.</summary>
        [Test]
        public void GetTargets_GameplayCanPauseAndEnd()
        {
            CollectionAssert.Contains(GameStateTransitions.GetTargets(GameState.Gameplay), GameState.Paused);
            CollectionAssert.Contains(GameStateTransitions.GetTargets(GameState.Gameplay), GameState.GameOver);
        }
    }
}
