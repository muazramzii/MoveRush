using UnityEngine;

namespace MoveRush.Gameplay.Pooling
{
    /// <summary>
    /// Marker stamped onto every pooled instance, recording which pool produced it. Returning an
    /// object is then an O(1) dictionary lookup instead of a search through every pool.
    /// </summary>
    [DisallowMultipleComponent]
    public class PooledInstance : MonoBehaviour
    {
        /// <summary>Key of the owning pool. Assigned by the pool service on creation.</summary>
        public int PoolKey { get; set; }
    }
}
