using System;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MoveRush.Player.Input
{
    /// <summary>
    /// Temporary keyboard controller: A and D change lane, Space jumps, S slides, with the arrow
    /// keys mirrored for comfort. It is the reference implementation of
    /// <see cref="IInputProvider"/>, and the Phase 3 pose estimator replaces it by registering a
    /// different component on the bootstrap root - no gameplay script changes.
    /// </summary>
    [DisallowMultipleComponent]
    public class KeyboardInputProvider : MonoBehaviour, IInputProvider, IGameService
    {
        [Tooltip("Emits commands from the moment the game starts. Normally driven by the flow.")]
        [SerializeField] private bool enabledOnStart = true;

        /// <inheritdoc />
        public int InitializationOrder => 5;

        /// <inheritdoc />
        public bool IsInitialized { get; private set; }

        /// <inheritdoc />
        public bool IsEnabled { get; private set; }

        /// <inheritdoc />
        public event Action<InputCommand> CommandIssued;

        /// <inheritdoc />
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            IsEnabled = enabledOnStart;
            ServiceLocator.Register<IInputProvider>(this);

            IsInitialized = true;
            Log.Info("KeyboardInputProvider initialised. A/D lane, Space jump, S slide.", this);
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            CommandIssued = null;
            IsEnabled = false;
            ServiceLocator.Unregister<IInputProvider>();
            IsInitialized = false;
        }

        /// <inheritdoc />
        public void SetEnabled(bool enabled) => IsEnabled = enabled;

        /// <summary>Polls the keyboard and turns key presses into commands.</summary>
        private void Update()
        {
            if (!IsEnabled)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                Issue(InputCommand.MoveLeft);
            }

            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                Issue(InputCommand.MoveRight);
            }

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame ||
                keyboard.upArrowKey.wasPressedThisFrame)
            {
                Issue(InputCommand.Jump);
            }

            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                Issue(InputCommand.Slide);
            }
        }

        /// <summary>Raises one command.</summary>
        /// <param name="command">Command to raise.</param>
        private void Issue(InputCommand command) => CommandIssued?.Invoke(command);
    }
}
