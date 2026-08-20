using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoveRush.Gameplay.Config
{
    /// <summary>One step of the difficulty ramp: from a distance, run at a speed.</summary>
    [Serializable]
    public class SpeedStep
    {
        [Tooltip("Distance in meters at which this step becomes active.")]
        [SerializeField, Min(0f)] private float distance;

        [Tooltip("Forward speed in meters per second for this step.")]
        [SerializeField, Min(1f)] private float speed = 5f;

        /// <summary>Distance in meters at which this step becomes active.</summary>
        public float Distance => distance;

        /// <summary>Forward speed in meters per second for this step.</summary>
        public float Speed => speed;
    }

    /// <summary>
    /// The difficulty ramp. Steps declare the target speed at a distance and the manager eases
    /// towards it, so the run gets harder without ever jolting the player with an instant
    /// speed jump - which is what makes a runner feel unfair.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyConfig", menuName = "MoveRush/Config/Difficulty Config", order = 11)]
    public class DifficultyConfig : ScriptableObject
    {
        [Header("Speed Ramp")]
        [Tooltip("Ordered by distance. The first entry is the speed a run starts at.")]
        [SerializeField]
        private List<SpeedStep> steps = new List<SpeedStep>();

        [Tooltip("Meters per second the actual speed gains per second while easing to the target.")]
        [SerializeField, Range(0.05f, 3f)] private float acceleration = 0.35f;

        [Header("Spawn Pressure")]
        [Tooltip("Extra obstacle rows unlocked as the run approaches the final step.")]
        [SerializeField, Range(0f, 1f)] private float densityAtMaxTier = 1f;

        /// <summary>Meters per second gained per second while easing towards the target speed.</summary>
        public float Acceleration => acceleration;

        /// <summary>Number of configured steps.</summary>
        public int StepCount => steps?.Count ?? 0;

        /// <summary>Speed the first step declares, used as the starting speed of a run.</summary>
        public float StartSpeed => StepCount > 0 ? steps[0].Speed : 5f;

        /// <summary>Speed the final step declares.</summary>
        public float MaxSpeed => StepCount > 0 ? steps[StepCount - 1].Speed : 5f;

        /// <summary>Spawn density multiplier reached at the final step.</summary>
        public float DensityAtMaxTier => densityAtMaxTier;

        /// <summary>Finds the index of the step active at a distance.</summary>
        /// <param name="distance">Distance travelled in meters.</param>
        /// <returns>Zero based step index.</returns>
        public int GetTier(float distance)
        {
            int tier = 0;

            for (int i = 0; i < StepCount; i++)
            {
                if (distance >= steps[i].Distance)
                {
                    tier = i;
                }
            }

            return tier;
        }

        /// <summary>Returns the speed the run should be easing towards at a distance.</summary>
        /// <param name="distance">Distance travelled in meters.</param>
        /// <returns>Target speed in meters per second.</returns>
        public float GetTargetSpeed(float distance)
        {
            return StepCount == 0 ? 5f : steps[GetTier(distance)].Speed;
        }

        /// <summary>Expresses a speed in the 0..1 range between the first and the final step.</summary>
        /// <param name="speed">Speed to normalise.</param>
        /// <returns>Normalised intensity in the 0..1 range.</returns>
        public float GetNormalizedIntensity(float speed)
        {
            float min = StartSpeed;
            float max = MaxSpeed;
            return Mathf.Approximately(max, min) ? 0f : Mathf.Clamp01((speed - min) / (max - min));
        }

        /// <summary>Seeds the default ramp so a freshly created asset is already playable.</summary>
        private void Reset()
        {
            steps = new List<SpeedStep>();
        }
    }
}
