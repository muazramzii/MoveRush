using System.Collections.Generic;
using UnityEngine;

namespace MoveRush.Player
{
    /// <summary>Visual states the character can be in.</summary>
    public enum CharacterAnimationState
    {
        /// <summary>Standing still, outside a run.</summary>
        Idle = 0,

        /// <summary>Running forward.</summary>
        Run = 1,

        /// <summary>Rising after a take-off.</summary>
        Jump = 2,

        /// <summary>Falling back towards the ground.</summary>
        Fall = 3,

        /// <summary>Sliding under an obstacle.</summary>
        Slide = 4,

        /// <summary>Killed by an obstacle.</summary>
        Dead = 5
    }

    /// <summary>
    /// Drives the character animation state machine. Two things make it safe on a project whose
    /// art is not authored yet: every Animator parameter is checked for existence before it is
    /// written, so an incomplete controller cannot spam the console, and a procedural lean stands
    /// in for real clips so each state is still readable with nothing but a capsule.
    /// Only rotation is driven here - the slide owns scale - so the two never fight.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterAnimator : MonoBehaviour
    {
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int GroundedParameter = Animator.StringToHash("Grounded");
        private static readonly int SlidingParameter = Animator.StringToHash("Sliding");
        private static readonly int DeadParameter = Animator.StringToHash("Dead");
        private static readonly int JumpParameter = Animator.StringToHash("Jump");

        [Tooltip("Optional Animator. The procedural fallback runs when it is missing.")]
        [SerializeField] private Animator animator;

        [Tooltip("Transform leaned to sell the current state. Rotation only.")]
        [SerializeField] private Transform visualRoot;

        [Tooltip("Leans the visual when no Animator controller is assigned.")]
        [SerializeField] private bool proceduralFallback = true;

        [Tooltip("Forward lean in degrees while running at full speed.")]
        [SerializeField, Range(0f, 25f)] private float runLean = 8f;

        [Tooltip("Backward lean in degrees while rising.")]
        [SerializeField, Range(0f, 25f)] private float jumpLean = 10f;

        [Tooltip("Degrees per second the lean eases at.")]
        [SerializeField, Min(1f)] private float leanSpeed = 8f;

        private readonly HashSet<int> availableParameters = new HashSet<int>();
        private float currentLean;

        /// <summary>Visual state the character is currently in.</summary>
        public CharacterAnimationState State { get; private set; } = CharacterAnimationState.Idle;

        /// <summary>Caches which Animator parameters actually exist on the assigned controller.</summary>
        public void Initialize()
        {
            availableParameters.Clear();

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                availableParameters.Add(parameters[i].nameHash);
            }
        }

        /// <summary>Moves the character into a visual state.</summary>
        /// <param name="next">State to enter.</param>
        public void SetState(CharacterAnimationState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;

            SetBool(GroundedParameter, next != CharacterAnimationState.Jump && next != CharacterAnimationState.Fall);
            SetBool(SlidingParameter, next == CharacterAnimationState.Slide);
            SetBool(DeadParameter, next == CharacterAnimationState.Dead);

            if (next == CharacterAnimationState.Jump)
            {
                SetTrigger(JumpParameter);
            }
        }

        /// <summary>Feeds the running speed in and advances the procedural lean.</summary>
        /// <param name="deltaTime">Frame time.</param>
        /// <param name="normalizedSpeed">Run speed in the 0..1 range.</param>
        public void Tick(float deltaTime, float normalizedSpeed)
        {
            SetFloat(SpeedParameter, normalizedSpeed);

            if (!proceduralFallback || visualRoot == null)
            {
                return;
            }

            float targetLean = State switch
            {
                CharacterAnimationState.Run => runLean * Mathf.Clamp01(normalizedSpeed),
                CharacterAnimationState.Jump => -jumpLean,
                CharacterAnimationState.Fall => jumpLean * 0.5f,
                CharacterAnimationState.Slide => 55f,
                CharacterAnimationState.Dead => 80f,
                _ => 0f
            };

            currentLean = Mathf.Lerp(currentLean, targetLean, 1f - Mathf.Exp(-leanSpeed * deltaTime));
            visualRoot.localRotation = Quaternion.Euler(currentLean, 0f, 0f);
        }

        /// <summary>Clears the lean and returns to the idle pose.</summary>
        public void ResetState()
        {
            State = CharacterAnimationState.Idle;
            currentLean = 0f;

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.identity;
            }
        }

        /// <summary>Writes a float parameter when the controller declares it.</summary>
        /// <param name="parameter">Parameter hash.</param>
        /// <param name="value">Value to write.</param>
        private void SetFloat(int parameter, float value)
        {
            if (animator != null && availableParameters.Contains(parameter))
            {
                animator.SetFloat(parameter, value);
            }
        }

        /// <summary>Writes a bool parameter when the controller declares it.</summary>
        /// <param name="parameter">Parameter hash.</param>
        /// <param name="value">Value to write.</param>
        private void SetBool(int parameter, bool value)
        {
            if (animator != null && availableParameters.Contains(parameter))
            {
                animator.SetBool(parameter, value);
            }
        }

        /// <summary>Fires a trigger when the controller declares it.</summary>
        /// <param name="parameter">Parameter hash.</param>
        private void SetTrigger(int parameter)
        {
            if (animator != null && availableParameters.Contains(parameter))
            {
                animator.SetTrigger(parameter);
            }
        }
    }
}
