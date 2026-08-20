using MoveRush.Core.Events;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using Unity.Cinemachine;
using UnityEngine;

namespace MoveRush.Player.Cameras
{
    /// <summary>
    /// Adds run feel on top of a Cinemachine follow camera: the field of view opens slightly as
    /// the run speeds up, the frame tilts a few degrees into a lane change, and landing fires an
    /// impulse shake scaled by the height of the fall.
    /// Every effect here is deliberately small and eased. A runner is played for long stretches
    /// with the camera locked behind the character, which is exactly the setup that causes motion
    /// sickness, so the field of view boost is capped, the tilt stays within a few degrees, and
    /// nothing is applied instantly.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunnerCameraController : MonoBehaviour
    {
        [Header("Cinemachine")]
        [Tooltip("Camera that follows the character. Falls back to a component on this object.")]
        [SerializeField] private CinemachineCamera runCamera;

        [Tooltip("Impulse source that produces the landing shake.")]
        [SerializeField] private CinemachineImpulseSource landingImpulse;

        [Header("Speed Zoom")]
        [Tooltip("Field of view at the slowest configured speed.")]
        [SerializeField, Range(30f, 90f)] private float baseFieldOfView = 58f;

        [Tooltip("Extra degrees of field of view at the fastest configured speed.")]
        [SerializeField, Range(0f, 20f)] private float maxFieldOfViewBoost = 8f;

        [Tooltip("How quickly the field of view follows the speed. Low values stay comfortable.")]
        [SerializeField, Range(0.2f, 5f)] private float fieldOfViewLerpSpeed = 1.2f;

        [Header("Lane Tilt")]
        [Tooltip("Maximum roll in degrees while changing lane. Keep this small.")]
        [SerializeField, Range(0f, 8f)] private float maxTiltDegrees = 3f;

        [Tooltip("Seconds the tilt takes to settle.")]
        [SerializeField, Range(0.05f, 0.5f)] private float tiltSmoothTime = 0.18f;

        [Header("Landing Shake")]
        [Tooltip("Impacts below this strength do not shake the camera at all.")]
        [SerializeField, Range(0f, 1f)] private float minimumImpact = 0.35f;

        [Tooltip("Scales the impulse force. Keep it low to avoid a jarring frame.")]
        [SerializeField, Range(0f, 1f)] private float impulseScale = 0.35f;

        private IPlayerService player;
        private IDifficultyService difficulty;
        private float currentTilt;
        private float tiltVelocity;

        /// <summary>Subscribes to the landing event that drives the shake.</summary>
        private void Awake()
        {
            EventBus<PlayerLandedEvent>.Subscribe(OnPlayerLanded);
        }

        /// <summary>Resolves the services the framing reads from.</summary>
        private void Start()
        {
            ServiceLocator.TryGet(out player);
            ServiceLocator.TryGet(out difficulty);

            if (runCamera == null)
            {
                TryGetComponent(out runCamera);
            }

            if (runCamera == null)
            {
                Log.Warning("RunnerCameraController: no CinemachineCamera assigned, dynamic framing is off.", this);
                enabled = false;
                return;
            }

            runCamera.Lens.FieldOfView = baseFieldOfView;
        }

        /// <summary>Releases the subscription with the scene.</summary>
        private void OnDestroy()
        {
            EventBus<PlayerLandedEvent>.Unsubscribe(OnPlayerLanded);
        }

        /// <summary>
        /// Applies the framing after the character has moved for this frame, so the tilt matches
        /// the lateral motion the player can actually see.
        /// </summary>
        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;

            float intensity = difficulty != null ? difficulty.NormalizedIntensity : 0f;
            float targetFieldOfView = baseFieldOfView + maxFieldOfViewBoost * intensity;
            float blend = 1f - Mathf.Exp(-fieldOfViewLerpSpeed * deltaTime);
            runCamera.Lens.FieldOfView = Mathf.Lerp(runCamera.Lens.FieldOfView, targetFieldOfView, blend);

            float lateral = player != null ? player.LateralVelocityNormalized : 0f;
            currentTilt = Mathf.SmoothDamp(currentTilt, -lateral * maxTiltDegrees, ref tiltVelocity, tiltSmoothTime);
            runCamera.Lens.Dutch = currentTilt;
        }

        /// <summary>Shakes the camera in proportion to the landing impact.</summary>
        /// <param name="payload">Landing payload.</param>
        private void OnPlayerLanded(PlayerLandedEvent payload)
        {
            if (landingImpulse == null || payload.Impact < minimumImpact)
            {
                return;
            }

            landingImpulse.GenerateImpulseWithForce(payload.Impact * impulseScale);
        }
    }
}
