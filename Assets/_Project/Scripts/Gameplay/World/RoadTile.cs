using System.Collections.Generic;
using MoveRush.Gameplay.Pooling;
using UnityEngine;

namespace MoveRush.Gameplay.World
{
    /// <summary>
    /// One recycled segment of road. The tile owns the lifetime of everything spawned onto it:
    /// when it is recycled it hands its coins and obstacles back to their pools, so no spawner
    /// has to track what it placed or where.
    /// </summary>
    [DisallowMultipleComponent]
    public class RoadTile : MonoBehaviour, IPoolable
    {
        [Tooltip("Optional parent for spawned content. Falls back to this transform.")]
        [SerializeField] private Transform contentRoot;

        [Tooltip("Optional ground mesh, stretched along Z to match the configured tile length.")]
        [SerializeField] private Transform groundRoot;

        [Tooltip("Authored length in meters, used when no ground mesh is assigned.")]
        [SerializeField, Min(1f)] private float length = 20f;

        private readonly List<Component> content = new List<Component>(32);
        private readonly List<LaneReservation> reservations = new List<LaneReservation>(8);

        /// <summary>Length of the tile in meters.</summary>
        public float Length => length;

        /// <summary>World Z position of the tile start.</summary>
        public float StartZ => transform.position.z;

        /// <summary>World Z position of the tile end.</summary>
        public float EndZ => StartZ + length;

        /// <summary>Transform spawned content is parented to.</summary>
        public Transform ContentRoot => contentRoot != null ? contentRoot : transform;

        /// <summary>
        /// Positions the tile and stretches the ground mesh so the visual always matches the
        /// configured tile length, even when the track length is re-tuned after the art is made.
        /// </summary>
        /// <param name="startPosition">World position of the tile start.</param>
        /// <param name="tileLength">Length of the tile in meters.</param>
        public void Place(Vector3 startPosition, float tileLength)
        {
            length = Mathf.Max(1f, tileLength);
            transform.SetPositionAndRotation(startPosition, Quaternion.identity);

            if (groundRoot == null)
            {
                return;
            }

            Vector3 scale = groundRoot.localScale;
            scale.z = length;
            groundRoot.localScale = scale;

            Vector3 local = groundRoot.localPosition;
            local.z = length * 0.5f;
            groundRoot.localPosition = local;
        }

        /// <summary>Hands an object to the tile so it is recycled together with it.</summary>
        /// <param name="instance">Pooled instance placed on this tile.</param>
        public void AddContent(Component instance)
        {
            if (instance != null)
            {
                content.Add(instance);
            }
        }

        /// <summary>
        /// Drops an object from the tile without recycling it. A coin that is collected mid-run
        /// returns itself to its pool, and must stop being tracked here or the tile would return
        /// it a second time after it had already been handed out again.
        /// </summary>
        /// <param name="instance">Instance to stop tracking.</param>
        public void RemoveContent(Component instance)
        {
            if (instance != null)
            {
                content.Remove(instance);
            }
        }

        /// <summary>Marks a stretch of a lane as occupied so coins are not placed inside it.</summary>
        /// <param name="lane">Lane index.</param>
        /// <param name="worldStartZ">World Z where the reservation begins.</param>
        /// <param name="worldEndZ">World Z where the reservation ends.</param>
        public void ReserveLane(int lane, float worldStartZ, float worldEndZ)
        {
            reservations.Add(new LaneReservation(lane, worldStartZ, worldEndZ));
        }

        /// <summary>Tests whether a stretch of a lane is still free.</summary>
        /// <param name="lane">Lane index.</param>
        /// <param name="worldStartZ">World Z where the test range begins.</param>
        /// <param name="worldEndZ">World Z where the test range ends.</param>
        /// <returns>True when nothing is reserved in that range.</returns>
        public bool IsLaneFree(int lane, float worldStartZ, float worldEndZ)
        {
            for (int i = 0; i < reservations.Count; i++)
            {
                if (reservations[i].Overlaps(lane, worldStartZ, worldEndZ))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Returns every object placed on this tile to its pool.</summary>
        /// <param name="poolService">Service that owns the pools.</param>
        public void ReleaseContent(IPoolService poolService)
        {
            for (int i = 0; i < content.Count; i++)
            {
                if (content[i] != null)
                {
                    poolService?.Return(content[i]);
                }
            }

            content.Clear();
            reservations.Clear();
        }

        /// <inheritdoc />
        public void OnSpawnedFromPool()
        {
            content.Clear();
            reservations.Clear();
        }

        /// <inheritdoc />
        public void OnReturnedToPool()
        {
            content.Clear();
            reservations.Clear();
        }

        /// <summary>A stretch of one lane that is already occupied.</summary>
        private readonly struct LaneReservation
        {
            private readonly int lane;
            private readonly float startZ;
            private readonly float endZ;

            /// <summary>Creates the reservation.</summary>
            /// <param name="lane">Lane index.</param>
            /// <param name="startZ">World Z where the reservation begins.</param>
            /// <param name="endZ">World Z where the reservation ends.</param>
            public LaneReservation(int lane, float startZ, float endZ)
            {
                this.lane = lane;
                this.startZ = startZ;
                this.endZ = endZ;
            }

            /// <summary>Returns true when a range on a lane intersects this reservation.</summary>
            /// <param name="otherLane">Lane index to test.</param>
            /// <param name="otherStart">World Z where the test range begins.</param>
            /// <param name="otherEnd">World Z where the test range ends.</param>
            /// <returns>True when the ranges overlap on the same lane.</returns>
            public bool Overlaps(int otherLane, float otherStart, float otherEnd)
            {
                return otherLane == lane && otherStart <= endZ && otherEnd >= startZ;
            }
        }
    }
}
