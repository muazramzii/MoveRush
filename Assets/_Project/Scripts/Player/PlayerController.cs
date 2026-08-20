using MoveRush.Core;
using MoveRush.Core.Config;
using MoveRush.Core.Events;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using MoveRush.Player.Config;
using UnityEngine;

namespace MoveRush.Player
{
    /// <summary>
    /// Composes the character out of four single-purpose modules - lane, jump, slide and
    /// animation - and is the only one of them that talks to the rest of the game. It publishes
    /// itself as <see cref="IPlayerService"/> so obstacles and coins can read its state without
    /// the gameplay assembly ever referencing this one, and it takes its forward speed from the
    /// difficulty service so pace lives in exactly one place.
    /// Motion is applied to the transform rather than to the rigidbody: an endless runner needs a
    /// jump arc that is identical every time, which physics integration cannot guarantee. The
    /// kinematic rigidbody exists purely so obstacle triggers fire.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour, IPlayerService
    {
        [Header("Configuration")]
        [Tooltip("Movement feel. Required.")]
        [SerializeField] private PlayerMovementConfig movementConfig;

        [Header("Modules")]
        [SerializeField] private LaneMovement laneMovement;
        [SerializeField] private JumpController jumpController;
        [SerializeField] private SlideController slideController;
        [SerializeField] private CharacterAnimator characterAnimator;

        [Header("Fallback")]
        [Tooltip("Speed used when no difficulty service is registered, so the scene stays testable alone.")]
        [SerializeField, Min(1f)] private float fallbackSpeed = 5f;

        private IConfigProvider configProvider;
        private IDifficultyService difficulty;
        private IInputProvider input;
        private IGameStateService gameState;
        private float groundY;

        /// <inheritdoc />
        public Transform Transform => transform;

        /// <inheritdoc />
        public Vector3 Position => transform.position;

        /// <inheritdoc />
        public bool IsAlive { get; private set; } = true;

        /// <inheritdoc />
        public bool IsAirborne => jumpController != null && jumpController.IsAirborne;

        /// <inheritdoc />
        public bool IsSliding => slideController != null && slideController.IsSliding;

        /// <inheritdoc />
        public int CurrentLane => laneMovement != null ? laneMovement.CurrentLane : 0;

        /// <inheritdoc />
        public float ForwardSpeed { get; private set; }

        /// <inheritdoc />
        public float LateralVelocityNormalized => laneMovement != null ? laneMovement.LateralVelocityNormalized : 0f;

        /// <summary>Publishes the character before any obstacle can look for it.</summary>
        private void Awake()
        {
            ServiceLocator.Register<IPlayerService>(this);
        }

        /// <summary>Resolves services, initialises the modules and hooks the input provider.</summary>
        private void Start()
        {
            ServiceLocator.TryGet(out configProvider);
            ServiceLocator.TryGet(out difficulty);
            ServiceLocator.TryGet(out gameState);

            if (movementConfig == null)
            {
                Log.Error("PlayerController: assign the movement config.", this);
                enabled = false;
                return;
            }

            TrackConfig track = configProvider?.Track;
            groundY = track != null ? track.GroundHeight : 0f;

            laneMovement?.Initialize(track, movementConfig);
            jumpController?.Initialize(movementConfig, groundY);
            slideController?.Initialize(movementConfig);
            characterAnimator?.Initialize();

            if (ServiceLocator.TryGet(out input))
            {
                input.CommandIssued += OnCommandIssued;
            }
            else
            {
                Log.Warning("PlayerController: no input provider is registered, the character cannot be steered.", this);
            }

            PrepareForRun();
        }

        /// <summary>Releases the registration and the subscription with the scene.</summary>
        private void OnDestroy()
        {
            if (input != null)
            {
                input.CommandIssued -= OnCommandIssued;
            }

            ServiceLocator.Unregister<IPlayerService>();
        }

        /// <summary>Advances forward motion, the modules and the animation state.</summary>
        private void Update()
        {
            float deltaTime = Time.deltaTime;

            if (!IsAlive || gameState == null || gameState.CurrentState != GameState.Gameplay)
            {
                characterAnimator?.Tick(deltaTime, 0f);
                return;
            }

            ForwardSpeed = difficulty != null ? difficulty.CurrentSpeed : fallbackSpeed;
            slideController?.Tick(deltaTime);

            Vector3 position = transform.position;
            float x = laneMovement != null ? laneMovement.Evaluate(deltaTime) : position.x;
            float y = jumpController != null ? jumpController.Evaluate(deltaTime, position.y) : groundY;
            float z = position.z + ForwardSpeed * deltaTime;

            transform.position = new Vector3(x, y, z);

            UpdateAnimation(deltaTime);
        }

        /// <inheritdoc />
        public void Kill(string cause)
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            ForwardSpeed = 0f;

            slideController?.Cancel();
            characterAnimator?.SetState(CharacterAnimationState.Dead);

            EventBus<PlayerDiedEvent>.Publish(new PlayerDiedEvent(cause, transform.position));
            Log.Info($"PlayerController: killed by '{cause}'.", this);
        }

        /// <inheritdoc />
        public void PrepareForRun()
        {
            IsAlive = true;
            ForwardSpeed = 0f;

            TrackConfig track = configProvider?.Track;
            int centerLane = track != null ? track.CenterLaneIndex : 0;

            laneMovement?.ResetToLane(centerLane);
            jumpController?.ResetState();
            slideController?.ResetState();
            characterAnimator?.ResetState();
            characterAnimator?.SetState(CharacterAnimationState.Run);

            // The Z position is deliberately kept: a retry continues from where the run ended, so
            // the camera never has to fly back to the origin between attempts.
            transform.position = new Vector3(
                track != null ? track.GetLaneOffset(centerLane) : 0f,
                groundY,
                transform.position.z);
        }

        /// <summary>Turns a command into a module call, ignoring input outside a live run.</summary>
        /// <param name="command">Command that was issued.</param>
        private void OnCommandIssued(InputCommand command)
        {
            if (!IsAlive || gameState == null || gameState.CurrentState != GameState.Gameplay)
            {
                return;
            }

            switch (command)
            {
                case InputCommand.MoveLeft:
                    laneMovement?.TryMove(-1);
                    break;

                case InputCommand.MoveRight:
                    laneMovement?.TryMove(1);
                    break;

                case InputCommand.Jump:
                    if (movementConfig.JumpCancelsSlide)
                    {
                        slideController?.Cancel();
                    }

                    jumpController?.TryJump();
                    break;

                case InputCommand.Slide:
                    slideController?.TrySlide(IsAirborne);
                    break;
            }
        }

        /// <summary>Picks the visual state that matches the current motion.</summary>
        /// <param name="deltaTime">Frame time.</param>
        private void UpdateAnimation(float deltaTime)
        {
            if (characterAnimator == null)
            {
                return;
            }

            CharacterAnimationState state;

            if (IsSliding)
            {
                state = CharacterAnimationState.Slide;
            }
            else if (jumpController != null && jumpController.IsFalling)
            {
                state = CharacterAnimationState.Fall;
            }
            else if (IsAirborne)
            {
                state = CharacterAnimationState.Jump;
            }
            else
            {
                state = CharacterAnimationState.Run;
            }

            characterAnimator.SetState(state);
            characterAnimator.Tick(deltaTime, difficulty != null ? difficulty.NormalizedIntensity : 0.5f);
        }
    }
}
