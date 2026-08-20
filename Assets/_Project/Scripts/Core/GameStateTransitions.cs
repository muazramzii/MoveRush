using System;
using System.Collections.Generic;

namespace MoveRush.Core
{
    /// <summary>
    /// Declarative table of legal state transitions. Keeping the rules in one readable place
    /// makes the whole application flow reviewable at a glance, and turns a wiring mistake into
    /// a logged rejection instead of an undefined state.
    /// </summary>
    public static class GameStateTransitions
    {
        private static readonly Dictionary<GameState, GameState[]> Map =
            new Dictionary<GameState, GameState[]>
            {
                { GameState.Boot, new[] { GameState.Splash, GameState.Loading } },
                { GameState.Splash, new[] { GameState.Loading, GameState.MainMenu } },
                { GameState.MainMenu, new[] { GameState.Loading } },
                { GameState.Loading, new[] { GameState.Splash, GameState.MainMenu, GameState.Gameplay } },
                { GameState.Gameplay, new[] { GameState.Paused, GameState.GameOver, GameState.Loading } },
                { GameState.Paused, new[] { GameState.Gameplay, GameState.MainMenu, GameState.Loading } },
                { GameState.GameOver, new[] { GameState.Gameplay, GameState.MainMenu, GameState.Loading } }
            };

        /// <summary>Returns true when moving between the two states is legal.</summary>
        /// <param name="from">State the application is in.</param>
        /// <param name="to">State being requested.</param>
        /// <returns>True when the transition is allowed.</returns>
        public static bool IsAllowed(GameState from, GameState to)
        {
            if (from == to || !Map.TryGetValue(from, out GameState[] targets))
            {
                return false;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == to)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Lists every state reachable from the given state.</summary>
        /// <param name="from">State to inspect.</param>
        /// <returns>The reachable states, or an empty list when the state is terminal.</returns>
        public static IReadOnlyList<GameState> GetTargets(GameState from)
        {
            return Map.TryGetValue(from, out GameState[] targets) ? targets : Array.Empty<GameState>();
        }
    }
}
