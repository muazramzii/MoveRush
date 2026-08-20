using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using UnityEngine;

namespace MoveRush.Gameplay.UI
{
    /// <summary>
    /// Starts a run from the main menu on the first control the player touches. It listens to
    /// <see cref="IInputProvider"/> rather than to a keyboard, so the day the pose input replaces
    /// the keyboard this still works untouched.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunStarter : MonoBehaviour
    {
        [Tooltip("Seconds to wait after the menu opens before input is accepted.")]
        [SerializeField, Min(0f)] private float inputDelay = 0.3f;

        private IGameStateService gameState;
        private IInputProvider input;
        private float enableTime;

        /// <summary>Resolves the services and arms the starter.</summary>
        private void Start()
        {
            ServiceLocator.TryGet(out gameState);

            if (!ServiceLocator.TryGet(out input))
            {
                Log.Error("RunStarter: no input provider is registered. Start from the Bootstrap scene.", this);
                return;
            }

            enableTime = Time.unscaledTime + inputDelay;
            input.SetEnabled(true);
            input.CommandIssued += OnCommandIssued;
        }

        /// <summary>Releases the subscription with the scene.</summary>
        private void OnDestroy()
        {
            if (input != null)
            {
                input.CommandIssued -= OnCommandIssued;
            }
        }

        /// <summary>Loads the gameplay scene on the first accepted command.</summary>
        /// <param name="command">Command that was issued.</param>
        private void OnCommandIssued(InputCommand command)
        {
            if (gameState == null || Time.unscaledTime < enableTime)
            {
                return;
            }

            input.CommandIssued -= OnCommandIssued;
            gameState.StartRun();
        }
    }
}
